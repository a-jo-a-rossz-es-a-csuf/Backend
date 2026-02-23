using Microsoft.AspNetCore.Mvc;
using AutoPartsApi.Services;
using Dapper;

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
    [HttpPost]
    public async Task<IActionResult> HandleAction(
        [FromQuery] string action = "list",
        [FromQuery] int id = 0,
        [FromQuery] string? cikkszam = null,
        [FromQuery] int modell_id = 0,
        [FromQuery] int motor_id = 0,
        [FromQuery] string? tipus = null,
        [FromQuery] string? kategoria = null,
        [FromQuery] string? gyarto = null,
        [FromQuery] string? kereses = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 12)
    {
        try
        {
            using var conn = _db.CreateConnection();
            conn.Open();

            switch (action)
            {
                case "search_cikkszam":
                {
                    if (string.IsNullOrEmpty(cikkszam))
                        return Ok(new { success = false, error = "Cikkszam megadasa kotelezo" });

                    var search = "%" + cikkszam + "%";
                    var products = await conn.QueryAsync<dynamic>(@"
                        SELECT a.*, k.nev as kategoria 
                        FROM alkatreszek a 
                        LEFT JOIN kategoriak k ON a.kategoria_id = k.id
                        WHERE (a.cikkszam LIKE @S OR a.oe_szam LIKE @S OR a.nev LIKE @S) AND a.aktiv = 1
                        ORDER BY a.nev", new { S = search });

                    return Ok(new { success = true, products });
                }

                case "search":
                {
                    var sql = @"
                        SELECT DISTINCT a.*, k.nev as kategoria_nev 
                        FROM alkatreszek a 
                        LEFT JOIN kategoriak k ON a.kategoria_id = k.id
                        INNER JOIN alkatresz_auto aa ON a.id = aa.alkatresz_id
                        WHERE aa.modell_id = @ModellId AND a.aktiv = 1";

                    var parameters = new DynamicParameters();
                    parameters.Add("ModellId", modell_id);

                    if (motor_id > 0)
                    {
                        sql += " AND (aa.motor_id = @MotorId OR aa.motor_id IS NULL)";
                        parameters.Add("MotorId", motor_id);
                    }

                    sql += " ORDER BY a.nev";
                    var products = await conn.QueryAsync<dynamic>(sql, parameters);
                    return Ok(new { success = true, products });
                }

                case "list":
                {
                    var where = "a.aktiv = 1";
                    var parameters = new DynamicParameters();

                    if (!string.IsNullOrEmpty(kategoria))
                    {
                        where += " AND a.kategoria_id = @Kat";
                        parameters.Add("Kat", kategoria);
                    }
                    if (!string.IsNullOrEmpty(gyarto))
                    {
                        where += " AND a.gyarto = @Gy";
                        parameters.Add("Gy", gyarto);
                    }
                    if (!string.IsNullOrEmpty(kereses))
                    {
                        where += " AND (a.nev LIKE @Ker OR a.cikkszam LIKE @Ker OR a.oe_szam LIKE @Ker)";
                        parameters.Add("Ker", "%" + kereses + "%");
                    }
                    if (modell_id > 0)
                    {
                        where += " AND aa.modell_id = @Mid";
                        parameters.Add("Mid", modell_id);
                    }

                    page = Math.Max(1, page);
                    limit = Math.Clamp(limit, 1, 100);
                    int offset = (page - 1) * limit;

                    parameters.Add("Limit", limit);
                    parameters.Add("Offset", offset);

                    var sql = $@"
                        SELECT DISTINCT a.*, k.nev as kategoria_nev 
                        FROM alkatreszek a 
                        LEFT JOIN kategoriak k ON a.kategoria_id = k.id
                        LEFT JOIN alkatresz_auto aa ON a.id = aa.alkatresz_id
                        WHERE {where}
                        ORDER BY a.letrehozva DESC
                        LIMIT @Limit OFFSET @Offset";

                    var products = await conn.QueryAsync<dynamic>(sql, parameters);
                    return Ok(new { success = true, products });
                }

                case "get":
                {
                    var product = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                        SELECT a.*, k.nev as kategoria_nev 
                        FROM alkatreszek a 
                        LEFT JOIN kategoriak k ON a.kategoria_id = k.id 
                        WHERE a.id = @Id", new { Id = id });

                    if (product != null)
                        return Ok(new { success = true, product });
                    return Ok(new { success = false, error = "Termek nem talalhato" });
                }

                case "create":
                {
                    var data = await ReadBody();
                    if (data == null) return Ok(new { success = false, error = "Hibas JSON" });

                    var newId = await conn.ExecuteScalarAsync<int>(@"
                        INSERT INTO alkatreszek (cikkszam, nev, leiras, kategoria_id, ar, akcios_ar, keszlet, gyarto, oe_szam, kep_url)
                        VALUES (@cikkszam, @nev, @leiras, @kategoria_id, @ar, @akcios_ar, @keszlet, @gyarto, @oe_szam, @kep_url);
                        SELECT LAST_INSERT_ID()",
                        new
                        {
                            cikkszam = GetStr(data, "cikkszam"),
                            nev = GetStr(data, "nev"),
                            leiras = GetStr(data, "leiras"),
                            kategoria_id = GetIntNull(data, "kategoria_id"),
                            ar = GetDecimal(data, "ar"),
                            akcios_ar = GetDecimalNull(data, "akcios_ar"),
                            keszlet = GetInt(data, "keszlet"),
                            gyarto = GetStr(data, "gyarto"),
                            oe_szam = GetStr(data, "oe_szam"),
                            kep_url = GetStr(data, "kep_url")
                        });

                    return StatusCode(201, new { success = true, id = newId, message = "Termek sikeresen letrehozva" });
                }

                case "update":
                {
                    var data = await ReadBody();
                    if (data == null) return Ok(new { success = false, error = "Hibas JSON" });

                    await conn.ExecuteAsync(@"
                        UPDATE alkatreszek SET 
                            cikkszam=@cikkszam, nev=@nev, leiras=@leiras, kategoria_id=@kategoria_id,
                            ar=@ar, akcios_ar=@akcios_ar, keszlet=@keszlet, gyarto=@gyarto, oe_szam=@oe_szam, kep_url=@kep_url
                        WHERE id=@id",
                        new
                        {
                            id = GetInt(data, "id"),
                            cikkszam = GetStr(data, "cikkszam"),
                            nev = GetStr(data, "nev"),
                            leiras = GetStr(data, "leiras"),
                            kategoria_id = GetIntNull(data, "kategoria_id"),
                            ar = GetDecimal(data, "ar"),
                            akcios_ar = GetDecimalNull(data, "akcios_ar"),
                            keszlet = GetInt(data, "keszlet"),
                            gyarto = GetStr(data, "gyarto"),
                            oe_szam = GetStr(data, "oe_szam"),
                            kep_url = GetStr(data, "kep_url")
                        });

                    return Ok(new { success = true, message = "Termek sikeresen frissitve" });
                }

                case "delete":
                {
                    var data = await ReadBody();
                    int delId = GetInt(data, "id");
                    await conn.ExecuteAsync("DELETE FROM alkatresz_auto WHERE alkatresz_id = @Id", new { Id = delId });
                    await conn.ExecuteAsync("DELETE FROM alkatreszek WHERE id = @Id", new { Id = delId });
                    return Ok(new { success = true, message = "Termek sikeresen torolve" });
                }

                case "all":
                {
                    var products = await conn.QueryAsync<dynamic>(@"
                        SELECT a.*, k.nev as kategoria_nev 
                        FROM alkatreszek a 
                        LEFT JOIN kategoriak k ON a.kategoria_id = k.id 
                        ORDER BY a.letrehozva DESC");
                    return Ok(new { success = true, products });
                }

                case "categories":
                {
                    var categories = await conn.QueryAsync<dynamic>("SELECT * FROM kategoriak ORDER BY nev");
                    return Ok(new { success = true, categories });
                }

                default:
                    return Ok(new { success = false, error = "Ismeretlen muvelet" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = "Szerver hiba: " + ex.Message });
        }
    }

    private async Task<Dictionary<string, object?>?> ReadBody()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            if (string.IsNullOrEmpty(body)) return null;
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(body);
        }
        catch { return null; }
    }

    private static string GetStr(Dictionary<string, object?>? d, string k) =>
        d != null && d.ContainsKey(k) && d[k] != null ? d[k]!.ToString()! : "";

    private static int GetInt(Dictionary<string, object?>? d, string k, int def = 0) =>
        d != null && d.ContainsKey(k) && d[k] != null && int.TryParse(d[k]!.ToString(), out int v) ? v : def;

    private static int? GetIntNull(Dictionary<string, object?>? d, string k) =>
        d != null && d.ContainsKey(k) && d[k] != null && int.TryParse(d[k]!.ToString(), out int v) && v > 0 ? v : null;

    private static decimal GetDecimal(Dictionary<string, object?>? d, string k) =>
        d != null && d.ContainsKey(k) && d[k] != null && decimal.TryParse(d[k]!.ToString(), out decimal v) ? v : 0;

    private static decimal? GetDecimalNull(Dictionary<string, object?>? d, string k) =>
        d != null && d.ContainsKey(k) && d[k] != null && decimal.TryParse(d[k]!.ToString(), out decimal v) && v > 0 ? v : null;
}
