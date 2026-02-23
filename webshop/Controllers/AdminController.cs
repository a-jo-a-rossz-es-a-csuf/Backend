using Microsoft.AspNetCore.Mvc;
using AutoPartsApi.Services;
using Dapper;
using System.Security.Cryptography;

namespace AutoPartsApi.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly DbService _db;

    public AdminController(DbService db)
    {
        _db = db;
    }

    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpDelete]
    public async Task<IActionResult> HandleAction([FromQuery] string action = "", [FromQuery] int id = 0)
    {
        try
        {
            using var conn = _db.CreateConnection();
            conn.Open();

            switch (action)
            {
                case "login":
                    {
                        var data = await ReadBody();
                        string login = GetStr(data, "email");
                        if (string.IsNullOrEmpty(login)) login = GetStr(data, "felhasznalonev");
                        string jelszo = GetStr(data, "jelszo");
                        if (string.IsNullOrEmpty(jelszo)) jelszo = GetStr(data, "password");

                        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(jelszo))
                            return BadRequest(new { success = false, error = "Email es jelszo megadasa kotelezo" });

                        var rows = await conn.QueryAsync(
                            @"SELECT id, felhasznalonev, email, jelszo, vezeteknev, keresztnev, telefon,
                                 CAST(szerepkor AS CHAR) as szerepkor
                          FROM users WHERE (email = @L OR felhasznalonev = @L) AND szerepkor = 'admin'",
                            new { L = login });
                        var user = rows.FirstOrDefault();

                        if (user == null)
                            return Unauthorized(new { success = false, error = "Hibas email vagy jelszo" });

                        var row = (IDictionary<string, object>)user;
                        if (row["jelszo"]?.ToString() != jelszo)
                            return Unauthorized(new { success = false, error = "Hibas email vagy jelszo" });

                        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower();
                        await conn.ExecuteAsync("UPDATE users SET utolso_belepes = NOW() WHERE id = @Id", new { Id = row["id"] });

                        var userDict = new Dictionary<string, object?>
                        {
                            ["id"] = row["id"],
                            ["felhasznalonev"] = row["felhasznalonev"]?.ToString(),
                            ["email"] = row["email"]?.ToString(),
                            ["vezeteknev"] = row["vezeteknev"]?.ToString(),
                            ["keresztnev"] = row["keresztnev"]?.ToString(),
                            ["szerepkor"] = row["szerepkor"]?.ToString()
                        };

                        return Ok(new { success = true, user = userDict, token });
                    }

                case "stats":
                    {
                        var termekek = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM alkatreszek WHERE aktiv = 1");
                        var felhasznalok = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users WHERE szerepkor = 'user'");

                        int rendelesek = 0;
                        decimal bevetel = 0;
                        int alacsonyKeszlet = 0;
                        decimal keszletErtek = 0;

                        try
                        {
                            rendelesek = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM rendelesek");
                            bevetel = await conn.ExecuteScalarAsync<decimal>("SELECT COALESCE(SUM(vegosszeg), 0) FROM rendelesek WHERE statusz != 'torolve'");
                        }
                        catch { /* rendelesek tabla nem letezik meg */ }

                        alacsonyKeszlet = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM alkatreszek WHERE keszlet < 10 AND keszlet > 0");
                        keszletErtek = await conn.ExecuteScalarAsync<decimal>("SELECT COALESCE(SUM(ar * keszlet), 0) FROM alkatreszek WHERE aktiv = 1");

                        return Ok(new
                        {
                            success = true,
                            stats = new
                            {
                                termekek,
                                felhasznalok,
                                rendelesek,
                                bevetel,
                                alacsony_keszlet = alacsonyKeszlet,
                                keszlet_ertek = keszletErtek
                            }
                        });
                    }

                case "users":
                    {
                        var users = await conn.QueryAsync<dynamic>(@"
                        SELECT id, felhasznalonev, email, vezeteknev, keresztnev, telefon, szerepkor, letrehozva, utolso_belepes 
                        FROM users ORDER BY letrehozva DESC");
                        return Ok(new { success = true, users });
                    }

                case "orders":
                    {
                        var orders = await conn.QueryAsync<dynamic>(@"
                        SELECT r.*, (SELECT COUNT(*) FROM rendeles_tetelek WHERE rendeles_id = r.id) as tetelek_szama
                        FROM rendelesek r ORDER BY r.letrehozva DESC");
                        return Ok(new { success = true, orders });
                    }

                case "order_details":
                    {
                        var order = await conn.QueryFirstOrDefaultAsync<dynamic>(
                            "SELECT * FROM rendelesek WHERE id = @Id", new { Id = id });

                        if (order != null)
                        {
                            var tetelek = await conn.QueryAsync<dynamic>(
                                "SELECT * FROM rendeles_tetelek WHERE rendeles_id = @Id", new { Id = id });

                            var orderDict = ((IDictionary<string, object>)order).ToDictionary(k => k.Key, k => k.Value);
                            orderDict["tetelek"] = tetelek.ToList();

                            return Ok(new { success = true, order = orderDict });
                        }
                        return Ok(new { success = false, error = "Rendeles nem talalhato" });
                    }

                case "update_order_status":
                    {
                        var data = await ReadBody();
                        await conn.ExecuteAsync("UPDATE rendelesek SET statusz = @S WHERE id = @Id",
                            new { S = GetStr(data, "statusz"), Id = GetInt(data, "id") });
                        return Ok(new { success = true });
                    }

                case "kategoriak":
                    {
                        var kategoriak = await conn.QueryAsync<dynamic>("SELECT * FROM kategoriak ORDER BY szulo_id, nev");
                        return Ok(new { success = true, kategoriak });
                    }

                case "products":
                    {
                        var products = await conn.QueryAsync<dynamic>(@"
                        SELECT a.*, k.nev as kategoria_nev 
                        FROM alkatreszek a LEFT JOIN kategoriak k ON a.kategoria_id = k.id 
                        ORDER BY a.id DESC");
                        return Ok(new { success = true, data = products });
                    }

                case "add_product":
                    {
                        var data = await ReadBody();
                        if (data == null) return Ok(new { success = false, error = "Hibas JSON adat" });

                        try
                        {
                            var newId = await conn.ExecuteScalarAsync<int>(@"
                            INSERT INTO alkatreszek (cikkszam, gyarto, nev, leiras, kategoria_id, oe_szam, ar, akcios_ar, keszlet, kep_url, aktiv) 
                            VALUES (@cikkszam, @gyarto, @nev, @leiras, @kategoria_id, @oe_szam, @ar, @akcios_ar, @keszlet, @kep_url, 1);
                            SELECT LAST_INSERT_ID()",
                                new
                                {
                                    cikkszam = GetStr(data, "cikkszam"),
                                    gyarto = GetStr(data, "gyarto"),
                                    nev = GetStr(data, "nev"),
                                    leiras = GetStr(data, "leiras"),
                                    kategoria_id = GetIntNull(data, "kategoria_id"),
                                    oe_szam = GetStr(data, "oe_szam"),
                                    ar = GetDecimal(data, "ar"),
                                    akcios_ar = GetDecimalNull(data, "akcios_ar"),
                                    keszlet = GetInt(data, "keszlet"),
                                    kep_url = GetStr(data, "kep_url")
                                });

                            return StatusCode(201, new { success = true, id = newId, message = "Termek sikeresen hozzaadva" });
                        }
                        catch (Exception ex)
                        {
                            return Ok(new { success = false, error = "Adatbazis hiba: " + ex.Message });
                        }
                    }

                case "update_product":
                    {
                        var data = await ReadBody();
                        if (data == null) return Ok(new { success = false, error = "Hibas JSON" });

                        await conn.ExecuteAsync(@"
                        UPDATE alkatreszek SET cikkszam=@cikkszam, gyarto=@gyarto, nev=@nev, leiras=@leiras,
                            kategoria_id=@kategoria_id, oe_szam=@oe_szam, ar=@ar, akcios_ar=@akcios_ar, keszlet=@keszlet, kep_url=@kep_url
                        WHERE id=@id",
                            new
                            {
                                id = id > 0 ? id : GetInt(data, "id"),
                                cikkszam = GetStr(data, "cikkszam"),
                                gyarto = GetStr(data, "gyarto"),
                                nev = GetStr(data, "nev"),
                                leiras = GetStr(data, "leiras"),
                                kategoria_id = GetIntNull(data, "kategoria_id"),
                                oe_szam = GetStr(data, "oe_szam"),
                                ar = GetDecimal(data, "ar"),
                                akcios_ar = GetDecimalNull(data, "akcios_ar"),
                                keszlet = GetInt(data, "keszlet"),
                                kep_url = GetStr(data, "kep_url")
                            });
                        return Ok(new { success = (object)true });
                    }

                case "delete_product":
                    {
                        await conn.ExecuteAsync("UPDATE alkatreszek SET aktiv = 0 WHERE id = @Id", new { Id = id });
                        return Ok(new { success = (object)true });
                    }

                case "get_product":
                    {
                        var product = await conn.QueryFirstOrDefaultAsync<dynamic>(
                            "SELECT * FROM alkatreszek WHERE id = @Id", new { Id = id });
                        return Ok(new { success = true, product });
                    }

                default:
                    return Ok(new { success = false, error = "Ismeretlen muvelet: " + action });
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
