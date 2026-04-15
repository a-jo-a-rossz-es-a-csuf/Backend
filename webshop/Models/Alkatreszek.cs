using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace webshop.Models;

public partial class Alkatreszek
{
    public int Id { get; set; }

    public string Cikkszam { get; set; } = null!;

    public string Nev { get; set; } = null!;

    public string? Leiras { get; set; }

    public int? KategoriaId { get; set; }

    public decimal Ar { get; set; }

    public decimal? AkciosAr { get; set; }

    [ConcurrencyCheck]
    public int? Keszlet { get; set; }

    public string? Gyarto { get; set; }

    public string? OeSzam { get; set; }

    public string? KepUrl { get; set; }

    public bool? Aktiv { get; set; }

    public DateTime? Letrehozva { get; set; }
}
