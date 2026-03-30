using System;
using System.Collections.Generic;

namespace webshop.Models;

public partial class Kategoriak
{
    public int Id { get; set; }

    public string Nev { get; set; } = null!;

    public int? SzuloId { get; set; }

    public string? Tipus { get; set; }
}
