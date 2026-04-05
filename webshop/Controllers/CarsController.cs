using AutoPartsApi.Services;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using webshop.Models;

namespace AutoPartsApi.Controllers;

[ApiController]
[Route("api/cars")]
public class CarsController : ControllerBase
{
    private readonly DbService _db;

    public CarsController(DbService db)
    {
        _db = db;
    }


    [HttpGet]

    public IActionResult GetAllJarmuvek()
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                var result = cx.Jarmuveks.ToList();
                return StatusCode(200, result);
            }
        }
        catch (Exception ex)
        {

            return StatusCode(500, ex.Message);
        }
    }
    [HttpPost]
    public IActionResult PostJarmuvek(Jarmuvek jarmu)
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                if (jarmu == null)
                {
                    return StatusCode(409, "Adj meg minden paramétert");

                }
                cx.Jarmuveks.Add(jarmu);
                cx.SaveChanges();
                return StatusCode(200, "Sikeres hozzáadás");
            }
        }
        catch (Exception ex)
        {

            return StatusCode(500, ex.Message);
        }
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> PutOlaj(int id, [FromBody] Jarmuvek jarmu)
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                var existing = cx.Jarmuveks.FirstOrDefault(f => f.Id == id);
                if (existing == null)
                    return NotFound("Jármû not found");

                jarmu.Id = id;

                cx.Entry(existing).CurrentValues.SetValues(jarmu);

                await cx.SaveChangesAsync();

                return StatusCode(200, "Sikeres módosítás");
            }
        }
        catch (Exception ex)
        {

            return StatusCode(500, ex.Message);
        }
    }


    [HttpDelete]
    public IActionResult DeleteJarmuvek(int id)
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                var result = cx.Jarmuveks.FirstOrDefault(f => f.Id == id);
                if (result == null) return NotFound("Nincs ilyen járgány");
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
}


    
