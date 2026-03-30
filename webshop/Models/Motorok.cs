using System;
using System.Collections.Generic;

namespace webshop.Models;

public partial class Motorok
{
    public int Id { get; set; }

    public int ModellId { get; set; }

    public string? MotorKod { get; set; }

    public int? Hengerurtartalom { get; set; }

    public int? TeljesitmenyLe { get; set; }

    public int? TeljesitmenyKw { get; set; }

    public string? Uzemanyag { get; set; }

    public int? Nyomatek { get; set; }

    public int? Hengerszam { get; set; }
}
