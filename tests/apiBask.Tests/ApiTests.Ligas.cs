using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace apiBask.Tests;

// Ligas: se arma la temporada con su fixture, se juegan partidos y la API pública muestra la tabla, los líderes y el MVP.
public partial class ApiTests
{
    [Fact]
    public async Task Una_liga_publica_muestra_posiciones_lideres_y_partidos_en_vivo()
    {
        var (cliente, usuario) = await ClienteLogueadoAsync();
        var equipos = new int[4];
        for (var i = 0; i < 4; i++) equipos[i] = await CrearEquipoRapidoAsync(cliente, usuario, $"Equipo {i}", 4, 5);

        var nombre = "Liga " + Guid.NewGuid().ToString("N")[..8];
        var ligaId = (await LeerAsync(await cliente.PostAsJsonAsync("/Liga/Crear", new { nombre, temporada = "Apertura" }))).Data.GetInt32();
        var liga = (await LeerAsync(await cliente.GetAsync($"/Liga/Obtener?id={ligaId}"))).Data;
        var slug = liga.GetProperty("slug").GetString()!;
        Assert.Equal(nombre.ToLowerInvariant().Replace(' ', '-'), slug);
        var temporadaId = liga.GetProperty("temporada").GetProperty("id").GetInt32();

        var divisionA = await CrearDivisionAsync(cliente, temporadaId, "A");
        var divisionB = await CrearDivisionAsync(cliente, temporadaId, "B");
        var inscripciones = await cliente.PutAsJsonAsync("/Liga/Inscripciones", new
        {
            temporadaId,
            equipos = new[]
            {
                new { equipoId = equipos[0], divisionId = divisionA }, new { equipoId = equipos[1], divisionId = divisionA },
                new { equipoId = equipos[2], divisionId = divisionB }, new { equipoId = equipos[3], divisionId = divisionB },
            },
        });
        Assert.Equal(HttpStatusCode.OK, inscripciones.StatusCode);

        // Dos equipos por división, ida y vuelta: dos fechas con dos partidos cada una.
        var fixture = await LeerAsync(await cliente.PostAsJsonAsync("/Liga/GenerarFixture",
            new { temporadaId, desde = "2026-10-05", diasEntreFechas = 7, idaYVuelta = true }));
        Assert.Equal(4, fixture.Data.GetInt32());
        var programados = (await LeerAsync(await cliente.GetAsync($"/Liga/Obtener?id={ligaId}"))).Data
            .GetProperty("temporada").GetProperty("partidos").EnumerateArray().ToList();
        Assert.All(programados, p => Assert.Equal("Programado", p.GetProperty("estado").GetString()));
        Assert.Equal(new[] { 1, 1, 2, 2 }, programados.Select(p => p.GetProperty("jornada").GetInt32()));

        // Los partidos sin jugar no aparecen en la lista de partidos del usuario.
        var mios = (await LeerAsync(await cliente.GetAsync("/Partido/ListarTodos"))).Data;
        Assert.Equal(0, mios.GetArrayLength());

        // Se juega un partido de la división A y queda otro de la B en curso.
        var terminado = programados.First(p => p.GetProperty("local").GetProperty("id").GetInt32() is var l && (l == equipos[0] || l == equipos[1]));
        var enCurso = programados.First(p => p.GetProperty("local").GetProperty("id").GetInt32() is var l && (l == equipos[2] || l == equipos[3]));
        var anotador = await JugarAsync(cliente, terminado, triples: 2, finalizar: true);
        await JugarAsync(cliente, enCurso, triples: 1, finalizar: false);

        // La parte pública no pide login.
        var publico = factory.CreateClient();
        var datos = (await LeerAsync(await publico.GetAsync($"/publico/ligas/{slug}"))).Data;
        Assert.Equal("Apertura", datos.GetProperty("temporada").GetProperty("nombre").GetString());
        Assert.Equal(2, datos.GetProperty("temporada").GetProperty("divisiones").GetArrayLength());

        var partidos = (await LeerAsync(await publico.GetAsync($"/publico/ligas/{slug}/partidos"))).Data.EnumerateArray().ToList();
        var vivo = partidos.Single(p => p.GetProperty("estado").GetString() == "EnCurso");
        Assert.Equal(3, vivo.GetProperty("local").GetProperty("puntos").GetInt32());
        var final = partidos.Single(p => p.GetProperty("estado").GetString() == "Finalizado");
        Assert.Equal(6, final.GetProperty("local").GetProperty("puntos").GetInt32());
        Assert.Equal(anotador, final.GetProperty("mvp").GetProperty("jugador").GetProperty("id").GetInt32());

        var posiciones = (await LeerAsync(await publico.GetAsync($"/publico/ligas/{slug}/posiciones"))).Data;
        var tablaA = posiciones.EnumerateArray().Single(t => t.GetProperty("division").GetProperty("nombre").GetString() == "A");
        var primero = tablaA.GetProperty("filas")[0];
        Assert.Equal(final.GetProperty("local").GetProperty("id").GetInt32(), primero.GetProperty("equipo").GetProperty("id").GetInt32());
        Assert.Equal(1, primero.GetProperty("ganados").GetInt32());
        Assert.Equal(2, primero.GetProperty("puntos").GetInt32());
        Assert.Equal(6, primero.GetProperty("diferencia").GetInt32());
        // El partido en curso todavía no cuenta en la tabla de la B.
        var tablaB = posiciones.EnumerateArray().Single(t => t.GetProperty("division").GetProperty("nombre").GetString() == "B");
        Assert.All(tablaB.GetProperty("filas").EnumerateArray(), f => Assert.Equal(0, f.GetProperty("jugados").GetInt32()));

        var lideres = (await LeerAsync(await publico.GetAsync($"/publico/ligas/{slug}/lideres"))).Data;
        var puntos = lideres.GetProperty("categorias").EnumerateArray().Single(c => c.GetProperty("clave").GetString() == "puntos");
        Assert.Equal(anotador, puntos.GetProperty("jugadores")[0].GetProperty("jugador").GetProperty("id").GetInt32());
        Assert.Equal(6, puntos.GetProperty("jugadores")[0].GetProperty("promedio").GetDouble());

        var mvp = (await LeerAsync(await publico.GetAsync($"/publico/ligas/{slug}/mvp"))).Data;
        Assert.Equal(anotador, mvp.GetProperty("semana").GetProperty("mvp").GetProperty("jugador").GetProperty("id").GetInt32());
        Assert.Equal(anotador, mvp.GetProperty("temporada").GetProperty("mvp").GetProperty("jugador").GetProperty("id").GetInt32());

        var detalle = (await LeerAsync(await publico.GetAsync($"/publico/ligas/{slug}/partidos/{vivo.GetProperty("id").GetInt32()}"))).Data;
        Assert.Equal(2, detalle.GetProperty("jugadas").GetArrayLength());
        Assert.Equal(3, detalle.GetProperty("planilla").EnumerateArray().Sum(p => p.GetProperty("estadistica").GetProperty("puntos").GetInt32()));

        var ficha = (await LeerAsync(await publico.GetAsync($"/publico/ligas/{slug}/jugadores/{anotador}"))).Data;
        Assert.Equal(6, ficha.GetProperty("temporadas")[0].GetProperty("promedios").GetProperty("puntos").GetDouble());

        // Otra liga no ve partidos ajenos, y una liga con partidos jugados no se borra.
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync($"/publico/ligas/no-existe-{Guid.NewGuid():N}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.DeleteAsync($"/Liga/Eliminar?id={ligaId}")).StatusCode);
        var sacar = await cliente.PutAsJsonAsync("/Liga/Inscripciones", new { temporadaId, equipos = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, sacar.StatusCode);

        // Descartar un partido de liga lo devuelve al fixture sin lo anotado.
        await cliente.DeleteAsync($"/Partido/Eliminar?id={vivo.GetProperty("id").GetInt32()}");
        var descartado = (await LeerAsync(await publico.GetAsync($"/publico/ligas/{slug}/partidos"))).Data.EnumerateArray()
            .Single(p => p.GetProperty("id").GetInt32() == vivo.GetProperty("id").GetInt32());
        Assert.Equal("Programado", descartado.GetProperty("estado").GetString());
        Assert.Equal(JsonValueKind.Null, descartado.GetProperty("local").GetProperty("puntos").ValueKind);
    }

    [Fact]
    public async Task Una_liga_no_se_puede_administrar_desde_otra_cuenta()
    {
        var (duenio, _) = await ClienteLogueadoAsync();
        var ligaId = (await LeerAsync(await duenio.PostAsJsonAsync("/Liga/Crear", new { nombre = "Liga " + Guid.NewGuid().ToString("N")[..8] }))).Data.GetInt32();
        var (otro, _) = await ClienteLogueadoAsync();

        Assert.Equal(0, (await LeerAsync(await otro.GetAsync($"/Liga/Obtener?id={ligaId}"))).Exito);
        Assert.Equal(0, (await LeerAsync(await otro.PostAsJsonAsync("/Liga/Temporada", new { ligaId, nombre = "Clausura" }))).Exito);
    }

    private static async Task<int> CrearDivisionAsync(HttpClient cliente, int temporadaId, string nombre) =>
        (await LeerAsync(await cliente.PostAsJsonAsync("/Liga/Division", new { temporadaId, nombre }))).Data.GetInt32();

    // Empieza a anotar un partido del fixture, mete triples del primer jugador local y devuelve su id.
    private static async Task<int> JugarAsync(HttpClient cliente, JsonElement programado, int triples, bool finalizar)
    {
        var id = programado.GetProperty("id").GetInt32();
        var localId = programado.GetProperty("local").GetProperty("id").GetInt32();
        var iniciar = await LeerAsync(await cliente.PostAsJsonAsync("/Partido/Iniciar", new { partidoId = id, duracionCuartoMin = 10 }));
        Assert.Equal(id, iniciar.Data.GetInt32());
        var anotador = (await JugadoresAsync(cliente, localId))[0];
        var jugadas = new List<object> { Jugada(1, "Cambio", localId, anotador, relojMs: 600_000) };
        for (var i = 0; i < triples; i++) jugadas.Add(Jugada(i + 2, "Tiro", localId, anotador, relojMs: 590_000 - i * 1000, acierto: true, valor: 3));
        Assert.Equal(1, (await LeerAsync(await cliente.PostAsJsonAsync($"/Partido/Jugadas?partidoId={id}", jugadas))).Exito);
        if (finalizar) Assert.Equal(1, (await LeerAsync(await cliente.PostAsJsonAsync($"/Partido/Finalizar?id={id}", new { cuarto = 4, relojMs = 0 }))).Exito);
        return anotador;
    }
}
