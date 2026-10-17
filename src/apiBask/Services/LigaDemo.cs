using apiBask.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Services
{
    public class OpcionesDemo
    {
        // Apagada salvo que se pida (Demo__Activa=true en el entorno).
        public bool Activa { get; set; }
        public string Slug { get; set; } = "liga-demo";
        public string Usuario { get; set; } = "demo";
        // Clave de la cuenta de la demo (Demo__Clave). Al arrancar se le pone esta clave aunque la cuenta ya existiera.
        // Si no se configura, la cuenta queda con una clave al azar y nadie puede entrar con ella.
        public string? Clave { get; set; }
        public double SegundosEntreJugadas { get; set; } = 4;
        public double MinutosEntrePartidos { get; set; } = 5;
        // Cada cuántas horas se borra todo lo de la cuenta de la demo y se arma la liga de nuevo (0 = nunca).
        public double HorasEntreReinicios { get; set; } = 24;
    }

    // Liga de demo que se juega sola: la crea si no existe y simula sus partidos de a uno, en orden del fixture.
    // Cuando se termina el fixture arranca otra temporada con los mismos equipos.
    // Solo juega mientras alguien la mira (la página pública o SignalR): sin visitas no toca la base,
    // así la base puede pausarse y no se gasta cómputo en partidos que nadie ve.
    public class LigaDemo(IServiceScopeFactory scopes, ReproductorDePartidos reproductor, OpcionesDemo opciones, ILogger<LigaDemo> log)
        : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(20);
        // Después de la última visita a la página se sigue jugando este tiempo, por si la persona sigue mirando.
        private static readonly TimeSpan SinVisitas = TimeSpan.FromMinutes(10);
        private bool _jugando;
        private DateTime? _termino;
        private DateTime _ultimaVisita = DateTime.MinValue;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _mirando = new();
        private readonly SemaphoreSlim _despertar = new(0, 1);
        // El reinicio y la revisión de la liga no pueden correr a la vez.
        private readonly SemaphoreSlim _ocupada = new(1, 1);

        public bool EsLaDemo(string? slug) => opciones.Activa && slug == opciones.Slug;

        // Alguien pidió datos de la liga de demo.
        public void Visita(string? slug)
        {
            if (!EsLaDemo(slug)) return;
            var estabaQuieta = !HayPublico;
            _ultimaVisita = DateTime.UtcNow;
            if (estabaQuieta) Despertar();
        }

        // Una conexión de SignalR sigue la liga de demo o uno de sus partidos.
        public void Entra(string conexion)
        {
            _ultimaVisita = DateTime.UtcNow;
            if (_mirando.TryAdd(conexion, 0) && _mirando.Count == 1) Despertar();
        }

        public void Sale(string conexion) => _mirando.TryRemove(conexion, out _);

        private bool HayPublico => !_mirando.IsEmpty || DateTime.UtcNow - _ultimaVisita < SinVisitas;

        private void Despertar()
        {
            try { _despertar.Release(); }
            catch (SemaphoreFullException) { }
        }

        private static readonly string[] Equipos = ["Leones", "Tigres", "Halcones", "Toros", "Lobos", "Cóndores"];
        private static readonly string[] Apellidos =
        [
            "Ruiz", "Pérez", "Gómez", "Silva", "Castro", "Díaz", "Méndez", "Rossi", "Suárez", "Ferreira", "Lima", "Acosta",
            "Núñez", "Vega", "Sosa", "Romero", "Ortiz", "Cabrera", "Pereyra", "Rey", "Medina", "Benítez", "Rodríguez", "Martínez",
            "Fernández", "López", "González", "Sánchez", "Ramírez", "Torres", "Flores", "Rivera", "Morales", "Herrera", "Aguirre",
            "Molina", "Rojas", "Correa", "Ríos", "Álvarez", "Giménez", "Paz", "Cardozo", "Olivera", "Viera", "Techera", "Bentancor",
            "Duarte", "Píriz", "Machado", "Barrios", "Larrosa", "Cuello", "Lemos", "Sena", "Varela", "Arias", "Quintana", "Bruno", "Ibarra",
        ];
        private static readonly int[] Numeros = [4, 5, 6, 7, 8, 9, 10, 11, 12, 13];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!opciones.Activa) return;
            var primeraVuelta = true;
            var claveLista = opciones.Clave == null;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!claveLista)
                    {
                        await using var conClave = scopes.CreateAsyncScope();
                        await FijarClaveAsync(conClave.ServiceProvider);
                        claveLista = true;
                    }
                    // Sin nadie mirando no se hace nada; el partido que esté en curso termina solo.
                    if (!HayPublico)
                    {
                        await _despertar.WaitAsync(Intervalo, stoppingToken);
                        continue;
                    }
                    await using var scope = scopes.CreateAsyncScope();
                    await _ocupada.WaitAsync(stoppingToken);
                    try
                    {
                        await ReiniciarSiCorrespondeAsync(scope.ServiceProvider);
                        await RevisarAsync(scope.ServiceProvider, primeraVuelta);
                    }
                    finally { _ocupada.Release(); }
                    primeraVuelta = false;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    log.LogError(e, "Falló la liga de demo.");
                }
                await _despertar.WaitAsync(Intervalo, stoppingToken);
            }
        }

        // Lo que los visitantes cambiaron con la cuenta de la demo dura hasta el próximo reinicio.
        // Se llama mientras alguien mira la liga y cuando alguien entra con la cuenta de la demo.
        public async Task ReiniciarSiHaceFaltaAsync(IServiceProvider servicios, string usuario)
        {
            if (!opciones.Activa || usuario != opciones.Usuario) return;
            await _ocupada.WaitAsync();
            try { await ReiniciarSiCorrespondeAsync(servicios); }
            // Si falla, se entra igual con los datos que haya.
            catch (Exception e) { log.LogError(e, "No se pudieron reiniciar los datos de la demo."); }
            finally { _ocupada.Release(); }
        }

        private async Task ReiniciarSiCorrespondeAsync(IServiceProvider servicios)
        {
            if (opciones.HorasEntreReinicios <= 0) return;
            var db = servicios.GetRequiredService<BasketContext>();
            var creada = await db.Ligas.Where(l => l.Slug == opciones.Slug).Select(l => (DateTime?)l.CreadaEn).FirstOrDefaultAsync();
            if (creada == null || creada > DateTime.UtcNow.AddHours(-opciones.HorasEntreReinicios)) return;

            var usuarioId = await db.Usuarios.Where(u => u.Nombre == opciones.Usuario).Select(u => u.Id).FirstAsync();
            await db.EnTransaccionAsync(async () =>
            {
                var partidos = db.Partidos.Where(p => p.UsuarioId == usuarioId);
                // Primero se bloquean los partidos: si hay uno simulándose, su próxima jugada espera a que termine
                // el reinicio (y después falla porque el partido ya no existe) en lugar de colarse entre los borrados.
                await partidos.ExecuteUpdateAsync(p => p.SetProperty(x => x.Activo, false));
                await db.Jugadas.Where(j => partidos.Any(p => p.Id == j.PartidoId)).ExecuteDeleteAsync();
                await db.EstadisticasJugador.Where(e => partidos.Any(p => p.Id == e.PartidoId)).ExecuteDeleteAsync();
                await db.EstadisticasPartido.Where(e => partidos.Any(p => p.Id == e.PartidoId)).ExecuteDeleteAsync();
                await partidos.ExecuteDeleteAsync();
                await db.Inscripciones.Where(i => i.Temporada.Liga.UsuarioId == usuarioId).ExecuteDeleteAsync();
                await db.Divisiones.Where(d => d.Temporada.Liga.UsuarioId == usuarioId).ExecuteDeleteAsync();
                await db.Temporadas.Where(t => t.Liga.UsuarioId == usuarioId).ExecuteDeleteAsync();
                await db.Ligas.Where(l => l.UsuarioId == usuarioId).ExecuteDeleteAsync();
                await db.Jugadores.Where(j => j.UsuarioId == usuarioId).ExecuteDeleteAsync();
                await db.Equipos.Where(e => e.UsuarioId == usuarioId).ExecuteDeleteAsync();
            });
            _jugando = false;
            _termino = null;
            await CrearAsync(servicios, db);
            log.LogInformation("Se reiniciaron los datos de la cuenta de la demo.");
        }

        private async Task FijarClaveAsync(IServiceProvider servicios)
        {
            var db = servicios.GetRequiredService<BasketContext>();
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Nombre == opciones.Usuario);
            if (usuario == null)
            {
                await servicios.GetRequiredService<IUserService>().RegistrarAsync(new() { Usuario = opciones.Usuario, Pass = opciones.Clave! });
                return;
            }
            var hasher = servicios.GetRequiredService<IPasswordHasher<Usuario>>();
            if (hasher.VerifyHashedPassword(usuario, usuario.Pass, opciones.Clave!) != PasswordVerificationResult.Failed) return;
            usuario.Pass = hasher.HashPassword(usuario, opciones.Clave!);
            await db.SaveChangesAsync();
        }

        private async Task RevisarAsync(IServiceProvider servicios, bool alArrancar)
        {
            var db = servicios.GetRequiredService<BasketContext>();
            var temporada = await TemporadaActualAsync(servicios, db);
            var partidos = db.Partidos.Where(p => p.TemporadaId == temporada.Id && p.Activo);

            // La simulación vive en memoria: si la API se reinició, el partido que estaba en curso quedó a medias y se vuelve a jugar.
            if (alArrancar)
            {
                foreach (var cortado in await partidos.Where(p => p.Estado == EstadoPartido.EnCurso).ToListAsync())
                {
                    await db.Jugadas.Where(j => j.PartidoId == cortado.Id).ExecuteDeleteAsync();
                    cortado.Estado = EstadoPartido.Programado;
                }
                await db.SaveChangesAsync();
            }

            if (await partidos.AnyAsync(p => p.Estado == EstadoPartido.EnCurso))
            {
                _jugando = true;
                return;
            }
            // Entre partido y partido hay una pausa, para que se vea el resultado final.
            if (_jugando)
            {
                _jugando = false;
                _termino = DateTime.UtcNow;
            }
            if (_termino > DateTime.UtcNow.AddMinutes(-opciones.MinutosEntrePartidos)) return;

            var siguiente = await partidos.Where(p => p.Estado == EstadoPartido.Programado)
                .OrderBy(p => p.Jornada).ThenBy(p => p.Id).FirstOrDefaultAsync();
            if (siguiente == null)
            {
                await NuevaTemporadaAsync(db, temporada);
                return;
            }
            siguiente.Fecha = DateTime.UtcNow;
            await reproductor.EmpezarAsync(db, servicios.GetRequiredService<AvisosEnVivo>(), siguiente,
                TimeSpan.FromSeconds(opciones.SegundosEntreJugadas));
        }

        private async Task<Temporada> TemporadaActualAsync(IServiceProvider servicios, BasketContext db)
        {
            var liga = await db.Ligas.Include(l => l.Temporadas).FirstOrDefaultAsync(l => l.Slug == opciones.Slug)
                ?? await CrearAsync(servicios, db);
            return liga.Temporadas.OrderByDescending(t => t.Id).First();
        }

        private async Task<Liga> CrearAsync(IServiceProvider servicios, BasketContext db)
        {
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Nombre == opciones.Usuario);
            if (usuario == null)
            {
                await servicios.GetRequiredService<IUserService>().RegistrarAsync(new()
                {
                    Usuario = opciones.Usuario,
                    Pass = opciones.Clave ?? Guid.NewGuid().ToString(),
                });
                usuario = await db.Usuarios.FirstAsync(u => u.Nombre == opciones.Usuario);
            }

            var apellido = 0;
            var equipos = Equipos.Select(nombre => new Equipo
            {
                Nombre = nombre,
                UsuarioId = usuario.Id,
                Activo = true,
                Jugadores = Numeros.Select(numero => new Jugador
                {
                    Nombre = Apellidos[apellido++ % Apellidos.Length],
                    Numero = numero,
                    UsuarioId = usuario.Id,
                    Activo = true,
                }).ToList(),
            }).ToList();
            db.Equipos.AddRange(equipos);

            var liga = new Liga
            {
                Nombre = "Liga de demo",
                Slug = opciones.Slug,
                Descripcion = "Partidos simulados que se juegan solos, uno detrás de otro, para ver StatsApp en vivo.",
                UsuarioId = usuario.Id,
            };
            db.Ligas.Add(liga);
            await db.SaveChangesAsync();
            await NuevaTemporadaAsync(db, liga, equipos.Select(e => e.Id).ToList(), 1);
            log.LogInformation("Se creó la liga de demo {Slug}.", opciones.Slug);
            return liga;
        }

        private async Task NuevaTemporadaAsync(BasketContext db, Temporada anterior)
        {
            var equipos = await db.Inscripciones.Where(i => i.TemporadaId == anterior.Id).Select(i => i.EquipoId).ToListAsync();
            var liga = await db.Ligas.Include(l => l.Temporadas).FirstAsync(l => l.Id == anterior.LigaId);
            await NuevaTemporadaAsync(db, liga, equipos, liga.Temporadas.Count + 1);
        }

        private static async Task NuevaTemporadaAsync(BasketContext db, Liga liga, List<int> equipos, int numero)
        {
            var temporada = new Temporada { LigaId = liga.Id, Nombre = $"Temporada {numero}" };
            temporada.Inscripciones.AddRange(equipos.Select(id => new Inscripcion { EquipoId = id }));
            temporada.Partidos.AddRange(TablaLiga.TodosContraTodos(equipos, idaYVuelta: true).Select(c => new Partido
            {
                UsuarioId = liga.UsuarioId,
                LocalId = c.LocalId,
                VisitanteId = c.VisitanteId,
                Jornada = c.Jornada,
                Estado = EstadoPartido.Programado,
                Activo = true,
            }));
            liga.Temporadas.Add(temporada);
            await db.SaveChangesAsync();
        }
    }
}
