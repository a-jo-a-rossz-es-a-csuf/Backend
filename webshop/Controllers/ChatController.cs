using AutoPartsApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using webshop.Models;

namespace AutoPartsApi.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly DbService _db;

    public ChatController(DbService db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetAllMessages()
    {
        try
        {
            using var cx = new AutoalkatreszDbContext();
            var data = cx.ChatUzeneteks
                .Include(c => c.User)
                .Include(c => c.Admin)
                .OrderByDescending(c => c.Letrehozva)
                .Select(c => new
                {
                    id = c.Id,
                    userId = c.UserId,
                    user = c.User == null ? null : new
                    {
                        id = c.User.Id,
                        felhasznalonev = c.User.Felhasznalonev,
                        email = c.User.Email
                    },
                    uzenet = c.Uzenet,
                    adminValasz = c.AdminValasz,
                    adminId = c.AdminId,
                    admin = c.Admin == null ? null : new
                    {
                        id = c.Admin.Id,
                        felhasznalonev = c.Admin.Felhasznalonev,
                        email = c.Admin.Email
                    },
                    statusz = c.Statusz,
                    letrehozva = c.Letrehozva,
                    valaszolva = c.Valaszolva
                })
                .ToList();

            return StatusCode(200, data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    public class ChatCreateDto
    {
        public int UserId { get; set; }
        public string Uzenet { get; set; } = string.Empty;
    }

    [HttpPost]
    public IActionResult PostMessage([FromBody] ChatCreateDto? dto)
    {
        try
        {
            if (dto == null || dto.UserId <= 0 || string.IsNullOrWhiteSpace(dto.Uzenet))
                return BadRequest("Hianyzo adatok");

            using var cx = new AutoalkatreszDbContext();

            var user = cx.Users.FirstOrDefault(u => u.Id == dto.UserId);
            if (user == null) return BadRequest("Nincs ilyen felhasználó");

            var msg = new ChatUzenetek
            {
                UserId = dto.UserId,
                Uzenet = dto.Uzenet.Trim(),
                Statusz = "uj",
                Letrehozva = DateTime.Now,
                User = user
            };

            cx.ChatUzeneteks.Add(msg);
            cx.SaveChanges();

            var saved = cx.ChatUzeneteks
                .Include(c => c.User)
                .Where(c => c.Id == msg.Id)
                .Select(c => new
                {
                    id = c.Id,
                    userId = c.UserId,
                    user = new { id = c.User.Id, felhasznalonev = c.User.Felhasznalonev, email = c.User.Email },
                    uzenet = c.Uzenet,
                    statusz = c.Statusz,
                    letrehozva = c.Letrehozva
                })
                .FirstOrDefault();

            return StatusCode(201, new { success = true, message = "Üzenet elküldve", data = saved });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    public class ChatUpdateDto
    {
        public string? AdminValasz { get; set; }
        public int? AdminId { get; set; }
        public string? Statusz { get; set; }
    }

    [HttpPut("{id}")]
    public IActionResult PutMessage(int id, [FromBody] ChatUpdateDto? dto)
    {
        try
        {
            if (dto == null) return BadRequest("Hianyzo body");

            using var cx = new AutoalkatreszDbContext();
            var existing = cx.ChatUzeneteks.Include(c => c.Admin).FirstOrDefault(c => c.Id == id);
            if (existing == null) return NotFound("Üzenet nem található");

            if (!string.IsNullOrEmpty(dto.AdminValasz))
            {
                if (!dto.AdminId.HasValue || dto.AdminId.Value <= 0)
                    return BadRequest("Admin ID kötelezõ, ha válasz kerül mentésre");

                var admin = cx.Users.FirstOrDefault(u => u.Id == dto.AdminId.Value);
                if (admin == null) return BadRequest("Nincs ilyen admin felhasználó");

                existing.AdminValasz = dto.AdminValasz.Trim();
                existing.AdminId = dto.AdminId;
                existing.Admin = admin;
                existing.Valaszolva = DateTime.Now;
                existing.Statusz = dto.Statusz ?? "megvalaszolva";
            }
            else if (!string.IsNullOrEmpty(dto.Statusz))
            {
                existing.Statusz = dto.Statusz;
                if (dto.Statusz == "lezart") existing.Valaszolva = DateTime.Now;
            }

            cx.SaveChanges();
            return StatusCode(200, new { success = true, message = "Sikeres módosítás" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteMessage(int id)
    {
        try
        {
            using var cx = new AutoalkatreszDbContext();
            var existing = cx.ChatUzeneteks.FirstOrDefault(c => c.Id == id);
            if (existing == null) return NotFound("Nincs ilyen üzenet");

            cx.ChatUzeneteks.Remove(existing);
            cx.SaveChanges();
            return StatusCode(200, new { success = true, message = "Üzenet törölve" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
