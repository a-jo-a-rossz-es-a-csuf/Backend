using System.Collections.Generic;

namespace webshop.Models;

public class OrderWithItemsDto
{
    public Rendelesek? Order { get; set; }

    public List<TetelDto>? Tetelek { get; set; }
}

public class TetelDto
{
    public int? AlkatreszId { get; set; }
    public int? OlajId { get; set; }
    public string? TermekNev { get; set; }
    public int Mennyiseg { get; set; }
    public decimal Egysegar { get; set; }
    public decimal Osszeg { get; set; }
}
