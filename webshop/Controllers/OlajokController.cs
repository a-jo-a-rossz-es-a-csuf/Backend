using Microsoft.AspNetCore.Mvc;
using AutoPartsApi.Services;
using Dapper;

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
        [FromQuery] string? kategoria = null,
        [FromQuery] string? viszkozitas = null,
        [FromQuery] string? gyarto = null)
    {
        try
        {
            using var conn = _db.CreateConnection();
            conn.Open();

            switch (action)
            {
                case "list":
                {
                    var where = "aktiv = 1";
                    var parameters = new DynamicParameters();

                    if (!string.IsNullOrEmpty(kategoria))
                    {
                        where += " AND kategoria = @Kat";
                        parameters.Add("Kat", kategoria);
                    }
                    if (!string.IsNullOrEmpty(viszkozitas))
                    {
                        where += " AND viszkozitas = @Vis";
                        parameters.Add("Vis", viszkozitas);
                    }
                    if (!string.IsNullOrEmpty(gyarto))
                    {
                        where += " AND gyarto = @Gy";
                        parameters.Add("Gy", gyarto);
                    }

                    var data = await conn.QueryAsync<dynamic>(
                        $"SELECT * FROM olajok WHERE {where} ORDER BY nev", parameters);
                    return Ok(new { success = true, data });
                }

                case "categories":
                {
                    var data = await conn.QueryAsync<string>(
                        "SELECT DISTINCT kategoria FROM olajok WHERE aktiv = 1 ORDER BY kategoria");
                    return Ok(new { success = true, data });
                }

                case "viscosities":
                {
                    var data = await conn.QueryAsync<string>(
                        "SELECT DISTINCT viszkozitas FROM olajok WHERE viszkozitas IS NOT NULL AND aktiv = 1 ORDER BY viszkozitas");
                    return Ok(new { success = true, data });
                }

                case "brands":
                {
                    var data = await conn.QueryAsync<string>(
                        "SELECT DISTINCT gyarto FROM olajok WHERE aktiv = 1 ORDER BY gyarto");
                    return Ok(new { success = true, data });
                }

                default:
                    return Ok(new { success = false, error = "Ismeretlen muvelet" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = "Adatbazis hiba: " + ex.Message });
        }
    }
}
