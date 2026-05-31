using ServidorAjedrez.Domain.Enums;
using ServidorAjedrez.Domain.ValueObjects;

namespace ServidorAjedrez.Domain.Entities
{
    /// <summary>
    /// Partida de ajedrez entre dos jugadores: raíz del agregado que orquesta el turno, el estado
    /// del juego, las ofertas de tablas y de revancha, y las condiciones de fin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Los movimientos siguen un esquema de confirmación en dos fases:
    /// <see cref="RealizarMovimiento"/> los aplica de forma tentativa y deja un
    /// <see cref="MovimientoPendiente"/>; después <see cref="ConfirmarMovimiento"/> los consolida
    /// (cambia el turno y evalúa el jaque mate) o <see cref="DeshacerMovimiento"/> los revierte.
    /// </para>
    /// <para>
    /// Para poder deshacer sin depender de los datos del cliente, la partida guarda su propio
    /// estado de reversión: la pieza capturada, el indicador "se ha movido" previo y el
    /// desplazamiento de la torre en un enroque. Los colores se reparten al azar al crearse y las
    /// blancas siempre mueven primero.
    /// </para>
    /// </remarks>
    public class Partida
    {
        private string _id;
        private string _salaId;
        private Tablero _tablero;
        private Jugador _jugadorBlancas;
        private Jugador _jugadorNegras;
        private Color _turnoActual;
        private int _numeroTurnos;
        private DateTime _fechaInicio;
        private TimeSpan _tiempoTranscurrido;
        private EstadoPartida _estado;
        private ResultadoPartida? _resultado;
        private TipoFinPartida? _tipoFin;
        private bool _tablasBlancas;
        private bool _tablasNegras;
        private bool _reinicioBlancas;
        private bool _reinicioNegras;
        private Movimiento? _movimientoPendiente;

        // Estado para poder deshacer el movimiento pendiente sin depender del DTO del cliente
        private Pieza? _capturaPendiente;
        private bool _seHaMovidoPiezaAntes;
        private (Pieza torre, Posicion origen, bool seHaMovido)? _enroquePendiente;

        /// <summary>Identidad estable de la partida (GUID generado al crearla).</summary>
        public string Id => _id;

        /// <summary>Id de la <see cref="Sala"/> en la que se disputa la partida.</summary>
        public string SalaId => _salaId;

        /// <summary>Tablero con las piezas y el motor de reglas de esta partida.</summary>
        public Tablero Tablero => _tablero;

        public Jugador JugadorBlancas => _jugadorBlancas;
        public Jugador JugadorNegras => _jugadorNegras;

        /// <summary>Color al que le toca mover.</summary>
        public Color TurnoActual => _turnoActual;

        /// <summary>Número de medios movimientos (plies) jugados desde el inicio de la partida.</summary>
        public int NumeroTurnos => _numeroTurnos;

        /// <summary>Instante de inicio de la partida, en UTC.</summary>
        public DateTime FechaInicio => _fechaInicio;

        /// <summary>Tiempo de juego acumulado, según lo reporta el cliente mediante <see cref="ActualizarTiempo"/>.</summary>
        public TimeSpan TiempoTranscurrido => _tiempoTranscurrido;

        /// <summary>Fase actual de la partida.</summary>
        public EstadoPartida Estado => _estado;

        /// <summary>Resultado final, o <c>null</c> mientras la partida no haya terminado.</summary>
        public ResultadoPartida? Resultado => _resultado;

        /// <summary>Motivo del fin de la partida, o <c>null</c> mientras siga en curso.</summary>
        public TipoFinPartida? TipoFin => _tipoFin;

        /// <summary>Indica si las blancas tienen una oferta de tablas en pie.</summary>
        public bool TablasBlancas => _tablasBlancas;

        /// <summary>Indica si las negras tienen una oferta de tablas en pie.</summary>
        public bool TablasNegras => _tablasNegras;

        /// <summary>Indica si las blancas han solicitado revancha.</summary>
        public bool ReinicioBlancas => _reinicioBlancas;

        /// <summary>Indica si las negras han solicitado revancha.</summary>
        public bool ReinicioNegras => _reinicioNegras;

