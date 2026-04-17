using AutoPartsApi.Services;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using webshop.Models;

namespace AutoPartsApi.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly DbService _db;

    public OrdersController(DbService db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetAllOrders()
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                var result = cx.Rendeleseks.ToList();
                return StatusCode(200, result);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public IActionResult PostOrder([FromBody] OrderWithItemsDto orderDto)
    {
        try
        {
            if (orderDto == null || orderDto.Order == null)
            {
                return StatusCode(409, "Adj meg minden paramétert");
            }

            using (var cx = new AutoalkatreszDbContext())
            {
                // Tranzakció indítása: hiba esetén semmi nem mentődik el (nincs félkész rendelés)
                using var transaction = cx.Database.BeginTransaction();

                try
                {
                    var order = orderDto.Order;

                    if (string.IsNullOrEmpty(order.Statusz)) order.Statusz = "fuggoben";

                    cx.Rendeleseks.Add(order);
                    cx.SaveChanges(); // Elmentjük, hogy megkapja az ID-t

                    int rendelesSzam = order.Id;

                    if (orderDto.Tetelek != null && orderDto.Tetelek.Count > 0)
                    {
                        foreach (var tetel in orderDto.Tetelek)
                        {
                            var rendelesTetele = new RendelesTetelek
                            {
                                RendelesId = rendelesSzam,
                                AlkatreszId = tetel.AlkatreszId,
                                OlajId = tetel.OlajId,
                                TermekNev = tetel.TermekNev,
                                Mennyiseg = tetel.Mennyiseg,
                                Egysegar = tetel.Egysegar,
                                Osszeg = tetel.Osszeg
                            };
                            cx.RendelesTeteleks.Add(rendelesTetele);

                            // Készlet csökkentése
                            if (tetel.AlkatreszId.HasValue)
                            {
                                var alkatreszek = cx.Alkatreszeks.Find(tetel.AlkatreszId.Value);
                                if (alkatreszek != null && alkatreszek.Keszlet.HasValue)
                                {
                                    // Ellenőrzés: van-e elég készlet?
                                    if (alkatreszek.Keszlet < tetel.Mennyiseg)
                                    {
                                        transaction.Rollback();
                                        return BadRequest($"Sajnos a(z) {tetel.TermekNev} időközben elfogyott vagy nincs belőle elég készlet.");
                                    }
                                    alkatreszek.Keszlet -= tetel.Mennyiseg;
                                }
                            }
                            else if (tetel.OlajId.HasValue)
                            {
                                var olajok = cx.Olajoks.Find(tetel.OlajId.Value);
                                if (olajok != null && olajok.Keszlet.HasValue)
                                {
                                    // Ellenőrzés: van-e elég készlet?
                                    if (olajok.Keszlet < tetel.Mennyiseg)
                                    {
                                        transaction.Rollback();
                                        return BadRequest($"Sajnos a(z) {tetel.TermekNev} időközben elfogyott vagy nincs belőle elég készlet.");
                                    }
                                    olajok.Keszlet -= tetel.Mennyiseg;
                                }
                            }
                        }
                        cx.SaveChanges();
                    }

                    // Az első vásárlás kedvezményt FALSE-ra állítjuk, hogy másodjára már ne kapjon kedvezményt
                    if (order.UserId.HasValue)
                    {
                        var user = cx.Users.Find(order.UserId.Value);
                        if (user != null)
                        {
                            user.ElsoVasarolasKedvezmeny = false;
                            cx.SaveChanges();
                        }
                    }

                    // Minden sikeres volt, véglegesítjük a tranzakciót
                    transaction.Commit();

                    return StatusCode(200, new { message = "Sikeres hozzáadás", rendeles_szam = order.RendelesSzam });
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Ha a [ConcurrencyCheck] hibát dob a mentésnél (overselling)
                    transaction.Rollback();
                    return StatusCode(409, "A rendelés feldolgozása közben a készlet megváltozott. Kérjük, frissítsd a kosarad!");
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutOrder(int id, [FromBody] Rendelesek order)
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                var existing = cx.Rendeleseks.FirstOrDefault(f => f.Id == id);
                if (existing == null)
                    return NotFound("Rendelés nem található");

                order.Id = id;
                cx.Entry(existing).CurrentValues.SetValues(order);

                await cx.SaveChangesAsync();
                return StatusCode(200, "Sikeres módosítás");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPatch("{id}/status")]
    public IActionResult UpdateOrderStatus(int id, [FromBody] StatusUpdateDto statusDto)
    {
        try
        {
            if (statusDto == null || string.IsNullOrEmpty(statusDto.ujStatusz))
            {
                return BadRequest("Az új státusz megadása kötelező.");
            }

            using (var cx = new AutoalkatreszDbContext())
            {
                var order = cx.Rendeleseks.FirstOrDefault(r => r.Id == id);
                if (order == null)
                {
                    return NotFound("Rendelés nem található.");
                }

                string regiStatusz = order.Statusz ?? "fuggoben";
                string ujStatusz = statusDto.ujStatusz.ToLower();

                if (regiStatusz == ujStatusz)
                {
                    return Ok(new { message = "A státusz már erre az értékre van állítva." });
                }

                using var transaction = cx.Database.BeginTransaction();
                try
                {
                    // Ha töröltre állítjuk, visszaadjuk a készletet
                    if (ujStatusz == "torolve" && regiStatusz != "torolve")
                    {
                        var tetelek = cx.RendelesTeteleks.Where(t => t.RendelesId == id).ToList();

                        foreach (var tetel in tetelek)
                        {
                            if (tetel.AlkatreszId.HasValue)
                            {
                                var alkatresz = cx.Alkatreszeks.Find(tetel.AlkatreszId.Value);
                                if (alkatresz != null && alkatresz.Keszlet.HasValue)
                                {
                                    alkatresz.Keszlet += tetel.Mennyiseg;
                                }
                            }
                            else if (tetel.OlajId.HasValue)
                            {
                                var olaj = cx.Olajoks.Find(tetel.OlajId.Value);
                                if (olaj != null && olaj.Keszlet.HasValue)
                                {
                                    olaj.Keszlet += tetel.Mennyiseg;
                                }
                            }
                        }
                    }

                    // Státusz frissítése
                    order.Statusz = ujStatusz;
                    cx.SaveChanges();

                    transaction.Commit();
                    return Ok(new { message = "Státusz sikeresen frissítve." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, "Hiba történt a státusz módosítása során: " + ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete]
    public IActionResult DeleteOrder(int id)
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                var result = cx.Rendeleseks.FirstOrDefault(f => f.Id == id);
                if (result == null) return NotFound("Nincs ilyen rendelés");
                cx.Remove(result);
                cx.SaveChanges();
                return StatusCode(200, "Sikeres törlés");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{id}/items")]
    public IActionResult GetOrderItems(int id)
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                var items = cx.RendelesTeteleks.Where(f => f.RendelesId == id).ToList();
                return StatusCode(200, items);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}

// Segédosztály a PATCH kéréshez
public class StatusUpdateDto
{
    public string ujStatusz { get; set; }
}
