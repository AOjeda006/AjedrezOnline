using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.Enums;
using ServidorAjedrez.Domain.ValueObjects;

namespace ServidorAjedrez.Domain.Entities
{
    /// <summary>
    /// Tablero de ajedrez y motor de reglas: mantiene las piezas y el historial de movimientos,
    /// genera los movimientos legales y detecta el jaque y el jaque mate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Coordenadas en base cero alineadas con el frontend: las blancas ocupan las filas 6–7 y las
    /// negras las filas 0–1; las blancas avanzan hacia filas menores.
    /// </para>
    /// <para>
    /// Las piezas capturadas no se quitan de la colección: se marcan como
    /// <see cref="Pieza.Eliminada"/> (borrado lógico), lo que permite simular y deshacer
    /// movimientos. La generación distingue dos niveles: los movimientos <em>pseudo-legales</em>
    /// (según el patrón de cada pieza) y los <em>legales</em>, que además descartan los que
    /// dejarían al propio rey en jaque (véase <see cref="ObtenerMovimientosPosibles"/>).
    /// </para>
    /// </remarks>
    public class Tablero
    {
        private List<Pieza> _piezas;
        private List<Movimiento> _historialMovimientos;

        /// <summary>Vista de solo lectura de todas las piezas, incluidas las capturadas (<see cref="Pieza.Eliminada"/>).</summary>
        public IReadOnlyList<Pieza> Piezas => _piezas.AsReadOnly();

        /// <summary>
        /// Vista de solo lectura del historial de movimientos, en orden cronológico. El último
        /// movimiento se consulta para detectar la captura al paso.
        /// </summary>
        public IReadOnlyList<Movimiento> HistorialMovimientos => _historialMovimientos.AsReadOnly();

        /// <summary>
        /// Crea un tablero vacío, sin piezas ni historial. Usa <see cref="InicializarPiezas"/>
        /// para colocar la posición inicial.
        /// </summary>
        public Tablero()
        {
            _piezas = new List<Pieza>();
            _historialMovimientos = new List<Movimiento>();
        }

        /// <summary>
        /// Vacía el tablero y coloca las 32 piezas en la posición inicial estándar.
        /// </summary>
        /// <remarks>
        /// También limpia el historial, por lo que sirve tanto para empezar una partida como para
        /// reiniciarla (revancha). La disposición sigue las coordenadas alineadas con el frontend:
        /// blancas en las filas 6–7 y negras en las filas 0–1.
        /// </remarks>
        public void InicializarPiezas()
        {
            _piezas.Clear();
            _historialMovimientos.Clear();

            // Coordenadas alineadas con el frontend:
            // Blancas en filas 7/6, Negras en filas 0/1

            // Peones blancos (fila 6)
            for (int i = 0; i < 8; i++)
                _piezas.Add(Pieza.Create(TipoPieza.Peon, Color.Blanca, Posicion.Create(6, i)));

            // Peones negros (fila 1)
            for (int i = 0; i < 8; i++)
                _piezas.Add(Pieza.Create(TipoPieza.Peon, Color.Negra, Posicion.Create(1, i)));

            // Piezas blancas (fila 7)
            _piezas.Add(Pieza.Create(TipoPieza.Torre, Color.Blanca, Posicion.Create(7, 0)));
            _piezas.Add(Pieza.Create(TipoPieza.Caballo, Color.Blanca, Posicion.Create(7, 1)));
            _piezas.Add(Pieza.Create(TipoPieza.Alfil, Color.Blanca, Posicion.Create(7, 2)));
            _piezas.Add(Pieza.Create(TipoPieza.Reina, Color.Blanca, Posicion.Create(7, 3)));
            _piezas.Add(Pieza.Create(TipoPieza.Rey, Color.Blanca, Posicion.Create(7, 4)));
            _piezas.Add(Pieza.Create(TipoPieza.Alfil, Color.Blanca, Posicion.Create(7, 5)));
            _piezas.Add(Pieza.Create(TipoPieza.Caballo, Color.Blanca, Posicion.Create(7, 6)));
            _piezas.Add(Pieza.Create(TipoPieza.Torre, Color.Blanca, Posicion.Create(7, 7)));

            // Piezas negras (fila 0)
            _piezas.Add(Pieza.Create(TipoPieza.Torre, Color.Negra, Posicion.Create(0, 0)));
            _piezas.Add(Pieza.Create(TipoPieza.Caballo, Color.Negra, Posicion.Create(0, 1)));
            _piezas.Add(Pieza.Create(TipoPieza.Alfil, Color.Negra, Posicion.Create(0, 2)));
            _piezas.Add(Pieza.Create(TipoPieza.Reina, Color.Negra, Posicion.Create(0, 3)));
            _piezas.Add(Pieza.Create(TipoPieza.Rey, Color.Negra, Posicion.Create(0, 4)));
            _piezas.Add(Pieza.Create(TipoPieza.Alfil, Color.Negra, Posicion.Create(0, 5)));
            _piezas.Add(Pieza.Create(TipoPieza.Caballo, Color.Negra, Posicion.Create(0, 6)));
            _piezas.Add(Pieza.Create(TipoPieza.Torre, Color.Negra, Posicion.Create(0, 7)));
        }

