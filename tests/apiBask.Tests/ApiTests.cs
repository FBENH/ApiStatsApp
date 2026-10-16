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
            partido = new { equipoLocal = localId, equipoVisitante = visitanteId, ganador = localId, lugar = "Cancha", idUsuario = usuario },
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
        Assert.Equal(2, partido.GetProperty("statPartido").GetArrayLength());
        var stat = Assert.Single(Assert.Single(partido.GetProperty("jugadores").EnumerateArray())
            .GetProperty("estadisticasJugador").EnumerateArray());
        Assert.Equal(5, stat.GetProperty("puntos").GetInt32());
        Assert.Equal("00:10:00", stat.GetProperty("minutos").GetString());

        var lista = await LeerAsync(await cliente.GetAsync($"/Partido/ListarTodos?idUsuario={usuario}"));
        Assert.Single(lista.Data.EnumerateArray());
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
