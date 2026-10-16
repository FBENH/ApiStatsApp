using apiBask.Models;
using Microsoft.EntityFrameworkCore;

namespace apiBask;

public class BasketContext(DbContextOptions<BasketContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<Jugador> Jugadores => Set<Jugador>();
    public DbSet<Partido> Partidos => Set<Partido>();
    public DbSet<EstadisticaJugador> EstadisticasJugador => Set<EstadisticaJugador>();
    public DbSet<EstadisticaPartido> EstadisticasPartido => Set<EstadisticaPartido>();
    public DbSet<Jugada> Jugadas => Set<Jugada>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.Property(u => u.Nombre).HasMaxLength(16);
            e.HasIndex(u => u.Nombre).IsUnique();
            // Hash de PasswordHasher (PBKDF2), que ocupa 84 caracteres.
            e.Property(u => u.Pass).HasMaxLength(100);
            e.Property(u => u.Email).HasMaxLength(255);
        });

        modelBuilder.Entity<Equipo>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(30);
            e.Property(x => x.Ciudad).HasMaxLength(50);
            e.Property(x => x.Entrenador).HasMaxLength(50);
            e.Property(x => x.Escudo).HasMaxLength(500);
        });

        modelBuilder.Entity<Jugador>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(30);
            e.Property(x => x.Posicion).HasMaxLength(20);
            e.Property(x => x.Foto).HasMaxLength(500);
            e.Property(x => x.Altura).HasPrecision(5, 1);
            e.Property(x => x.Peso).HasPrecision(5, 1);
        });

        modelBuilder.Entity<Partido>(e =>
        {
            e.Property(x => x.Lugar).HasMaxLength(100);
            e.HasOne(x => x.Local).WithMany().HasForeignKey(x => x.LocalId);
            e.HasOne(x => x.Visitante).WithMany().HasForeignKey(x => x.VisitanteId);
            e.HasOne(x => x.Ganador).WithMany().HasForeignKey(x => x.GanadorId);
        });

        modelBuilder.Entity<EstadisticaJugador>(e =>
        {
            e.HasKey(x => new { x.PartidoId, x.JugadorId });
            e.HasOne(x => x.Partido).WithMany(p => p.EstadisticasJugador).HasForeignKey(x => x.PartidoId);
            e.HasOne(x => x.Equipo).WithMany().HasForeignKey(x => x.EquipoId);
            e.HasOne(x => x.Jugador).WithMany(j => j.Estadisticas).HasForeignKey(x => x.JugadorId);
        });

        modelBuilder.Entity<EstadisticaPartido>(e =>
        {
            e.HasKey(x => new { x.PartidoId, x.EquipoId });
            e.HasOne(x => x.Partido).WithMany(p => p.EstadisticasPartido).HasForeignKey(x => x.PartidoId);
            e.HasOne(x => x.Equipo).WithMany(eq => eq.EstadisticasPartido).HasForeignKey(x => x.EquipoId);
        });

        modelBuilder.Entity<Jugada>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.PartidoId, x.Numero });
            e.HasOne(x => x.Partido).WithMany(p => p.Jugadas).HasForeignKey(x => x.PartidoId);
            e.HasOne<Equipo>().WithMany().HasForeignKey(x => x.EquipoId);
            e.HasOne<Jugador>().WithMany().HasForeignKey(x => x.JugadorId);
            e.HasOne<Jugador>().WithMany().HasForeignKey(x => x.SaleId);
        });

        // Nada se borra en cascada: lo que tiene historial se da de baja con Activo = false.
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
