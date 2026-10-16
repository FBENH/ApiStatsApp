using apiBask.Models;
using apiBask.Services;

namespace apiBask.Tests;

public class SimuladorTests
{
    [Fact]
    public void Un_partido_simulado_es_valido_y_no_termina_empatado()
    {
        var partido = new Partido { Id = 1, LocalId = 10, VisitanteId = 20, DuracionCuartoMin = 10 };
        var locales = Enumerable.Range(100, 8).ToList();
        var visitantes = Enumerable.Range(200, 6).ToList();

        for (var semilla = 0; semilla < 20; semilla++)
        {
            var jugadas = Simulador.Generar(partido, locales, visitantes, new Random(semilla));

            Assert.Equal(Enumerable.Range(1, jugadas.Count), jugadas.Select(j => j.Numero));
            Assert.All(jugadas.Where(j => j.Tipo == TipoJugada.Tiro), t =>
            {
                Assert.InRange(t.X!.Value, 0, 1);
                Assert.InRange(t.Y!.Value, 0, 1);
                Assert.Equal(Simulador.ValorDeTiro(t.X.Value, t.Y.Value), t.Valor);
            });
            Assert.All(jugadas.Where(j => j.JugadorId != null), j =>
                Assert.Contains(j.JugadorId!.Value, j.EquipoId == 10 ? locales : visitantes));

            var planilla = Planilla.Calcular(partido, jugadas.Select(j => new Jugada
            {
                Id = j.Id, Numero = j.Numero, Cuarto = j.Cuarto, RelojMs = j.RelojMs, Tipo = j.Tipo, EquipoId = j.EquipoId,
                JugadorId = j.JugadorId, SaleId = j.SaleId, Acierto = j.Acierto, Valor = j.Valor, Ofensivo = j.Ofensivo,
            }), (jugadas[^1].Cuarto, 0));
            var puntos = planilla.Equipos.Select(e => e.Puntos).ToList();
            Assert.NotEqual(puntos[0], puntos[1]);
            Assert.InRange(puntos.Max(), 30, 160);
            // Nadie juega más que lo que duró el partido.
            Assert.All(planilla.Jugadores, e => Assert.True(e.Minutos <= TimeSpan.FromMinutes(40 + 5 * (jugadas[^1].Cuarto - 4))));
        }
    }
}
