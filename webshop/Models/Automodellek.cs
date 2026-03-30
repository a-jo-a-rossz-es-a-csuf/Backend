using System;
using System.Collections.Generic;

namespace webshop.Models;

public partial class Automodellek
{
    public int Id { get; set; }

    public int MarkaId { get; set; }

    public string ModellNev { get; set; } = null!;

    public string? Generacio { get; set; }

    public int? EvjaratTol { get; set; }

    public int? EvjaratIg { get; set; }

    public string? Karosszeria { get; set; }
}
