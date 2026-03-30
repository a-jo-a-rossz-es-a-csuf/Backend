using System;
using System.Collections.Generic;

namespace webshop.Models;

public partial class Rendelesek
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string RendelesSzam { get; set; } = null!;

    public string? Statusz { get; set; }

    public string? Nev { get; set; }

    public string? Email { get; set; }

    public string? Telefon { get; set; }

    public string? Iranyitoszam { get; set; }

    public string? Varos { get; set; }

    public string? Utca { get; set; }

    public string? Hazszam { get; set; }

    public string? Megjegyzes { get; set; }

    public decimal Osszeg { get; set; }

    public decimal? SzallitasiDij { get; set; }

    public decimal Vegosszeg { get; set; }

    public string? FizetesiMod { get; set; }

    public DateTime? Letrehozva { get; set; }
}
