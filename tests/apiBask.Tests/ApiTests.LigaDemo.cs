using Microsoft.AspNetCore.Hosting;

namespace apiBask.Tests;

// La liga de demo se crea sola al arrancar y va jugando su fixture sin que nadie anote.
public partial class ApiTests
{
    [Fact]
    public async Task La_liga_de_demo_se_crea_y_juega_sus_partidos_sola()
    {
        await using var conDemo = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Demo:Activa", "true");
            b.UseSetting("Demo:Slug", "demo-de-prueba");
            b.UseSetting("Demo:Usuario", "demo-prueba");
            b.UseSetting("Demo:SegundosEntreJugadas", "0");
            b.UseSetting("Demo:MinutosEntrePartidos", "0");
        });
        var cliente = conDemo.CreateClient();

        var finalizados = 0;
        for (var intento = 0; intento < 60 && finalizados == 0; intento++)
        {
            await Task.Delay(1000, TestContext.Current.CancellationToken);
            var respuesta = await cliente.GetAsync("/publico/ligas/demo-de-prueba/partidos", TestContext.Current.CancellationToken);
            if (!respuesta.IsSuccessStatusCode) continue;
            var partidos = (await LeerAsync(respuesta)).Data.EnumerateArray().ToList();
            Assert.Equal(30, partidos.Count); // 6 equipos, ida y vuelta.
            finalizados = partidos.Count(p => p.GetProperty("estado").GetString() == "Finalizado");
        }
        Assert.True(finalizados > 0, "La demo no terminó ningún partido.");

        var posiciones = (await LeerAsync(await cliente.GetAsync("/publico/ligas/demo-de-prueba/posiciones", TestContext.Current.CancellationToken))).Data;
        var filas = posiciones[0].GetProperty("filas").EnumerateArray().ToList();
        Assert.Equal(6, filas.Count);
        Assert.Equal(2, filas.Sum(f => f.GetProperty("jugados").GetInt32()));
    }
}
