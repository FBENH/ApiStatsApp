using apiBask.Models;
using apiBask.Models.Request;

namespace apiBask.Services
{
    // Inventa un partido creíble jugada a jugada, para la demo: tiros con ubicación, rebotes, asistencias,
    // robos, faltas con libres y cambios. Los porcentajes son los típicos de una liga amateur.
    public static class Simulador
    {
        // Misma cancha que el front (core/cancha.ts): 15 m de ancho y los primeros 10 m, en decímetros.
        private const double Ancho = 150, Largo = 100, AroX = 75, AroY = 15.75, RadioTriple = 67.5, EsquinaX = 9;
        private static readonly double EsquinaY = AroY + Math.Sqrt(RadioTriple * RadioTriple - (AroX - EsquinaX) * (AroX - EsquinaX));

        public static int ValorDeTiro(double x, double y)
        {
            var (px, py) = (x * Ancho, y * Largo);
            if (py < EsquinaY) return px < EsquinaX || px > Ancho - EsquinaX ? 3 : 2;
            return Math.Sqrt((px - AroX) * (px - AroX) + (py - AroY) * (py - AroY)) > RadioTriple ? 3 : 2;
        }

        public static List<JugadaDto> Generar(Partido partido, IReadOnlyList<int> locales, IReadOnlyList<int> visitantes, Random azar)
        {
            var jugadas = new List<JugadaDto>();
            var plantel = new Dictionary<int, IReadOnlyList<int>> { [partido.LocalId] = locales, [partido.VisitanteId] = visitantes };
            var enCancha = plantel.ToDictionary(p => p.Key, p => p.Value.Take(5).ToList());
            var duracion = partido.DuracionCuartoMin * 60_000;
            var cuarto = 1;
            var reloj = duracion;
            var puntos = plantel.Keys.ToDictionary(e => e, _ => 0);

            Guid Anotar(TipoJugada tipo, int equipoId, int? jugadorId, int? saleId = null, bool? acierto = null, int? valor = null,
                bool? ofensivo = null, double? x = null, double? y = null, Guid? relacionada = null)
            {
                var id = Guid.NewGuid();
                jugadas.Add(new JugadaDto(id, jugadas.Count + 1, cuarto, reloj, tipo, equipoId, jugadorId, saleId, acierto, valor, ofensivo,
                    x, y, relacionada));
                if (acierto == true) puntos[equipoId] += tipo == TipoJugada.Libre ? 1 : valor ?? 0;
                return id;
            }
            int Rival(int equipoId) => equipoId == partido.LocalId ? partido.VisitanteId : partido.LocalId;
            int Alguno(int equipoId, int? salvo = null)
            {
                var candidatos = enCancha[equipoId].Where(j => j != salvo).ToList();
                return candidatos.Count == 0 ? enCancha[equipoId][0] : candidatos[azar.Next(candidatos.Count)];
            }

            foreach (var (equipoId, titulares) in enCancha)
                foreach (var j in titulares) Anotar(TipoJugada.Cambio, equipoId, j);

            var ataca = partido.LocalId;
            var proximoCambio = duracion - 150_000;
            while (true)
            {
                reloj -= azar.Next(7_000, 22_000);
                if (reloj <= 0)
                {
                    // Termina el cuarto. Después del cuarto, si hay empate, se juegan prórrogas de 5 minutos.
                    if (cuarto >= 4 && (puntos[partido.LocalId] != puntos[partido.VisitanteId] || cuarto >= 7)) break;
                    cuarto++;
                    reloj = cuarto <= 4 ? duracion : 5 * 60_000;
                    proximoCambio = reloj - 150_000;
                    ataca = Rival(ataca);
                    continue;
                }
                if (reloj < proximoCambio)
                {
                    proximoCambio -= 150_000;
                    foreach (var equipoId in plantel.Keys)
                    {
                        var banco = plantel[equipoId].Except(enCancha[equipoId]).ToList();
                        if (banco.Count == 0) continue;
                        var entra = banco[azar.Next(banco.Count)];
                        var sale = Alguno(equipoId);
                        enCancha[equipoId][enCancha[equipoId].IndexOf(sale)] = entra;
                        Anotar(TipoJugada.Cambio, equipoId, entra, sale);
                    }
                }

                var defiende = Rival(ataca);
                var suerte = azar.NextDouble();
                if (suerte < 0.12)
                {
                    // Pérdida; la mitad de las veces es un robo del rival.
                    Guid? robo = azar.NextDouble() < 0.5 ? Anotar(TipoJugada.Robo, defiende, Alguno(defiende)) : null;
                    Anotar(TipoJugada.Perdida, ataca, Alguno(ataca), relacionada: robo);
                    ataca = defiende;
                }
                else if (suerte < 0.22)
                {
                    Anotar(TipoJugada.Falta, defiende, Alguno(defiende));
                    var tirador = Alguno(ataca);
                    Anotar(TipoJugada.Libre, ataca, tirador, acierto: azar.NextDouble() < 0.72);
                    var ultimo = azar.NextDouble() < 0.72;
                    Anotar(TipoJugada.Libre, ataca, tirador, acierto: ultimo);
                    ataca = ultimo ? defiende : Rebote(ataca, defiende);
                }
                else
                {
                    var (x, y) = LugarDeTiro(azar.NextDouble() < 0.35);
                    var valor = ValorDeTiro(x, y);
                    var tirador = Alguno(ataca);
                    var entra = azar.NextDouble() < (valor == 3 ? 0.34 : 0.5);
                    var tiro = Anotar(TipoJugada.Tiro, ataca, tirador, acierto: entra, valor: valor, x: x, y: y);
                    if (entra)
                    {
                        if (azar.NextDouble() < 0.58) Anotar(TipoJugada.Asistencia, ataca, Alguno(ataca, tirador), relacionada: tiro);
                        ataca = defiende;
                    }
                    else
                    {
                        if (valor == 2 && azar.NextDouble() < 0.08) Anotar(TipoJugada.Tapon, defiende, Alguno(defiende), relacionada: tiro);
                        ataca = Rebote(ataca, defiende);
                    }
                }
            }
            return jugadas;

            // Devuelve quién ataca después del rebote.
            int Rebote(int ataca, int defiende)
            {
                var ofensivo = azar.NextDouble() < 0.28;
                var equipoId = ofensivo ? ataca : defiende;
                Anotar(TipoJugada.Rebote, equipoId, Alguno(equipoId), ofensivo: ofensivo);
                return equipoId;
            }

            (double X, double Y) LugarDeTiro(bool triple)
            {
                // Ángulo desde la línea de fondo y distancia al aro, en decímetros.
                var angulo = Math.PI * (0.04 + 0.92 * azar.NextDouble());
                var distancia = triple ? RadioTriple + 3 + azar.NextDouble() * 12 : 4 + Math.Pow(azar.NextDouble(), 1.4) * 58;
                var px = Math.Clamp(AroX + distancia * Math.Cos(angulo), 2, Ancho - 2);
                var py = Math.Clamp(AroY + distancia * Math.Sin(angulo), 2, Largo - 2);
                return (Math.Round(px / Ancho, 4), Math.Round(py / Largo, 4));
            }
        }
    }
}
