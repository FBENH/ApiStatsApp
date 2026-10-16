using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace apiBask.Tests;

// Partidos anotados jugada a jugada.
public partial class ApiTests
{
    private record Partido(HttpClient Cliente, int Id, int LocalId, int VisitanteId, int[] Locales, int[] Visitantes);

    private async Task<Partido> IniciarPartidoAsync()
    {
        var (cliente, usuario) = await ClienteLogueadoAsync();
        var localId = await CrearEquipoRapidoAsync(cliente, usuario, "Local", 4, 5);
        var visitanteId = await CrearEquipoRapidoAsync(cliente, usuario, "Visitante", 6, 7);
        var partidoId = (await LeerAsync(await cliente.PostAsJsonAsync("/Partido/Iniciar",
            new { equipoLocal = localId, equipoVisitante = visitanteId, duracionCuartoMin = 10 }))).Data.GetInt32();
        return new(cliente, partidoId, localId, visitanteId,
            await JugadoresAsync(cliente, localId), await JugadoresAsync(cliente, visitanteId));
    }

    private static async Task<int[]> JugadoresAsync(HttpClient cliente, int equipoId) =>
        (await LeerAsync(await cliente.GetAsync($"/Jugadores/ListarPorEquipo?equipoId={equipoId}"))).Data
            .EnumerateArray().OrderBy(j => j.GetProperty("numero").GetInt32()).Select(j => j.GetProperty("id").GetInt32()).ToArray();

    private static Task<HttpResponseMessage> EnviarAsync(Partido p, params object[] jugadas) =>
        p.Cliente.PostAsJsonAsync($"/Partido/Jugadas?partidoId={p.Id}", jugadas);

    private static object Jugada(int numero, string tipo, int equipoId, int? jugadorId = null, int cuarto = 1, int? relojMs = null,
        bool? acierto = null, int? valor = null, int? saleId = null, bool? ofensivo = null, Guid? id = null) =>
        new { id = id ?? Guid.NewGuid(), numero, cuarto, relojMs, tipo, equipoId, jugadorId, saleId, acierto, valor, ofensivo };

    [Fact]
    public async Task Un_partido_en_vivo_se_retoma_y_se_finaliza_con_la_planilla_calculada()
    {
        var p = await IniciarPartidoAsync();
        var (a, b) = (p.Locales[0], p.Locales[1]);
        var rival = p.Visitantes[0];
        var triple = Guid.NewGuid();
        var primerEnvio = new[]
        {
            Jugada(1, "Cambio", p.LocalId, a, relojMs: 600_000),
            Jugada(2, "Cambio", p.VisitanteId, rival, relojMs: 600_000),
            Jugada(3, "Tiro", p.LocalId, a, relojMs: 590_000, acierto: true, valor: 3, id: triple),
            Jugada(4, "Tiro", p.VisitanteId, rival, relojMs: 560_000, acierto: false, valor: 2),
            Jugada(5, "Rebote", p.LocalId, a, relojMs: 558_000, ofensivo: false),
        };
        Assert.Equal(1, (await LeerAsync(await EnviarAsync(p, primerEnvio))).Exito);

        // El mismo envío de nuevo (un reintento) no duplica nada.
        var reintento = await LeerAsync(await EnviarAsync(p, primerEnvio));
        Assert.Equal(0, reintento.Data.GetInt32());

        // Otro dispositivo encuentra el partido en curso y descarga sus jugadas.
        var enCurso = Assert.Single((await LeerAsync(await p.Cliente.GetAsync("/Partido/EnCurso"))).Data.EnumerateArray());
        Assert.Equal(p.Id, enCurso.GetProperty("id").GetInt32());
        var jugadas = (await LeerAsync(await p.Cliente.GetAsync($"/Partido/Jugadas?partidoId={p.Id}"))).Data;
        Assert.Equal(5, jugadas.GetArrayLength());
        Assert.Equal("Tiro", jugadas[2].GetProperty("tipo").GetString());

        // Sigue anotando: cambio a los 2 minutos del segundo cuarto y un libre, y se deshace una jugada.
        var errada = Guid.NewGuid();
        await EnviarAsync(p,
            Jugada(6, "Cambio", p.LocalId, b, cuarto: 2, relojMs: 480_000, saleId: a),
            Jugada(7, "Libre", p.LocalId, b, cuarto: 2, relojMs: 470_000, acierto: true),
            Jugada(8, "Libre", p.LocalId, b, cuarto: 2, relojMs: 470_000, acierto: true, id: errada));
        await p.Cliente.DeleteAsync($"/Partido/Jugadas?partidoId={p.Id}&id={errada}");
        await p.Cliente.DeleteAsync($"/Partido/Jugadas?partidoId={p.Id}&id={errada}");

        var fin = await LeerAsync(await p.Cliente.PostAsJsonAsync($"/Partido/Finalizar?id={p.Id}", new { cuarto = 2, relojMs = 300_000 }));
        Assert.Equal(1, fin.Exito);
        Assert.Empty((await LeerAsync(await p.Cliente.GetAsync("/Partido/EnCurso"))).Data.EnumerateArray());

        var partido = (await LeerAsync(await p.Cliente.GetAsync($"/Partido/ConStats?id={p.Id}"))).Data;
        Assert.Equal(p.LocalId, partido.GetProperty("ganador").GetInt32());
        var equipos = partido.GetProperty("statPartido").EnumerateArray()
            .ToDictionary(e => e.GetProperty("equipoId").GetInt32(), e => e.GetProperty("puntos").GetInt32());
        Assert.Equal(4, equipos[p.LocalId]);
        Assert.Equal(0, equipos[p.VisitanteId]);

        var filas = partido.GetProperty("jugadores").EnumerateArray()
            .ToDictionary(j => j.GetProperty("id").GetInt32(), j => j.GetProperty("estadisticasJugador")[0]);
        // a jugó el primer cuarto entero más 2 minutos del segundo; b, de ahí hasta el final (3 minutos).
        Assert.Equal("00:12:00", filas[a].GetProperty("minutos").GetString());
        Assert.Equal("00:03:00", filas[b].GetProperty("minutos").GetString());
        Assert.Equal("00:15:00", filas[rival].GetProperty("minutos").GetString());
        Assert.Equal(3, filas[a].GetProperty("puntos").GetInt32());
        Assert.Equal(1, filas[a].GetProperty("rebotesDefensivos").GetInt32());
        Assert.Equal(1, filas[b].GetProperty("aciertos1Punto").GetInt32());
        Assert.Equal(1, filas[rival].GetProperty("intentos2Puntos").GetInt32());
    }

