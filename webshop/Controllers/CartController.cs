using Microsoft.AspNetCore.Mvc;
using AutoPartsApi.Services;
using Dapper;

namespace AutoPartsApi.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly DbService _db;

    public CartController(DbService db)
    {
        _db = db;
    }

    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpDelete]
    public async Task<IActionResult> HandleAction(
        [FromQuery] string action = "get",
        [FromQuery] int user_id = 0)
    {
        try
        {
            using var conn = _db.CreateConnection();
            conn.Open();

            switch (action)
            {
                case "get": return await GetCart(conn, user_id);
                case "add": return await AddToCart(conn);
                case "update": return await UpdateCart(conn);
                case "remove": return await RemoveFromCart(conn);
                case "clear": return await ClearCart(conn, user_id);
                default: return NotFound(new { success = false, error = "Endpoint not found" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    private async Task<IActionResult> GetCart(System.Data.IDbConnection conn, int userId)
    {
        if (userId == 0)
        {
            var data = await ReadBody();
            userId = GetInt(data, "user_id");
        }

        if (userId == 0)
            return Ok(new { success = true, items = Array.Empty<object>(), total = 0, logged_in = false });

        var items = (await conn.QueryAsync(@"
            SELECT k.id, k.mennyiseg, k.alkatresz_id, k.olaj_id,
                   COALESCE(a.nev, o.nev) as nev,
                   COALESCE(a.cikkszam, o.cikkszam) as cikkszam,
                   COALESCE(COALESCE(a.akcios_ar, a.ar), COALESCE(o.akcios_ar, o.ar)) as ar,
                   COALESCE(a.gyarto, o.gyarto) as gyarto
            FROM kosar k
            LEFT JOIN alkatreszek a ON k.alkatresz_id = a.id
            LEFT JOIN olajok o ON k.olaj_id = o.id
            WHERE k.user_id = @UserId", new { UserId = userId })).ToList();

        decimal total = 0;
        var result = new List<Dictionary<string, object?>>();
        foreach (var rawItem in items)
        {
            var r = (IDictionary<string, object>)rawItem;
            decimal ar = r["ar"] != null ? Convert.ToDecimal(r["ar"]) : 0;
            int menny = Convert.ToInt32(r["mennyiseg"]);
            decimal osszeg = ar * menny;
            total += osszeg;

            result.Add(new Dictionary<string, object?>
            {
                ["id"] = Convert.ToInt32(r["id"]),
                ["mennyiseg"] = menny,
                ["alkatresz_id"] = r["alkatresz_id"] is DBNull ? null : r["alkatresz_id"],
                ["olaj_id"] = r["olaj_id"] is DBNull ? null : r["olaj_id"],
                ["nev"] = r["nev"]?.ToString(),
                ["cikkszam"] = r["cikkszam"]?.ToString(),
                ["ar"] = ar,
                ["gyarto"] = r["gyarto"]?.ToString(),
                ["osszeg"] = osszeg
            });
        }

        return Ok(new { success = true, items = result, total, logged_in = true });
    }

    private async Task<IActionResult> AddToCart(System.Data.IDbConnection conn)
    {
        var data = await ReadBody();
        int userId = GetInt(data, "user_id");
        int alkatreszId = GetInt(data, "alkatresz_id");
        int olajId = GetInt(data, "olaj_id");
        int mennyiseg = GetInt(data, "mennyiseg", 1);

        if (userId == 0)
            return Ok(new { success = false, error = "A kosarba rakashoz be kell jelentkezni!", require_login = true });

        if (alkatreszId == 0 && olajId == 0)
            return Ok(new { success = false, error = "Hianyzo termek azonosito" });

        var existing = (await conn.QueryAsync(
            alkatreszId > 0
                ? "SELECT id, mennyiseg FROM kosar WHERE user_id = @U AND alkatresz_id = @A"
                : "SELECT id, mennyiseg FROM kosar WHERE user_id = @U AND olaj_id = @O",
            new { U = userId, A = alkatreszId, O = olajId })).FirstOrDefault();

        if (existing != null)
        {
            var er = (IDictionary<string, object>)existing;
            await conn.ExecuteAsync(
                "UPDATE kosar SET mennyiseg = mennyiseg + @M WHERE id = @Id",
                new { M = mennyiseg, Id = Convert.ToInt32(er["id"]) });
        }
        else
        {
            await conn.ExecuteAsync(
                "INSERT INTO kosar (user_id, alkatresz_id, olaj_id, mennyiseg) VALUES (@U, @A, @O, @M)",
                new { U = userId, A = alkatreszId > 0 ? (int?)alkatreszId : null, O = olajId > 0 ? (int?)olajId : null, M = mennyiseg });
        }

        return Ok(new { success = true, message = "Termek hozzaadva a kosarhoz" });
    }

    private async Task<IActionResult> UpdateCart(System.Data.IDbConnection conn)
    {
        var data = await ReadBody();
        int cartId = GetInt(data, "cart_id");
        int mennyiseg = GetInt(data, "mennyiseg", 1);
        int userId = GetInt(data, "user_id");

        if (userId == 0) return Ok(new { success = false, error = "Nincs bejelentkezve" });
        if (cartId == 0) return Ok(new { success = false, error = "Hianyzo kosar ID" });

        if (mennyiseg <= 0)
            await conn.ExecuteAsync("DELETE FROM kosar WHERE id = @Id AND user_id = @U", new { Id = cartId, U = userId });
        else
            await conn.ExecuteAsync("UPDATE kosar SET mennyiseg = @M WHERE id = @Id AND user_id = @U",
                new { M = mennyiseg, Id = cartId, U = userId });

        return Ok(new { success = true });
    }

    private async Task<IActionResult> RemoveFromCart(System.Data.IDbConnection conn)
    {
        var data = await ReadBody();
        int cartId = GetInt(data, "cart_id");
        int userId = GetInt(data, "user_id");

        if (userId == 0) return Ok(new { success = false, error = "Nincs bejelentkezve" });
        if (cartId == 0) return Ok(new { success = false, error = "Hianyzo kosar ID" });

        await conn.ExecuteAsync("DELETE FROM kosar WHERE id = @Id AND user_id = @U", new { Id = cartId, U = userId });
        return Ok(new { success = true });
    }

    private async Task<IActionResult> ClearCart(System.Data.IDbConnection conn, int userId)
    {
        if (userId == 0)
        {
            var data = await ReadBody();
            userId = GetInt(data, "user_id");
        }
        if (userId == 0) return Ok(new { success = false, error = "Nincs bejelentkezve" });

        await conn.ExecuteAsync("DELETE FROM kosar WHERE user_id = @U", new { U = userId });
        return Ok(new { success = true });
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

    private static int GetInt(Dictionary<string, object?>? data, string key, int defaultValue = 0)
    {
        if (data == null || !data.ContainsKey(key) || data[key] == null) return defaultValue;
        var val = data[key]!.ToString();
        return int.TryParse(val, out int result) ? result : defaultValue;
    }
}
