using Microsoft.AspNetCore.Mvc;
using AutoPartsApi.Services;
using Dapper;
using System.Data;

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
    public async Task<IActionResult> HandleAction(
        [FromQuery] string action = "list",
        [FromQuery] string? kategoria = null, // A frontend kategoria néven küldi, de mi a 'tipus' oszlopban keressük
        [FromQuery] string? viszkozitas = null,
        [FromQuery] string? gyarto = null)
    {
        try
        {
            using var conn = _db.CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();

            string act = (action ?? "list").ToLower();

            switch (act)
            {
                case "list":
                    {
                        // A képed alapján: tipus, viszkozitas, gyarto, aktiv oszlopok vannak
                        var sql = "SELECT * FROM olajok WHERE aktiv = 1";
                        var parameters = new DynamicParameters();

                        if (!string.IsNullOrEmpty(kategoria))
                        {
                            sql += " AND tipus = @Kat";
                            parameters.Add("Kat", kategoria);
                        }
                        if (!string.IsNullOrEmpty(viszkozitas))
                        {
                            sql += " AND viszkozitas = @Vis";
                            parameters.Add("Vis", viszkozitas);
                        }
                        if (!string.IsNullOrEmpty(gyarto))
                        {
                            sql += " AND gyarto = @Gy";
                            parameters.Add("Gy", gyarto);
                        }
                        sql += " ORDER BY nev";

                        var data = await conn.QueryAsync<dynamic>(sql, parameters);
                        return Ok(new { success = true, data = data.ToList() });
                    }

                case "categories":
                    {
                        // A képeden a 'tipus' oszlopban van pl. a 'motorolaj'
                        var data = await conn.QueryAsync<string>(
                            "SELECT DISTINCT tipus FROM olajok WHERE tipus IS NOT NULL AND aktiv = 1");
                        return Ok(new { success = true, data = data.ToList() });
                    }

                case "viscosities":
                    {
                        var data = await conn.QueryAsync<string>(
                            "SELECT DISTINCT viszkozitas FROM olajok WHERE viszkozitas IS NOT NULL AND aktiv = 1");
                        return Ok(new { success = true, data = data.ToList() });
                    }

                case "brands":
                    {
                        var data = await conn.QueryAsync<string>(
                            "SELECT DISTINCT gyarto FROM olajok WHERE gyarto IS NOT NULL AND aktiv = 1");
                        return Ok(new { success = true, data = data.ToList() });
                    }

                default:
                    return BadRequest(new { success = false, error = "Ismeretlen muvelet" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}