    [Fact]
    public async Task Corregir_un_partido_terminado_recalcula_la_planilla()
    {
        var p = await IniciarPartidoAsync();
        var tiro = Guid.NewGuid();
        await EnviarAsync(p, Jugada(1, "Tiro", p.VisitanteId, p.Visitantes[0], acierto: true, valor: 2, id: tiro));
        await p.Cliente.PostAsJsonAsync($"/Partido/Finalizar?id={p.Id}", new { });
        await p.Cliente.DeleteAsync($"/Partido/Jugadas?partidoId={p.Id}&id={tiro}");
        await EnviarAsync(p, Jugada(2, "Tiro", p.LocalId, p.Locales[0], acierto: true, valor: 3));

        var partido = (await LeerAsync(await p.Cliente.GetAsync($"/Partido/ConStats?id={p.Id}"))).Data;
        Assert.Equal(p.LocalId, partido.GetProperty("ganador").GetInt32());
        // Sin reloj no se cuentan minutos.
        var fila = Assert.Single(partido.GetProperty("jugadores").EnumerateArray());
        Assert.Equal("00:00:00", fila.GetProperty("estadisticasJugador")[0].GetProperty("minutos").GetString());
    }

    [Fact]
    public async Task Jugadas_invalidas_o_ajenas_se_rechazan()
    {
        var p = await IniciarPartidoAsync();
        var (otro, usuarioOtro) = await ClienteLogueadoAsync();
        var equipoAjeno = await CrearEquipoRapidoAsync(otro, usuarioOtro, "Ajeno", 9);
        var jugadorAjeno = (await JugadoresAsync(otro, equipoAjeno))[0];

        Assert.Equal(HttpStatusCode.BadRequest, (await EnviarAsync(p, Jugada(1, "Tiro", p.LocalId, p.Locales[0], acierto: true))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await EnviarAsync(p, Jugada(1, "Robo", equipoAjeno))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await EnviarAsync(p, Jugada(1, "Robo", p.LocalId, jugadorAjeno))).StatusCode);

        // Otro usuario no puede ver ni cargar jugadas en el partido.
        var ajeno = await LeerAsync(await otro.PostAsJsonAsync($"/Partido/Jugadas?partidoId={p.Id}", new[] { Jugada(1, "Robo", p.LocalId) }));
        Assert.Equal(0, ajeno.Exito);
        Assert.Equal(0, (await LeerAsync(await otro.GetAsync($"/Partido/Jugadas?partidoId={p.Id}"))).Exito);
    }
}
