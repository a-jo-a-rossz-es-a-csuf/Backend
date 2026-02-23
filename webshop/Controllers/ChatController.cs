using Microsoft.AspNetCore.Mvc;
using AutoPartsApi.Services;
using Dapper;

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
    [HttpPost]
    [HttpPut]
    public async Task<IActionResult> HandleAction(
        [FromQuery] string action = "",
        [FromQuery] int user_id = 0,
        [FromQuery] string? statusz = null)
    {
        try
        {
            using var conn = _db.CreateConnection();
            conn.Open();

            // Ellenorizzuk, hogy letezik-e a chat_uzenetek tabla
            var tableExists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'chat_uzenetek'");

            if (tableExists == 0 && action != "send")
                return Ok(new { success = true, data = Array.Empty<object>(), info = "A chat_uzenetek tabla nem letezik meg." });

            switch (action)
            {
                case "send":
                {
                    if (tableExists == 0)
                        return Ok(new { success = false, error = "A chat_uzenetek tabla nem letezik! Futtasd le a database_chat.sql fajlt phpMyAdmin-ban." });

                    var data = await ReadBody();
                    int userId = GetInt(data, "user_id");
                    string uzenet = GetStr(data, "uzenet").Trim();

                    if (userId <= 0)
                        return Ok(new { success = false, error = "Hianyzo vagy ervenytelen user_id" });
                    if (string.IsNullOrEmpty(uzenet))
                        return Ok(new { success = false, error = "Ures uzenet" });

                    var newId = await conn.ExecuteScalarAsync<int>(@"
                        INSERT INTO chat_uzenetek (user_id, uzenet, statusz, letrehozva) VALUES (@UserId, @Uzenet, 'uj', NOW());
                        SELECT LAST_INSERT_ID()", new { UserId = userId, Uzenet = uzenet });

                    return Ok(new { success = true, message = "Uzenet elkuldve", id = newId });
                }

                case "get_user_messages":
                {
                    if (user_id <= 0)
                        return Ok(new { success = false, error = "Hianyzo user_id" });

                    var messages = await conn.QueryAsync<dynamic>(@"
                        SELECT c.*, u.email as user_email, u.vezeteknev, u.keresztnev, a.email as admin_email
                        FROM chat_uzenetek c
                        LEFT JOIN users u ON c.user_id = u.id
                        LEFT JOIN users a ON c.admin_id = a.id
                        WHERE c.user_id = @UserId
                        ORDER BY c.letrehozva ASC", new { UserId = user_id });

                    return Ok(new { success = true, data = messages });
                }

                case "get_all_messages":
                {
                    var sql = @"
                        SELECT c.*, u.email as user_email, u.vezeteknev, u.keresztnev
                        FROM chat_uzenetek c
                        LEFT JOIN users u ON c.user_id = u.id";

                    var parameters = new DynamicParameters();
                    if (!string.IsNullOrEmpty(statusz))
                    {
                        sql += " WHERE c.statusz = @Statusz";
                        parameters.Add("Statusz", statusz);
                    }
                    sql += " ORDER BY c.letrehozva DESC";

                    var messages = await conn.QueryAsync<dynamic>(sql, parameters);
                    return Ok(new { success = true, data = messages });
                }

                case "reply":
                {
                    var data = await ReadBody();
                    int messageId = GetInt(data, "message_id");
                    int adminId = GetInt(data, "admin_id");
                    string valasz = GetStr(data, "valasz").Trim();

                    if (messageId <= 0 || adminId <= 0 || string.IsNullOrEmpty(valasz))
                        return Ok(new { success = false, error = "Hianyzo adatok" });

                    await conn.ExecuteAsync(@"
                        UPDATE chat_uzenetek SET admin_valasz = @V, admin_id = @AId, statusz = 'megvalaszolva', valaszolva = NOW()
                        WHERE id = @Id",
                        new { V = valasz, AId = adminId, Id = messageId });

                    return Ok(new { success = true, message = "Valasz elkuldve" });
                }

                case "close":
                {
                    var data = await ReadBody();
                    int messageId = GetInt(data, "message_id");

                    if (messageId <= 0)
                        return Ok(new { success = false, error = "Hianyzo message_id" });

                    await conn.ExecuteAsync("UPDATE chat_uzenetek SET statusz = 'lezart' WHERE id = @Id",
                        new { Id = messageId });
                    return Ok(new { success = true, message = "Chat lezarva" });
                }

                default:
                    return NotFound(new { success = false, error = "Endpoint not found: " + action });
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
}
