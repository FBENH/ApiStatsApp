using apiBask.Models;
using apiBask.Services;

namespace apiBask.Tests;

public class TablaLigaTests
{
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public void Todos_contra_todos_cruza_a_cada_par_una_vez_y_nadie_juega_dos_veces_por_fecha(int cantidad)
    {
        var equipos = Enumerable.Range(1, cantidad).ToList();

        var cruces = TablaLiga.TodosContraTodos(equipos, idaYVuelta: false);

        Assert.Equal(cantidad * (cantidad - 1) / 2, cruces.Count);
        Assert.Equal(cruces.Count, cruces.Select(c => (Math.Min(c.LocalId, c.VisitanteId), Math.Max(c.LocalId, c.VisitanteId))).Distinct().Count());
        foreach (var fecha in cruces.GroupBy(c => c.Jornada))
        {
            var juegan = fecha.SelectMany(c => new[] { c.LocalId, c.VisitanteId }).ToList();
            Assert.Equal(juegan.Count, juegan.Distinct().Count());
        }
    }

    [Fact]
    public void Ida_y_vuelta_invierte_la_localia()
    {
        var cruces = TablaLiga.TodosContraTodos([1, 2, 3, 4], idaYVuelta: true);

        Assert.Equal(12, cruces.Count);
        Assert.Equal(6, cruces.Max(c => c.Jornada));
        Assert.All(cruces, c => Assert.Contains(cruces, v => v.LocalId == c.VisitanteId && v.VisitanteId == c.LocalId));
    }

    [Fact]
    public void Posiciones_ordena_por_puntos_y_diferencia()
    {
        var resultados = new[]
        {
            new TablaLiga.Resultado(1, null, 1, 2, 80, 70),
            new TablaLiga.Resultado(2, null, 3, 1, 60, 75),
            new TablaLiga.Resultado(3, null, 2, 3, 90, 60),
        };

        var tabla = TablaLiga.Posiciones([1, 2, 3], resultados);

        Assert.Equal([1, 2, 3], tabla.Select(f => f.EquipoId));
        Assert.Equal(4, tabla[0].PuntosTabla); // 2 ganados
        Assert.Equal(25, tabla[0].Diferencia);
        Assert.Equal(3, tabla[1].PuntosTabla); // 1 ganado, 1 perdido
    }

    [Fact]
    public void La_valoracion_resta_tiros_fallados_y_perdidas()
    {
        var e = new EstadisticaJugador
        {
            Puntos = 20, Rebotes = 8, Asistencias = 5, Robos = 2, Tapones = 1, Perdidas = 3,
            Intentos2Puntos = 10, Aciertos2Puntos = 6, Intentos3Puntos = 4, Aciertos3Puntos = 2, Intentos1Punto = 4, Aciertos1Punto = 2,
        };

        Assert.Equal(20 + 8 + 5 + 2 + 1 - 4 - 2 - 2 - 3, TablaLiga.Valoracion(e));
    }

    [Fact]
    public void La_semana_va_de_lunes_a_domingo()
    {
        var (desde, hasta) = TablaLiga.Semana(new DateTime(2026, 10, 18)); // domingo

        Assert.Equal(new DateTime(2026, 10, 12), desde);
        Assert.Equal(new DateTime(2026, 10, 19), hasta);
    }
}