        /// <summary>Devuelve la pieza activa (no capturada) situada en una casilla.</summary>
        /// <param name="posicion">Casilla a consultar.</param>
        /// <returns>La pieza en esa casilla, o <c>null</c> si está vacía.</returns>
        public Pieza? ObtenerPieza(Posicion posicion)
        {
            return _piezas.FirstOrDefault(p => p.Posicion.Equals(posicion) && !p.Eliminada);
        }

        /// <summary>Busca una pieza por su identificador, incluidas las capturadas.</summary>
        /// <remarks>
        /// A diferencia de <see cref="ObtenerPieza"/>, encuentra también piezas marcadas como
        /// <see cref="Pieza.Eliminada"/>; es lo que permite restaurarlas al deshacer un movimiento.
        /// </remarks>
        /// <param name="piezaId">Identificador de la pieza.</param>
        /// <returns>La pieza, o <c>null</c> si ningún elemento tiene ese id.</returns>
        public Pieza? ObtenerPiezaPorId(string piezaId)
        {
            return _piezas.FirstOrDefault(p => p.Id == piezaId);
        }

        /// <summary>Devuelve las piezas activas (no capturadas) de un color.</summary>
        /// <param name="color">Bando cuyas piezas se quieren obtener.</param>
        /// <returns>Lista (posiblemente vacía) de las piezas en juego de ese color.</returns>
        public List<Pieza> ObtenerPiezasPorColor(Color color)
        {
            return _piezas.Where(p => p.Color == color && !p.Eliminada).ToList();
        }

        /// <summary>Añade una pieza a la colección del tablero, sin validar su casilla.</summary>
        /// <param name="pieza">Pieza a incorporar.</param>
        public void AgregarPieza(Pieza pieza)
        {
            _piezas.Add(pieza);
        }

        /// <summary>Captura (borra lógicamente) la pieza con el id indicado; no hace nada si no existe.</summary>
        /// <param name="piezaId">Identificador de la pieza a retirar.</param>
        public void RemoverPieza(string piezaId)
        {
            var pieza = ObtenerPiezaPorId(piezaId);
            if (pieza != null)
                pieza.Eliminar();
        }

        /// <summary>Registra un movimiento al final del historial.</summary>
        /// <param name="movimiento">Movimiento a añadir.</param>
        public void RegistrarMovimiento(Movimiento movimiento)
        {
            _historialMovimientos.Add(movimiento);
        }

        /// <summary>Elimina el último movimiento del historial; no hace nada si está vacío. Se usa al deshacer.</summary>
        public void RemoverUltimoMovimiento()
        {
            if (_historialMovimientos.Count > 0)
                _historialMovimientos.RemoveAt(_historialMovimientos.Count - 1);
        }

        /// <summary>Devuelve el último movimiento registrado, o <c>null</c> si no hay ninguno.</summary>
        /// <returns>El movimiento más reciente, base para detectar la captura al paso.</returns>
        public Movimiento? ObtenerUltimoMovimiento()
        {
            return _historialMovimientos.LastOrDefault();
        }

        /// <summary>
        /// Devuelve los movimientos legales de una pieza.
        /// </summary>
        /// <remarks>
        /// Parte de los movimientos pseudo-legales y descarta los que dejarían al propio rey en
        /// jaque: clavadas, no responder a un jaque existente o llevar al rey a una casilla atacada.
        /// </remarks>
        /// <param name="pieza">Pieza cuyos movimientos se calculan.</param>
        /// <returns>Lista (posiblemente vacía) de casillas de destino legales.</returns>
        public List<Posicion> ObtenerMovimientosPosibles(Pieza pieza)
        {
            return ObtenerMovimientosPseudoLegales(pieza)
                .Where(m => m.EsValida() && !MovimientoDejaEnJaque(pieza, m))
                .ToList();
        }

