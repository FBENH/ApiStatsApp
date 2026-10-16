using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace apiBask.Tests;

// SignalR y el simulador: un partido de liga simulado llega jugada a jugada a quien lo está mirando.
public partial class ApiTests
{
    [Fact]
    public async Task Un_partido_simulado_se_ve_en_vivo_por_SignalR_y_termina_solo()
    {
        var (cliente, usuario) = await ClienteLogueadoAsync();
        var a = await CrearEquipoRapidoAsync(cliente, usuario, "Azules", 4, 5, 6, 7, 8, 9, 10);
        var b = await CrearEquipoRapidoAsync(cliente, usuario, "Rojos", 11, 12, 13, 14, 15, 16);
        var ligaId = (await LeerAsync(await cliente.PostAsJsonAsync("/Liga/Crear", new { nombre = "Liga " + Guid.NewGuid().ToString("N")[..8] }))).Data.GetInt32();
        var liga = (await LeerAsync(await cliente.GetAsync($"/Liga/Obtener?id={ligaId}"))).Data;
        var slug = liga.GetProperty("slug").GetString()!;
        var temporadaId = liga.GetProperty("temporada").GetProperty("id").GetInt32();
        await cliente.PutAsJsonAsync("/Liga/Inscripciones", new { temporadaId, equipos = new[] { new { equipoId = a, divisionId = (int?)null }, new { equipoId = b, divisionId = (int?)null } } });
        await cliente.PostAsJsonAsync("/Liga/GenerarFixture", new { temporadaId, idaYVuelta = true });
        var partidos = (await LeerAsync(await cliente.GetAsync($"/Liga/Obtener?id={ligaId}"))).Data
            .GetProperty("temporada").GetProperty("partidos").EnumerateArray().Select(p => p.GetProperty("id").GetInt32()).ToList();

        // Un partido del fixture se puede cambiar mientras no se jugó.
        var cambio = await LeerAsync(await cliente.PutAsJsonAsync("/Liga/Partido",
            new { id = partidos[1], localId = a, visitanteId = b, fecha = "2026-11-02", lugar = "Club Atenas", jornada = 5 }));
        Assert.Equal(1, cambio.Exito);

        // Alguien mira la liga y el partido sin estar logueado.
        await using var conexion = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/partidos"), o =>
            {
                o.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                o.Transports = HttpTransportType.LongPolling;
            })
            .Build();
        var recibidas = new ConcurrentBag<JsonElement>();
        var terminado = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        conexion.On<int, JsonElement[]>("jugadas", (_, jugadas) => { foreach (var j in jugadas) recibidas.Add(j); });
        conexion.On<JsonElement>("partido", p =>
        {
            if (p.GetProperty("estado").GetString() == "Finalizado") terminado.TrySetResult(p);
        });
        await conexion.StartAsync();
        await conexion.InvokeAsync("SeguirLiga", slug);
        await conexion.InvokeAsync("SeguirPartido", partidos[0]);

        var simular = await LeerAsync(await cliente.PostAsync($"/Liga/Simular?partidoId={partidos[0]}&segundosEntreJugadas=0", null));
        Assert.Equal(1, simular.Exito);
        var cantidad = simular.Data.GetInt32();

        var final = await terminado.Task.WaitAsync(TimeSpan.FromSeconds(60));
        Assert.Equal(cantidad, recibidas.Count);
        var (local, visitante) = (final.GetProperty("local").GetProperty("puntos").GetInt32(), final.GetProperty("visitante").GetProperty("puntos").GetInt32());
        Assert.NotEqual(local, visitante);
        var puntosEnJugadas = recibidas
            .Where(j => j.GetProperty("acierto").ValueKind == JsonValueKind.True && j.GetProperty("equipoId").GetInt32() == a)
            .Sum(j => j.GetProperty("tipo").GetString() == "Libre" ? 1 : j.GetProperty("valor").GetInt32());
        Assert.Equal(puntosEnJugadas, final.GetProperty("local").GetProperty("id").GetInt32() == a ? local : visitante);

        // La tabla ya cuenta el partido.
        var posiciones = (await LeerAsync(await factory.CreateClient().GetAsync($"/publico/ligas/{slug}/posiciones"))).Data;
        Assert.All(posiciones[0].GetProperty("filas").EnumerateArray(), f => Assert.Equal(1, f.GetProperty("jugados").GetInt32()));

        // Un partido suelto (sin liga) no se puede seguir.
        var suelto = await IniciarPartidoAsync();
        await Assert.ThrowsAsync<HubException>(() => conexion.InvokeAsync("SeguirPartido", suelto.Id));
    }
}
