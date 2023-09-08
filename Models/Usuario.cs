using System;
using System.Collections.Generic;

namespace apiBask.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string Usuario1 { get; set; } = null!;

    public string Pass { get; set; } = null!;

    public string? Email { get; set; }

    public virtual ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();

    public virtual ICollection<Jugador> Jugadors { get; set; } = new List<Jugador>();

    public virtual ICollection<Partido> Partidos { get; set; } = new List<Partido>();
}
