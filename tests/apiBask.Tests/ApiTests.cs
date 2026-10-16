using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace apiBask.Tests;

// Tests de caracterización: fijan cómo responde hoy la API para poder cambiarla con seguridad.
[Collection(nameof(ApiCollection))]
public class ApiTests(ApiFactory factory)
{
    private record Respuesta(int Exito, string? Mensaje, JsonElement Data);

    private static async Task<Respuesta> LeerAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<Respuesta>(JsonSerializerOptions.Web))!;

    // Registra un usuario nuevo y devuelve un cliente con su token.
    private async Task<(HttpClient Cliente, string Usuario)> ClienteLogueadoAsync()
    {
        var usuario = "u" + Guid.NewGuid().ToString("N")[..10];
        var cliente = factory.CreateClient();

        var registro = await cliente.PostAsJsonAsync("/Usuario/Registro", new { usuario, pass = "secreta" });
        Assert.Equal(HttpStatusCode.OK, registro.StatusCode);

        var login = await LeerAsync(await cliente.PostAsJsonAsync("/Usuario/LogIn", new { usuario, password = "secreta" }));
        Assert.Equal(1, login.Exito);
        var token = login.Data.GetProperty("token").GetString();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (cliente, usuario);
    }

    [Fact]
    public async Task Login_con_contraseña_incorrecta_no_devuelve_token()
    {
        var (_, usuario) = await ClienteLogueadoAsync();

        var login = await LeerAsync(await factory.CreateClient()
            .PostAsJsonAsync("/Usuario/LogIn", new { usuario, password = "otra" }));

        Assert.Equal(0, login.Exito);
    }

    [Fact]
    public async Task Registrar_un_usuario_repetido_falla()
    {
        var (cliente, usuario) = await ClienteLogueadoAsync();

        var registro = await cliente.PostAsJsonAsync("/Usuario/Registro", new { usuario, pass = "otra" });

        Assert.Equal(HttpStatusCode.BadRequest, registro.StatusCode);
    }

    [Fact]
    public async Task Los_endpoints_de_datos_piden_token()
    {
        var response = await factory.CreateClient().GetAsync("/Equipo/Listar?idUsuario=nadie");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Crear_equipo_y_listarlo()
    {
        var (cliente, usuario) = await ClienteLogueadoAsync();

        var crear = await LeerAsync(await cliente.PostAsJsonAsync("/Equipo/Crear",
            new { nombre = "Aguada", ciudad = "Montevideo", usuario }));
        Assert.Equal(1, crear.Exito);

        var lista = await LeerAsync(await cliente.GetAsync($"/Equipo/Listar?idUsuario={usuario}"));
        var equipo = Assert.Single(lista.Data.EnumerateArray());
        Assert.Equal("Aguada", equipo.GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task Crear_equipo_rapido_con_jugadores()
    {
        var (cliente, usuario) = await ClienteLogueadoAsync();

        var equipoId = await CrearEquipoRapidoAsync(cliente, usuario, "Rápido", 4, 5);

        var jugadores = await LeerAsync(await cliente.GetAsync($"/Jugadores/ListarPorEquipo?equipoId={equipoId}"));
        Assert.Equal([4, 5], jugadores.Data.EnumerateArray().Select(j => j.GetProperty("numero").GetInt32()).Order());
    }

    [Fact]
    public async Task Crear_partido_con_estadisticas_y_leerlo()
    {
        var (cliente, usuario) = await ClienteLogueadoAsync();
        var localId = await CrearEquipoRapidoAsync(cliente, usuario, "Local", 7);
        var visitanteId = await CrearEquipoRapidoAsync(cliente, usuario, "Visitante", 10);
        var jugadorLocal = (await LeerAsync(await cliente.GetAsync($"/Jugadores/ListarPorEquipo?equipoId={localId}")))
            .Data[0].GetProperty("id").GetInt32();

        var crear = await LeerAsync(await cliente.PostAsJsonAsync("/Partido/Crear", new
        {
            partido = new { equipoLocal = localId, equipoVisitante = visitanteId, ganador = localId, fecha = "2026-10-16", lugar = "Cancha", idUsuario = usuario },
            statJ = new[]
            {
                new { jugadorId = jugadorLocal, equipoId = localId, puntos = 5, aciertos2Puntos = 1, intentos2Puntos = 2, aciertos3Puntos = 1, intentos3Puntos = 1, minutos = 600000L }
            },
            statP = new[]
            {
                new { equipoId = localId, puntos = 5 },
                new { equipoId = visitanteId, puntos = 2 }
            }
        }));
        Assert.Equal(1, crear.Exito);
        var partidoId = crear.Data.GetInt32();

        var partido = (await LeerAsync(await cliente.GetAsync($"/Partido/ConStats?id={partidoId}"))).Data;
        Assert.Equal("Local", partido.GetProperty("ganadorNombre").GetString());
        Assert.StartsWith("2026-10-16", partido.GetProperty("fecha").GetString());
        Assert.Equal(2, partido.GetProperty("statPartido").GetArrayLength());
        var stat = Assert.Single(Assert.Single(partido.GetProperty("jugadores").EnumerateArray())
            .GetProperty("estadisticasJugador").EnumerateArray());
        Assert.Equal(5, stat.GetProperty("puntos").GetInt32());
        Assert.Equal("00:10:00", stat.GetProperty("minutos").GetString());

        var lista = await LeerAsync(await cliente.GetAsync($"/Partido/ListarTodos?idUsuario={usuario}"));
        Assert.Single(lista.Data.EnumerateArray());
    }

    [Fact]
    public async Task Un_usuario_no_ve_ni_cambia_los_equipos_de_otro()
    {
        var (dueño, usuarioDueño) = await ClienteLogueadoAsync();
        var (otro, _) = await ClienteLogueadoAsync();
        var equipoId = await CrearEquipoRapidoAsync(dueño, usuarioDueño, "Ajeno", 1);

        // Mandar el id de otro usuario ya no sirve: la lista sale del token.
        var lista = await LeerAsync(await otro.GetAsync($"/Equipo/Listar?idUsuario={usuarioDueño}"));
        Assert.Empty(lista.Data.EnumerateArray());

        var detalle = await LeerAsync(await otro.GetAsync($"/Equipo/ListarPorId?idEquipo={equipoId}"));
        Assert.Equal(0, detalle.Exito);

        var modificar = await LeerAsync(await otro.PutAsJsonAsync("/Equipo/Modificar", new { id = equipoId, nombre = "Hackeado" }));
        Assert.Equal(0, modificar.Exito);

        await otro.DeleteAsync($"/Equipo/Eliminar?id={equipoId}");
        var jugadores = await LeerAsync(await otro.GetAsync($"/Jugadores/ListarPorEquipo?equipoId={equipoId}"));
        Assert.Empty(jugadores.Data.EnumerateArray());

        var propio = await LeerAsync(await dueño.GetAsync($"/Equipo/ListarPorId?idEquipo={equipoId}"));
        Assert.Equal("Ajeno", propio.Data.GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task Un_usuario_no_ve_los_partidos_de_otro_ni_usa_sus_equipos()
    {
        var (dueño, usuarioDueño) = await ClienteLogueadoAsync();
        var (otro, usuarioOtro) = await ClienteLogueadoAsync();
        var localId = await CrearEquipoRapidoAsync(dueño, usuarioDueño, "Local", 7);
        var visitanteId = await CrearEquipoRapidoAsync(dueño, usuarioDueño, "Visitante", 8);
        var partidoId = (await LeerAsync(await dueño.PostAsJsonAsync("/Partido/Crear", new
        {
            partido = new { equipoLocal = localId, equipoVisitante = visitanteId },
            statJ = Array.Empty<object>(),
            statP = Array.Empty<object>()
        }))).Data.GetInt32();

        var ajeno = await LeerAsync(await otro.GetAsync($"/Partido/ConStats?id={partidoId}"));
        Assert.Equal(0, ajeno.Exito);

        var conEquiposAjenos = await otro.PostAsJsonAsync("/Partido/Crear", new
        {
            partido = new { equipoLocal = localId, equipoVisitante = visitanteId, idUsuario = usuarioOtro },
            statJ = Array.Empty<object>(),
            statP = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.BadRequest, conEquiposAjenos.StatusCode);
    }

    [Fact]
    public async Task La_lista_de_jugadores_trae_el_nombre_del_equipo()
    {
        var (cliente, usuario) = await ClienteLogueadoAsync();
        await CrearEquipoRapidoAsync(cliente, usuario, "Nacional", 23);

        var lista = await LeerAsync(await cliente.GetAsync("/Jugadores/Listar"));

        var jugador = Assert.Single(lista.Data.EnumerateArray());
        Assert.Equal("Nacional", jugador.GetProperty("equipo").GetString());
        Assert.Equal(23, jugador.GetProperty("numero").GetInt32());
    }

    [Fact]
    public async Task Un_partido_invalido_se_rechaza()
    {
        var (cliente, usuario) = await ClienteLogueadoAsync();
        var equipoId = await CrearEquipoRapidoAsync(cliente, usuario, "Solo", 1);
        var otroId = await CrearEquipoRapidoAsync(cliente, usuario, "Otro", 2);
        var jugadorId = (await LeerAsync(await cliente.GetAsync($"/Jugadores/ListarPorEquipo?equipoId={equipoId}")))
            .Data[0].GetProperty("id").GetInt32();

        var mismoEquipo = await cliente.PostAsJsonAsync("/Partido/Crear", new
        {
            partido = new { equipoLocal = equipoId, equipoVisitante = equipoId },
            statJ = Array.Empty<object>(),
            statP = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.BadRequest, mismoEquipo.StatusCode);

        var statSinEquipo = await cliente.PostAsJsonAsync("/Partido/Crear", new
        {
            partido = new { equipoLocal = equipoId, equipoVisitante = otroId },
            statJ = new[] { new { jugadorId } },
            statP = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.BadRequest, statSinEquipo.StatusCode);

        var statRepetida = await cliente.PostAsJsonAsync("/Partido/Crear", new
        {
            partido = new { equipoLocal = equipoId, equipoVisitante = otroId },
            statJ = Array.Empty<object>(),
            statP = new[] { new { equipoId }, new { equipoId } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, statRepetida.StatusCode);
    }

    [Fact]
    public async Task Subir_imagen_pide_token_y_solo_acepta_imagenes()
    {
        var sinToken = await factory.CreateClient().PostAsync("/Imagen/Upload", Archivo("text/plain"));
        Assert.Equal(HttpStatusCode.Unauthorized, sinToken.StatusCode);

        var (cliente, _) = await ClienteLogueadoAsync();
        var texto = await cliente.PostAsync("/Imagen/Upload", Archivo("text/plain"));
        Assert.Equal(HttpStatusCode.BadRequest, texto.StatusCode);

        // El listado y el borrado de imágenes ya no existen.
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/Imagen")).StatusCode);
    }

    private static MultipartFormDataContent Archivo(string contentType)
    {
        var archivo = new ByteArrayContent([1, 2, 3]);
        archivo.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { archivo, "image", "archivo" } };
    }

    private static async Task<int> CrearEquipoRapidoAsync(HttpClient cliente, string usuario, string nombre, params int[] numeros)
    {
        var jugadores = numeros.Select(n => new { nombre = "N/D", numero = n, usuario });
        var crear = await LeerAsync(await cliente.PostAsJsonAsync("/Equipo/CrearEquipoRapido",
            new { nombre, idUsuario = usuario, jugadores }));
        Assert.Equal(1, crear.Exito);
        return crear.Data.GetInt32();
    }
}
