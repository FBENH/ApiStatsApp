using System;
using System.Collections.Generic;

namespace apiBask.Models;

public partial class Equipo
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Ciudad { get; set; }

    public string? Escudo { get; set; }

    public string? Entrenador { get; set; }

    public string IdUsuario { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual ICollection<EstadisticaJugador> EstadisticaJugadors { get; set; } = new List<EstadisticaJugador>();

    public virtual ICollection<EstadisticaPartido> EstadisticaPartidos { get; set; } = new List<EstadisticaPartido>();

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<Jugador> Jugadors { get; set; } = new List<Jugador>();

    public virtual ICollection<Partido> PartidoEquipoLocalNavigations { get; set; } = new List<Partido>();

    public virtual ICollection<Partido> PartidoEquipoVisitanteNavigations { get; set; } = new List<Partido>();
}
