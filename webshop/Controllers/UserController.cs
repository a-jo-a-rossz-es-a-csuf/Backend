using AutoPartsApi.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using webshop.Models;

namespace AutoPartsApi.Controllers;

[ApiController]
[Route("api/admin")]
public class UserController : ControllerBase
{
    private readonly DbService _db;

    public UserController(DbService db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetAllUsers()
    {
        try
        {
            using var cx = new AutoalkatreszDbContext();
            var users = cx.Users
                .Select(u => new
                {
                    u.Id,
                    u.Felhasznalonev,
                    u.Email,
                    u.Vezeteknev,
                    u.Keresztnev,
                    u.Telefon,
                    u.Szerepkor,
                    u.Letrehozva,
                    u.UtolsoBelepes
                })
                .OrderByDescending(u => u.Letrehozva)
                .ToList();

            return StatusCode(200, users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public IActionResult PostUser([FromBody] User? user)
    {
        try
        {
            if (user == null)
                return BadRequest(new { success = false, error = "Adj meg minden paramétert" });

            if (string.IsNullOrWhiteSpace(user.Felhasznalonev) || string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.Jelszo))
                return BadRequest(new { success = false, error = "Felhasznalonev, email es jelszo kotelezo" });

            using var cx = new AutoalkatreszDbContext();

            bool exists = cx.Users.Any(u => u.Email == user.Email || u.Felhasznalonev == user.Felhasznalonev);
            if (exists) return Conflict(new { success = false, error = "Mar letezo felhasznalo" });

            user.Letrehozva ??= DateTime.Now;
            user.Szerepkor ??= "user";

            cx.Users.Add(user);
            cx.SaveChanges();

            return StatusCode(201, new { success = true, id = user.Id, message = "Felhasznalo sikeresen letrehozva" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutUser(int id, [FromBody] User? user)
    {
        try
        {
            if (user == null) return BadRequest(new { success = false, error = "Missing body (User)" });

            using var cx = new AutoalkatreszDbContext();
            var existing = cx.Users.FirstOrDefault(u => u.Id == id);
            if (existing == null) return NotFound(new { success = false, error = "Felhasznalo nem talalhato" });

            string incomingPassword = user.Jelszo ?? string.Empty;
            existing.Felhasznalonev = user.Felhasznalonev;
            existing.Email = user.Email;
            if (!string.IsNullOrWhiteSpace(incomingPassword))
            {
                existing.Jelszo = incomingPassword;
            }
            existing.Vezeteknev = user.Vezeteknev;
            existing.Keresztnev = user.Keresztnev;
            existing.Telefon = user.Telefon;
            existing.Szerepkor = user.Szerepkor ?? existing.Szerepkor;

            await cx.SaveChangesAsync();
            return StatusCode(200, new { success = true, message = "Sikeres modositas" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteUser(int id)
    {
        try
        {
            using var cx = new AutoalkatreszDbContext();
            var existing = cx.Users.FirstOrDefault(u => u.Id == id);
            if (existing == null) return NotFound(new { success = false, error = "Felhasznalo nem talalhato" });

            cx.Users.Remove(existing);
            cx.SaveChanges();
            return StatusCode(200, new { success = true, message = "Felhasznalo torolve" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
