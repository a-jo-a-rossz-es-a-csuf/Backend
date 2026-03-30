using AutoPartsApi.Services;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using webshop.Models;

namespace AutoPartsApi.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly DbService _db;

    public ProductsController(DbService db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetAllProducts()
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                var result = cx.Alkatreszeks.ToList();
                return StatusCode(200, result);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public IActionResult PostProduct(Alkatreszek product)
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                if (product == null)
                {
                    return StatusCode(409, "Adj meg minden parametert");
                }

                cx.Alkatreszeks.Add(product);
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
    public async Task<IActionResult> PutProduct(int id, [FromBody] Alkatreszek product)
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                var existing = cx.Alkatreszeks.FirstOrDefault(f => f.Id == id);
                if (existing == null)
                    return NotFound("Termek nem talalhato");

                product.Id = id;
                cx.Entry(existing).CurrentValues.SetValues(product);

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
    public IActionResult DeleteProduct(int id)
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                var result = cx.Alkatreszeks.FirstOrDefault(f => f.Id == id);
                if (result == null) return NotFound("Nincs ilyen termek");
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
