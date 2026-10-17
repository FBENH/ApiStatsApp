using apiBask.Services;
using Microsoft.EntityFrameworkCore.Storage;

namespace apiBask.Tests;

public class TransaccionesTests
{
    [Fact]
    public void Agotar_los_reintentos_cuenta_como_base_no_disponible()
    {
        var error = new InvalidOperationException("envoltorio", new RetryLimitExceededException("sin respuesta"));

        Assert.True(Transacciones.BaseNoDisponible(error));
    }

    [Fact]
    public void Otros_errores_no_cuentan_como_base_no_disponible()
    {
        Assert.False(Transacciones.BaseNoDisponible(new InvalidOperationException("otro")));
        Assert.False(Transacciones.BaseNoDisponible(null));
    }
}
