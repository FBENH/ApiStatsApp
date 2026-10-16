using apiBask.Models;

namespace apiBask.Services
{
    // Calcula la planilla de un partido a partir de su registro de jugadas.
    // El front tiene la misma lógica en core/registro.ts; los dos se prueban con los mismos casos.
    public static class Planilla
    {
        private const int DuracionProrrogaMs = 5 * 60 * 1000;

        public record Resultado(List<EstadisticaJugador> Jugadores, List<EstadisticaPartido> Equipos);

        /// <param name="fin">Cuarto y reloj en que terminó el partido, para cerrar los minutos de los que estaban en cancha.</param>
        public static Resultado Calcular(Partido partido, IEnumerable<Jugada> jugadas, (int Cuarto, int RelojMs)? fin = null)
        {
            var equipos = new Dictionary<int, EstadisticaPartido>
            {
                [partido.LocalId] = new() { PartidoId = partido.Id, EquipoId = partido.LocalId },
                [partido.VisitanteId] = new() { PartidoId = partido.Id, EquipoId = partido.VisitanteId },
            };
            var jugadores = new Dictionary<int, EstadisticaJugador>();
            // Momento del partido (ms desde el inicio) en que entró cada jugador que está en cancha.
            var enCancha = new Dictionary<int, long?>();
            var ordenadas = jugadas.OrderBy(j => j.Numero).ToList();

            EstadisticaJugador Fila(int jugadorId, int equipoId) =>
                jugadores.TryGetValue(jugadorId, out var fila) ? fila
                    : jugadores[jugadorId] = new() { PartidoId = partido.Id, JugadorId = jugadorId, EquipoId = equipoId };

            void Salir(int jugadorId, long? momento)
            {
                if (!enCancha.Remove(jugadorId, out var entrada)) return;
                if (entrada != null && momento != null && momento > entrada)
                    jugadores[jugadorId].Minutos += TimeSpan.FromMilliseconds(momento.Value - entrada.Value);
            }

            foreach (var j in ordenadas)
            {
                var momento = Momento(partido, j.Cuarto, j.RelojMs);
                if (j.Tipo == TipoJugada.Cambio)
                {
                    if (j.SaleId is int sale) Salir(sale, momento);
                    if (j.JugadorId is int entra)
                    {
                        Fila(entra, j.EquipoId);
                        enCancha[entra] = momento;
                    }
                    continue;
                }
                if (!equipos.TryGetValue(j.EquipoId, out var equipo)) continue;
                Sumar(equipo, j);
                if (j.JugadorId is int id) Sumar(Fila(id, j.EquipoId), j);
            }

            var final = fin ?? (ordenadas.Count == 0 ? (1, 0) : (ordenadas.Max(j => j.Cuarto), 0));
            var momentoFinal = Momento(partido, final.Cuarto, final.RelojMs);
            foreach (var id in enCancha.Keys.ToList()) Salir(id, momentoFinal);

            return new(jugadores.Values.ToList(), equipos.Values.ToList());
        }

        // Milisegundos de juego desde el inicio. Los cuartos 5 en adelante son prórrogas de 5 minutos.
        private static long? Momento(Partido partido, int cuarto, int? relojMs)
        {
            if (relojMs == null) return null;
            long Duracion(int c) => c <= 4 ? partido.DuracionCuartoMin * 60_000L : DuracionProrrogaMs;
            long previos = 0;
            for (var c = 1; c < cuarto; c++) previos += Duracion(c);
            return previos + Duracion(cuarto) - relojMs.Value;
        }

        private static void Sumar(Estadistica e, Jugada j)
        {
            switch (j.Tipo)
            {
                case TipoJugada.Tiro when j.Valor == 3:
                    e.Intentos3Puntos++;
                    if (j.Acierto == true) { e.Aciertos3Puntos++; e.Puntos += 3; }
                    break;
                case TipoJugada.Tiro:
                    e.Intentos2Puntos++;
                    if (j.Acierto == true) { e.Aciertos2Puntos++; e.Puntos += 2; }
                    break;
                case TipoJugada.Libre:
                    e.Intentos1Punto++;
                    if (j.Acierto == true) { e.Aciertos1Punto++; e.Puntos += 1; }
                    break;
                case TipoJugada.Rebote:
                    e.Rebotes++;
                    if (j.Ofensivo == true) e.RebotesOfensivos++; else e.RebotesDefensivos++;
                    break;
                case TipoJugada.Asistencia: e.Asistencias++; break;
                case TipoJugada.Robo: e.Robos++; break;
                case TipoJugada.Tapon: e.Tapones++; break;
                case TipoJugada.Perdida: e.Perdidas++; break;
                case TipoJugada.Falta: e.Faltas++; break;
            }
        }
    }
}
