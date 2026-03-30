using Microsoft.AspNetCore.Mvc;
using AutoPartsApi.Services;
using Dapper;

namespace AutoPartsApi.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly DbService _db;

    public OrdersController(DbService db)
    {
        _db = db;
    }

    [HttpGet]
    [HttpPost]
    [HttpPut]
    public async Task<IActionResult> HandleAction([FromQuery] string action = "")
    {
        try
        {
            using var conn = _db.CreateConnection();
            conn.Open();

            switch (action)
            {
                case "create": return await CreateOrder(conn);
                case "list": return await ListOrders(conn);
                case "get": return await GetOrder(conn);
                case "update_status": return await UpdateOrderStatus(conn);
                default: return NotFound(new { success = false, error = "Endpoint not found" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = "Szerver hiba: " + ex.Message });
        }
    }

    private async Task<IActionResult> CreateOrder(System.Data.IDbConnection conn)
    {
        var data = await ReadBody();
        if (data == null) return Ok(new { success = false, error = "Hibas JSON adat" });

        int userId = GetInt(data, "user_id");
        string nev = GetStr(data, "nev");
        string email = GetStr(data, "email");
        string telefon = GetStr(data, "telefon");
        string iranyitoszam = GetStr(data, "iranyitoszam");
        string varos = GetStr(data, "varos");
        string utca = GetStr(data, "utca");
        string hazszam = GetStr(data, "hazszam");
        string megjegyzes = GetStr(data, "megjegyzes");
        string fizetesiMod = GetStr(data, "fizetesi_mod");
        if (string.IsNullOrEmpty(fizetesiMod)) fizetesiMod = "utanvet";

        if (userId == 0 || string.IsNullOrEmpty(nev) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(telefon) || string.IsNullOrEmpty(iranyitoszam) ||
            string.IsNullOrEmpty(varos) || string.IsNullOrEmpty(utca))
            return Ok(new { success = false, error = "Hianyzo adatok" });

        var cartItems = (await conn.QueryAsync(@"
            SELECT k.*, 
                   COALESCE(a.nev, o.nev) as termek_nev,
                   COALESCE(a.cikkszam, o.cikkszam) as cikkszam,
                   COALESCE(COALESCE(a.akcios_ar, a.ar), COALESCE(o.akcios_ar, o.ar)) as ar
            FROM kosar k
            LEFT JOIN alkatreszek a ON k.alkatresz_id = a.id
            LEFT JOIN olajok o ON k.olaj_id = o.id
            WHERE k.user_id = @UserId", new { UserId = userId })).ToList();

        if (cartItems.Count == 0) return Ok(new { success = false, error = "A kosar ures" });

        // --- KEDVEZMÉNY JOGOSULTSÁG ELLENÕRZÉSE ---
        int korabbiRendelesekSzama = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(id) FROM rendelesek WHERE user_id = @UserId", new { UserId = userId });

        bool isElsoVasarlas = korabbiRendelesekSzama == 0;

        decimal osszeg = 0;
        var veglegesTetelek = new List<dynamic>(); // Ebbe tesszük a kiszámolt, végleges árakat

        foreach (var rawItem in cartItems)
        {
            var r = (IDictionary<string, object>)rawItem;
            decimal ar = r["ar"] != null && r["ar"] is not DBNull ? Convert.ToDecimal(r["ar"]) : 0;

            // Ha jogosult a kedvezményre, azonnal módosítjuk a tételes árat
            if (isElsoVasarlas)
            {
                ar = Math.Round(ar * 0.85m);
            }

            int menny = Convert.ToInt32(r["mennyiseg"]);
            decimal itemOsszeg = ar * menny;
            osszeg += itemOsszeg;

            // Biztonságosan kimentjük a végleges adatokat egy új objektumba
            veglegesTetelek.Add(new
            {
                alkatresz_id = r["alkatresz_id"] is not DBNull ? r["alkatresz_id"] : null,
                olaj_id = r["olaj_id"] is not DBNull ? r["olaj_id"] : null,
                termek_nev = r["termek_nev"]?.ToString() ?? "Ismeretlen termek",
                mennyiseg = menny,
                egysegar = ar,
                osszeg = itemOsszeg
            });
        }

        decimal szallitasiDij = 1490;
        decimal vegosszeg = osszeg + szallitasiDij;
        string rendelesSzam = "AP-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(1, 9999).ToString("D4");

        using var transaction = conn.BeginTransaction();
        try
        {
            // Fõ rendelés mentése
            await conn.ExecuteAsync(@"
                INSERT INTO rendelesek (user_id, rendeles_szam, nev, email, telefon, iranyitoszam, varos, utca, hazszam, megjegyzes, osszeg, szallitasi_dij, vegosszeg, fizetesi_mod, statusz)
                VALUES (@UserId, @RSz, @Nev, @Email, @Tel, @Ir, @Var, @Utca, @Hsz, @Megj, @Ossz, @Szall, @Veg, @FizMod, 'fuggoben')",
                new { UserId = userId, RSz = rendelesSzam, Nev = nev, Email = email, Tel = telefon, Ir = iranyitoszam, Var = varos, Utca = utca, Hsz = hazszam, Megj = megjegyzes, Ossz = osszeg, Szall = szallitasiDij, Veg = vegosszeg, FizMod = fizetesiMod },
                transaction);

            var rendelesId = await conn.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()", transaction: transaction);

            // Tételek mentése az ÚJ, VÉGLEGESÍTETT listából ÉS készlet csökkentése
            foreach (var tetel in veglegesTetelek)
            {
                int? aId = tetel.alkatresz_id != null ? Convert.ToInt32(tetel.alkatresz_id) : null;
                int? oId = tetel.olaj_id != null ? Convert.ToInt32(tetel.olaj_id) : null;
                if (aId == 0) aId = null;
                if (oId == 0) oId = null;

                // 1. Tétel beszúrása
                await conn.ExecuteAsync(@"
                    INSERT INTO rendeles_tetelek (rendeles_id, alkatresz_id, olaj_id, termek_nev, mennyiseg, egysegar, osszeg)
                    VALUES (@RId, @AId, @OId, @TNev, @Menny, @Ar, @Ossz)",
                    new
                    {
                        RId = rendelesId,
                        AId = aId,
                        OId = oId,
                        TNev = tetel.termek_nev,
                        Menny = tetel.mennyiseg,
                        Ar = tetel.egysegar,
                        Ossz = tetel.osszeg
                    },
                    transaction);

                // 2. Készlet csökkentése
                if (aId.HasValue)
                {
                    await conn.ExecuteAsync(@"
                        UPDATE alkatreszek 
                        SET keszlet = keszlet - @Menny 
                        WHERE id = @Id",
                        new { Menny = tetel.mennyiseg, Id = aId.Value },
                        transaction);
                }
                else if (oId.HasValue)
                {
                    await conn.ExecuteAsync(@"
                        UPDATE olajok 
                        SET keszlet = keszlet - @Menny 
                        WHERE id = @Id",
                        new { Menny = tetel.mennyiseg, Id = oId.Value },
                        transaction);
                }
            }

            // Kosár ürítés
            await conn.ExecuteAsync("DELETE FROM kosar WHERE user_id = @U", new { U = userId }, transaction);
            transaction.Commit();

            return Ok(new { success = true, rendeles_szam = rendelesSzam, vegosszeg, message = "Rendeles sikeresen rogzitve" });
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private async Task<IActionResult> ListOrders(System.Data.IDbConnection conn)
    {
        var orders = await conn.QueryAsync(@"
            SELECT r.*, COUNT(rt.id) as tetelek_szama
            FROM rendelesek r
            LEFT JOIN rendeles_tetelek rt ON r.id = rt.rendeles_id
            GROUP BY r.id
            ORDER BY r.letrehozva DESC");
        return Ok(new { success = true, orders });
    }

    private async Task<IActionResult> GetOrder(System.Data.IDbConnection conn)
    {
        int orderId = 0;
        int.TryParse(Request.Query["id"].FirstOrDefault(), out orderId);

        if (orderId == 0) return Ok(new { success = false, error = "Hianyzo rendeles ID" });

        var order = (await conn.QueryAsync(
            "SELECT * FROM rendelesek WHERE id = @Id", new { Id = orderId })).FirstOrDefault();

        if (order == null) return Ok(new { success = false, error = "Rendeles nem talalhato" });

        var items = await conn.QueryAsync(
            "SELECT * FROM rendeles_tetelek WHERE rendeles_id = @Id", new { Id = orderId });

        var orderRow = (IDictionary<string, object>)order;
        var orderDict = new Dictionary<string, object?>();
        foreach (var kv in orderRow)
        {
            orderDict[kv.Key] = kv.Value is DBNull ? null : kv.Value;
        }
        orderDict["tetelek"] = items.ToList();

        return Ok(new { success = true, order = orderDict });
    }

    private async Task<IActionResult> UpdateOrderStatus(System.Data.IDbConnection conn)
    {
        var data = await ReadBody();
        int orderId = GetInt(data, "order_id");
        if (orderId == 0) orderId = GetInt(data, "id");
        string statusz = GetStr(data, "statusz");

        if (orderId == 0 || string.IsNullOrEmpty(statusz)) return Ok(new { success = false, error = "Hianyzo adatok" });

        await conn.ExecuteAsync("UPDATE rendelesek SET statusz = @S WHERE id = @Id",
            new { S = statusz, Id = orderId });
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

    private static string GetStr(Dictionary<string, object?>? d, string k) =>
        d != null && d.ContainsKey(k) && d[k] != null ? d[k]!.ToString()! : "";

    private static int GetInt(Dictionary<string, object?>? d, string k, int def = 0) =>
        d != null && d.ContainsKey(k) && d[k] != null && int.TryParse(d[k]!.ToString(), out int v) ? v : def;
}