using System;
using System.Collections.Generic;

namespace webshop.Models;

public partial class Jarmuvek
{
    public int Id { get; set; }

    public string Alvazszam { get; set; } = null!;

    public int ModellId { get; set; }

    public int? MotorId { get; set; }

    public int? Evjarat { get; set; }

    public string? Szin { get; set; }

    public DateTime? Letrehozva { get; set; }
}
