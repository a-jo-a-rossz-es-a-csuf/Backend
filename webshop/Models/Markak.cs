using System;
using System.Collections.Generic;

namespace webshop.Models;

public partial class Markak
{
    public int Id { get; set; }

    public string Nev { get; set; } = null!;

    public string? Tipus { get; set; }

    public string? LogoUrl { get; set; }

    public bool? Aktiv { get; set; }
}
