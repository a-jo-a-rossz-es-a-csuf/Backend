using System;
using System.Collections.Generic;

namespace webshop.Models;

public partial class Cimek
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? CimTipus { get; set; }

    public string? Nev { get; set; }

    public string? Iranyitoszam { get; set; }

    public string? Varos { get; set; }

    public string? Utca { get; set; }

    public string? Hazszam { get; set; }

    public string? Telefon { get; set; }

    public bool? Alapertelmezett { get; set; }
}
