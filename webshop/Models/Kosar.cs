using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace webshop.Models;

public partial class Kosar
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? AlkatreszId { get; set; }

    public int? OlajId { get; set; }

    public int? Mennyiseg { get; set; }

    public DateTime? Hozzaadva { get; set; }

    [JsonIgnore]
    public virtual User? User { get; set; }
}
