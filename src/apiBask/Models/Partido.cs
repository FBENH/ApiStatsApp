using System;
using System.Collections.Generic;

namespace apiBask.Models;

public partial class Partido
{
    public int Id { get; set; }

    public int? EquipoLocal { get; set; }

    public int? EquipoVisitante { get; set; }

    public int? Ganador { get; set; }

    public DateTime? Fecha { get; set; }

    public string? Lugar { get; set; }

    public string IdUsuario { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual Equipo? EquipoLocalNavigation { get; set; }

    public virtual Equipo? EquipoVisitanteNavigation { get; set; }

    public virtual ICollection<EstadisticaJugador> EstadisticaJugadors { get; set; } = new List<EstadisticaJugador>();

    public virtual ICollection<EstadisticaPartido> EstadisticaPartidos { get; set; } = new List<EstadisticaPartido>();

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
