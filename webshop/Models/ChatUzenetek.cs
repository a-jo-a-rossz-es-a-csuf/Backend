using System;
using System.Collections.Generic;

namespace webshop.Models;

public partial class ChatUzenetek
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Uzenet { get; set; } = null!;

    public string? AdminValasz { get; set; }

    public int? AdminId { get; set; }

    public string? Statusz { get; set; }

    public DateTime? Letrehozva { get; set; }

    public DateTime? Valaszolva { get; set; }

    public virtual User? Admin { get; set; }

    public virtual User User { get; set; } = null!;
}