        /// <summary>
        /// Genera los movimientos pseudo-legales de una pieza: los que permite su patrón de
        /// movimiento, sin comprobar todavía si dejan al propio rey en jaque.
        /// </summary>
        /// <param name="pieza">Pieza cuyos movimientos se generan.</param>
        /// <returns>Casillas alcanzables según el tipo de la pieza, todas dentro del tablero.</returns>
        private List<Posicion> ObtenerMovimientosPseudoLegales(Pieza pieza)
        {
            var movimientos = new List<Posicion>();

            switch (pieza.Tipo)
            {
                case TipoPieza.Peon:
                    movimientos.AddRange(ObtenerMovimientoPeon(pieza));
                    break;
                case TipoPieza.Torre:
                    movimientos.AddRange(ObtenerMovimientosRectos(pieza));
                    break;
                case TipoPieza.Caballo:
                    movimientos.AddRange(ObtenerMovimientoCaballo(pieza));
                    break;
                case TipoPieza.Alfil:
                    movimientos.AddRange(ObtenerMovimientosDiagonales(pieza));
                    break;
                case TipoPieza.Reina:
                    movimientos.AddRange(ObtenerMovimientosRectos(pieza));
                    movimientos.AddRange(ObtenerMovimientosDiagonales(pieza));
                    break;
                case TipoPieza.Rey:
                    movimientos.AddRange(ObtenerMovimientoRey(pieza));
                    break;
            }

            return movimientos.Where(m => m.EsValida()).ToList();
        }

        /// <summary>
        /// Calcula los movimientos pseudo-legales de un peón: avance simple, avance doble desde su
        /// fila inicial, capturas en diagonal y captura al paso.
        /// </summary>
        /// <param name="pieza">Peón cuyos movimientos se generan.</param>
        /// <returns>Casillas alcanzables por el peón.</returns>
        private List<Posicion> ObtenerMovimientoPeon(Pieza pieza)
        {
            var movimientos = new List<Posicion>();
            // Blancas avanzan de fila 6 hacia 0 (-1); negras de fila 1 hacia 7 (+1)
            var direccion = pieza.Color == Color.Blanca ? -1 : 1;
            var filaInicial = pieza.Color == Color.Blanca ? 6 : 1;

            var filaSiguiente = pieza.Posicion.Fila + direccion;
            var posicionDelante = new Posicion(filaSiguiente, pieza.Posicion.Columna);

            if (posicionDelante.EsValida() && ObtenerPieza(posicionDelante) == null)
            {
                movimientos.Add(posicionDelante);

                // Movimiento doble desde posición inicial
                if (pieza.Posicion.Fila == filaInicial)
                {
                    var posicionDoble = new Posicion(filaSiguiente + direccion, pieza.Posicion.Columna);
                    if (posicionDoble.EsValida() && ObtenerPieza(posicionDoble) == null)
                        movimientos.Add(posicionDoble);
                }
            }

            // Captura en diagonal
            for (int col = -1; col <= 1; col += 2)
            {
                var posicionCaptura = new Posicion(filaSiguiente, pieza.Posicion.Columna + col);
                if (posicionCaptura.EsValida())
                {
                    var piezaCaptura = ObtenerPieza(posicionCaptura);
                    if (piezaCaptura != null && piezaCaptura.Color != pieza.Color)
                        movimientos.Add(posicionCaptura);
                }
            }

            // Captura al paso (en passant)
            AgregarCapturasAlPaso(pieza, movimientos, direccion);

            return movimientos;
        }

