using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ServidorAjedrez.Domain.DTOs;
using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.Enums;
using ServidorAjedrez.Domain.Exceptions;
using ServidorAjedrez.Domain.Interfaces;
using ServidorAjedrez.Domain.Repositories;
using System.Threading.Tasks;

namespace ServidorAjedrez.Hubs
{
    /// <summary>
    /// Hub de SignalR que expone la API en tiempo real del juego: gestión de salas y el flujo de
    /// movimientos, tablas, revancha, rendición y abandono.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cada método público es invocable por el cliente; el hub delega la lógica en los casos de uso
    /// del dominio y comunica los resultados a los jugadores de la sala mediante eventos. Los errores
    /// esperados se devuelven al cliente con el evento <c>Error</c> en lugar de propagarse.
    /// </para>
    /// <para>Principales eventos que emite hacia el cliente:</para>
    /// <list type="bullet">
    ///   <item><description><c>SalaCreada</c>, <c>PartidaIniciada</c>: alta de sala e inicio de partida.</description></item>
    ///   <item><description><c>MovimientoRealizado</c>, <c>TurnoActualizado</c>, <c>TableroActualizado</c>: progreso del juego.</description></item>
    ///   <item><description><c>PromocionRequerida</c>: el movimiento confirmado exige coronar un peón.</description></item>
    ///   <item><description><c>TablasActualizadas</c>, <c>ReinicioActualizado</c>: estado de las ofertas de tablas y de revancha.</description></item>
    ///   <item><description><c>PartidaFinalizada</c>, <c>JugadorAbandonado</c>: fin de la partida y abandono de un jugador.</description></item>
    ///   <item><description><c>Error</c>: mensaje de error dirigido al jugador que originó la acción.</description></item>
    /// </list>
    /// </remarks>
    public class AjedrezHub : Hub
    {
        private readonly ICrearSalaUseCase _crearSalaUseCase;
        private readonly IUnirseSalaUseCase _unirseSalaUseCase;
        private readonly IRealizarMovimientoUseCase _realizarMovimientoUseCase;
        private readonly IConfirmarMovimientoUseCase _confirmarMovimientoUseCase;
        private readonly IDeshacerMovimientoUseCase _deshacerMovimientoUseCase;
        private readonly ISolicitarTablasUseCase _solicitarTablasUseCase;
        private readonly IRetirarTablasUseCase _retirarTablasUseCase;
        private readonly IRendirseUseCase _rendirseUseCase;
        private readonly IPromocionarPeonUseCase _promocionarPeonUseCase;
        private readonly ISolicitarReinicioUseCase _solicitarReinicioUseCase;
        private readonly IRetirarReinicioUseCase _retirarReinicioUseCase;
        private readonly IAbandonarSalaUseCase _abandonarSalaUseCase;
        private readonly ISalaRepository _salaRepository;
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<AjedrezHub> _logger;

        /// <summary>Crea el hub con los casos de uso, el repositorio de salas y el gestor de conexiones inyectados.</summary>
        public AjedrezHub(
            ICrearSalaUseCase crearSalaUseCase,
            IUnirseSalaUseCase unirseSalaUseCase,
            IRealizarMovimientoUseCase realizarMovimientoUseCase,
            IConfirmarMovimientoUseCase confirmarMovimientoUseCase,
            IDeshacerMovimientoUseCase deshacerMovimientoUseCase,
            ISolicitarTablasUseCase solicitarTablasUseCase,
            IRetirarTablasUseCase retirarTablasUseCase,
            IRendirseUseCase rendirseUseCase,
            IPromocionarPeonUseCase promocionarPeonUseCase,
            ISolicitarReinicioUseCase solicitarReinicioUseCase,
            IRetirarReinicioUseCase retirarReinicioUseCase,
            IAbandonarSalaUseCase abandonarSalaUseCase,
            ISalaRepository salaRepository,
            IConnectionManager connectionManager,
            ILogger<AjedrezHub> logger)
        {
            _crearSalaUseCase = crearSalaUseCase;
            _unirseSalaUseCase = unirseSalaUseCase;
            _realizarMovimientoUseCase = realizarMovimientoUseCase;
            _confirmarMovimientoUseCase = confirmarMovimientoUseCase;
            _deshacerMovimientoUseCase = deshacerMovimientoUseCase;
            _solicitarTablasUseCase = solicitarTablasUseCase;
            _retirarTablasUseCase = retirarTablasUseCase;
            _rendirseUseCase = rendirseUseCase;
            _promocionarPeonUseCase = promocionarPeonUseCase;
            _solicitarReinicioUseCase = solicitarReinicioUseCase;
            _retirarReinicioUseCase = retirarReinicioUseCase;
            _abandonarSalaUseCase = abandonarSalaUseCase;
            _salaRepository = salaRepository;
            _connectionManager = connectionManager;
            _logger = logger;
        }

