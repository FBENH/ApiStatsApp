using System;
using System.Collections.Generic;

namespace apiBask.Models;

public partial class Jugador
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Posicion { get; set; }

    public int? EquipoId { get; set; }

    public string? Foto { get; set; }

    public decimal? Altura { get; set; }

    public decimal? Peso { get; set; }

    public string IdUsuario { get; set; } = null!;

    public bool Activo { get; set; }

    public string? Equipo { get; set; }

    public int Numero { get; set; }

    public virtual Equipo? EquipoNavigation { get; set; }

    public virtual ICollection<EstadisticaJugador> EstadisticaJugadors { get; set; } = new List<EstadisticaJugador>();

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