        /// <summary>
        /// Movimiento aplicado de forma tentativa a la espera de confirmación, o <c>null</c> si no
        /// hay ninguno pendiente (véase <see cref="RealizarMovimiento"/>).
        /// </summary>
        public Movimiento? MovimientoPendiente => _movimientoPendiente;

        /// <summary>
        /// Crea una partida en estado <see cref="EstadoPartida.Esperando"/>, repartiendo los colores
        /// al azar entre los dos jugadores.
        /// </summary>
        /// <param name="salaId">Id de la sala a la que pertenece; no puede ser nulo ni estar en blanco.</param>
        /// <param name="jugador1">Primer jugador; no puede ser nulo.</param>
        /// <param name="jugador2">Segundo jugador; no puede ser nulo.</param>
        /// <exception cref="ArgumentException"><paramref name="salaId"/> es nulo o está en blanco.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="jugador1"/> o <paramref name="jugador2"/> son nulos.</exception>
        public Partida(string salaId, Jugador jugador1, Jugador jugador2)
        {
            if (string.IsNullOrWhiteSpace(salaId))
                throw new ArgumentException("El ID de la sala no puede estar vacío.");
            if (jugador1 == null || jugador2 == null)
                throw new ArgumentNullException("Los jugadores no pueden ser nulos.");

            _id = Guid.NewGuid().ToString();
            _salaId = salaId;
            _tablero = new Tablero();
            _numeroTurnos = 0;
            _fechaInicio = DateTime.UtcNow;
            _tiempoTranscurrido = TimeSpan.Zero;
            _estado = EstadoPartida.Esperando;
            _resultado = null;
            _tipoFin = null;
            _tablasBlancas = false;
            _tablasNegras = false;
            _reinicioBlancas = false;
            _reinicioNegras = false;
            _movimientoPendiente = null;

            // Asignar colores aleatoriamente
            AsignarColoresAleatorios(jugador1, jugador2);

            _jugadorBlancas = jugador1.Color == Color.Blanca ? jugador1 : jugador2;
            _jugadorNegras = jugador1.Color == Color.Blanca ? jugador2 : jugador1;
            _turnoActual = Color.Blanca;
        }

        /// <summary>Reparte al azar el color blanco y el negro entre los dos jugadores.</summary>
        private void AsignarColoresAleatorios(Jugador j1, Jugador j2)
        {
            var random = new Random();
            if (random.Next(2) == 0)
            {
                j1.AsignarColor(Color.Blanca);
                j2.AsignarColor(Color.Negra);
            }
            else
            {
                j1.AsignarColor(Color.Negra);
                j2.AsignarColor(Color.Blanca);
            }
        }

        /// <summary>
        /// Coloca la posición inicial y pone la partida <see cref="EstadoPartida.EnCurso"/>, con el
        /// turno en las blancas.
        /// </summary>
        /// <remarks>
        /// Reinicia también el resultado y el estado de reversión, por lo que sirve tanto para
        /// arrancar la primera partida como para empezar una revancha sobre la misma instancia.
        /// </remarks>
        public void IniciarPartida()
        {
            _tablero.InicializarPiezas();
            _estado = EstadoPartida.EnCurso;
            _numeroTurnos = 0;
            _turnoActual = Color.Blanca;

            // Limpiar el resultado de cualquier partida anterior (revancha)
            _resultado = null;
            _tipoFin = null;
            _movimientoPendiente = null;
            _capturaPendiente = null;
            _enroquePendiente = null;
        }