        /// <summary>
        /// Añade la captura al paso a la lista de movimientos si el último movimiento fue el avance
        /// doble de un peón rival situado en una columna adyacente.
        /// </summary>
        /// <param name="pieza">Peón que podría capturar al paso.</param>
        /// <param name="movimientos">Lista de destinos en construcción; se le añade la captura si procede.</param>
        /// <param name="direccion">Sentido de avance del peón: -1 para blancas, +1 para negras.</param>
        private void AgregarCapturasAlPaso(Pieza pieza, List<Posicion> movimientos, int direccion)
        {
            var ultimo = ObtenerUltimoMovimiento();
            if (ultimo == null) return;

            var piezaMovida = ObtenerPiezaPorId(ultimo.PiezaId);
            if (piezaMovida == null || piezaMovida.Tipo != TipoPieza.Peon || piezaMovida.Color == pieza.Color)
                return;

            var distancia = Math.Abs(ultimo.Destino.Fila - ultimo.Origen.Fila);
            if (distancia != 2) return;

            if (piezaMovida.Posicion.Fila == pieza.Posicion.Fila &&
                Math.Abs(piezaMovida.Posicion.Columna - pieza.Posicion.Columna) == 1)
            {
                var destino = new Posicion(pieza.Posicion.Fila + direccion, piezaMovida.Posicion.Columna);
                if (destino.EsValida())
                    movimientos.Add(destino);
            }
        }

        /// <summary>
        /// Genera los desplazamientos en línea recta (horizontal y vertical) de la torre y la reina,
        /// deteniéndose en la primera pieza encontrada; si esa pieza es rival, su casilla es capturable.
        /// </summary>
        /// <param name="pieza">Pieza que se desplaza en línea recta.</param>
        /// <returns>Casillas alcanzables en las cuatro direcciones rectas.</returns>
        private List<Posicion> ObtenerMovimientosRectos(Pieza pieza)
        {
            var movimientos = new List<Posicion>();
            var pos = pieza.Posicion;

            var direcciones = new int[][] { new int[] { 0, 1 }, new int[] { 0, -1 }, new int[] { 1, 0 }, new int[] { -1, 0 } };

            foreach (var dir in direcciones)
            {
                for (int i = 1; i < 8; i++)
                {
                    var nuevaPos = new Posicion(pos.Fila + (dir[0] * i), pos.Columna + (dir[1] * i));
                    if (!nuevaPos.EsValida())
                        break;

                    var piezaEnPosicion = ObtenerPieza(nuevaPos);
                    if (piezaEnPosicion == null)
                        movimientos.Add(nuevaPos);
                    else
                    {
                        if (piezaEnPosicion.Color != pieza.Color)
                            movimientos.Add(nuevaPos);
                        break;
                    }
                }
            }

            return movimientos;
        }

        /// <summary>
        /// Genera los desplazamientos en diagonal del alfil y la reina, deteniéndose en la primera
        /// pieza encontrada; si esa pieza es rival, su casilla es capturable.
        /// </summary>
        /// <param name="pieza">Pieza que se desplaza en diagonal.</param>
        /// <returns>Casillas alcanzables en las cuatro diagonales.</returns>
        private List<Posicion> ObtenerMovimientosDiagonales(Pieza pieza)
        {
            var movimientos = new List<Posicion>();
            var pos = pieza.Posicion;

            var direcciones = new int[][] { new int[] { 1, 1 }, new int[] { 1, -1 }, new int[] { -1, 1 }, new int[] { -1, -1 } };

            foreach (var dir in direcciones)
            {
                for (int i = 1; i < 8; i++)
                {
                    var nuevaPos = new Posicion(pos.Fila + (dir[0] * i), pos.Columna + (dir[1] * i));
                    if (!nuevaPos.EsValida())
                        break;

                    var piezaEnPosicion = ObtenerPieza(nuevaPos);
                    if (piezaEnPosicion == null)
                        movimientos.Add(nuevaPos);
                    else
                    {
                        if (piezaEnPosicion.Color != pieza.Color)
                            movimientos.Add(nuevaPos);
                        break;
                    }
                }
            }

            return movimientos;
        }

        /// <summary>
        /// Genera los ocho saltos en "L" del caballo que caen dentro del tablero y no terminan sobre
        /// una pieza propia.
        /// </summary>
        /// <param name="pieza">Caballo cuyos saltos se generan.</param>
        /// <returns>Casillas alcanzables por el caballo.</returns>
        private List<Posicion> ObtenerMovimientoCaballo(Pieza pieza)
        {
            var movimientos = new List<Posicion>();
            var pos = pieza.Posicion;

            int[][] saltos = new int[][]
            {
                new int[] { 2, 1 }, new int[] { 2, -1 },
                new int[] { -2, 1 }, new int[] { -2, -1 },
                new int[] { 1, 2 }, new int[] { 1, -2 },
                new int[] { -1, 2 }, new int[] { -1, -2 }
            };

            foreach (var salto in saltos)
            {
                var nuevaPos = new Posicion(pos.Fila + salto[0], pos.Columna + salto[1]);
                if (nuevaPos.EsValida())
                {
                    var piezaEnPosicion = ObtenerPieza(nuevaPos);
                    if (piezaEnPosicion == null || piezaEnPosicion.Color != pieza.Color)
                        movimientos.Add(nuevaPos);
                }
            }

            return movimientos;
        }