        /// <inheritdoc/>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Cliente conectado: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Trata la desconexión como un abandono: si el jugador estaba en una partida en curso,
        /// notifica al rival con <c>JugadorAbandonado</c> y <c>PartidaFinalizada</c> para que no se
        /// quede esperando, y a continuación olvida la conexión en el gestor de conexiones.
        /// </remarks>
        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            _logger.LogInformation("Cliente desconectado: {ConnectionId}", Context.ConnectionId);

            // Si el jugador estaba en una partida en curso, tratar la desconexión como abandono
            // para que el rival sea notificado y no se quede esperando indefinidamente.
            try
            {
                var (salaId, partidaId, resultado, ganadorId) = await _abandonarSalaUseCase.ExecuteAsync(Context.ConnectionId);
                if (!string.IsNullOrEmpty(salaId))
                {
                    await Clients.Group(salaId).SendAsync("JugadorAbandonado", Context.ConnectionId);
                    if (!string.IsNullOrEmpty(partidaId))
                    {
                        await Clients.Group(salaId).SendAsync("PartidaFinalizada", resultado, TipoFinPartida.Abandono, ganadorId ?? "");
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "Error al procesar abandono por desconexión para {ConnectionId}", Context.ConnectionId);
            }

            try
            {
                _connectionManager.RemoverConnection(Context.ConnectionId);
                _connectionManager.RemoverNombreJugador(Context.ConnectionId);
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "Error al remover connection en OnDisconnectedAsync");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>Registra el nombre del jugador para la conexión actual.</summary>
        /// <remarks>
        /// Conviene llamarlo antes de <see cref="CrearSala"/>. No emite eventos; los errores se
        /// registran sin interrumpir la conexión.
        /// </remarks>
        /// <param name="nombreJugador">Nombre elegido por el jugador; si está vacío, se ignora.</param>
        public Task SetNombreJugador(string nombreJugador)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombreJugador))
                {
                    _logger.LogWarning("SetNombreJugador recibió nombre vacío para connection {ConnectionId}", Context.ConnectionId);
                    return Task.CompletedTask;
                }