        /// <summary>
        /// Aplica un movimiento de forma tentativa: ejecuta la captura, el enroque y el
        /// desplazamiento, y lo deja como <see cref="MovimientoPendiente"/> a la espera de confirmación.
        /// </summary>
        /// <remarks>
        /// No cambia el turno (eso ocurre al confirmar). Antes de mover guarda el estado necesario
        /// para deshacer: la pieza capturada, el indicador "se ha movido" previo y, en el enroque,
        /// la torre con su posición original. La captura al paso se resuelve aquí, retirando el peón
        /// rival aunque no esté en la casilla de destino.
        /// </remarks>
        /// <param name="movimiento">Movimiento a aplicar; debe ser legal en la posición actual.</param>
        /// <param name="jugadorId">Id del jugador que mueve; debe coincidir con el del turno.</param>
        /// <exception cref="InvalidOperationException">
        /// La partida no está en curso, no es el turno del jugador, la pieza no existe o el
        /// movimiento no es legal.
        /// </exception>
        public void RealizarMovimiento(Movimiento movimiento, string jugadorId)
        {
            if (_estado != EstadoPartida.EnCurso)
                throw new InvalidOperationException("La partida no está en curso.");

            if (!EsTurnoJugador(jugadorId))
                throw new InvalidOperationException("No es el turno de este jugador.");

            var pieza = _tablero.ObtenerPiezaPorId(movimiento.PiezaId);
            if (pieza == null)
                throw new InvalidOperationException("La pieza no existe.");

            if (!_tablero.EsMovimientoValido(movimiento))
                throw new InvalidOperationException("El movimiento no es válido.");

            var origen = pieza.Posicion;
            _seHaMovidoPiezaAntes = pieza.SeHaMovido;

            bool esEnroque = pieza.Tipo == TipoPieza.Rey && Math.Abs(origen.Columna - movimiento.Destino.Columna) == 2;
            bool esAlPaso = pieza.Tipo == TipoPieza.Peon
                && origen.Columna != movimiento.Destino.Columna
                && _tablero.ObtenerPieza(movimiento.Destino) == null;

            // Determinar y capturar la pieza (al paso: el peón capturado no está en el destino)
            Pieza? capturada = esAlPaso
                ? _tablero.ObtenerPieza(new Posicion(origen.Fila, movimiento.Destino.Columna))
                : _tablero.ObtenerPieza(movimiento.Destino);

            if (capturada != null && capturada.Color == pieza.Color)
                capturada = null; // no se captura una pieza propia

            capturada?.Eliminar();
            _capturaPendiente = capturada;

            // Ejecutar movimiento del rey/pieza
            pieza.Mover(movimiento.Destino);

            // Enroque: mover también la torre
            _enroquePendiente = null;
            if (esEnroque)
            {
                bool enroqueCorto = movimiento.Destino.Columna == 6;
                var columnaTorre = enroqueCorto ? 7 : 0;
                var torre = _tablero.ObtenerPieza(new Posicion(origen.Fila, columnaTorre));
                if (torre != null)
                {
                    var origenTorre = torre.Posicion;
                    var torreMovidaAntes = torre.SeHaMovido;
                    torre.Mover(new Posicion(origen.Fila, enroqueCorto ? 5 : 3));
                    _enroquePendiente = (torre, origenTorre, torreMovidaAntes);
                }
            }

            _tablero.RegistrarMovimiento(movimiento);
            _movimientoPendiente = movimiento;
        }

        /// <summary>
        /// Confirma el <see cref="MovimientoPendiente"/>, cambia el turno y evalúa si hay jaque mate.
        /// </summary>
        /// <remarks>
        /// Si el movimiento conlleva promoción, conserva el estado pendiente para que
        /// <see cref="PromocionarPeon"/> complete la coronación; en caso contrario lo limpia. Cuando
        /// detecta jaque mate, finaliza la partida con <see cref="TipoFinPartida.JaqueMate"/>.
        /// </remarks>
        /// <param name="jugadorId">Id del jugador que confirma; debe coincidir con el del turno (el que acaba de mover).</param>
        /// <exception cref="InvalidOperationException">No hay movimiento pendiente, o no es el turno del jugador.</exception>
        public void ConfirmarMovimiento(string jugadorId)
        {
            if (_movimientoPendiente == null)
                throw new InvalidOperationException("No hay movimiento pendiente para confirmar.");

            if (!EsTurnoJugador(jugadorId))
                throw new InvalidOperationException("No es el turno de este jugador.");

            _movimientoPendiente.Confirmar();
            CambiarTurno();

            // Don't clear the pending movement if it's a promotion
            // It will be cleared after PromocionarPeon is called
            if (!_movimientoPendiente.EsPromocion)
            {
                _movimientoPendiente = null;
                _capturaPendiente = null;
                _enroquePendiente = null;
            }

            // Verificar si hay jaque mate
            if (_tablero.HayJaqueMate(_turnoActual))
            {
                var ganador = _turnoActual == Color.Blanca ? _jugadorNegras : _jugadorBlancas;
                var resultadoGanador = _turnoActual == Color.Blanca ? ResultadoPartida.VictoriaNegras : ResultadoPartida.VictoriaBlancas;
                Finalizar(TipoFinPartida.JaqueMate, ganador.Id);
            }
        }