        /// <summary>
        /// Calcula los movimientos pseudo-legales del rey: las casillas adyacentes y los enroques
        /// disponibles.
        /// </summary>
        /// <param name="pieza">Rey cuyos movimientos se generan.</param>
        /// <returns>Casillas adyacentes alcanzables más, si proceden, los destinos de enroque.</returns>
        private List<Posicion> ObtenerMovimientoRey(Pieza pieza)
        {
            var movimientos = new List<Posicion>();
            movimientos.AddRange(ObtenerCasillasAdyacentesRey(pieza));
            AgregarEnroques(pieza, movimientos);
            return movimientos;
        }

        /// <summary>
        /// Devuelve las hasta 8 casillas contiguas a las que el rey puede ir, sin considerar el
        /// enroque y excluyendo las ocupadas por piezas propias.
        /// </summary>
        /// <param name="pieza">Rey cuyas casillas adyacentes se calculan.</param>
        /// <returns>Casillas adyacentes alcanzables.</returns>
        private List<Posicion> ObtenerCasillasAdyacentesRey(Pieza pieza)
        {
            var movimientos = new List<Posicion>();
            var pos = pieza.Posicion;

            for (int f = -1; f <= 1; f++)
            {
                for (int c = -1; c <= 1; c++)
                {
                    if (f == 0 && c == 0)
                        continue;

                    var nuevaPos = new Posicion(pos.Fila + f, pos.Columna + c);
                    if (nuevaPos.EsValida())
                    {
                        var piezaEnPosicion = ObtenerPieza(nuevaPos);
                        if (piezaEnPosicion == null || piezaEnPosicion.Color != pieza.Color)
                            movimientos.Add(nuevaPos);
                    }
                }
            }

            return movimientos;
        }

        /// <summary>
        /// Añade a la lista los enroques disponibles (corto y largo) cuando se cumplen sus reglas.
        /// </summary>
        /// <remarks>
        /// Requiere que el rey no se haya movido ni esté en jaque; cada enroque concreto comprueba
        /// además la torre, que las casillas intermedias estén vacías y que el rey no cruce ni
        /// aterrice en una casilla atacada (véase <see cref="IntentarEnroque"/>).
        /// </remarks>
        /// <param name="rey">Rey que podría enrocar.</param>
        /// <param name="movimientos">Lista de destinos en construcción a la que se añaden los enroques válidos.</param>
        private void AgregarEnroques(Pieza rey, List<Posicion> movimientos)
        {
            if (rey.SeHaMovido) return;
            if (HayJaque(rey.Color)) return;

            var fila = rey.Posicion.Fila;
            // Enroque corto: torre en columna 7, el rey va a la 6 cruzando la 5
            IntentarEnroque(rey, fila, columnaTorre: 7, columnaDestinoRey: 6, columnaPaso: 5, movimientos);
            // Enroque largo: torre en columna 0, el rey va a la 2 cruzando la 3
            IntentarEnroque(rey, fila, columnaTorre: 0, columnaDestinoRey: 2, columnaPaso: 3, movimientos);
        }

