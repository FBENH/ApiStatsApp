namespace apiBask.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Pass { get; set; } = null!;
    public string? Email { get; set; }

    public List<Equipo> Equipos { get; set; } = [];
    public List<Jugador> Jugadores { get; set; } = [];
    public List<Partido> Partidos { get; set; } = [];
}

public class Equipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Ciudad { get; set; }
    public string? Escudo { get; set; }
    public string? Entrenador { get; set; }
    public int UsuarioId { get; set; }
    // Baja lógica: un equipo con partidos o jugadores no se borra, se desactiva.
    public bool Activo { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public List<Jugador> Jugadores { get; set; } = [];
    public List<EstadisticaPartido> EstadisticasPartido { get; set; } = [];
}

public class Jugador
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Posicion { get; set; }
    public int Numero { get; set; }
    public int? EquipoId { get; set; }
    public string? Foto { get; set; }
    // Centímetros y kilos.
    public decimal? Altura { get; set; }
    public decimal? Peso { get; set; }
    public int UsuarioId { get; set; }
    public bool Activo { get; set; }

    public Equipo? Equipo { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public List<EstadisticaJugador> Estadisticas { get; set; } = [];
}

public class Partido
{
    public int Id { get; set; }
    public int LocalId { get; set; }
    public int VisitanteId { get; set; }
    // Null si fue empate.
    public int? GanadorId { get; set; }
    public DateTime? Fecha { get; set; }
    public string? Lugar { get; set; }
    public int UsuarioId { get; set; }
    public bool Activo { get; set; }

    public Equipo Local { get; set; } = null!;
    public Equipo Visitante { get; set; } = null!;
    public Equipo? Ganador { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public List<EstadisticaJugador> EstadisticasJugador { get; set; } = [];
    public List<EstadisticaPartido> EstadisticasPartido { get; set; } = [];
}

// Contadores comunes a la planilla de un jugador y a la de un equipo en un partido.
public abstract class Estadistica
{
    public int PartidoId { get; set; }
    public int EquipoId { get; set; }
    public int Puntos { get; set; }
    public int Asistencias { get; set; }
    public int Rebotes { get; set; }
    public int RebotesOfensivos { get; set; }
    public int RebotesDefensivos { get; set; }
    public int Robos { get; set; }
    public int Tapones { get; set; }
    public int Intentos1Punto { get; set; }
    public int Aciertos1Punto { get; set; }
    public int Intentos2Puntos { get; set; }
    public int Aciertos2Puntos { get; set; }
    public int Intentos3Puntos { get; set; }
    public int Aciertos3Puntos { get; set; }
    public int Faltas { get; set; }
    public int Perdidas { get; set; }

    public Partido Partido { get; set; } = null!;
    public Equipo Equipo { get; set; } = null!;
}

public class EstadisticaJugador : Estadistica
{
    public int JugadorId { get; set; }
    public TimeSpan Minutos { get; set; }

    public Jugador Jugador { get; set; } = null!;
}

// Una fila por equipo y partido. Los puntos del equipo se guardan aparte porque se pueden corregir a mano.
public class EstadisticaPartido : Estadistica;