                _logger.LogInformation("SetNombreJugador: {Nombre} para connection {ConnectionId}", nombreJugador, Context.ConnectionId);
                _connectionManager.AsociarNombreJugador(Context.ConnectionId, nombreJugador);
                return Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error en SetNombreJugador para connection {ConnectionId}", Context.ConnectionId);
                return Task.CompletedTask;
            }
        }

        /// <summary>Crea una sala con el nombre indicado y añade la conexión a su grupo.</summary>
        /// <remarks>
        /// Requiere haber fijado antes el nombre con <see cref="SetNombreJugador"/>. Emite
        /// <c>SalaCreada</c> al creador con el <see cref="SalaDTO"/>; si falta el nombre o algo falla,
        /// emite <c>Error</c>.
        /// </remarks>
        /// <param name="nombreSala">Nombre de la sala a crear.</param>
        public async Task CrearSala(string nombreSala)
        {
            try
            {
                _logger.LogInformation("Creando sala: {NombreSala} (solicitado por {ConnectionId})", nombreSala, Context.ConnectionId);

                // FIX: Get player name from ConnectionManager
                var nombreJugador = _connectionManager.ObtenerNombrePorConnection(Context.ConnectionId);
                if (string.IsNullOrWhiteSpace(nombreJugador))
                {
                    await EnviarError("Debe establecer su nombre antes de crear una sala");
                    return;
                }

                var sala = await _crearSalaUseCase.ExecuteAsync(nombreSala, nombreJugador, Context.ConnectionId);

                _connectionManager.AsociarConnectionASala(Context.ConnectionId, sala.Id);
                await Groups.AddToGroupAsync(Context.ConnectionId, sala.Id);

                var salaDTO = SalaDTO.FromDomain(sala);
                await Clients.Caller.SendAsync("SalaCreada", salaDTO);

                _logger.LogInformation("Sala {SalaId} creada exitosamente por connection {ConnectionId}", sala.Id, Context.ConnectionId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al crear sala (connection {ConnectionId})", Context.ConnectionId);
                await EnviarError($"Error al crear sala: {ex.Message}");
            }
        }

        /// <summary>Une al jugador a una sala existente e inicia la partida.</summary>
        /// <remarks>
        /// Añade la conexión al grupo de la sala y emite <c>PartidaIniciada</c> a ambos jugadores con
        /// el <see cref="PartidaDTO"/>. Si la sala no existe o ya está completa, emite <c>Error</c>.
        /// </remarks>
        /// <param name="nombreSala">Nombre de la sala a la que unirse.</param>
        /// <param name="nombreJugador">Nombre del jugador que se une.</param>
        public async Task UnirseSala(string nombreSala, string nombreJugador)
        {
            try
            {
                _logger.LogInformation("Jugador {NombreJugador} (connection {ConnectionId}) intenta unirse a sala: {NombreSala}", nombreJugador, Context.ConnectionId, nombreSala);

                if (!string.IsNullOrWhiteSpace(nombreJugador))
                {
                    _connectionManager.AsociarNombreJugador(Context.ConnectionId, nombreJugador);
                }

                var partida = await _unirseSalaUseCase.ExecuteAsync(nombreSala, nombreJugador, Context.ConnectionId);

                _connectionManager.AsociarConnectionASala(Context.ConnectionId, partida.SalaId);
                await Groups.AddToGroupAsync(Context.ConnectionId, partida.SalaId);

                var partidaDTO = PartidaDTO.FromDomain(partida, true);
                await Clients.Group(partida.SalaId).SendAsync("PartidaIniciada", partidaDTO);

                _logger.LogInformation("Partida {PartidaId} iniciada en sala {SalaId}", partida.Id, partida.SalaId);
            }
            catch (SalaNoEncontradaException ex)
            {
                _logger.LogWarning(ex, "Sala no encontrada: {Sala}", nombreSala);
                await EnviarError(ex.Message);
            }
            catch (SalaCompletaException ex)
            {
                _logger.LogWarning(ex, "Sala completa: {Sala}", nombreSala);
                await EnviarError(ex.Message);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al unirse a sala (connection {ConnectionId})", Context.ConnectionId);
                await EnviarError($"Error al unirse a sala: {ex.Message}");
            }
        }

        /// <summary>
        /// Resuelve el id de la partida en curso a partir de la conexión, pasando por la sala asociada.
        /// </summary>
        /// <param name="connectionId">Conexión del jugador.</param>
        /// <returns>El id de la partida actual, o <c>null</c> si la conexión no tiene sala o partida.</returns>
        private async Task<string?> ObtenerPartidaIdDesdeConnectionAsync(string connectionId)
        {
            try
            {
                var salaId = _connectionManager.ObtenerSalaPorConnection(connectionId);
                if (string.IsNullOrEmpty(salaId)) return null;

                var sala = await _salaRepository.ObtenerPorIdAsync(salaId);
                if (sala == null) return null;

                return sala.PartidaActualId;
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "Error obteniendo partidaId desde connection {ConnectionId}", connectionId);
                return null;
            }
        }

        /// <summary>Aplica de forma tentativa el movimiento recibido del cliente.</summary>
        /// <remarks>Emite <c>MovimientoRealizado</c> al grupo con el movimiento y el tablero resultantes; ante un error, emite <c>Error</c>.</remarks>
        /// <param name="movimientoDTO">Movimiento solicitado por el jugador.</param>
        public async Task RealizarMovimiento(MovimientoDTO movimientoDTO)
        {
            try
            {
                var partidaId = await ObtenerPartidaIdDesdeConnectionAsync(Context.ConnectionId);
                if (string.IsNullOrEmpty(partidaId))
                {
                    await EnviarError("No se encontró la partida asociada a tu sala");
                    return;
                }

                var (movimiento, tablero) = await _realizarMovimientoUseCase.ExecuteAsync(partidaId, movimientoDTO, Context.ConnectionId);

                var salaId = await ObtenerSalaIdPorPartidaAsync(partidaId) ?? string.Empty;
                await Clients.Group(salaId).SendAsync("MovimientoRealizado",
                    MovimientoDTO.FromDomain(movimiento),
                    TableroDTO.FromDomain(tablero));

                _logger.LogInformation("Movimiento realizado en partida {PartidaId} por connection {ConnectionId}", partidaId, Context.ConnectionId);
            }
            catch (PartidaNoEncontradaException ex)
            {
                _logger.LogWarning(ex, "Partida no encontrada al realizar movimiento");
                await EnviarError(ex.Message);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al realizar movimiento (connection {ConnectionId})", Context.ConnectionId);
                await EnviarError($"Error al realizar movimiento: {ex.Message}");
            }
        }

        /// <summary>Confirma el movimiento pendiente del jugador y consolida el turno.</summary>
        /// <remarks>
        /// Emite <c>TurnoActualizado</c> al grupo. Si hay jaque mate, emite además
        /// <c>PartidaFinalizada</c>; si el movimiento exige promoción, emite <c>PromocionRequerida</c>
        /// solo al jugador que acaba de mover.
        /// </remarks>
        public async Task ConfirmarMovimiento()
        {
            try
            {
                var partidaId = await ObtenerPartidaIdDesdeConnectionAsync(Context.ConnectionId);
                if (string.IsNullOrEmpty(partidaId))
                {
                    await EnviarError("No se encontró la partida asociada a tu sala");
                    return;
                }

                var (turnoActual, numeroTurnos, hayJaqueMate, esPromocion, resultado, ganadorId) = await _confirmarMovimientoUseCase.ExecuteAsync(partidaId, Context.ConnectionId);

                var salaId = await ObtenerSalaIdPorPartidaAsync(partidaId) ?? string.Empty;
                await Clients.Group(salaId).SendAsync("TurnoActualizado", turnoActual, numeroTurnos);

                if (hayJaqueMate)
                {
                    await Clients.Group(salaId).SendAsync("PartidaFinalizada", resultado, TipoFinPartida.JaqueMate, ganadorId ?? "");
                }
                else if (esPromocion)
                {
                    // Emit PromocionRequerida after confirming the turn
                    await Clients.Caller.SendAsync("PromocionRequerida");
                    _logger.LogInformation("Promoción requerida emitida para partida {PartidaId} después de confirmar movimiento", partidaId);
                }
            }
            catch (PartidaNoEncontradaException ex)
            {
                _logger.LogWarning(ex, "Partida no encontrada al confirmar movimiento");
                await EnviarError(ex.Message);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al confirmar movimiento (connection {ConnectionId})", Context.ConnectionId);
                await EnviarError($"Error al confirmar movimiento: {ex.Message}");
            }
        }

        /// <summary>Deshace el movimiento pendiente del jugador.</summary>
        /// <remarks>Emite <c>TableroActualizado</c> al grupo con el tablero restaurado.</remarks>
        public async Task DeshacerMovimiento()
        {
            try
            {
                var partidaId = await ObtenerPartidaIdDesdeConnectionAsync(Context.ConnectionId);
                if (string.IsNullOrEmpty(partidaId))
                {
                    await EnviarError("No se encontró la partida asociada a tu sala");
                    return;
                }

                var tablero = await _deshacerMovimientoUseCase.ExecuteAsync(partidaId, Context.ConnectionId);

                var salaId = await ObtenerSalaIdPorPartidaAsync(partidaId) ?? string.Empty;
                await Clients.Group(salaId).SendAsync("TableroActualizado", TableroDTO.FromDomain(tablero));
                _logger.LogInformation("Movimiento deshecho en partida {PartidaId} por connection {ConnectionId}", partidaId, Context.ConnectionId);
            }
            catch (PartidaNoEncontradaException ex)
            {
                _logger.LogWarning(ex, "Partida no encontrada al deshacer movimiento");
                await EnviarError(ex.Message);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al deshacer movimiento (connection {ConnectionId})", Context.ConnectionId);
                await EnviarError($"Error al deshacer movimiento: {ex.Message}");
            }
        }

        /// <summary>Registra la oferta de tablas del jugador.</summary>
        /// <remarks>
        /// Emite <c>TablasActualizadas</c> al grupo con el estado de cada bando; si ambos jugadores
        /// las han ofrecido, emite además <c>PartidaFinalizada</c> con el motivo de tablas.
        /// </remarks>
        public async Task SolicitarTablas()
        {
            try
            {
                var partidaId = await ObtenerPartidaIdDesdeConnectionAsync(Context.ConnectionId);
                if (string.IsNullOrEmpty(partidaId))
                {
                    await EnviarError("No se encontró la partida asociada a tu sala");
                    return;
                }

                (bool blancas, bool negras, bool aceptadas) = await _solicitarTablasUseCase.ExecuteAsync(partidaId, Context.ConnectionId);

                var salaId = await ObtenerSalaIdPorPartidaAsync(partidaId) ?? string.Empty;
                await Clients.Group(salaId).SendAsync("TablasActualizadas", blancas, negras);

                if (aceptadas)
                {
                    await Clients.Group(salaId).SendAsync("PartidaFinalizada", null, TipoFinPartida.Tablas, "");
                }

                _logger.LogInformation("Solicitud de tablas procesada en partida {PartidaId} (blancas:{Blancas}, negras:{Negras}, aceptadas:{Aceptadas})", partidaId, blancas, negras, aceptadas);
            }
            catch (PartidaNoEncontradaException ex)
            {
                _logger.LogWarning(ex, "Partida no encontrada al solicitar tablas");
                await EnviarError(ex.Message);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al solicitar tablas (connection {ConnectionId})", Context.ConnectionId);
                await EnviarError($"Error al solicitar tablas: {ex.Message}");
            }
        }

        /// <summary>Retira la oferta de tablas del jugador.</summary>
        /// <remarks>Emite <c>TablasActualizadas</c> al grupo con el nuevo estado de cada bando.</remarks>
        public async Task RetirarTablas()
        {
            try
            {
                var partidaId = await ObtenerPartidaIdDesdeConnectionAsync(Context.ConnectionId);
                if (string.IsNullOrEmpty(partidaId))
                {
                    await EnviarError("No se encontró la partida asociada a tu sala");
                    return;
                }

                (bool blancas, bool negras) = await _retirarTablasUseCase.ExecuteAsync(partidaId, Context.ConnectionId);

                var salaId = await ObtenerSalaIdPorPartidaAsync(partidaId) ?? string.Empty;
                await Clients.Group(salaId).SendAsync("TablasActualizadas", blancas, negras);

                _logger.LogInformation("Retiro de tablas procesado en partida {PartidaId} (blancas:{Blancas}, negras:{Negras})", partidaId, blancas, negras);
            }
            catch (PartidaNoEncontradaException ex)
            {
                _logger.LogWarning(ex, "Partida no encontrada al retirar tablas");
                await EnviarError(ex.Message);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al retirar tablas (connection {ConnectionId})", Context.ConnectionId);
                await EnviarError($"Error al retirar tablas: {ex.Message}");
            }
        }

        /// <summary>Procesa la rendición del jugador.</summary>
        /// <remarks>Emite <c>PartidaFinalizada</c> al grupo con el resultado y el motivo de rendición.</remarks>
        public async Task Rendirse()
        {
            try
            {
                var partidaId = await ObtenerPartidaIdDesdeConnectionAsync(Context.ConnectionId);
                if (string.IsNullOrEmpty(partidaId))
                {
                    await EnviarError("No se encontró la partida asociada a tu sala");
                    return;
                }

                (ResultadoPartida? resultado, string jugadorId) = await _rendirseUseCase.ExecuteAsync(partidaId, Context.ConnectionId);

                var salaId = await ObtenerSalaIdPorPartidaAsync(partidaId) ?? string.Empty;
                await Clients.Group(salaId).SendAsync("PartidaFinalizada", resultado, TipoFinPartida.Rendicion, jugadorId);

                _logger.LogInformation("Jugador {ConnectionId} se rindió en partida {PartidaId}", Context.ConnectionId, partidaId);
            }
            catch (PartidaNoEncontradaException ex)
            {
                _logger.LogWarning(ex, "Partida no encontrada al rendirse");
                await EnviarError(ex.Message);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al rendirse (connection {ConnectionId})", Context.ConnectionId);
                await EnviarError($"Error al rendirse: {ex.Message}");
            }
        }

        /// <summary>Corona el peón del movimiento pendiente al tipo indicado.</summary>
        /// <remarks>
        /// Emite <c>TableroActualizado</c> al grupo; si la coronación provoca jaque mate, emite además
        /// <c>PartidaFinalizada</c>. Si <paramref name="tipoPieza"/> no corresponde a un
        /// <see cref="TipoPieza"/> válido, emite <c>Error</c>.
        /// </remarks>
        /// <param name="tipoPieza">Nombre del <see cref="TipoPieza"/> al que coronar (por ejemplo, "Reina").</param>
        public async Task PromocionarPeon(string tipoPieza)
        {
            try
            {
                var partidaId = await ObtenerPartidaIdDesdeConnectionAsync(Context.ConnectionId);
                if (string.IsNullOrEmpty(partidaId))
                {
                    await EnviarError("No se encontró la partida asociada a tu sala");
                    return;
                }

                if (!System.Enum.TryParse<TipoPieza>(tipoPieza, out var tipo))
                {
                    await EnviarError("Tipo de pieza inválido para promoción");
                    return;
                }

                var (tablero, hayJaqueMate, resultado, ganadorId) = await _promocionarPeonUseCase.ExecuteAsync(partidaId, tipo, Context.ConnectionId);

                var salaId = await ObtenerSalaIdPorPartidaAsync(partidaId) ?? string.Empty;
                await Clients.Group(salaId).SendAsync("TableroActualizado", TableroDTO.FromDomain(tablero));

                // Una coronación puede dar jaque mate: finalizar la partida si procede
                if (hayJaqueMate)
                {
                    await Clients.Group(salaId).SendAsync("PartidaFinalizada", resultado, TipoFinPartida.JaqueMate, ganadorId ?? "");
                }

                _logger.LogInformation("Promoción de peón procesada en partida {PartidaId} por connection {ConnectionId}", partidaId, Context.ConnectionId);
            }
            catch (PartidaNoEncontradaException ex)
            {
                _logger.LogWarning(ex, "Partida no encontrada al promocionar peón");
                await EnviarError(ex.Message);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al promocionar peón (connection {ConnectionId})", Context.ConnectionId);
                await EnviarError($"Error al promocionar peón: {ex.Message}");
            }
        }

        /// <summary>Registra la solicitud de revancha del jugador.</summary>
        /// <remarks>
        /// Emite <c>ReinicioActualizado</c> al grupo con el estado de cada bando; si ambos jugadores
        /// la han solicitado, emite además <c>PartidaIniciada</c> con la partida ya reiniciada.
        /// </remarks>
        public async Task SolicitarReinicio()
        {
            try
            {
                var partidaId = await ObtenerPartidaIdDesdeConnectionAsync(Context.ConnectionId);
                if (string.IsNullOrEmpty(partidaId))
                {
                    await EnviarError("No se encontró la partida asociada a tu sala");
                    return;
                }

                (bool reinicioBlancas, bool reinicioNegras, Partida? partidaReiniciada) = await _solicitarReinicioUseCase.ExecuteAsync(partidaId, Context.ConnectionId);

                var salaId = await ObtenerSalaIdPorPartidaAsync(partidaId) ?? string.Empty;
                await Clients.Group(salaId).SendAsync("ReinicioActualizado", reinicioBlancas, reinicioNegras);

                if (partidaReiniciada != null)
                {
                    await Clients.Group(salaId).SendAsync("PartidaIniciada", PartidaDTO.FromDomain(partidaReiniciada, true));
                }

                _logger.LogInformation("Solicitud de reinicio procesada en partida {PartidaId}", partidaId);
            }
            catch (PartidaNoEncontradaException ex)
            {
                _logger.LogWarning(ex, "Partida no encontrada al solicitar reinicio");
                await EnviarError(ex.Message);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al solicitar reinicio (connection {ConnectionId})", Context.ConnectionId);
                await EnviarError($"Error al solicitar reinicio: {ex.Message}");
            }
        }

        /// <summary>Retira la solicitud de revancha del jugador.</summary>
        /// <remarks>Emite <c>ReinicioActualizado</c> al grupo restableciendo ambos indicadores a falso.</remarks>
        public async Task RetirarReinicio()
        {
            try
            {
                var partidaId = await ObtenerPartidaIdDesdeConnectionAsync(Context.ConnectionId);
                if (string.IsNullOrEmpty(partidaId))
                {
                    await EnviarError("No se encontró la partida asociada a tu sala");
                    return;
                }

                await _retirarReinicioUseCase.ExecuteAsync(partidaId, Context.ConnectionId);

                var salaId = await ObtenerSalaIdPorPartidaAsync(partidaId) ?? string.Empty;
                await Clients.Group(salaId).SendAsync("ReinicioActualizado", false, false);

                _logger.LogInformation("Retiro de reinicio procesado en partida {PartidaId}", partidaId);
            }
            catch (PartidaNoEncontradaException ex)
            {
                _logger.LogWarning(ex, "Partida no encontrada al retirar reinicio");
                await EnviarError(ex.Message);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al retirar reinicio (connection {ConnectionId})", Context.ConnectionId);
                await EnviarError($"Error al retirar reinicio: {ex.Message}");
            }
        }

        /// <summary>Procesa el abandono explícito de la sala por parte del jugador.</summary>
        /// <remarks>
        /// Emite <c>JugadorAbandonado</c> al grupo y, si había una partida en curso,
        /// <c>PartidaFinalizada</c> por abandono; después saca la conexión del grupo y la olvida.
        /// </remarks>
        public async Task AbandonarSala()
        {
            try
            {
                _logger.LogInformation("Jugador {ConnectionId} abandona la sala", Context.ConnectionId);
                var (salaId, partidaId, resultado, ganadorId) = await _abandonarSalaUseCase.ExecuteAsync(Context.ConnectionId);

                if (!string.IsNullOrEmpty(salaId))
                {
                    await Clients.Group(salaId).SendAsync("JugadorAbandonado", Context.ConnectionId);
                    if (!string.IsNullOrEmpty(partidaId))
                    {
                        await Clients.Group(salaId).SendAsync("PartidaFinalizada", resultado, TipoFinPartida.Abandono, ganadorId ?? "");
                    }
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, salaId);
                }

                _connectionManager.RemoverConnection(Context.ConnectionId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al abandonar sala (connection {ConnectionId})", Context.ConnectionId);
                await EnviarError($"Error al abandonar sala: {ex.Message}");
            }
        }

        /// <summary>Envía el evento <c>Error</c> con el mensaje indicado solo al jugador que originó la acción.</summary>
        /// <param name="mensaje">Texto del error que se mostrará en el cliente.</param>
        private async Task EnviarError(string mensaje)
        {
            try
            {
                await Clients.Caller.SendAsync("Error", mensaje);
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar el error al cliente (connection {ConnectionId})", Context.ConnectionId);
            }
        }

        /// <summary>Busca el id de la sala que tiene asociada la partida indicada.</summary>
        /// <param name="partidaId">Id de la partida.</param>
        /// <returns>El id de la sala, o <c>null</c> si no se encuentra o se produce un error.</returns>
        private async Task<string?> ObtenerSalaIdPorPartidaAsync(string partidaId)
        {
            try
            {
                // Intento buscar la sala por listado simple (si tu repo tiene método directo, úsalo)
                var salas = await _salaRepository.ObtenerTodasAsync();
                var sala = salas.FirstOrDefault(s => s.PartidaActualId == partidaId);
                return sala?.Id;
            }
            catch
            {
                return null;
            }
        }
    }
}
