using apiBask.Models.Request;
using apiBask.Models.Response;

namespace apiBask.Models
{
    // Copias campo a campo entre entidades, requests y responses, en un solo lugar.
    public static class Mapeos
    {
        public static void CopiarDe(this EstadisticaJugador s, StatJugadorRequest r)
        {
            s.JugadorId = r.JugadorId;
            s.EquipoId = r.EquipoId;
            s.Puntos = r.Puntos;
            s.Asistencias = r.Asistencias;
            s.Rebotes = r.Rebotes;
            s.RebotesOfensivos = r.RebotesOfensivos;
            s.RebotesDefensivos = r.RebotesDefensivos;
            s.Robos = r.Robos;
            s.Tapones = r.Tapones;
            s.Intentos2Puntos = r.Intentos2Puntos;
            s.Aciertos2Puntos = r.Aciertos2Puntos;
            s.Intentos3Puntos = r.Intentos3Puntos;
            s.Aciertos3Puntos = r.Aciertos3Puntos;
            s.Intentos1Punto = r.Intentos1Punto;
            s.Aciertos1Punto = r.Aciertos1Punto;
            s.Faltas = r.Faltas;
            s.Perdidas = r.Perdidas;
            s.Minutos = TimeSpan.FromMilliseconds(r.Minutos);
        }

        public static void CopiarDe(this EstadisticaPartido s, StatPartidoRequest r)
        {
            s.EquipoId = r.EquipoId;
            s.Puntos = r.Puntos;
            s.Asistencias = r.Asistencias;
            s.Rebotes = r.Rebotes;
            s.RebotesOfensivos = r.RebotesOfensivos;
            s.RebotesDefensivos = r.RebotesDefensivos;
            s.Robos = r.Robos;
            s.Tapones = r.Tapones;
            s.Intentos2Puntos = r.Intentos2Puntos;
            s.Aciertos2Puntos = r.Aciertos2Puntos;
            s.Intentos3Puntos = r.Intentos3Puntos;
            s.Aciertos3Puntos = r.Aciertos3Puntos;
            s.Intentos1Punto = r.Intentos1Punto;
            s.Aciertos1Punto = r.Aciertos1Punto;
            s.Faltas = r.Faltas;
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
