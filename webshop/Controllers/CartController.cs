using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using webshop.Models;

namespace AutoPartsApi.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAllCartItems()
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                var result = cx.Kosars
                    .Select(k => new {
                        k.Id,
                        k.UserId,
                        k.AlkatreszId,
                        k.OlajId,
                        k.Mennyiseg,
                        k.Hozzaadva
                    })
                    .ToList();

                return StatusCode(200, result);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }


    public class CartItemDto
    {
        public int UserId { get; set; }
        public int? AlkatreszId { get; set; }
        public int? OlajId { get; set; }
        public int Mennyiseg { get; set; } = 1;
    }

    [HttpPost]
    public IActionResult PostCartItem([FromBody] CartItemDto? dto)
    {
        try
        {
            if (dto == null) return BadRequest(new { success = false, error = "Adj meg minden paramétert" });

            using (var cx = new AutoalkatreszDbContext())
            {
                var user = cx.Users.FirstOrDefault(u => u.Id == dto.UserId);
                if (user == null) return BadRequest(new { success = false, error = "Nincs ilyen felhasználó" });

                var toSave = new Kosar
                {
                    UserId = dto.UserId,
                    AlkatreszId = dto.AlkatreszId,
                    OlajId = dto.OlajId,
                    Mennyiseg = dto.Mennyiseg,
                    Hozzaadva = DateTime.Now,
                    User = user 
                };

                cx.Kosars.Add(toSave);
                cx.SaveChanges();

                var saved = cx.Kosars
                    .Include(k => k.User)
                    .Where(k => k.Id == toSave.Id)
                    .Select(k => new
                    {
                        id = k.Id,
                        userId = k.UserId,
                        alkatreszId = k.AlkatreszId,
                        olajId = k.OlajId,
                        mennyiseg = k.Mennyiseg,
                        hozzaadva = k.Hozzaadva,
                        user = k.User == null ? null : new
                        {
                            id = k.User.Id,
                            felhasznalonev = k.User.Felhasznalonev,
                            email = k.User.Email,
                            vezeteknev = k.User.Vezeteknev,
                            keresztnev = k.User.Keresztnev,
                            telefon = k.User.Telefon
                        }
                    })
                    .FirstOrDefault();

                return StatusCode(200, new { success = true, message = "Sikeres hozzáadás a kosárhoz", cart = saved });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("{id}")]
    public IActionResult PutCartItem(int id, [FromBody] CartItemDto? dto)
    {
        try
        {
            if (dto == null) return BadRequest(new { success = false, error = "Missing body (CartItemDto)" });

            using (var cx = new AutoalkatreszDbContext())
            {
                var existing = cx.Kosars.FirstOrDefault(f => f.Id == id);
                if (existing == null) return NotFound(new { success = false, error = "Kosár tétel nem található" });

                var user = cx.Users.FirstOrDefault(u => u.Id == dto.UserId);
                if (user == null) return BadRequest(new { success = false, error = "Nincs ilyen felhasználó" });

                existing.UserId = dto.UserId;
                existing.AlkatreszId = dto.AlkatreszId;
                existing.OlajId = dto.OlajId;
                existing.Mennyiseg = dto.Mennyiseg;
                existing.User = user;

                cx.SaveChanges();

                return StatusCode(200, new { success = true, message = "Sikeres módosítás" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteCartItem(int id)
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                var result = cx.Kosars.FirstOrDefault(f => f.Id == id);
                if (result == null) return NotFound(new { success = false, error = "Nincs ilyen kosár tétel" });
                cx.Remove(result);
                cx.SaveChanges();
                return StatusCode(200, new { success = true, message = "Sikeres törlés" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}