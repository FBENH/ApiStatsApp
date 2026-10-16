using apiBask.Models;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Services
{
    public class OpcionesDemo
    {
        // Apagada salvo que se pida (Demo__Activa=true en el entorno).
        public bool Activa { get; set; }
        public string Slug { get; set; } = "liga-demo";
        public string Usuario { get; set; } = "demo";
        // Si no se configura, la cuenta de la demo queda con una clave al azar y nadie puede entrar con ella.
        public string? Clave { get; set; }
        public double SegundosEntreJugadas { get; set; } = 4;
        public double MinutosEntrePartidos { get; set; } = 5;
    }

    // Liga de demo que se juega sola: la crea si no existe y simula sus partidos de a uno, en orden del fixture.
    // Cuando se termina el fixture arranca otra temporada con los mismos equipos.
    public class LigaDemo(IServiceScopeFactory scopes, ReproductorDePartidos reproductor, OpcionesDemo opciones, ILogger<LigaDemo> log)
        : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(20);
        private bool _jugando;
        private DateTime? _termino;

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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    await RevisarAsync(scope.ServiceProvider, primeraVuelta);
                    primeraVuelta = false;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    log.LogError(e, "Falló la liga de demo.");
                }
                await Task.Delay(Intervalo, stoppingToken);
            }
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
