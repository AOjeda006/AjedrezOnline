namespace ServidorAjedrez.Domain.Enums
{
    /// <summary>
    /// Tipo de pieza de ajedrez. Determina las reglas de movimiento que aplica el
    /// motor en <see cref="ServidorAjedrez.Domain.Entities.Tablero"/> y el destino
    /// válido de una promoción de peón.
    /// </summary>
    /// <remarks>
    /// Se serializa como texto (p. ej. "Reina") hacia el cliente. El cliente envía el
    /// tipo elegido al coronar mediante el nombre de este enum
    /// (véase <c>AjedrezHub.PromocionarPeon</c>).
    /// </remarks>
    public enum TipoPieza
    {
        /// <summary>Peón. Único tipo que puede promocionar al alcanzar la última fila.</summary>
        Peon = 0,

        /// <summary>Torre. Se mueve en líneas rectas; interviene en el enroque junto al rey.</summary>
        Torre = 1,

        /// <summary>Caballo. Único que salta por encima de otras piezas.</summary>
        Caballo = 2,

        /// <summary>Alfil. Se mueve en diagonal.</summary>
        Alfil = 3,

        /// <summary>Dama. Combina los movimientos de torre y alfil.</summary>
        Reina = 4,

        /// <summary>Rey. Su jaque mate determina el fin de la partida; no es destino válido de promoción.</summary>
        Rey = 5
    }
}
