using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace webshop.Models;

public partial class User
{
    public int Id { get; set; }

    public string Felhasznalonev { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Jelszo { get; set; } = null!;

    public string? Vezeteknev { get; set; }

    public string? Keresztnev { get; set; }

    public string? Telefon { get; set; }

    public string? Szerepkor { get; set; }

    public DateTime? Letrehozva { get; set; }

    public DateTime? UtolsoBelepes { get; set; }


    [JsonIgnore]
    public virtual ICollection<ChatUzenetek> ChatUzenetekAdmins { get; set; } = new List<ChatUzenetek>();

    [JsonIgnore]
    public virtual ICollection<ChatUzenetek> ChatUzenetekUsers { get; set; } = new List<ChatUzenetek>();

    [JsonIgnore]
    public virtual ICollection<Kosar> Kosars { get; set; } = new List<Kosar>();
}
