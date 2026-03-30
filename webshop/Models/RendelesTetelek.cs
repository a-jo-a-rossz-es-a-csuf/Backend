using System;
using System.Collections.Generic;

namespace webshop.Models;

public partial class RendelesTetelek
{
    public int Id { get; set; }

    public int RendelesId { get; set; }

    public int? AlkatreszId { get; set; }

    public int? OlajId { get; set; }

    public string? TermekNev { get; set; }

    public int Mennyiseg { get; set; }

    public decimal Egysegar { get; set; }

    public decimal Osszeg { get; set; }
}
