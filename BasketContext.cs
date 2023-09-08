using System;
using System.Collections.Generic;
using apiBask.Models;
using Microsoft.EntityFrameworkCore;

namespace apiBask;

public partial class BasketContext : DbContext
{
    
    public BasketContext()
    {
    } 

    public BasketContext(DbContextOptions<BasketContext> options)
       : base(options)
    {
    }

    public virtual DbSet<Equipo> Equipos { get; set; }

    public virtual DbSet<EstadisticaJugador> EstadisticaJugadors { get; set; }

    public virtual DbSet<EstadisticaPartido> EstadisticaPartidos { get; set; }

    public virtual DbSet<Jugador> Jugadors { get; set; }

    public virtual DbSet<Partido> Partidos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Equipo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Equipo__3213E83F03317E3D");

            entity.ToTable("Equipo");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Activo).HasColumnName("activo");
            entity.Property(e => e.Ciudad)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ciudad");
            entity.Property(e => e.Entrenador)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("entrenador");
            entity.Property(e => e.Escudo)
                .IsUnicode(false)
                .HasColumnName("escudo");
            entity.Property(e => e.IdUsuario)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasColumnName("id_usuario");
            entity.Property(e => e.Nombre)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("nombre");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Equipos)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Equipo__id_usuar__0519C6AF");
        });

        modelBuilder.Entity<EstadisticaJugador>(entity =>
        {
            entity.HasKey(e => new { e.JugadorId, e.PartidoId }).HasName("PK__Estadist__B03BB8B81B0907CE");

            entity.ToTable("EstadisticaJugador");

            entity.Property(e => e.JugadorId).HasColumnName("jugador_id");
            entity.Property(e => e.PartidoId).HasColumnName("partido_id");
            entity.Property(e => e.Aciertos1Punto).HasColumnName("aciertos_1_punto");
            entity.Property(e => e.Aciertos2Puntos).HasColumnName("aciertos_2_puntos");
            entity.Property(e => e.Aciertos3Puntos).HasColumnName("aciertos_3_puntos");
            entity.Property(e => e.Asistencias).HasColumnName("asistencias");
            entity.Property(e => e.EquipoId).HasColumnName("equipo_id");
            entity.Property(e => e.Faltas).HasColumnName("faltas");
            entity.Property(e => e.Intentos1Punto).HasColumnName("intentos_1_punto");
            entity.Property(e => e.Intentos2Puntos).HasColumnName("intentos_2_puntos");
            entity.Property(e => e.Intentos3Puntos).HasColumnName("intentos_3_puntos");
            entity.Property(e => e.Minutos).HasColumnName("minutos");
            entity.Property(e => e.Perdidas).HasColumnName("perdidas");
            entity.Property(e => e.Puntos).HasColumnName("puntos");
            entity.Property(e => e.Rebotes).HasColumnName("rebotes");
            entity.Property(e => e.RebotesDefensivos).HasColumnName("rebotes_defensivos");
            entity.Property(e => e.RebotesOfensivos).HasColumnName("rebotes_ofensivos");
            entity.Property(e => e.Robos).HasColumnName("robos");
            entity.Property(e => e.Tapones).HasColumnName("tapones");

            entity.HasOne(d => d.Equipo).WithMany(p => p.EstadisticaJugadors)
                .HasForeignKey(d => d.EquipoId)
                .HasConstraintName("FK__Estadisti__equip__1ED998B2");

            entity.HasOne(d => d.Jugador).WithMany(p => p.EstadisticaJugadors)
                .HasForeignKey(d => d.JugadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Estadisti__jugad__1CF15040");

            entity.HasOne(d => d.Partido).WithMany(p => p.EstadisticaJugadors)
                .HasForeignKey(d => d.PartidoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Estadisti__parti__1DE57479");
        });

        modelBuilder.Entity<EstadisticaPartido>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Estadist__3213E83F15502E78");

            entity.ToTable("EstadisticaPartido");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Aciertos1Punto).HasColumnName("aciertos_1_punto");
            entity.Property(e => e.Aciertos2Puntos).HasColumnName("aciertos_2_puntos");
            entity.Property(e => e.Aciertos3Puntos).HasColumnName("aciertos_3_puntos");
            entity.Property(e => e.Asistencias).HasColumnName("asistencias");
            entity.Property(e => e.EquipoId).HasColumnName("equipo_id");
            entity.Property(e => e.Faltas).HasColumnName("faltas");
            entity.Property(e => e.Intentos1Punto).HasColumnName("intentos_1_punto");
            entity.Property(e => e.Intentos2Puntos).HasColumnName("intentos_2_puntos");
            entity.Property(e => e.Intentos3Puntos).HasColumnName("intentos_3_puntos");
            entity.Property(e => e.PartidoId).HasColumnName("partido_id");
            entity.Property(e => e.Perdidas).HasColumnName("perdidas");
            entity.Property(e => e.Puntos).HasColumnName("puntos");
            entity.Property(e => e.Rebotes).HasColumnName("rebotes");
            entity.Property(e => e.RebotesDefensivos).HasColumnName("rebotes_defensivos");
            entity.Property(e => e.RebotesOfensivos).HasColumnName("rebotes_ofensivos");
            entity.Property(e => e.Robos).HasColumnName("robos");
            entity.Property(e => e.Tapones).HasColumnName("tapones");

            entity.HasOne(d => d.Equipo).WithMany(p => p.EstadisticaPartidos)
                .HasForeignKey(d => d.EquipoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Estadisti__equip__182C9B23");

            entity.HasOne(d => d.Partido).WithMany(p => p.EstadisticaPartidos)
                .HasForeignKey(d => d.PartidoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Estadisti__parti__173876EA");
        });

        modelBuilder.Entity<Jugador>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Jugador__3213E83F07F6335A");

            entity.ToTable("Jugador");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Activo).HasColumnName("activo");
            entity.Property(e => e.Altura)
                .HasColumnType("decimal(18, 0)")
                .HasColumnName("altura");
            entity.Property(e => e.Equipo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("equipo");
            entity.Property(e => e.EquipoId).HasColumnName("equipo_id");
            entity.Property(e => e.Foto)
                .IsUnicode(false)
                .HasColumnName("foto");
            entity.Property(e => e.IdUsuario)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasColumnName("id_usuario");
            entity.Property(e => e.Nombre)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("nombre");
            entity.Property(e => e.Numero).HasColumnName("numero");
            entity.Property(e => e.Peso)
                .HasColumnType("decimal(18, 0)")
                .HasColumnName("peso");
            entity.Property(e => e.Posicion)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("posicion");

            entity.HasOne(d => d.EquipoNavigation).WithMany(p => p.Jugadors)
                .HasForeignKey(d => d.EquipoId)
                .HasConstraintName("FK__Jugador__equipo___09DE7BCC");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Jugadors)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Jugador__id_usua__0AD2A005");
        });

        modelBuilder.Entity<Partido>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Partido__3213E83F0EA330E9");

            entity.ToTable("Partido");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Activo).HasColumnName("activo");
            entity.Property(e => e.EquipoLocal).HasColumnName("equipo_local");
            entity.Property(e => e.EquipoVisitante).HasColumnName("equipo_visitante");
            entity.Property(e => e.Fecha)
                .HasColumnType("datetime")
                .HasColumnName("fecha");
            entity.Property(e => e.Ganador).HasColumnName("ganador");
            entity.Property(e => e.IdUsuario)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasColumnName("id_usuario");
            entity.Property(e => e.Lugar)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("lugar");

            entity.HasOne(d => d.EquipoLocalNavigation).WithMany(p => p.PartidoEquipoLocalNavigations)
                .HasForeignKey(d => d.EquipoLocal)
                .HasConstraintName("FK__Partido__equipo___108B795B");

            entity.HasOne(d => d.EquipoVisitanteNavigation).WithMany(p => p.PartidoEquipoVisitanteNavigations)
                .HasForeignKey(d => d.EquipoVisitante)
                .HasConstraintName("FK__Partido__equipo___117F9D94");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Partidos)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Partido__id_usua__1273C1CD");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Usuario1).HasName("PK__Usuario__9AFF8FC77F60ED59");

            entity.ToTable("Usuario");

            entity.Property(e => e.Usuario1)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasColumnName("usuario");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.Pass)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("pass");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
