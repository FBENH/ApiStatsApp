using System;
using System.Collections.Generic;

namespace apiBask.Models;

public partial class EstadisticaJugador
{
    public int JugadorId { get; set; }

    public int PartidoId { get; set; }

    public int? EquipoId { get; set; }

    public int? Puntos { get; set; }

    public int? Asistencias { get; set; }

    public int? Rebotes { get; set; }

    public int? RebotesOfensivos { get; set; }

    public int? RebotesDefensivos { get; set; }

    public int? Robos { get; set; }

    public int? Tapones { get; set; }

    public int? Intentos2Puntos { get; set; }

    public int? Aciertos2Puntos { get; set; }

    public int? Intentos3Puntos { get; set; }

    public int? Aciertos3Puntos { get; set; }

    public int? Intentos1Punto { get; set; }

    public int? Aciertos1Punto { get; set; }

    public int? Faltas { get; set; }

    public TimeSpan? Minutos { get; set; }

    public int? Perdidas { get; set; }

    public virtual Equipo? Equipo { get; set; }

    public virtual Jugador Jugador { get; set; } = null!;

    public virtual Partido Partido { get; set; } = null!;
}