        /// <summary>
        /// Revierte el <see cref="MovimientoPendiente"/> aún no confirmado, dejando el tablero como
        /// estaba antes de aplicarlo.
        /// </summary>
        /// <remarks>
        /// Restaura la posición y el estado de la pieza, la posible captura (incluida la captura al
        /// paso) y, si lo hubo, el enroque, a partir del estado de reversión guardado por el
        /// servidor. También retira el movimiento del historial.
        /// </remarks>
        /// <param name="jugadorId">Id del jugador que deshace; debe coincidir con el del turno.</param>
        /// <exception cref="InvalidOperationException">No hay movimiento pendiente, o no es el turno del jugador.</exception>
        public void DeshacerMovimiento(string jugadorId)
        {
            if (_movimientoPendiente == null)
                throw new InvalidOperationException("No hay movimiento pendiente para deshacer.");

            if (!EsTurnoJugador(jugadorId))
                throw new InvalidOperationException("No es el turno de este jugador.");

            // Restaurar posición original de la pieza (incluido su estado de "se ha movido")
            var pieza = _tablero.ObtenerPiezaPorId(_movimientoPendiente.PiezaId);
            pieza?.RevertirA(_movimientoPendiente.Origen, _seHaMovidoPiezaAntes);

            // Restaurar la pieza capturada (incluida la captura al paso), usando el estado
            // registrado por el servidor en lugar de confiar en el DTO del cliente
            if (_capturaPendiente != null)
            {
                _capturaPendiente.Restaurar();
                _capturaPendiente = null;
            }

            // Deshacer el enroque: devolver la torre a su posición original
            if (_enroquePendiente != null)
            {
                var enroque = _enroquePendiente.Value;
                enroque.torre.RevertirA(enroque.origen, enroque.seHaMovido);
                _enroquePendiente = null;
            }

            // Eliminar el movimiento del historial para no dejar entradas obsoletas
            _tablero.RemoverUltimoMovimiento();

            _movimientoPendiente = null;
        }

        /// <summary>Alterna el turno al color contrario e incrementa el contador de movimientos.</summary>
        public void CambiarTurno()
        {
            _turnoActual = _turnoActual == Color.Blanca ? Color.Negra : Color.Blanca;
            _numeroTurnos++;
        }

        /// <summary>Registra el tiempo de juego transcurrido reportado por el cliente.</summary>
        /// <param name="tiempo">Tiempo acumulado de la partida.</param>
        public void ActualizarTiempo(TimeSpan tiempo)
        {
            _tiempoTranscurrido = tiempo;
        }

        /// <summary>
        /// Registra la oferta de tablas del jugador indicado; si ambos las han ofrecido, las acepta
        /// de forma automática.
        /// </summary>
        /// <param name="jugadorId">Id del jugador que ofrece tablas. Si no pertenece a la partida, no surte efecto.</param>
        /// <seealso cref="AceptarTablas"/>
        public void SolicitarTablas(string jugadorId)
        {
            if (jugadorId == _jugadorBlancas.Id)
                _tablasBlancas = true;
            else if (jugadorId == _jugadorNegras.Id)
                _tablasNegras = true;

            // Si ambos solicitan tablas, se aceptan automáticamente
            if (_tablasBlancas && _tablasNegras)
            {
                AceptarTablas();
            }
        }

        /// <summary>Retira la oferta de tablas del jugador indicado.</summary>
        /// <param name="jugadorId">Id del jugador que retira su oferta. Si no pertenece a la partida, no surte efecto.</param>
        public void RetirarTablas(string jugadorId)
        {
            if (jugadorId == _jugadorBlancas.Id)
                _tablasBlancas = false;
            else if (jugadorId == _jugadorNegras.Id)
                _tablasNegras = false;
        }

        /// <summary>Finaliza la partida en empate por tablas acordadas.</summary>
        public void AceptarTablas()
        {
            _resultado = ResultadoPartida.Empate;
            _tipoFin = TipoFinPartida.Tablas;
            _estado = EstadoPartida.Finalizada;
        }

