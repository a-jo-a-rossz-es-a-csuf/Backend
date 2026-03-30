using System;
using System.Collections.Generic;

namespace webshop.Models;

public partial class AlkatreszAuto
{
    public int Id { get; set; }

    public int AlkatreszId { get; set; }

    public int ModellId { get; set; }

    public int? MotorId { get; set; }
}