        /// <summary>
        /// Comprueba un enroque concreto y, si es legal, añade el destino del rey a la lista.
        /// </summary>
        /// <remarks>
        /// La torre debe seguir en su columna sin haberse movido, las casillas entre el rey y la
        /// torre deben estar vacías, y ni la casilla de paso ni la de destino del rey pueden estar
        /// atacadas.
        /// </remarks>
        /// <param name="rey">Rey que enroca.</param>
        /// <param name="fila">Fila en la que se produce el enroque (la del rey).</param>
        /// <param name="columnaTorre">Columna donde debe estar la torre (7 para el corto, 0 para el largo).</param>
        /// <param name="columnaDestinoRey">Columna a la que llega el rey (6 para el corto, 2 para el largo).</param>
        /// <param name="columnaPaso">Columna intermedia que el rey cruza y que no puede estar atacada.</param>
        /// <param name="movimientos">Lista de destinos en construcción; recibe el destino del rey si el enroque es legal.</param>
        private void IntentarEnroque(Pieza rey, int fila, int columnaTorre, int columnaDestinoRey, int columnaPaso, List<Posicion> movimientos)
        {
            var torre = ObtenerPieza(new Posicion(fila, columnaTorre));
            if (torre == null || torre.Tipo != TipoPieza.Torre || torre.Color != rey.Color || torre.SeHaMovido)
                return;

            // Todas las casillas entre el rey y la torre deben estar vacías
            var desde = Math.Min(rey.Posicion.Columna, columnaTorre);
            var hasta = Math.Max(rey.Posicion.Columna, columnaTorre);
            for (int col = desde + 1; col < hasta; col++)
            {
                if (ObtenerPieza(new Posicion(fila, col)) != null)
                    return;
            }

            // El rey no puede cruzar ni aterrizar en una casilla atacada
            if (EstaCasillaAtacada(new Posicion(fila, columnaPaso), rey.Color)) return;
            if (EstaCasillaAtacada(new Posicion(fila, columnaDestinoRey), rey.Color)) return;

            movimientos.Add(new Posicion(fila, columnaDestinoRey));
        }

        /// <summary>
        /// Indica si un movimiento es legal en la posición actual.
        /// </summary>
        /// <remarks>
        /// Comprueba que la pieza exista y no esté capturada, que parta de la casilla de origen
        /// declarada y que el destino figure entre sus movimientos legales.
        /// </remarks>
        /// <param name="movimiento">Movimiento a validar.</param>
        /// <returns><c>true</c> si el movimiento es legal; en caso contrario, <c>false</c>.</returns>
        public bool EsMovimientoValido(Movimiento movimiento)
        {
            var pieza = ObtenerPiezaPorId(movimiento.PiezaId);
            if (pieza == null || pieza.Eliminada)
                return false;

            if (!pieza.Posicion.Equals(movimiento.Origen))
                return false;

            var movimientosPosibles = ObtenerMovimientosPosibles(pieza);
            return movimientosPosibles.Any(m => m.Equals(movimiento.Destino));
        }

        /// <summary>
        /// Indica si el rey del color dado está en jaque (su casilla está atacada por el rival).
        /// </summary>
        /// <remarks>
        /// Usa la generación de ataques "en crudo" (<see cref="ObtenerAtaques"/>) en lugar de los
        /// movimientos legales, para evitar la recursión con el propio filtro de jaque.
        /// </remarks>
        /// <param name="color">Color del rey que se comprueba.</param>
        /// <returns><c>true</c> si ese rey está en jaque; <c>false</c> si no lo está o no hay rey de ese color.</returns>
        public bool HayJaque(Color color)
        {
            var rey = ObtenerPiezasPorColor(color).FirstOrDefault(p => p.Tipo == TipoPieza.Rey);
            if (rey == null)
                return false;

            return EstaCasillaAtacada(rey.Posicion, color);
        }