        /// <summary>
        /// Finaliza la partida por rendición del jugador indicado, otorgando la victoria al rival.
        /// </summary>
        /// <param name="jugadorId">
        /// Id del jugador que se rinde. Si no pertenece a la partida, el resultado queda sin determinar.
        /// </param>
        public void Rendirse(string jugadorId)
        {
            if (jugadorId == _jugadorBlancas.Id)
            {
                _resultado = ResultadoPartida.VictoriaNegras;
            }
            else if (jugadorId == _jugadorNegras.Id)
            {
                _resultado = ResultadoPartida.VictoriaBlancas;
            }

            _tipoFin = TipoFinPartida.Rendicion;
            _estado = EstadoPartida.Finalizada;
        }

        /// <summary>
        /// Completa la promoción del peón del movimiento pendiente sustituyéndolo por el tipo
        /// elegido y vuelve a comprobar el jaque mate.
        /// </summary>
        /// <remarks>
        /// Se invoca tras <see cref="ConfirmarMovimiento"/> cuando el movimiento era una promoción.
        /// Como el turno ya cambió al confirmar, una pieza recién coronada puede dar mate, por lo que
        /// se reevalúa y, si procede, finaliza la partida.
        /// </remarks>
        /// <param name="tipo">Tipo al que se corona el peón (torre, caballo, alfil o reina).</param>
        /// <param name="jugadorId">Id del jugador que promociona.</param>
        /// <exception cref="InvalidOperationException">
        /// No hay movimiento pendiente que promocionar, o <paramref name="tipo"/> es
        /// <see cref="TipoPieza.Peon"/> o <see cref="TipoPieza.Rey"/> (destinos de coronación no válidos).
        /// </exception>
        public void PromocionarPeon(TipoPieza tipo, string jugadorId)
        {
            if (_movimientoPendiente == null)
                throw new InvalidOperationException("No hay movimiento pendiente para promoción.");

            var pieza = _tablero.ObtenerPiezaPorId(_movimientoPendiente.PiezaId);
            if (pieza != null && pieza.Tipo == TipoPieza.Peon)
            {
                pieza.Promocionar(tipo);
            }

            // Clear the pending movement after promotion
            _movimientoPendiente = null;
            _capturaPendiente = null;
            _enroquePendiente = null;

            // Reevaluar jaque mate tras la coronación (el turno ya cambió al confirmar):
            // una pieza recién coronada puede dar mate.
            if (_tablero.HayJaqueMate(_turnoActual))
            {
                var ganador = _turnoActual == Color.Blanca ? _jugadorNegras : _jugadorBlancas;
                Finalizar(TipoFinPartida.JaqueMate, ganador.Id);
            }
        }

        /// <summary>
        /// Registra la solicitud de revancha del jugador indicado; si ambos la han pedido, reinicia
        /// la partida de forma automática.
        /// </summary>
        /// <param name="jugadorId">Id del jugador que solicita revancha. Si no pertenece a la partida, no surte efecto.</param>
        /// <seealso cref="AceptarReinicio"/>
        public void SolicitarReinicio(string jugadorId)
        {
            if (jugadorId == _jugadorBlancas.Id)
                _reinicioBlancas = true;
            else if (jugadorId == _jugadorNegras.Id)
                _reinicioNegras = true;

            // Si ambos solicitan reinicio, se reinicia automáticamente
            if (_reinicioBlancas && _reinicioNegras)
            {
                AceptarReinicio();
            }
        }

        /// <summary>Retira la solicitud de revancha del jugador indicado.</summary>
        /// <param name="jugadorId">Id del jugador que retira su solicitud. Si no pertenece a la partida, no surte efecto.</param>
        public void RetirarReinicio(string jugadorId)
        {
            if (jugadorId == _jugadorBlancas.Id)
                _reinicioBlancas = false;
            else if (jugadorId == _jugadorNegras.Id)
                _reinicioNegras = false;
        }

        /// <summary>
        /// Reinicia la partida para una revancha: vuelve a la posición inicial y limpia las
        /// solicitudes de revancha y las ofertas de tablas.
        /// </summary>
        public void AceptarReinicio()
        {
            IniciarPartida();
            _reinicioBlancas = false;
            _reinicioNegras = false;
            _tablasBlancas = false;
            _tablasNegras = false;
        }

