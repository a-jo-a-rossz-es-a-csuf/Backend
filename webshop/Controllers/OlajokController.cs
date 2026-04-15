using Microsoft.AspNetCore.Mvc;
using AutoPartsApi.Services;
using Dapper;
using System.Data;
using webshop.Models;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AutoPartsApi.Controllers;

[ApiController]
[Route("api/olajok")]
public class OlajokController : ControllerBase
{
    private readonly DbService _db;

    public OlajokController(DbService db)
    {
        _db = db;
    }

    [HttpGet]

    public IActionResult GetAllOlaj()
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext()) { 
            var result = cx.Olajoks.ToList();
            return StatusCode(200, result);
            }
        }
        catch (Exception ex)
        {

            return StatusCode(500, ex.Message);
        }
    }
    [HttpPost]
    public IActionResult PostOlaj(Olajok olaj)
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                if(olaj == null)
                {
                    return StatusCode(409, "Adj meg minden param�tert");
                   
                }
                cx.Olajoks.Add(olaj);
                cx.SaveChanges();
                return StatusCode(200, "Sikeres hozz�ad�s");
            }
        }
        catch (Exception ex)
        {

            return StatusCode(500, ex.Message);
        }
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> PutOlaj(int id, [FromBody] Olajok olaj)
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                var existing = cx.Olajoks.FirstOrDefault(f => f.Id == id);
                if (existing == null)
                    return NotFound("Olaj not found");

                olaj.Id = id;

                cx.Entry(existing).CurrentValues.SetValues(olaj);

                await cx.SaveChangesAsync();

                return StatusCode(200, "Sikeres m�dos�t�s");
            }
        }
        catch (Exception ex)
        {

            return StatusCode(500, ex.Message);
        }
    }


    [HttpDelete]
    public IActionResult DeleteOlaj(int id)
    {
        try
        {
            using (var cx = new AutoalkatreszDbContext())
            {
                var result = cx.Olajoks.FirstOrDefault(f => f.Id == id);
                if (result == null) return NotFound("Nincs ilyen olaj");
                cx.Remove(result);
                cx.SaveChanges();
                return StatusCode(200, "Sikeres t�rl�s");
            }
        }
        catch (Exception ex)
        {

            return StatusCode(500,ex.Message);
        }
    }

}
