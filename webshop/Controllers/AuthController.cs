using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Cryptography;
using webshop.Models;
using BCrypt.Net;



namespace AutoPartsApi.Controllers;



[ApiController]

[Route("api")]

public class AuthController : ControllerBase

{

    [HttpPost("login")]

    public IActionResult Login([FromBody] LoginDto data)

    {

        try

        {

            if (data == null || string.IsNullOrWhiteSpace(data.Email) || string.IsNullOrWhiteSpace(data.Password))

                return BadRequest(new { success = false, error = "Email ?s jelsz? megad?sa k?telez?" });



            using var cx = new AutoalkatreszDbContext();

            var user = cx.Users.FirstOrDefault(u => u.Email == data.Email);

            if (user == null)

                return Unauthorized(new { success = false, error = "Hib?s email c?m vagy jelsz?" });



            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(data.Password, user.Jelszo);

            if (!isPasswordValid)

                return Unauthorized(new { success = false, error = "Hib?s email c?m vagy jelsz?" });



            user.UtolsoBelepes = DateTime.Now;

            cx.SaveChanges();



            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower();



            return Ok(new

            {

                success = true,

                user = new

                {

                    id = user.Id,

                    email = user.Email,

                    felhasznalonev = user.Felhasznalonev,

                    vezeteknev = user.Vezeteknev,

                    keresztnev = user.Keresztnev,

                    szerepkor = user.Szerepkor,

                    elsoVasarolasKedvezmeny = user.ElsoVasarolasKedvezmeny

                },

                token

            });

        }

        catch (Exception)

        {

            return StatusCode(500, new { success = false, error = "Szerver hiba t?rt?nt a bejelentkez?s sor?n." });

        }

    }



    [HttpPost("register")]

    public IActionResult Register([FromBody] RegisterDto data)

    {

        try

        {

            if (data == null || string.IsNullOrWhiteSpace(data.Email) || string.IsNullOrWhiteSpace(data.Password))

                return BadRequest(new { success = false, error = "Email ?s jelsz? megad?sa k?telez?" });



            using var cx = new AutoalkatreszDbContext();

            bool exists = cx.Users.Any(u => u.Email == data.Email);

            if (exists)

                return Conflict(new { success = false, error = "Ez az email c?m m?r foglalt" });



            var fnev = data.Email.Split('@')[0] + new Random().Next(100, 999);

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(data.Password);



            var user = new User

            {

                Felhasznalonev = fnev,

                Email = data.Email,

                Jelszo = hashedPassword,

                Vezeteknev = string.IsNullOrWhiteSpace(data.Vezeteknev) ? null : data.Vezeteknev,

                Keresztnev = string.IsNullOrWhiteSpace(data.Keresztnev) ? null : data.Keresztnev,

                Telefon = string.IsNullOrWhiteSpace(data.Telefon) ? null : data.Telefon,

                Szerepkor = "user",

                Letrehozva = DateTime.Now,

                ElsoVasarolasKedvezmeny = true

            };



            cx.Users.Add(user);

            cx.SaveChanges();



            return StatusCode(201, new

            {

                success = true,

                message = "Sikeres regisztr?ci?!",

                user = new

                {

                    id = user.Id,

                    email = user.Email,

                    felhasznalonev = user.Felhasznalonev,

                    vezeteknev = user.Vezeteknev,

                    keresztnev = user.Keresztnev,

                    szerepkor = user.Szerepkor,

                    elsoVasarolasKedvezmeny = user.ElsoVasarolasKedvezmeny

                }

            });

        }

        catch (Exception)

        {

            return StatusCode(500, new { success = false, error = "Szerver hiba t?rt?nt a regisztr?ci? sor?n." });

        }

    }



    [HttpGet("auth/verify")]

    public IActionResult Verify() => Ok(new { success = true });



    [HttpPost("auth/logout")]

    public IActionResult Logout() => Ok(new { success = true, message = "Sikeres kijelentkez?s" });

}


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
