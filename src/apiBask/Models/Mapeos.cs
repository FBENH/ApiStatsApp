using apiBask.Models.Request;
using apiBask.Models.Response;

namespace apiBask.Models
{
    // Copias campo a campo entre entidades, requests y responses, en un solo lugar.
    // Los contadores que el request manda en null se guardan como 0.
    public static class Mapeos
    {
        public static void CopiarDe(this EstadisticaJugador s, StatJugadorRequest r)
        {
            s.JugadorId = r.JugadorId;
            s.EquipoId = r.EquipoId!.Value;
            s.Puntos = r.Puntos ?? 0;
            s.Asistencias = r.Asistencias ?? 0;
            s.Rebotes = r.Rebotes ?? 0;
            s.RebotesOfensivos = r.RebotesOfensivos ?? 0;
            s.RebotesDefensivos = r.RebotesDefensivos ?? 0;
            s.Robos = r.Robos ?? 0;
            s.Tapones = r.Tapones ?? 0;
            s.Intentos2Puntos = r.Intentos2Puntos ?? 0;
            s.Aciertos2Puntos = r.Aciertos2Puntos ?? 0;
            s.Intentos3Puntos = r.Intentos3Puntos ?? 0;
            s.Aciertos3Puntos = r.Aciertos3Puntos ?? 0;
            s.Intentos1Punto = r.Intentos1Punto ?? 0;
            s.Aciertos1Punto = r.Aciertos1Punto ?? 0;
            s.Faltas = r.Faltas ?? 0;
            s.Perdidas = r.Perdidas;
            s.Minutos = TimeSpan.FromMilliseconds(r.Minutos);
        }

        public static void CopiarDe(this EstadisticaPartido s, StatPartidoRequest r)
        {
            s.EquipoId = r.EquipoId;
            s.Puntos = r.Puntos ?? 0;
            s.Asistencias = r.Asistencias ?? 0;
            s.Rebotes = r.Rebotes ?? 0;
            s.RebotesOfensivos = r.RebotesOfensivos ?? 0;
            s.RebotesDefensivos = r.RebotesDefensivos ?? 0;
            s.Robos = r.Robos ?? 0;
            s.Tapones = r.Tapones ?? 0;
            s.Intentos2Puntos = r.Intentos2Puntos ?? 0;
            s.Aciertos2Puntos = r.Aciertos2Puntos ?? 0;
            s.Intentos3Puntos = r.Intentos3Puntos ?? 0;
            s.Aciertos3Puntos = r.Aciertos3Puntos ?? 0;
            s.Intentos1Punto = r.Intentos1Punto ?? 0;
            s.Aciertos1Punto = r.Aciertos1Punto ?? 0;
            s.Faltas = r.Faltas ?? 0;
            s.Perdidas = r.Perdidas;
        }

        public static EstadisticaJResponse ToResponse(this EstadisticaJugador e) => new()
        {
            JugadorId = e.JugadorId,
            PartidoId = e.PartidoId,
            EquipoId = e.EquipoId,
            Puntos = e.Puntos,
            Asistencias = e.Asistencias,
            Rebotes = e.Rebotes,
            RebotesOfensivos = e.RebotesOfensivos,
            RebotesDefensivos = e.RebotesDefensivos,
            Robos = e.Robos,
            Tapones = e.Tapones,
            Intentos2Puntos = e.Intentos2Puntos,
            Aciertos2Puntos = e.Aciertos2Puntos,
            Intentos3Puntos = e.Intentos3Puntos,
            Aciertos3Puntos = e.Aciertos3Puntos,
            Intentos1Punto = e.Intentos1Punto,
            Aciertos1Punto = e.Aciertos1Punto,
            Faltas = e.Faltas,
            Minutos = e.Minutos,
            Perdidas = e.Perdidas,
        };

        public static EstadisticaPResponse ToResponse(this EstadisticaPartido e) => new()
        {
            EquipoId = e.EquipoId,
            PartidoId = e.PartidoId,
            Puntos = e.Puntos,
            Asistencias = e.Asistencias,
            Rebotes = e.Rebotes,
            RebotesOfensivos = e.RebotesOfensivos,
            RebotesDefensivos = e.RebotesDefensivos,
            Robos = e.Robos,
            Tapones = e.Tapones,
            Intentos2Puntos = e.Intentos2Puntos,
            Aciertos2Puntos = e.Aciertos2Puntos,
            Intentos3Puntos = e.Intentos3Puntos,
            Aciertos3Puntos = e.Aciertos3Puntos,
            Intentos1Punto = e.Intentos1Punto,
            Aciertos1Punto = e.Aciertos1Punto,
            Faltas = e.Faltas,
            Perdidas = e.Perdidas,
        };

        public static JugadorResponse ToResponse(this Jugador j) => new()
        {
            Id = j.Id,
            Nombre = j.Nombre,
            Posicion = j.Posicion,
            EquipoId = j.EquipoId,
            Foto = j.Foto,
            Altura = j.Altura,
            Peso = j.Peso,
            Numero = j.Numero,
        };

        public static JugadorListado ToListado(this Jugador j) =>
            new(j.Id, j.Nombre, j.Posicion, j.Numero, j.EquipoId, j.Equipo?.Nombre, j.Foto, j.Altura, j.Peso, j.Activo);

        public static EquipoResponse ToResponse(this Equipo e) => new()
        {
            Id = e.Id,
            Nombre = e.Nombre,
            Ciudad = e.Ciudad,
            Escudo = e.Escudo,
            Entrenador = e.Entrenador,
        };
    }
}
