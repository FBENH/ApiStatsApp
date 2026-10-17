using apiBask;
using apiBask.Models;
using apiBask.Models.Common;
using apiBask.Models.Response;
using apiBask.Models.Storage;
using apiBask.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

const string Cors = "MiCors";

var appSettingsSection = builder.Configuration.GetSection("AppSettings");
builder.Services.Configure<AppSettings>(appSettingsSection);

// La base gratis de Azure se pausa sola cuando nadie la usa y tarda hasta un minuto en despertar.
// Mientras despierta rechaza las conexiones con un error pasajero: se reintenta en lugar de devolver un 500.
builder.Services.AddDbContext<BasketContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("default"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

builder.Services.AddScoped<AvisosEnVivo>();
builder.Services.AddScoped<RegistroDePartido>();
builder.Services.AddSingleton<ReproductorDePartidos>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ReproductorDePartidos>());
builder.Services.AddSingleton(builder.Configuration.GetSection("Demo").Get<OpcionesDemo>() ?? new OpcionesDemo());
builder.Services.AddSingleton<LigaDemo>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<LigaDemo>());
builder.Services.AddControllers();
builder.Services.AddSignalR();

// JWT
var appSettings = appSettingsSection.Get<AppSettings>();
if (string.IsNullOrEmpty(appSettings?.Secreto) || appSettings.Secreto.Length < 32)
    throw new InvalidOperationException("AppSettings:Secreto tiene que estar configurado y tener al menos 32 caracteres.");
var llave = Encoding.ASCII.GetBytes(appSettings.Secreto);
builder.Services.AddAuthentication(d =>
{
    d.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    d.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(d =>
{
    d.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(llave),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// El token viaja en el header Authorization (no en cookies), así que se puede aceptar cualquier origen.
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: Cors, policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Retry-After"));
});

var app = builder.Build();

// Crea o actualiza la base al arrancar con las migraciones de EF.
// Si la base no responde (por ejemplo, porque está despertando) se reintenta un rato. Si sigue sin responder,
// la API arranca igual y lo deja en el log, así /salud contesta y el error se puede ver.
for (var intento = 1; ; intento++)
{
    try
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<BasketContext>().Database.Migrate();
        break;
    }
    catch (Exception e) when (intento < 6)
    {
        app.Logger.LogWarning(e, "No se pudo migrar la base (intento {Intento}); se reintenta en 10 segundos.", intento);
        Thread.Sleep(TimeSpan.FromSeconds(10));
    }
    catch (Exception e)
    {
        app.Logger.LogError(e, "No se pudo migrar la base. La API arranca sin migrar.");
        break;
    }
}

// Un error inesperado se registra en el log y el cliente recibe un mensaje genérico, sin detalles internos.
// Si la base sigue sin responder después de los reintentos se devuelve 503, así el front sabe que puede volver a probar.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    if (Transacciones.BaseNoDisponible(context.Features.Get<IExceptionHandlerFeature>()?.Error))
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = "10";
        await context.Response.WriteAsJsonAsync(new Response { mensaje = "La base de datos se está despertando. Probá de nuevo en unos segundos." });
        return;
    }
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new Response { mensaje = "Ocurrió un error inesperado." });
}));

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors(Cors);

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
// Para el ping que mantiene despierta la app: no toca la base, así la base se puede pausar.
app.MapGet("/salud", () => "ok").AllowAnonymous();
// El front lo llama apenas se abre la pantalla de ingreso, para que la base vaya despertando mientras la persona escribe.
app.MapGet("/salud/base", async (BasketContext db) =>
    await db.Database.CanConnectAsync() ? Results.Ok("ok") : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous();
app.MapHub<apiBask.Hubs.PartidosHub>("/hubs/partidos");

app.Run();

// Para los tests de integración (WebApplicationFactory).
public partial class Program { }
