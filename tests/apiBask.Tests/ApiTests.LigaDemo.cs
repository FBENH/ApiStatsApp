using System.Net.Http.Json;
using apiBask.Models;
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

    [Fact]
    public async Task Los_datos_de_la_cuenta_de_la_demo_se_reinician_al_entrar_si_pasaron_las_horas()
    {
        await using var conDemo = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Demo:Activa", "true");
            b.UseSetting("Demo:Slug", "demo-reinicio");
            b.UseSetting("Demo:Usuario", "demo-reinicio");
            b.UseSetting("Demo:Clave", "clave-demo");
            b.UseSetting("Demo:SegundosEntreJugadas", "0");
        });
        var cliente = conDemo.CreateClient();
        var cancelar = TestContext.Current.CancellationToken;
        async Task<int> IngresarAsync() =>
            (await LeerAsync(await cliente.PostAsJsonAsync("/Usuario/LogIn", new { usuario = "demo-reinicio", password = "clave-demo" }, cancelar))).Exito;

        // Una visita arma la liga.
        for (var intento = 0; intento < 30; intento++)
        {
            if ((await cliente.GetAsync("/publico/ligas/demo-reinicio", cancelar)).IsSuccessStatusCode) break;
            await Task.Delay(500, cancelar);
        }
        Assert.Equal(1, await IngresarAsync());

        int ligaAntes;
        using (var scope = conDemo.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BasketContext>();
            var liga = await db.Ligas.FirstAsync(l => l.Slug == "demo-reinicio", cancelar);
            ligaAntes = liga.Id;
            // Un visitante agrega un equipo.
            db.Equipos.Add(new Equipo { Nombre = "Del visitante", UsuarioId = liga.UsuarioId, Activo = true });
            await db.SaveChangesAsync(cancelar);

            // Antes de las 24 horas no se toca nada.
            Assert.Equal(1, await IngresarAsync());
            Assert.True(await db.Equipos.AnyAsync(e => e.Nombre == "Del visitante", cancelar));

            await db.Ligas.Where(l => l.Id == liga.Id).ExecuteUpdateAsync(l => l.SetProperty(x => x.CreadaEn, DateTime.UtcNow.AddHours(-25)), cancelar);
        }

        Assert.Equal(1, await IngresarAsync());

        using (var scope = conDemo.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BasketContext>();
            Assert.False(await db.Equipos.AnyAsync(e => e.Nombre == "Del visitante", cancelar));
            var liga = await db.Ligas.Include(l => l.Temporadas).FirstAsync(l => l.Slug == "demo-reinicio", cancelar);
            Assert.NotEqual(ligaAntes, liga.Id);
            Assert.Equal(6, await db.Equipos.CountAsync(e => e.UsuarioId == liga.UsuarioId, cancelar));
            Assert.Equal(30, await db.Partidos.CountAsync(p => p.TemporadaId == liga.Temporadas[0].Id, cancelar));
        }
    }
}
