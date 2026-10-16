using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace apiBask.Tests;

// La liga de demo juega su fixture sola, pero solo mientras alguien la está mirando.
public partial class ApiTests
{
    [Fact]
    public async Task La_liga_de_demo_juega_sola_solo_cuando_alguien_la_mira()
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

        // Sin visitas no hace nada: ni siquiera crea la liga.
        await Task.Delay(1500, TestContext.Current.CancellationToken);
        using (var scope = conDemo.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BasketContext>();
            Assert.False(await db.Ligas.AnyAsync(l => l.Slug == "demo-de-prueba", TestContext.Current.CancellationToken));
        }

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

    [Fact]
    public async Task La_cuenta_de_la_demo_usa_la_clave_configurada_aunque_ya_existiera()
    {
        async Task<int> IngresarAsync(string clave) =>
            (await LeerAsync(await factory.CreateClient()
                .PostAsJsonAsync("/Usuario/LogIn", new { usuario = "demo-clave", password = clave }, TestContext.Current.CancellationToken))).Exito;

        async Task ArrancarConClaveAsync(string clave)
        {
            await using var conDemo = factory.WithWebHostBuilder(b =>
            {
                b.UseSetting("Demo:Activa", "true");
                b.UseSetting("Demo:Slug", "demo-clave");
                b.UseSetting("Demo:Usuario", "demo-clave");
                b.UseSetting("Demo:Clave", clave);
            });
            conDemo.CreateClient();
            for (var intento = 0; intento < 20 && await IngresarAsync(clave) != 1; intento++)
                await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        await ArrancarConClaveAsync("primera");
        Assert.Equal(1, await IngresarAsync("primera"));

        await ArrancarConClaveAsync("segunda");
        Assert.Equal(1, await IngresarAsync("segunda"));
        Assert.Equal(0, await IngresarAsync("primera"));
    }
}
