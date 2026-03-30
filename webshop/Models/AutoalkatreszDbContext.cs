using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace webshop.Models;

public partial class AutoalkatreszDbContext : DbContext
{
    public AutoalkatreszDbContext()
    {
    }

    public AutoalkatreszDbContext(DbContextOptions<AutoalkatreszDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AlkatreszAuto> AlkatreszAutos { get; set; }

    public virtual DbSet<Alkatreszek> Alkatreszeks { get; set; }

    public virtual DbSet<Automodellek> Automodelleks { get; set; }

    public virtual DbSet<ChatUzenetek> ChatUzeneteks { get; set; }

    public virtual DbSet<Cimek> Cimeks { get; set; }

    public virtual DbSet<Jarmuvek> Jarmuveks { get; set; }

    public virtual DbSet<Kategoriak> Kategoriaks { get; set; }

    public virtual DbSet<Kosar> Kosars { get; set; }

    public virtual DbSet<Markak> Markaks { get; set; }

    public virtual DbSet<Motorok> Motoroks { get; set; }

    public virtual DbSet<Olajok> Olajoks { get; set; }

    public virtual DbSet<RendelesTetelek> RendelesTeteleks { get; set; }

    public virtual DbSet<Rendelesek> Rendeleseks { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySQL("SERVER=localhost;PORT=3306;DATABASE=autoalkatresz_db;USER=root;PASSWORD=;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AlkatreszAuto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("alkatresz_auto");

            entity.HasIndex(e => e.AlkatreszId, "idx_alkatresz_id");

            entity.HasIndex(e => e.ModellId, "idx_modell_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.AlkatreszId)
                .HasColumnType("int(11)")
                .HasColumnName("alkatresz_id");
            entity.Property(e => e.ModellId)
                .HasColumnType("int(11)")
                .HasColumnName("modell_id");
            entity.Property(e => e.MotorId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("motor_id");
        });

        modelBuilder.Entity<Alkatreszek>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("alkatreszek");

            entity.HasIndex(e => e.KategoriaId, "idx_kategoria_id");

            entity.HasIndex(e => e.Cikkszam, "unique_cikkszam").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.AkciosAr)
                .HasPrecision(10)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("akcios_ar");
            entity.Property(e => e.Aktiv)
                .HasDefaultValueSql("'1'")
                .HasColumnName("aktiv");
            entity.Property(e => e.Ar)
                .HasPrecision(10)
                .HasColumnName("ar");
            entity.Property(e => e.Cikkszam)
                .HasMaxLength(50)
                .HasColumnName("cikkszam");
            entity.Property(e => e.Gyarto)
                .HasMaxLength(100)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("gyarto");
            entity.Property(e => e.KategoriaId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("kategoria_id");
            entity.Property(e => e.KepUrl)
                .HasMaxLength(255)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("kep_url");
            entity.Property(e => e.Keszlet)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("keszlet");
            entity.Property(e => e.Leiras)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("text")
                .HasColumnName("leiras");
            entity.Property(e => e.Letrehozva)
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("letrehozva");
            entity.Property(e => e.Nev)
                .HasMaxLength(255)
                .HasColumnName("nev");
            entity.Property(e => e.OeSzam)
                .HasMaxLength(100)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("oe_szam");
        });

        modelBuilder.Entity<Automodellek>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("automodellek");

            entity.HasIndex(e => e.MarkaId, "idx_marka_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.EvjaratIg)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("evjarat_ig");
            entity.Property(e => e.EvjaratTol)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("evjarat_tol");
            entity.Property(e => e.Generacio)
                .HasMaxLength(50)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("generacio");
            entity.Property(e => e.Karosszeria)
                .HasMaxLength(50)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("karosszeria");
            entity.Property(e => e.MarkaId)
                .HasColumnType("int(11)")
                .HasColumnName("marka_id");
            entity.Property(e => e.ModellNev)
                .HasMaxLength(100)
                .HasColumnName("modell_nev");
        });

        modelBuilder.Entity<ChatUzenetek>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("chat_uzenetek");

            entity.HasIndex(e => e.AdminId, "admin_id");

            entity.HasIndex(e => e.Statusz, "idx_statusz");

            entity.HasIndex(e => e.UserId, "idx_user_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.AdminId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("admin_id");
            entity.Property(e => e.AdminValasz)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("text")
                .HasColumnName("admin_valasz");
            entity.Property(e => e.Letrehozva)
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("letrehozva");
            entity.Property(e => e.Statusz)
                .HasDefaultValueSql("'''uj'''")
                .HasColumnType("enum('uj','folyamatban','megvalaszolva','lezart')")
                .HasColumnName("statusz");
            entity.Property(e => e.UserId)
                .HasColumnType("int(11)")
                .HasColumnName("user_id");
            entity.Property(e => e.Uzenet)
                .HasColumnType("text")
                .HasColumnName("uzenet");
            entity.Property(e => e.Valaszolva)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("datetime")
                .HasColumnName("valaszolva");

            entity.HasOne(d => d.Admin).WithMany(p => p.ChatUzenetekAdmins)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("chat_uzenetek_ibfk_2");

            entity.HasOne(d => d.User).WithMany(p => p.ChatUzenetekUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("chat_uzenetek_ibfk_1");
        });