        /// <summary>
        /// Indica si una casilla está atacada por alguna pieza del color contrario al defensor.
        /// </summary>
        /// <param name="casilla">Casilla a evaluar.</param>
        /// <param name="colorDefensor">Color que defiende; se consideran atacantes las piezas del color opuesto.</param>
        /// <returns><c>true</c> si alguna pieza rival ataca la casilla; en caso contrario, <c>false</c>.</returns>
        private bool EstaCasillaAtacada(Posicion casilla, Color colorDefensor)
        {
            var colorAtacante = colorDefensor == Color.Blanca ? Color.Negra : Color.Blanca;

            foreach (var pieza in ObtenerPiezasPorColor(colorAtacante))
            {
                if (ObtenerAtaques(pieza).Any(m => m.Equals(casilla)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Devuelve las casillas que una pieza ataca, que no siempre coinciden con sus movimientos:
        /// el peón ataca solo en diagonal (no su avance) y el rey no amenaza mediante el enroque.
        /// </summary>
        /// <param name="pieza">Pieza cuyos ataques se calculan.</param>
        /// <returns>Casillas amenazadas por la pieza.</returns>
        private List<Posicion> ObtenerAtaques(Pieza pieza)
        {
            switch (pieza.Tipo)
            {
                case TipoPieza.Peon: return ObtenerAtaquesPeon(pieza);
                case TipoPieza.Torre: return ObtenerMovimientosRectos(pieza);
                case TipoPieza.Caballo: return ObtenerMovimientoCaballo(pieza);
                case TipoPieza.Alfil: return ObtenerMovimientosDiagonales(pieza);
                case TipoPieza.Reina:
                    var reina = ObtenerMovimientosRectos(pieza);
                    reina.AddRange(ObtenerMovimientosDiagonales(pieza));
                    return reina;
                case TipoPieza.Rey: return ObtenerCasillasAdyacentesRey(pieza);
                default: return new List<Posicion>();
            }
        }

        /// <summary>
        /// Devuelve las dos casillas en diagonal que un peón amenaza, con independencia de que
        /// estén ocupadas (a diferencia de la captura, que exige que haya una pieza rival).
        /// </summary>
        /// <param name="pieza">Peón cuyos ataques se calculan.</param>
        /// <returns>Las casillas diagonales delanteras que caen dentro del tablero.</returns>
        private List<Posicion> ObtenerAtaquesPeon(Pieza pieza)
        {
            var ataques = new List<Posicion>();
            var direccion = pieza.Color == Color.Blanca ? -1 : 1;
            var filaSiguiente = pieza.Posicion.Fila + direccion;

            for (int col = -1; col <= 1; col += 2)
            {
                var pos = new Posicion(filaSiguiente, pieza.Posicion.Columna + col);
                if (pos.EsValida())
                    ataques.Add(pos);
            }

            return ataques;
        }

        /// <summary>
        /// Simula un movimiento (incluida la captura al paso) y comprueba si dejaría en jaque al
        /// propio rey, restaurando después el estado del tablero.
        /// </summary>
        /// <remarks>
        /// Es la comprobación que convierte un movimiento pseudo-legal en legal. Desplaza la pieza
        /// y elimina la posible captura, evalúa el jaque y deshace ambos cambios mediante
        /// <see cref="Pieza.RevertirA"/> y <see cref="Pieza.Restaurar"/>.
        /// </remarks>
        /// <param name="pieza">Pieza que se movería.</param>
        /// <param name="destino">Casilla de destino simulada.</param>
        /// <returns><c>true</c> si tras el movimiento el propio rey quedaría en jaque; en caso contrario, <c>false</c>.</returns>
        private bool MovimientoDejaEnJaque(Pieza pieza, Posicion destino)
        {
            var posicionOriginal = pieza.Posicion;
            var seHaMovidoOriginal = pieza.SeHaMovido;

            // Detectar la pieza capturada (al paso: el peón no está en la casilla destino)
            bool esAlPaso = pieza.Tipo == TipoPieza.Peon
                && posicionOriginal.Columna != destino.Columna
                && ObtenerPieza(destino) == null;

            Pieza? piezaCapturada = esAlPaso
                ? ObtenerPieza(new Posicion(posicionOriginal.Fila, destino.Columna))
                : ObtenerPieza(destino);

            pieza.Mover(destino);
            piezaCapturada?.Eliminar();

            bool enJaque = HayJaque(pieza.Color);

            pieza.RevertirA(posicionOriginal, seHaMovidoOriginal);
            piezaCapturada?.Restaurar();

            return enJaque;
        }

        /// <summary>
        /// Indica si el color dado está en jaque mate: su rey está en jaque y ninguna de sus piezas
        /// dispone de un movimiento legal.
        /// </summary>
        /// <param name="color">Color que se comprueba.</param>
        /// <returns><c>true</c> si es jaque mate para ese color; en caso contrario, <c>false</c>.</returns>
        public bool HayJaqueMate(Color color)
        {
            if (!HayJaque(color))
                return false;

            foreach (var pieza in ObtenerPiezasPorColor(color))
            {
                if (ObtenerMovimientosPosibles(pieza).Count > 0)
                    return false;
            }

            return true;
        }

        /// <summary>Comprueba la invariante del tablero validando la posición de cada una de sus piezas.</summary>
        /// <exception cref="InvalidOperationException">Alguna pieza tiene una posición fuera del tablero.</exception>
        public void Validate()
        {
            foreach (var pieza in _piezas)
            {
                pieza.Validate();
            }
        }
    }
}