        /// <summary>
        /// Marca la partida como finalizada con el motivo indicado y fija el resultado cuando este
        /// depende de un ganador.
        /// </summary>
        /// <remarks>
        /// Para <see cref="TipoFinPartida.JaqueMate"/> y <see cref="TipoFinPartida.Abandono"/>
        /// determina el resultado a partir de <paramref name="ganadorId"/>; los demás motivos (por
        /// ejemplo tablas) fijan su resultado por otra vía.
        /// </remarks>
        /// <param name="tipo">Motivo del fin de la partida.</param>
        /// <param name="ganadorId">Id del jugador ganador cuando el motivo lo requiere; puede ser <c>null</c> en el resto de casos.</param>
        public void Finalizar(TipoFinPartida tipo, string? ganadorId = null)
        {
            _tipoFin = tipo;
            _estado = EstadoPartida.Finalizada;

            if (tipo == TipoFinPartida.JaqueMate)
            {
                _resultado = ganadorId == _jugadorBlancas.Id ? ResultadoPartida.VictoriaBlancas : ResultadoPartida.VictoriaNegras;
            }
            else if (tipo == TipoFinPartida.Abandono)
            {
                _resultado = ganadorId == _jugadorBlancas.Id ? ResultadoPartida.VictoriaBlancas : ResultadoPartida.VictoriaNegras;
            }
        }

        /// <summary>Devuelve el color que defiende el jugador indicado en esta partida.</summary>
        /// <param name="jugadorId">Id del jugador.</param>
        /// <returns>El color (<see cref="Color.Blanca"/> o <see cref="Color.Negra"/>) del jugador.</returns>
        /// <exception cref="InvalidOperationException">El jugador no pertenece a esta partida.</exception>
        public Color ObtenerColorJugador(string jugadorId)
        {
            if (_jugadorBlancas.Id == jugadorId)
                return Color.Blanca;
            if (_jugadorNegras.Id == jugadorId)
                return Color.Negra;

            throw new InvalidOperationException("El jugador no pertenece a esta partida.");
        }

        /// <summary>Indica si es el turno del jugador indicado.</summary>
        /// <param name="jugadorId">Id del jugador.</param>
        /// <returns><c>true</c> si el color del jugador coincide con <see cref="TurnoActual"/>.</returns>
        /// <exception cref="InvalidOperationException">El jugador no pertenece a esta partida.</exception>
        public bool EsTurnoJugador(string jugadorId)
        {
            var colorJugador = ObtenerColorJugador(jugadorId);
            return colorJugador == _turnoActual;
        }

        /// <summary>
        /// Traduce un identificador de conexión SignalR al <see cref="Jugador.Id"/> de dominio del
        /// jugador correspondiente.
        /// </summary>
        /// <param name="connectionId">Identificador de conexión de uno de los jugadores de la partida.</param>
        /// <returns>El id de dominio del jugador asociado a esa conexión.</returns>
        /// <exception cref="InvalidOperationException">Ninguna de las conexiones de la partida coincide.</exception>
        public string ObtenerJugadorIdPorConnectionId(string connectionId)
        {
            if (_jugadorBlancas.ConnectionId == connectionId)
                return _jugadorBlancas.Id;
            if (_jugadorNegras.ConnectionId == connectionId)
                return _jugadorNegras.Id;

            throw new InvalidOperationException("El jugador no pertenece a esta partida.");
        }

        /// <summary>Comprueba la invariante de la partida: su identificador no está vacío.</summary>
        /// <exception cref="InvalidOperationException">El identificador de la partida es nulo o está en blanco.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(_id))
                throw new InvalidOperationException("ID de partida inválido.");
        }

        /// <summary>Crea una partida. Método de fábrica equivalente al constructor.</summary>
        /// <param name="salaId">Id de la sala a la que pertenece; no puede ser nulo ni estar en blanco.</param>
        /// <param name="jugador1">Primer jugador; no puede ser nulo.</param>
        /// <param name="jugador2">Segundo jugador; no puede ser nulo.</param>
        /// <returns>La nueva instancia de <see cref="Partida"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="salaId"/> es nulo o está en blanco.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="jugador1"/> o <paramref name="jugador2"/> son nulos.</exception>
        public static Partida Create(string salaId, Jugador jugador1, Jugador jugador2)
        {
            return new Partida(salaId, jugador1, jugador2);
        }
    }
}