        modelBuilder.Entity<Cimek>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("cimek");

            entity.HasIndex(e => e.UserId, "idx_user_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Alapertelmezett)
                .HasDefaultValueSql("'0'")
                .HasColumnName("alapertelmezett");
            entity.Property(e => e.CimTipus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'''szallitas'''")
                .HasColumnName("cim_tipus");
            entity.Property(e => e.Hazszam)
                .HasMaxLength(20)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("hazszam");
            entity.Property(e => e.Iranyitoszam)
                .HasMaxLength(10)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("iranyitoszam");
            entity.Property(e => e.Nev)
                .HasMaxLength(100)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("nev");
            entity.Property(e => e.Telefon)
                .HasMaxLength(20)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("telefon");
            entity.Property(e => e.UserId)
                .HasColumnType("int(11)")
                .HasColumnName("user_id");
            entity.Property(e => e.Utca)
                .HasMaxLength(100)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("utca");
            entity.Property(e => e.Varos)
                .HasMaxLength(50)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("varos");
        });

        modelBuilder.Entity<Jarmuvek>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("jarmuvek");

            entity.HasIndex(e => e.ModellId, "idx_modell_id");

            entity.HasIndex(e => e.MotorId, "idx_motor_id");

            entity.HasIndex(e => e.Alvazszam, "unique_alvazszam").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Alvazszam)
                .HasMaxLength(17)
                .HasColumnName("alvazszam");
            entity.Property(e => e.Evjarat)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("evjarat");
            entity.Property(e => e.Letrehozva)
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("letrehozva");
            entity.Property(e => e.ModellId)
                .HasColumnType("int(11)")
                .HasColumnName("modell_id");
            entity.Property(e => e.MotorId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("motor_id");
            entity.Property(e => e.Szin)
                .HasMaxLength(50)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("szin");
        });

        modelBuilder.Entity<Kategoriak>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("kategoriak");

            entity.HasIndex(e => e.SzuloId, "idx_szulo_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Nev)
                .HasMaxLength(100)
                .HasColumnName("nev");
            entity.Property(e => e.SzuloId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("szulo_id");
            entity.Property(e => e.Tipus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'''szemely'''")
                .HasColumnName("tipus");
        });

        modelBuilder.Entity<Kosar>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("kosar");

            entity.HasIndex(e => e.UserId, "idx_user_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.AlkatreszId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("alkatresz_id");
            entity.Property(e => e.Hozzaadva)
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("hozzaadva");
            entity.Property(e => e.Mennyiseg)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("mennyiseg");
            entity.Property(e => e.OlajId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("olaj_id");
            entity.Property(e => e.UserId)
                .HasColumnType("int(11)")
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Kosars)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_kosar_user");
        });

        modelBuilder.Entity<Markak>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("markak");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Aktiv)
                .HasDefaultValueSql("'1'")
                .HasColumnName("aktiv");
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(255)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("logo_url");
            entity.Property(e => e.Nev)
                .HasMaxLength(50)
                .HasColumnName("nev");
            entity.Property(e => e.Tipus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'''szemely'''")
                .HasColumnName("tipus");
        });

        modelBuilder.Entity<Motorok>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("motorok");

            entity.HasIndex(e => e.ModellId, "idx_modell_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Hengerszam)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("hengerszam");
            entity.Property(e => e.Hengerurtartalom)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("hengerurtartalom");
            entity.Property(e => e.ModellId)
                .HasColumnType("int(11)")
                .HasColumnName("modell_id");
            entity.Property(e => e.MotorKod)
                .HasMaxLength(50)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("motor_kod");
            entity.Property(e => e.Nyomatek)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("nyomatek");
            entity.Property(e => e.TeljesitmenyKw)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("teljesitmeny_kw");
            entity.Property(e => e.TeljesitmenyLe)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("teljesitmeny_le");
            entity.Property(e => e.Uzemanyag)
                .HasMaxLength(20)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("uzemanyag");
        });

        modelBuilder.Entity<Olajok>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("olajok");

            entity.HasIndex(e => e.Cikkszam, "unique_cikkszam").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.AkciosAr)
                .HasPrecision(10)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("akcios_ar");
            entity.Property(e => e.Aktiv)
                .HasDefaultValueSql("'1'")
                .HasColumnName("aktiv");
            entity.Property(e => e.Ar)
                .HasPrecision(10)
                .HasColumnName("ar");
            entity.Property(e => e.Cikkszam)
                .HasMaxLength(50)
                .HasColumnName("cikkszam");
            entity.Property(e => e.Gyarto)
                .HasMaxLength(100)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("gyarto");
            entity.Property(e => e.KepUrl)
                .HasMaxLength(255)
                .HasColumnName("kep_url");
            entity.Property(e => e.Keszlet)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("keszlet");
            entity.Property(e => e.Kiszereles)
                .HasMaxLength(20)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("kiszereles");
            entity.Property(e => e.Leiras)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("text")
                .HasColumnName("leiras");
            entity.Property(e => e.Nev)
                .HasMaxLength(255)
                .HasColumnName("nev");
            entity.Property(e => e.Specifikacio)
                .HasMaxLength(255)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("specifikacio");
            entity.Property(e => e.Tipus)
                .HasMaxLength(50)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("tipus");
            entity.Property(e => e.Viszkozitas)
                .HasMaxLength(20)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("viszkozitas");
        });

        modelBuilder.Entity<RendelesTetelek>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("rendeles_tetelek");

            entity.HasIndex(e => e.RendelesId, "idx_rendeles_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.AlkatreszId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("alkatresz_id");
            entity.Property(e => e.Egysegar)
                .HasPrecision(10)
                .HasColumnName("egysegar");
            entity.Property(e => e.Mennyiseg)
                .HasColumnType("int(11)")
                .HasColumnName("mennyiseg");
            entity.Property(e => e.OlajId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("olaj_id");
            entity.Property(e => e.Osszeg)
                .HasPrecision(10)
                .HasColumnName("osszeg");
            entity.Property(e => e.RendelesId)
                .HasColumnType("int(11)")
                .HasColumnName("rendeles_id");
            entity.Property(e => e.TermekNev)
                .HasMaxLength(255)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("termek_nev");
        });

        modelBuilder.Entity<Rendelesek>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("rendelesek");

            entity.HasIndex(e => e.Statusz, "idx_statusz");

            entity.HasIndex(e => e.UserId, "idx_user_id");

            entity.HasIndex(e => e.RendelesSzam, "unique_rendeles_szam").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("email");
            entity.Property(e => e.FizetesiMod)
                .HasMaxLength(30)
                .HasDefaultValueSql("'''utanvet'''")
                .HasColumnName("fizetesi_mod");
            entity.Property(e => e.Hazszam)
                .HasMaxLength(20)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("hazszam");
            entity.Property(e => e.Iranyitoszam)
                .HasMaxLength(10)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("iranyitoszam");
            entity.Property(e => e.Letrehozva)
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("letrehozva");
            entity.Property(e => e.Megjegyzes)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("text")
                .HasColumnName("megjegyzes");
            entity.Property(e => e.Nev)
                .HasMaxLength(100)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("nev");
            entity.Property(e => e.Osszeg)
                .HasPrecision(10)
                .HasColumnName("osszeg");
            entity.Property(e => e.RendelesSzam)
                .HasMaxLength(50)
                .HasColumnName("rendeles_szam");
            entity.Property(e => e.Statusz)
                .HasMaxLength(30)
                .HasDefaultValueSql("'''fuggoben'''")
                .HasColumnName("statusz");
            entity.Property(e => e.SzallitasiDij)
                .HasPrecision(10)
                .HasDefaultValueSql("'1490.00'")
                .HasColumnName("szallitasi_dij");
            entity.Property(e => e.Telefon)
                .HasMaxLength(20)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("telefon");
            entity.Property(e => e.UserId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("user_id");
            entity.Property(e => e.Utca)
                .HasMaxLength(100)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("utca");
            entity.Property(e => e.Varos)
                .HasMaxLength(50)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("varos");
            entity.Property(e => e.Vegosszeg)
                .HasPrecision(10)
                .HasColumnName("vegosszeg");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "unique_email").IsUnique();

            entity.HasIndex(e => e.Felhasznalonev, "unique_felhasznalonev").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Felhasznalonev)
                .HasMaxLength(50)
                .HasColumnName("felhasznalonev");
            entity.Property(e => e.Jelszo)
                .HasMaxLength(255)
                .HasColumnName("jelszo");
            entity.Property(e => e.Keresztnev)
                .HasMaxLength(50)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("keresztnev");
            entity.Property(e => e.Letrehozva)
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("letrehozva");
            entity.Property(e => e.Szerepkor)
                .HasMaxLength(20)
                .HasDefaultValueSql("'''user'''")
                .HasColumnName("szerepkor");
            entity.Property(e => e.Telefon)
                .HasMaxLength(20)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("telefon");
            entity.Property(e => e.UtolsoBelepes)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("datetime")
                .HasColumnName("utolso_belepes");
            entity.Property(e => e.Vezeteknev)
                .HasMaxLength(50)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("vezeteknev");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
