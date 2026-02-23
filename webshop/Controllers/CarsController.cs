using Microsoft.AspNetCore.Mvc;
using AutoPartsApi.Services;
using Dapper;

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
    public async Task<IActionResult> HandleAction(
        [FromQuery] string action = "markak",
        [FromQuery] string? tipus = null,
        [FromQuery] int marka_id = 0,
        [FromQuery] int modell_id = 0,
        [FromQuery] string? vin = null)
    {
        try
        {
            using var conn = _db.CreateConnection();
            conn.Open();

            switch (action)
            {
                case "markak":
                    {
                        IEnumerable<dynamic> data;
                        if (!string.IsNullOrEmpty(tipus))
                            data = await conn.QueryAsync<dynamic>(
                                "SELECT * FROM markak WHERE aktiv = 1 AND tipus = @Tipus ORDER BY nev",
                                new { Tipus = tipus });
                        else
                            data = await conn.QueryAsync<dynamic>(
                                "SELECT * FROM markak WHERE aktiv = 1 ORDER BY nev");

                        var list = data.ToList();
                        return Ok(new { success = true, data = list, count = list.Count });
                    }

                case "modellek":
                    {
                        if (marka_id == 0)
                            return BadRequest(new { success = false, error = "marka_id parameter required" });

                        var data = (await conn.QueryAsync<dynamic>(
                            @"SELECT id, modell_nev, generacio, evjarat_tol, evjarat_ig, karosszeria 
                          FROM automodellek WHERE marka_id = @Id ORDER BY modell_nev, evjarat_tol DESC",
                            new { Id = marka_id })).ToList();

                        return Ok(new { success = true, data, count = data.Count });
                    }

                case "motorok":
                    {
                        if (modell_id == 0)
                            return BadRequest(new { success = false, error = "modell_id parameter required" });

                        var data = (await conn.QueryAsync<dynamic>(
                            @"SELECT id, motor_kod, hengerurtartalom, teljesitmeny_le, teljesitmeny_kw, 
                                 uzemanyag, nyomatek, hengerszam 
                          FROM motorok WHERE modell_id = @Id 
                          ORDER BY hengerurtartalom, teljesitmeny_le",
                            new { Id = modell_id })).ToList();

                        return Ok(new { success = true, data, count = data.Count });
                    }

                case "evjaratok":
                    {
                        if (modell_id == 0)
                            return BadRequest(new { success = false, error = "modell_id parameter required" });

                        var rows = await conn.QueryAsync(
                            "SELECT evjarat_tol, evjarat_ig FROM automodellek WHERE id = @Id",
                            new { Id = modell_id });
                        var result = rows.FirstOrDefault();

                        var evek = new List<int>();
                        if (result != null)
                        {
                            var r = (IDictionary<string, object>)result;
                            int kezdet = Convert.ToInt32(r["evjarat_tol"]);
                            int vege = r["evjarat_ig"] is not DBNull && r["evjarat_ig"] != null ? Convert.ToInt32(r["evjarat_ig"]) : DateTime.Now.Year;
                            for (int i = vege; i >= kezdet; i--)
                                evek.Add(i);
                        }

                        return Ok(new { success = true, data = evek, count = evek.Count });
                    }

                case "vin_search":
                    {
                        if (string.IsNullOrEmpty(vin) || vin.Length != 17)
                            return BadRequest(new { success = false, error = "Ervenyes 17 karakteres alvazszam szukseges" });

                        // Jarmu keresese VIN alapjan
                        var jarmu = (await conn.QueryAsync(
                            @"SELECT j.*, m.nev as marka_nev, m.tipus as marka_tipus,
                                 am.modell_nev, am.generacio, am.evjarat_tol, am.evjarat_ig, am.karosszeria,
                                 mo.motor_kod, mo.hengerurtartalom, mo.teljesitmeny_le, mo.uzemanyag
                          FROM jarmuvek j
                          INNER JOIN automodellek am ON j.modell_id = am.id
                          INNER JOIN markak m ON am.marka_id = m.id
                          LEFT JOIN motorok mo ON j.motor_id = mo.id
                          WHERE j.alvazszam = @Vin",
                            new { Vin = vin.ToUpper() })).FirstOrDefault();

                        if (jarmu == null)
                            return Ok(new { success = false, error = "Nem talalhato jarmu ezzel az alvazszammal" });

                        var jr = (IDictionary<string, object>)jarmu;
                        int modId = Convert.ToInt32(jr["modell_id"]);
                        int? motId = jr["motor_id"] != null && jr["motor_id"] is not DBNull ? Convert.ToInt32(jr["motor_id"]) : null;

                        // Alkatreszek keresese a jarmuhoz
                        var sql = @"SELECT DISTINCT a.*, k.nev as kategoria_nev
                                FROM alkatreszek a
                                LEFT JOIN kategoriak k ON a.kategoria_id = k.id
                                INNER JOIN alkatresz_auto aa ON a.id = aa.alkatresz_id
                                WHERE aa.modell_id = @ModellId AND a.aktiv = 1";
                        var parameters = new DynamicParameters();
                        parameters.Add("ModellId", modId);

                        if (motId.HasValue)
                        {
                            sql += " AND (aa.motor_id = @MotorId OR aa.motor_id IS NULL)";
                            parameters.Add("MotorId", motId.Value);
                        }
                        sql += " ORDER BY a.nev";

                        var parts = await conn.QueryAsync<dynamic>(sql, parameters);

                        // Jarmu info osszeallitasa
                        var vehicleInfo = new Dictionary<string, object?>
                        {
                            ["alvazszam"] = jr["alvazszam"]?.ToString(),
                            ["marka"] = jr["marka_nev"]?.ToString(),
                            ["tipus"] = jr["marka_tipus"]?.ToString(),
                            ["modell"] = jr["modell_nev"]?.ToString(),
                            ["generacio"] = jr["generacio"]?.ToString(),
                            ["evjarat"] = jr["evjarat"] is not DBNull && jr["evjarat"] != null ? Convert.ToInt32(jr["evjarat"]) : null,
                            ["karosszeria"] = jr["karosszeria"]?.ToString(),
                            ["szin"] = jr["szin"]?.ToString(),
                            ["motor_kod"] = jr["motor_kod"]?.ToString(),
                            ["hengerurtartalom"] = jr["hengerurtartalom"] is not DBNull && jr["hengerurtartalom"] != null ? Convert.ToInt32(jr["hengerurtartalom"]) : null,
                            ["teljesitmeny_le"] = jr["teljesitmeny_le"] is not DBNull && jr["teljesitmeny_le"] != null ? Convert.ToInt32(jr["teljesitmeny_le"]) : null,
                            ["uzemanyag"] = jr["uzemanyag"]?.ToString()
                        };

                        return Ok(new { success = true, vehicle = vehicleInfo, products = parts });
                    }

                default:
                    return NotFound(new { success = false, error = "Endpoint not found" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = "Szerver hiba: " + ex.Message });
        }
    }
}
