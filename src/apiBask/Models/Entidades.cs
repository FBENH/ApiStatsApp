using System.Text.Json.Serialization;

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
    public List<Liga> Ligas { get; set; } = [];
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
    public EstadoPartido Estado { get; set; }
    public int DuracionCuartoMin { get; set; } = 10;
    // Partidos de una liga: la temporada y la fecha del fixture (1, 2, 3...).
    public int? TemporadaId { get; set; }
    public int? Jornada { get; set; }

    public Equipo Local { get; set; } = null!;
    public Equipo Visitante { get; set; } = null!;
    public Equipo? Ganador { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public List<EstadisticaJugador> EstadisticasJugador { get; set; } = [];
    public List<EstadisticaPartido> EstadisticasPartido { get; set; } = [];
    public List<Jugada> Jugadas { get; set; } = [];
    public Temporada? Temporada { get; set; }
}

// Finalizado es 0 para que los partidos cargados de una vez (Partido/Crear) queden terminados.
// Programado es un partido del fixture de una liga que todavía no se empezó a anotar.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EstadoPartido { Finalizado, EnCurso, Programado }

// Una liga tiene una página pública (en otro dominio) que lee la API de /publico/ligas/{slug}.
public class Liga
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    // Identificador para la URL pública, por ejemplo "liga-del-barrio".
    public string Slug { get; set; } = null!;
    public string? Descripcion { get; set; }
    public int UsuarioId { get; set; }
    // La liga de demo se rearma cada tantas horas a partir de esta fecha.
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;

    public Usuario Usuario { get; set; } = null!;
    public List<Temporada> Temporadas { get; set; } = [];
}

public class Temporada
{
    public int Id { get; set; }
    public int LigaId { get; set; }
    public string Nombre { get; set; } = null!;

    public Liga Liga { get; set; } = null!;
    public List<Division> Divisiones { get; set; } = [];
    public List<Inscripcion> Inscripciones { get; set; } = [];
    public List<Partido> Partidos { get; set; } = [];
}

// Grupo o división dentro de una temporada. Una temporada sin divisiones tiene una sola tabla.
public class Division
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public string Nombre { get; set; } = null!;

    public Temporada Temporada { get; set; } = null!;
}

// Un equipo que juega una temporada, y en qué división.
public class Inscripcion
{
    public int TemporadaId { get; set; }
    public int EquipoId { get; set; }
    public int? DivisionId { get; set; }

    public Temporada Temporada { get; set; } = null!;
    public Equipo Equipo { get; set; } = null!;
    public Division? Division { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TipoJugada { Tiro, Libre, Rebote, Asistencia, Robo, Tapon, Perdida, Falta, Cambio, TiempoMuerto }

// Una jugada del registro del partido. La planilla se calcula a partir de estas filas.
public class Jugada
{
    // Lo genera el dispositivo que anota: si la misma jugada llega dos veces, no se duplica.
    public Guid Id { get; set; }
    public int PartidoId { get; set; }
    // Orden dentro del partido.
    public int Numero { get; set; }
    public int Cuarto { get; set; }
    // Tiempo que quedaba en el reloj del cuarto; null si no se lleva reloj.
    public int? RelojMs { get; set; }
    public TipoJugada Tipo { get; set; }
    public int EquipoId { get; set; }
    // Null en jugadas de equipo (por ejemplo, un rebote de equipo).
    // En un cambio es el que entra.
    public int? JugadorId { get; set; }
    // Solo en cambios: el que sale.
    public int? SaleId { get; set; }
    // Tiros y libres.
    public bool? Acierto { get; set; }
    // Tiros: 2 o 3.
    public int? Valor { get; set; }
    // Rebotes.
    public bool? Ofensivo { get; set; }
    // Lugar del tiro en la cancha, de 0 a 1.
    public double? X { get; set; }
    public double? Y { get; set; }
    // Por ejemplo, el tiro al que corresponde una asistencia.
    public Guid? RelacionadaId { get; set; }
    public DateTime CreadaEn { get; set; }

    public Partido Partido { get; set; } = null!;
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
