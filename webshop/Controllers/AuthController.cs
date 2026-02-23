using Microsoft.AspNetCore.Mvc;
using AutoPartsApi.Services;
using Dapper;
using System.Security.Cryptography;

namespace AutoPartsApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly DbService _db;

    public AuthController(DbService db)
    {
        _db = db;
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> HandleAction([FromQuery] string action = "")
    {
        try
        {
            using var conn = _db.CreateConnection();
            conn.Open();

            switch (action)
            {
                case "login": return await Login(conn);
                case "register": return await Register(conn);
                case "logout": return Ok(new { success = true, message = "Sikeres kijelentkezes" });
                case "verify": return Ok(new { success = true });
                default: return NotFound(new { success = false, error = "Endpoint not found: " + action });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = "Szerver hiba: " + ex.Message });
        }
    }

    private async Task<IActionResult> Login(System.Data.IDbConnection conn)
    {
        var data = await ReadBody();
        var email = data?.GetValueOrDefault("email")?.ToString() ?? "";
        var jelszo = data?.GetValueOrDefault("jelszo")?.ToString() ?? "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(jelszo))
            return BadRequest(new { success = false, error = "Email es jelszo megadasa kotelezo" });

        var rows = await conn.QueryAsync(
            @"SELECT id, felhasznalonev, email, jelszo, vezeteknev, keresztnev, telefon,
                     CAST(szerepkor AS CHAR) as szerepkor,
                     letrehozva, utolso_belepes
              FROM users WHERE email = @Email", new { Email = email });

        var user = rows.FirstOrDefault();

        if (user == null)
            return Unauthorized(new { success = false, error = "Hibas email cim vagy jelszo" });

        // IDictionary-kent kezeljuk a dynamic-ot
        var row = (IDictionary<string, object>)user;
        var dbJelszo = row["jelszo"]?.ToString() ?? "";

        if (dbJelszo != jelszo)
            return Unauthorized(new { success = false, error = "Hibas email cim vagy jelszo" });

        await conn.ExecuteAsync(
            "UPDATE users SET utolso_belepes = NOW() WHERE id = @Id", new { Id = row["id"] });

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower();

        var userDict = new Dictionary<string, object?>
        {
            ["id"] = row["id"],
            ["felhasznalonev"] = row["felhasznalonev"]?.ToString(),
            ["email"] = row["email"]?.ToString(),
            ["vezeteknev"] = row["vezeteknev"]?.ToString(),
            ["keresztnev"] = row["keresztnev"]?.ToString(),
            ["telefon"] = row["telefon"]?.ToString(),
            ["szerepkor"] = row["szerepkor"]?.ToString(),
            ["letrehozva"] = row["letrehozva"] is DateTime dt1 ? dt1.ToString("yyyy-MM-dd HH:mm:ss") : null,
            ["utolso_belepes"] = row["utolso_belepes"] is DateTime dt2 ? dt2.ToString("yyyy-MM-dd HH:mm:ss") : null
        };

        return Ok(new { success = true, user = userDict, token });
    }

    private async Task<IActionResult> Register(System.Data.IDbConnection conn)
    {
        var data = await ReadBody();
        var email = data?.GetValueOrDefault("email")?.ToString() ?? "";
        var jelszo = data?.GetValueOrDefault("jelszo")?.ToString() ?? "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(jelszo))
            return BadRequest(new { success = false, error = "Email es jelszo megadasa kotelezo" });

        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM users WHERE email = @Email", new { Email = email });

        if (existing != null)
            return Conflict(new { success = false, error = "Ez az email cim mar foglalt" });

        var felhasznalonev = email.Split('@')[0] + new Random().Next(100, 999);

        await conn.ExecuteAsync(
            @"INSERT INTO users (felhasznalonev, email, jelszo, vezeteknev, keresztnev, telefon, szerepkor) 
              VALUES (@Fnev, @Email, @Jelszo, @Vnev, @Knev, @Tel, 'user')",
            new
            {
                Fnev = felhasznalonev,
                Email = email,
                Jelszo = jelszo,
                Vnev = data?.GetValueOrDefault("vezeteknev")?.ToString() ?? "",
                Knev = data?.GetValueOrDefault("keresztnev")?.ToString() ?? "",
                Tel = data?.GetValueOrDefault("telefon")?.ToString() ?? ""
            });

        return StatusCode(201, new { success = true, message = "Sikeres regisztracio! Most mar bejelentkezhet." });
    }

    private async Task<Dictionary<string, object?>?> ReadBody()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(body);
    }
}
