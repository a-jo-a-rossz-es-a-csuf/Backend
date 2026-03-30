using Microsoft.AspNetCore.Mvc;
using AutoPartsApi.Services;
using Dapper;
using System.Security.Cryptography;
using System.Data;
// Ezt az új sort hozzáadtuk a BCrypt miatt:
using BCrypt.Net;

namespace AutoPartsApi.Controllers;

public class LoginDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Vezeteknev { get; set; } = "";
    public string Keresztnev { get; set; } = "";
    public string Telefon { get; set; } = "";
}

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly DbService _db;

    public AuthController(DbService db)
    {
        _db = db;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto data)
    {
        try
        {
            using var conn = _db.CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();

            var user = await conn.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT id, felhasznalonev, email, jelszo, vezeteknev, keresztnev, telefon,
                         CAST(szerepkor AS CHAR) as szerepkor
                  FROM users WHERE email = @Email", new { Email = data.Email });

            if (user == null)
            {
                return Unauthorized(new { success = false, error = "Hibás email cím vagy jelszó" });
            }

            var row = (IDictionary<string, object>)user;
            string dbJelszo = row["jelszo"]?.ToString() ?? "";

            // --- JAVÍTOTT JELSZÓ ELLENÕRZÉS ---
            // A BCrypt.Verify megnézi, hogy a sima jelszó (data.Password) megegyezik-e a titkosítottal (dbJelszo)
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(data.Password, dbJelszo);

            if (!isPasswordValid)
            {
                return Unauthorized(new { success = false, error = "Hibás email cím vagy jelszó" });
            }

            await conn.ExecuteAsync("UPDATE users SET utolso_belepes = NOW() WHERE id = @Id", new { Id = row["id"] });

            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower();

            return Ok(new
            {
                success = true,
                user = new
                {
                    id = row["id"],
                    email = row["email"],
                    felhasznalonev = row["felhasznalonev"],
                    vezeteknev = row["vezeteknev"],
                    keresztnev = row["keresztnev"],
                    szerepkor = row["szerepkor"]?.ToString()
                },
                token = token
            });
        }
        catch (Exception ex)
        {
            // Élesben érdemes csak egy általános hibaüzenetet küldeni
            return StatusCode(500, new { success = false, error = "Szerver hiba történt a bejelentkezés során." });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto data)
    {
        try
        {
            using var conn = _db.CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();

            var existing = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM users WHERE email = @Email", new { Email = data.Email });
            if (existing > 0)
            {
                return Conflict(new { success = false, error = "Ez az email cím már foglalt" });
            }

            var fnev = data.Email.Split('@')[0] + new Random().Next(100, 999);

            // --- JAVÍTOTT JELSZÓ MENTÉS ---
            // Itt titkosítjuk a jelszót, mielõtt betennénk az adatbázisba
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(data.Password);

            await conn.ExecuteAsync(
                @"INSERT INTO users (felhasznalonev, email, jelszo, vezeteknev, keresztnev, telefon, szerepkor, letrehozva)  
                  VALUES (@Fnev, @Email, @Jelszo, @Vnev, @Knev, @Tel, 'user', NOW())",
                new
                {
                    Fnev = fnev,
                    Email = data.Email,
                    Jelszo = hashedPassword, // Itt már a hashelt verziót adjuk át!
                    Vnev = data.Vezeteknev,
                    Knev = data.Keresztnev,
                    Tel = data.Telefon
                });

            return StatusCode(201, new { success = true, message = "Sikeres regisztráció!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = "Szerver hiba történt a regisztráció során." });
        }
    }

    [HttpGet("auth/verify")]
    public IActionResult Verify()
    {
        return Ok(new { success = true });
    }

    [HttpPost("auth/logout")]
    public IActionResult Logout()
    {
        return Ok(new { success = true, message = "Sikeres kijelentkezés" });
    }
}