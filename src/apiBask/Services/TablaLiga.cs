using apiBask.Models;

namespace apiBask.Services
{
    // Cálculos de una temporada a partir de los partidos terminados: posiciones, líderes y MVP.
    // Trabaja en memoria: una temporada de una liga amateur tiene pocos cientos de partidos.
    public static class TablaLiga
    {
        public record Resultado(int PartidoId, DateTime? Fecha, int LocalId, int VisitanteId, int PuntosLocal, int PuntosVisitante);

        public record Fila(int EquipoId, int Jugados, int Ganados, int Perdidos, int PuntosAFavor, int PuntosEnContra)
        {
            public int Diferencia => PuntosAFavor - PuntosEnContra;
            // Como en FIBA: 2 puntos por partido ganado y 1 por perdido.
            public int PuntosTabla => Ganados * 2 + Perdidos;
            public double Porcentaje => Jugados == 0 ? 0 : Math.Round((double)Ganados / Jugados, 3);
        }

        public static List<Fila> Posiciones(IEnumerable<int> equipos, IEnumerable<Resultado> resultados)
        {
            var filas = equipos.ToDictionary(id => id, id => new Fila(id, 0, 0, 0, 0, 0));
            foreach (var r in resultados)
            {
                Sumar(r.LocalId, r.PuntosLocal, r.PuntosVisitante);
                Sumar(r.VisitanteId, r.PuntosVisitante, r.PuntosLocal);
            }
            return filas.Values
                .OrderByDescending(f => f.PuntosTabla)
                .ThenByDescending(f => f.Porcentaje)
                .ThenByDescending(f => f.Diferencia)
                .ThenByDescending(f => f.PuntosAFavor)
                .ToList();

            void Sumar(int equipoId, int aFavor, int enContra)
            {
                // Un equipo que ya no está inscripto no entra en la tabla.
                if (!filas.TryGetValue(equipoId, out var f)) return;
                filas[equipoId] = f with
                {
                    Jugados = f.Jugados + 1,
                    Ganados = f.Ganados + (aFavor > enContra ? 1 : 0),
                    Perdidos = f.Perdidos + (aFavor < enContra ? 1 : 0),
                    PuntosAFavor = f.PuntosAFavor + aFavor,
                    PuntosEnContra = f.PuntosEnContra + enContra,
                };
            }
        }

        // Valoración clásica: lo positivo menos los tiros fallados y las pérdidas.
        public static int Valoracion(Estadistica e) =>
            e.Puntos + e.Rebotes + e.Asistencias + e.Robos + e.Tapones
            - (e.Intentos1Punto - e.Aciertos1Punto)
            - (e.Intentos2Puntos - e.Aciertos2Puntos)
            - (e.Intentos3Puntos - e.Aciertos3Puntos)
            - e.Perdidas;

        public record Categoria(string Clave, string Nombre, Func<EstadisticaJugador, int> Valor);

        public static readonly Categoria[] Categorias =
        [
            new("puntos", "Puntos", e => e.Puntos),
            new("rebotes", "Rebotes", e => e.Rebotes),
            new("asistencias", "Asistencias", e => e.Asistencias),
            new("robos", "Robos", e => e.Robos),
            new("tapones", "Tapones", e => e.Tapones),
            new("triples", "Triples", e => e.Aciertos3Puntos),
            new("valoracion", "Valoración", Valoracion),
        ];

        public record Lider(int JugadorId, int Partidos, double Promedio, int Total);

        // Promedio por partido de cada jugador con al menos `minPartidos` jugados, de mayor a menor.
        public static List<Lider> Lideres(IEnumerable<EstadisticaJugador> estadisticas, Func<EstadisticaJugador, int> valor,
            int minPartidos, int cantidad) =>
            estadisticas
                .GroupBy(e => e.JugadorId)
                .Where(g => g.Count() >= minPartidos)
                .Select(g => new Lider(g.Key, g.Count(), Math.Round(g.Average(valor), 1), g.Sum(valor)))
                .OrderByDescending(l => l.Promedio)
                .ThenByDescending(l => l.Total)
                .Take(cantidad)
                .ToList();

        // Por defecto, para aparecer en los líderes hay que haber jugado la mitad de los partidos del que más jugó.
        public static int MinimoDePartidos(IEnumerable<EstadisticaJugador> estadisticas)
        {
            var maximo = estadisticas.GroupBy(e => e.JugadorId).Select(g => g.Count()).DefaultIfEmpty(0).Max();
            return Math.Max(1, (int)Math.Ceiling(maximo / 2.0));
        }

        // El que más valoración sumó en los partidos de la lista (de una semana, un mes...).
        public static Lider? MasValioso(IEnumerable<EstadisticaJugador> estadisticas) =>
            estadisticas
                .GroupBy(e => e.JugadorId)
                .Select(g => new Lider(g.Key, g.Count(), Math.Round(g.Average(Valoracion), 1), g.Sum(Valoracion)))
                .OrderByDescending(l => l.Total)
                .ThenByDescending(l => l.Promedio)
                .FirstOrDefault();

        // Semana de lunes a domingo que contiene la fecha.
        public static (DateTime Desde, DateTime Hasta) Semana(DateTime fecha)
        {
            var desde = fecha.Date.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));
            return (desde, desde.AddDays(7));
        }

        public static (DateTime Desde, DateTime Hasta) Mes(DateTime fecha)
        {
            var desde = new DateTime(fecha.Year, fecha.Month, 1);
            return (desde, desde.AddMonths(1));
        }

        public record Cruce(int Jornada, int LocalId, int VisitanteId);

        // Todos contra todos con el método del círculo. Con un número impar de equipos, uno descansa en cada fecha.
        public static List<Cruce> TodosContraTodos(IReadOnlyList<int> equipos, bool idaYVuelta)
        {
            var lista = equipos.Select(e => (int?)e).ToList();
            if (lista.Count % 2 == 1) lista.Add(null);
            var n = lista.Count;
            var cruces = new List<Cruce>();
            for (var fecha = 0; fecha < n - 1; fecha++)
            {
                for (var i = 0; i < n / 2; i++)
                {
                    var (a, b) = (lista[i], lista[n - 1 - i]);
                    if (a is null || b is null) continue;
                    // Se alterna la localía para que nadie juegue siempre de local.
                    var (local, visitante) = (fecha + i) % 2 == 0 ? (a.Value, b.Value) : (b.Value, a.Value);
                    cruces.Add(new(fecha + 1, local, visitante));
                }
                // Rota todos menos el primero.
                var ultimo = lista[n - 1];
                lista.RemoveAt(n - 1);
                lista.Insert(1, ultimo);
            }
            if (idaYVuelta)
            {
                var ida = cruces.Count == 0 ? 0 : cruces.Max(c => c.Jornada);
                cruces.AddRange(cruces.ToList().Select(c => new Cruce(c.Jornada + ida, c.VisitanteId, c.LocalId)));
            }
            return cruces;
        }
    }
}
