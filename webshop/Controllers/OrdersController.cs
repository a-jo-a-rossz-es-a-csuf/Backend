using AutoPartsApi.Services;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using System;
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
    public IActionResult PostOrder(Rendelesek order)
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                if (order == null)
                {
                    return StatusCode(409, "Adj meg minden parametert");
                }

                if (string.IsNullOrEmpty(order.Statusz)) order.Statusz = "fuggoben";

                cx.Rendeleseks.Add(order);
                cx.SaveChanges();
                return StatusCode(200, "Sikeres hozzaadas");
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
                    return NotFound("Rendeles nem talalhato");

                order.Id = id;
                cx.Entry(existing).CurrentValues.SetValues(order);

                await cx.SaveChangesAsync();
                return StatusCode(200, "Sikeres modositas");
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
                if (result == null) return NotFound("Nincs ilyen rendeles");
                cx.Remove(result);
                cx.SaveChanges();
                return StatusCode(200, "Sikeres torles");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}