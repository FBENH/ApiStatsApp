using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace apiBask.Services
{
    public static class Transacciones
    {
        // Con los reintentos de conexión activados (la base puede estar despertando), EF exige que una transacción
        // vaya entera dentro de la estrategia de reintentos: si falla por un error pasajero se repite todo el bloque.
        // Los cambios se aceptan recién después del commit, así un reintento vuelve a guardarlos.
        public static async Task EnTransaccionAsync(this BasketContext db, Func<Task> trabajo)
        {
            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaccion = await db.Database.BeginTransactionAsync();
                await trabajo();
                await db.SaveChangesAsync(acceptAllChangesOnSuccess: false);
                await transaccion.CommitAsync();
            });
            db.ChangeTracker.AcceptAllChanges();
        }

        // La base no respondió ni después de los reintentos (por ejemplo, porque sigue despertando).
        public static bool BaseNoDisponible(Exception? e) => e switch
        {
            null => false,
            RetryLimitExceededException => true,
            // 40613: la base de Azure SQL no está disponible (pausada o despertando).
            Microsoft.Data.SqlClient.SqlException { Number: 40613 } => true,
            _ => BaseNoDisponible(e.InnerException),
        };
    }
}
