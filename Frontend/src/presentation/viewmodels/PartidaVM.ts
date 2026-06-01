/**
 * ViewModel (MobX) de la pantalla de partida: orquesta toda la lógica de juego del cliente.
 *
 * @remarks
 * Es el centro de la interacción durante una partida. Sus responsabilidades:
 *
 * - **Selección y envío de jugadas**: traduce los toques del usuario en movimientos,
 *   los valida localmente con el motor ({@link Tablero}) y los envía al servidor.
 * - **Sincronización**: se suscribe a los eventos del servidor (movimiento, turno, fin de
 *   partida, tablas, reinicio, abandono…) y actualiza su estado observable, que la
 *   pantalla refleja.
 * - **Flujo movimiento → confirmación**: una jugada queda **provisional**
 *   ({@link PartidaVM.movimientoPendiente}) hasta que el usuario la confirma o la deshace.
 *
 * El color del jugador local se determina por `connectionId` (ver {@link PartidaVM.handleReinicioPartida}).
 */

import { logger } from '../../core/logger';
import { isObservable, makeAutoObservable, runInAction, observable } from 'mobx';
import { Color, Posicion, posicionesIguales, ResultadoPartida, TipoFinPartida, TipoPieza } from '../../core/types';

/** Hace observable un objeto solo si no lo es ya (evita el error de doble `makeAutoObservable`). */
const safeObservable = (obj: any) => {
  if (obj && !isObservable(obj)) {
    makeAutoObservable(obj);
  }
};
import { Movimiento } from '../../domain/entities/Movimiento';
import { Partida } from '../../domain/entities/Partida';
import { Pieza } from '../../domain/entities/Pieza';
import { Tablero } from '../../domain/entities/Tablero';
import { IAjedrezUseCase } from '../../domain/interfaces/IAjedrezUseCase';

export class PartidaVM {
  partida: Partida | null = null;
  tablero: Tablero | null = null;
  /** Color del jugador local (determinado por connectionId al inicializar). */
  miColor: Color | null = null;
  miNombre: string = '';
  nombreOponente: string = '';
  /** Pieza seleccionada actualmente (origen del próximo movimiento), o `null`. */
  piezaSeleccionada: Pieza | null = null;
  /** Destinos legales de la pieza seleccionada (para resaltar en el tablero). */
  movimientosPosibles: Posicion[] = [];
  /** Movimiento enviado y pendiente de confirmar o deshacer. */
  movimientoPendiente: Movimiento | null = null;
  /** Si se debe mostrar el modal de promoción. */
  mostrarPromocion: boolean = false;
  /** Si se debe mostrar el modal de fin de partida. */
  mostrarFinPartida: boolean = false;
  mensajeTurno: string | null = null;
  /** Texto de aviso de jaque (`'¡Jaque!'`) o `null`. */
  mensajeJaque: string | null = null;
  /** Recuento de piezas blancas capturadas, por tipo. */
  piezasEliminadasBlancas = observable.map<TipoPieza, number>();
  /** Recuento de piezas negras capturadas, por tipo. */
  piezasEliminadasNegras = observable.map<TipoPieza, number>();
  error: string | null = null;
  /** El rival me ha ofrecido tablas (puedo aceptarlas). */
  tablasOfrecidas: boolean = false;
  /** Yo he ofrecido tablas. */
  solicitadasTablas: boolean = false;
  solicitadoReinicio: boolean = false;
  oponenteSolicitoReinicio: boolean = false;
  oponenteAbandono: boolean = false;

  private ajedrezUseCase: IAjedrezUseCase;
  private proximoMovimientoEsEnroque: boolean = false;
  private timer: ReturnType<typeof setInterval> | null = null;
  /** Bandera para ignorar eventos entrantes mientras se abandona la partida. */
  private estaSaliendo: boolean = false;

  constructor(useCase: IAjedrezUseCase) {
    this.ajedrezUseCase = useCase;
    makeAutoObservable(this);
  }

  /**
   * Inicializa el ViewModel para una partida y se suscribe a todos los eventos del servidor.
   *
   * @remarks
   * Establece tablero, color y oponente; arranca el temporizador de tiempo de juego y
   * registra los handlers de movimiento, turno, fin de partida, tablas, reinicio, etc.
   *
   * @param miColor - Color del jugador local.
   * @param miNombre - Nombre del jugador local (respaldo para identificación).
   */
  inicializarPartida(partida: Partida, miColor: Color, miNombre: string = ''): void {
    // Reset the leaving flag for new game
    this.estaSaliendo = false;

    this.partida = partida;
    safeObservable(this.partida);
    safeObservable(partida.tablero);
    this.miColor = miColor;
    this.miNombre = miNombre;
    this.tablero = partida.tablero;

    // Determinar nombre del oponente
    if (miColor === 'Blanca') {
      this.nombreOponente = partida.jugadorNegras.nombre;
    } else {
      this.nombreOponente = partida.jugadorBlancas.nombre;
    }

    // Inicializar contadores de piezas eliminadas
    this.actualizarPiezasEliminadas();
    this.actualizarMensajeTurno();

    // Iniciar timer para incrementar tiempo transcurrido
    this.startTimer();

    // Suscribirse a eventos
    this.ajedrezUseCase.subscribeMovimiento(this.handleMovimiento.bind(this));
    this.ajedrezUseCase.subscribeTableroActualizado(this.handleTableroActualizado.bind(this));
    this.ajedrezUseCase.subscribeTurno(this.handleTurnoActualizado.bind(this));
    this.ajedrezUseCase.subscribeJaque(this.handleJaqueActualizado.bind(this));
    this.ajedrezUseCase.subscribeFinPartida(this.handlePartidaFinalizada.bind(this));
    this.ajedrezUseCase.subscribePromocion(this.handlePromocionRequerida.bind(this));
    this.ajedrezUseCase.subscribeTablas(this.handleTablasActualizadas.bind(this));
    this.ajedrezUseCase.subscribeReinicio(this.handleReinicioActualizado.bind(this));
    this.ajedrezUseCase.subscribeAbandono(this.handleOponenteAbandono.bind(this));
    this.ajedrezUseCase.subscribePartidaIniciada(this.handleReinicioPartida.bind(this));
    this.ajedrezUseCase.subscribeError(this.handleError.bind(this));

    logger.log('[PartidaVM] inicializarPartida:', {
      id: partida.id,
      salaId: partida.salaId,
      miColor,
      miNombre,
      turnoActual: partida.turnoActual,
    });
  }

  /**
   * Maneja el toque sobre una casilla: selecciona pieza propia o ejecuta el movimiento.
   *
   * @remarks
   * Comportamiento según el contexto:
   * - Si hay pieza seleccionada y la casilla es un destino legal → realiza el movimiento.
   * - Si la casilla tiene una pieza propia → la selecciona y calcula sus destinos.
   * - En otro caso → deselecciona.
   *
   * Rechaza la interacción (fijando {@link PartidaVM.error}) si no hay tablero/color, si
   * hay un movimiento pendiente sin confirmar, o si no es el turno del jugador local.
   */
  seleccionarCasilla(posicion: Posicion): void {
    // Trazas para depuración: siempre registrar intento de selección
    logger.log('[TRACE PartidaVM] seleccionarCasilla invoked', {
      posicion,
      tableroExists: !!this.tablero,
      miColor: this.miColor,
      turnoActual: this.partida?.turnoActual,
      esMiTurno: this.esMiTurno(),
      movimientoPendiente: !!this.movimientoPendiente,
    });

    // Mensajes de ayuda en UI para depuración
    if (!this.tablero) {
      runInAction(() => { this.error = 'Tablero no inicializado'; });
      logger.warn('[PartidaVM] seleccionarCasilla: tablero no inicializado');
      return;
    }
    if (!this.miColor) {
      runInAction(() => { this.error = 'Color del jugador no determinado'; });
      logger.warn('[PartidaVM] seleccionarCasilla: miColor no definido');
      return;
    }

    // Si ya hay un movimiento pendiente, bloquear nuevas selecciones/movimientos
    if (this.movimientoPendiente) {
      runInAction(() => { this.error = 'Hay un movimiento pendiente. Confirma o deshaz antes de mover.'; });
      logger.warn('[PartidaVM] seleccionarCasilla: intento con movimiento pendiente');
      return;
    }

    if (!this.esMiTurno()) {
      // No es el turno del jugador local: informar en consola y en UI
      runInAction(() => { this.error = 'No es tu turno'; });
      logger.warn('[PartidaVM] seleccionarCasilla: intento fuera de turno');
      return;
    }

    // Limpiar error previo si todo correcto
    runInAction(() => { this.error = null; });

    const pieza = this.tablero.obtenerPieza(posicion);

    // Si hay pieza seleccionada y clickeamos un movimiento posible
    if (this.piezaSeleccionada && this.movimientosPosibles.some(m => posicionesIguales(m, posicion))) {
      logger.log('[TRACE PartidaVM] destino seleccionado es movimiento posible', { piezaSeleccionada: this.piezaSeleccionada.id, destino: posicion });
      this.realizarMovimientoLocal(this.piezaSeleccionada, posicion);
      return;
    }

    // Si hay pieza propia en la casilla, seleccionarla
    if (pieza && pieza.color === this.miColor) {
      logger.log('[TRACE PartidaVM] seleccionando pieza propia', { piezaId: pieza.id, posicion });
      const tablero = this.tablero; // Capturar para evitar error de null
      runInAction(() => {
        this.piezaSeleccionada = pieza;
        this.movimientosPosibles = tablero?.obtenerMovimientosPosibles(pieza) || [];
      });
      return;
    }

    // Si no, deseleccionar
    logger.log('[TRACE PartidaVM] casilla vacía o pieza enemiga (no seleccionada)', { posicion });
    runInAction(() => {
      this.piezaSeleccionada = null;
      this.movimientosPosibles = [];
    });
  }

  /**
   * Construye el movimiento (detectando enroque y promoción) y lo envía al servidor,
   * dejándolo como **pendiente** de confirmación.
   *
   * @remarks
   * No aplica el movimiento al tablero local: espera el evento `MovimientoRealizado` del
   * servidor, que trae el tablero ya actualizado.
   */
  private async realizarMovimientoLocal(pieza: Pieza, destino: Posicion): Promise<void> {
    logger.log('[TRACE PartidaVM] realizarMovimientoLocal called with pieza:', pieza, 'destino:', destino);
    if (!this.tablero || !this.partida) {
      logger.log('[TRACE PartidaVM] Early return: missing tablero or partida');
      return;
    }

    // Detectar si es enroque
    this.proximoMovimientoEsEnroque =
      pieza.tipo === 'Rey' && Math.abs(pieza.posicion.columna - destino.columna) === 2;
    logger.log('[TRACE PartidaVM] esEnroque:', this.proximoMovimientoEsEnroque);

    // Crear movimiento
    const piezaCapturada = this.tablero.obtenerPieza(destino);

    // Detectar si es promoción
    const esPromocion = pieza.tipo === 'Peon' &&
                       ((pieza.color === 'Blanca' && destino.fila === 0) ||
                        (pieza.color === 'Negra' && destino.fila === 7));

    logger.log('[TRACE PartidaVM] Detección de promoción:', {
      esPeon: pieza.tipo === 'Peon',
      colorPieza: pieza.color,
      destinoFila: destino.fila,
      esPromocion
    });

    const movimiento = new Movimiento({
      id: `mov-${Date.now()}`,
      piezaId: pieza.id,
      origen: pieza.posicion,
      destino,
      piezaCapturada: piezaCapturada?.id || null,
      esEnroque: this.proximoMovimientoEsEnroque,
      esPromocion,
    });
    logger.log('[TRACE PartidaVM] movimiento created:', movimiento);

    // Enviar el movimiento al backend (RealizarMovimiento)
    try {
      logger.log('[TRACE PartidaVM] Enviando movimiento al backend...');
      await this.ajedrezUseCase.moverPieza(movimiento);

      // El backend responderá con "MovimientoRealizado" que actualizará el tablero
      // Marcar el movimiento como pendiente de confirmación
      runInAction(() => {
        this.movimientoPendiente = movimiento;
        this.piezaSeleccionada = null;
        this.movimientosPosibles = [];
        this.error = null;
      });

      logger.log('[TRACE PartidaVM] Movimiento enviado al backend, esperando confirmación del usuario');
    } catch (error: any) {
      runInAction(() => {
        this.error = error?.message ?? String(error);
        this.piezaSeleccionada = null;
        this.movimientosPosibles = [];
      });
      logger.error('[ERROR PartidaVM] Error al enviar movimiento al backend:', error);
    }
  }


  /**
   * Confirma el movimiento pendiente en el servidor.
   *
   * @remarks
   * Si el movimiento es una promoción, mantiene el estado de promoción para que el
   * usuario elija pieza. Si la confirmación falla, intenta **deshacer** el movimiento.
   */
  async confirmarMovimiento(): Promise<void> {
    logger.log('[TRACE PartidaVM] confirmarMovimiento called');
    try {
      if (!this.movimientoPendiente) {
        logger.log('[PartidaVM] No movimiento pendiente');
        runInAction(() => { this.error = 'No hay movimiento pendiente para confirmar'; });
        return;
      }

      logger.log('[TRACE PartidaVM] Confirmando movimiento en el servidor...');

      // Check if the pending movement is a promotion
      const esPromocion = this.movimientoPendiente.esPromocion;

      // Llamar a confirmarJugada() que mapea a ConfirmarMovimiento del backend
      await this.ajedrezUseCase.confirmarJugada();
      logger.log('[TRACE PartidaVM] Movimiento confirmado en el backend');

      // El backend enviará TurnoActualizado y PromocionRequerida (si aplica)
      runInAction(() => {
        // Don't clear movimientoPendiente or mostrarPromocion if it's a promotion
        // They will be cleared after the promotion is completed
        if (!esPromocion) {
          this.movimientoPendiente = null;
          this.mostrarPromocion = false;
        }
        this.error = null;
      });
    } catch (error: any) {
      runInAction(() => {
        this.error = error?.message ?? String(error);
      });
      logger.error('Error confirmando movimiento:', error);

      // Si falla la confirmación, deshacer el movimiento
      try {
        await this.deshacerMovimiento();
      } catch (err) {
        logger.error('Error deshaciendo tras fallo de confirmación:', err);
      }
    }
  }

  /**
   * Deshace el movimiento pendiente en el servidor (que reenviará el tablero sincronizado).
   *
   * @remarks
   * Solo actúa si hay un movimiento pendiente; el servidor lo deshace porque ya lo recibió
   * vía {@link PartidaVM.realizarMovimientoLocal}.
   */
  async deshacerMovimiento(): Promise<void> {
    try {
      if (!this.tablero || !this.partida) {
        runInAction(() => { this.error = 'No hay partida activa'; });
        return;
      }

      // FIX: SIEMPRE llamar al backend porque el movimiento ya se envió con moverPieza()
      // El backend tiene el movimiento pendiente y necesita deshacerlo
      if (this.movimientoPendiente) {
        logger.log('[TRACE PartidaVM] Deshaciendo movimiento pendiente en el backend');
        await this.ajedrezUseCase.deshacerJugada();

        runInAction(() => {
          this.movimientoPendiente = null;
          this.piezaSeleccionada = null;
          this.movimientosPosibles = [];
          this.mostrarPromocion = false;
          this.error = null;
        });
        // El backend enviará TableroActualizado que sincronizará el estado
        return;
      }

      // Si no hay movimiento pendiente, no debería haber nada que deshacer
      runInAction(() => { this.error = 'No hay movimiento pendiente para deshacer'; });
      logger.warn('[PartidaVM] deshacerMovimiento: no hay movimiento pendiente');
    } catch (error: any) {
      runInAction(() => { this.error = error?.message ?? String(error); });
      logger.error('Error deshaciendo:', error);
    }
  }

  /**
   * Solicita tablas
   */
  async solicitarTablas(): Promise<void> {
    try {
      await this.ajedrezUseCase.pedirTablas();
      runInAction(() => {
        this.solicitadasTablas = true;
        this.error = null;
      });
      logger.log('[PartidaVM] solicitarTablas invoked');
    } catch (error: any) {
      runInAction(() => {
        this.error = error?.message ?? String(error);
      });
      logger.error('Error solicitando tablas:', error);
    }
  }

  /**
   * Retira solicitud de tablas
   */
  async retirarTablas(): Promise<void> {
    try {
      await this.ajedrezUseCase.cancelarTablas();
      runInAction(() => {
        this.solicitadasTablas = false;
        this.tablasOfrecidas = false;
        this.error = null;
      });
      logger.log('[PartidaVM] retirarTablas invoked');
    } catch (error: any) {
      runInAction(() => {
        this.error = error?.message ?? String(error);
      });
      logger.error('Error retirando tablas:', error);
    }
  }

  /**
   * Se rinde de la partida
   */
  async rendirse(): Promise<void> {
    logger.log('[PartidaVM] rendirse() invoked - starting');
    try {
      logger.log('[PartidaVM] Calling ajedrezUseCase.rendirsePartida()');
      await this.ajedrezUseCase.rendirsePartida();
      runInAction(() => { this.error = null; });
      logger.log('[PartidaVM] rendirse completed successfully');
    } catch (error: any) {
      logger.error('[PartidaVM] Error en rendirse:', error);
      runInAction(() => {
        this.error = `Error al rendirse: ${error?.message ?? String(error)}`;
      });
    }
  }

  /**
   * Envía al servidor el tipo de pieza elegido para coronar el peón y cierra el modal.
   *
   * @param tipo - Pieza a la que se corona (Torre, Caballo, Alfil o Reina).
   */
  async promocionarPeon(tipo: TipoPieza): Promise<void> {
    try {
      if (!this.movimientoPendiente) {
        throw new Error('No hay movimiento pendiente de promoción');
      }

      logger.log('[PartidaVM] Enviando promoción al backend:', tipo);
      await this.ajedrezUseCase.seleccionarPromocion(tipo);

      runInAction(() => {
        this.mostrarPromocion = false;
        this.movimientoPendiente = null;
        this.error = null;
      });

      logger.log('[PartidaVM] Promoción completada y movimiento limpiado');
    } catch (error: any) {
      runInAction(() => { this.error = error?.message ?? String(error); });
      logger.error('Error promocionando:', error);
    }
  }

  /**
   * Solicita reinicio de partida
   */
  async solicitarReinicio(): Promise<void> {
    try {
      await this.ajedrezUseCase.pedirReinicio();
      runInAction(() => { this.solicitadoReinicio = true; this.error = null; });
      logger.log('[PartidaVM] solicitarReinicio invoked');
    } catch (error: any) {
      runInAction(() => { this.error = error?.message ?? String(error); });
      logger.error('Error solicitando reinicio:', error);
    }
  }

  /**
   * Retira solicitud de reinicio
   */
  async retirarReinicio(): Promise<void> {
    try {
      await this.ajedrezUseCase.cancelarReinicio();
      runInAction(() => {
        this.solicitadoReinicio = false;
        this.oponenteSolicitoReinicio = false;
        this.error = null;
      });
      logger.log('[PartidaVM] retirarReinicio invoked');
    } catch (error: any) {
      runInAction(() => { this.error = error?.message ?? String(error); });
      logger.error('Error retirando reinicio:', error);
    }
  }

  /**
   * Cierra el modal de fin de partida explícitamente
   */
  cerrarModalFinPartida(): void {
    runInAction(() => {
      this.mostrarFinPartida = false;
    });
  }

  /**
   * Sale de la partida hacia el menú: notifica al servidor, desuscribe y limpia el estado.
   *
   * @remarks
   * Activa la bandera {@link PartidaVM.estaSaliendo} para ignorar eventos que aún puedan
   * llegar durante la salida, cierra el modal de fin de partida y avisa al servidor para
   * que notifique el abandono al rival.
   */
  volverAlMenu(): void {
    // Set flag to prevent processing any more events
    this.estaSaliendo = true;

    // Close the modal immediately to prevent it from persisting
    this.cerrarModalFinPartida();

    // Notify the server we're leaving so it sends JugadorAbandonado to the opponent
    this.ajedrezUseCase.salirDeSala().catch(() => {});
    this.ajedrezUseCase.unsubscribeAll();
    this.reset();
  }

  // Handlers de eventos del servidor

  /** Evento `MovimientoRealizado`: sustituye el tablero local por el del servidor y refresca los capturados. */
  handleMovimiento(movimiento: Movimiento, tablero: Tablero): void {
    logger.log('[PartidaVM] handleMovimiento recibido del backend', {
      movId: movimiento.id,
      esPromocion: movimiento.esPromocion,
      piezasEnTablero: tablero.piezas.length,
      piezasEliminadas: tablero.piezas.filter(p => p.eliminada).length
    });

    runInAction(() => {
      // FIX: Hacer observable el tablero recibido para que MobX detecte cambios
      safeObservable(tablero);

      // Actualizar el tablero con el estado del backend
      this.tablero = tablero;
      if (this.partida) {
        this.partida.tablero = tablero;
      }
      this.actualizarPiezasEliminadas();
      this.error = null;

      // Nota: el modal de promoción se muestra al recibir el evento
      // "PromocionRequerida" del servidor (tras confirmar el movimiento),
      // no aquí: los ids de movimiento de cliente y servidor no coinciden.
    });

    logger.log('[PartidaVM] Tablero actualizado desde el backend', {
      piezasActuales: this.tablero?.piezas.length
    });
  }

  handleTableroActualizado(tablero: Tablero): void {
    logger.log('[PartidaVM] handleTableroActualizado recibido del backend (deshacer)', {
      piezasEnTablero: tablero.piezas.length,
      piezasEliminadas: tablero.piezas.filter(p => p.eliminada).length
    });

    runInAction(() => {
      // FIX: Hacer observable el tablero recibido para que MobX detecte cambios
      safeObservable(tablero);

      // Actualizar el tablero con el estado del backend después de deshacer
      this.tablero = tablero;
      if (this.partida) {
        this.partida.tablero = tablero;
      }
      this.actualizarPiezasEliminadas();
      this.error = null;
    });

    logger.log('[PartidaVM] Tablero sincronizado después de deshacer', {
      piezasActuales: this.tablero?.piezas.length
    });
  }

  /**
   * Evento `TurnoActualizado`: actualiza turno y número de jugada.
   *
   * @remarks
   * El servidor no emite un evento de jaque por separado, así que el aviso de jaque se
   * **calcula en el cliente** con el motor local sobre el tablero ya sincronizado.
   */
  handleTurnoActualizado(turno: Color, numeroTurno: number): void {
    runInAction(() => {
      if (this.partida) {
        this.partida.turnoActual = turno;
        this.partida.numeroTurnos = numeroTurno;

        // El servidor no emite un evento de jaque por separado, así que lo
        // calculamos con el motor local sobre el tablero ya sincronizado.
        const enJaque = this.tablero?.hayJaque(turno) ?? false;
        this.partida.hayJaque = enJaque;
        this.mensajeJaque = enJaque ? '¡Jaque!' : null;

        this.actualizarMensajeTurno();
        this.error = null;
      }
    });
    logger.log('[PartidaVM] handleTurnoActualizado recibido', { turno, numeroTurno });
  }

  /**
   * Evento `TablasActualizadas`: actualiza el estado de oferta de tablas de ambos bandos.
   *
   * @remarks
   * Traduce el estado absoluto (blancas/negras) a las banderas relativas del jugador local
   * ({@link PartidaVM.tablasOfrecidas} = el rival me ofrece; {@link PartidaVM.solicitadasTablas}
   * = yo he ofrecido).
   */
  handleTablasActualizadas(blancas: boolean, negras: boolean): void {
    // Don't process events if we're leaving
    if (this.estaSaliendo) {
      logger.log('[PartidaVM] handleTablasActualizadas: ignorando evento porque estamos saliendo');
      return;
    }

    runInAction(() => {
      if (this.partida) {
        this.partida.tablasBlancas = blancas;
        this.partida.tablasNegras = negras;

        // Update our states based on opponent's state
        if (this.miColor === 'Blanca') {
          // We are white, opponent is black
          this.tablasOfrecidas = negras; // Show accept button if opponent offers
          this.solicitadasTablas = blancas; // We offered tables
        } else if (this.miColor === 'Negra') {
          // We are black, opponent is white
          this.tablasOfrecidas = blancas; // Show accept button if opponent offers
          this.solicitadasTablas = negras; // We offered tables
        }
        this.error = null;
      }
    });
    logger.log('[PartidaVM] handleTablasActualizadas recibido', { blancas, negras });
  }

  /**
   * Evento `PartidaFinalizada`: muestra el modal de fin de partida y guarda resultado y motivo.
   *
   * @param resultado - Resultado absoluto (la UI lo interpreta según el color local).
   * @param ganador - Id del jugador ganador (cuando aplica).
   */
  handlePartidaFinalizada(resultado: ResultadoPartida, tipo: TipoFinPartida, ganador?: string): void {
    // Don't process events if we're leaving
    if (this.estaSaliendo) {
      logger.log('[PartidaVM] handlePartidaFinalizada: ignorando evento porque estamos saliendo');
      return;
    }

    runInAction(() => {
      this.mostrarFinPartida = true;
      if (this.partida) {
        this.partida.estado = 'Finalizada';
        this.partida.resultado = resultado;
        this.partida.tipoFin = tipo;
      }
      this.error = null;
    });
    logger.log('[PartidaVM] handlePartidaFinalizada recibido', { resultado, tipo, ganador });
  }

  handleJaqueActualizado(hayJaque: boolean): void {
    runInAction(() => {
      if (this.partida) {
        this.partida.hayJaque = hayJaque;
        if (hayJaque) {
          this.mensajeJaque = '¡Jaque!';
        } else {
          this.mensajeJaque = null;
        }
        this.error = null;
      }
    });
    logger.log('[PartidaVM] handleJaqueActualizado recibido', hayJaque);
  }

  handlePromocionRequerida(): void {
    runInAction(() => {
      this.mostrarPromocion = true;
    });
    logger.log('[PartidaVM] handlePromocionRequerida recibido');
  }

  handleReinicioActualizado(blancas: boolean, negras: boolean): void {
    // Don't process events if we're leaving
    if (this.estaSaliendo) {
      logger.log('[PartidaVM] handleReinicioActualizado: ignorando evento porque estamos saliendo');
      return;
    }

    runInAction(() => {
      if (this.miColor === 'Blanca') {
        this.solicitadoReinicio = blancas;
        this.oponenteSolicitoReinicio = negras;
      } else {
        this.solicitadoReinicio = negras;
        this.oponenteSolicitoReinicio = blancas;
      }
    });
    logger.log('[PartidaVM] handleReinicioActualizado recibido', { blancas, negras, miColor: this.miColor });
  }

  /**
   * Evento `PartidaIniciada` durante una revancha: reemplaza la partida y reinicia el estado de la UI.
   *
   * @remarks
   * Vuelve a determinar el color del jugador local por `connectionId` (el servidor puede
   * intercambiar colores en la revancha), con el nombre como respaldo.
   */
  handleReinicioPartida(partida: Partida): void {
    logger.log('[PartidaVM] handleReinicioPartida: nueva partida recibida', partida.id);

    // Don't process events if we're leaving
    if (this.estaSaliendo) {
      logger.log('[PartidaVM] handleReinicioPartida: ignorando evento porque estamos saliendo');
      return;
    }

    runInAction(() => {
      safeObservable(partida);
      safeObservable(partida.tablero);
      this.partida = partida;
      this.tablero = partida.tablero;

      // Re-determinar el color (el servidor puede intercambiar colores en la revancha).
      // Preferimos el connectionId (único) y, como respaldo, el nombre.
      const miConnId = this.ajedrezUseCase.obtenerMiConnectionId();
      const miNombreNorm = this.miNombre.trim().toLowerCase();
      const nnNorm = (partida.jugadorNegras?.nombre ?? '').trim().toLowerCase();

      const soyNegras =
        (miConnId && partida.jugadorNegras?.connectionId === miConnId) ||
        (!miConnId && !!miNombreNorm && miNombreNorm === nnNorm);

      if (soyNegras) {
        this.miColor = 'Negra';
        this.nombreOponente = partida.jugadorBlancas?.nombre ?? '';
      } else {
        this.miColor = 'Blanca';
        this.nombreOponente = partida.jugadorNegras?.nombre ?? '';
      }

      this.piezaSeleccionada = null;
      this.movimientosPosibles = [];
      this.movimientoPendiente = null;
      this.mostrarPromocion = false;
      this.mostrarFinPartida = false;
      this.mensajeJaque = null;
      this.error = null;
      this.tablasOfrecidas = false;
      this.solicitadasTablas = false;
      this.solicitadoReinicio = false;
      this.oponenteSolicitoReinicio = false;
      this.oponenteAbandono = false;
      this.piezasEliminadasBlancas.clear();
      this.piezasEliminadasNegras.clear();
      this.actualizarMensajeTurno();
    });
  }

  handleOponenteAbandono(_connectionId: string): void {
    // Don't process events if we're leaving
    if (this.estaSaliendo) {
      logger.log('[PartidaVM] handleOponenteAbandono: ignorando evento porque estamos saliendo');
      return;
    }

    runInAction(() => {
      this.oponenteAbandono = true;
    });
    logger.log('[PartidaVM] handleOponenteAbandono: oponente se fue');
  }

  handleError(error: string): void {
    runInAction(() => {
      this.error = error;
    });
    logger.error('[PartidaVM] handleError recibido', error);
  }

  // Métodos auxiliares

  /** Arranca el temporizador que incrementa el tiempo de juego cada segundo (limpia el anterior si lo había). */
  private startTimer(): void {
    if (this.timer) {
      clearInterval(this.timer);
    }
    this.timer = setInterval(() => {
      if (this.partida) {
        this.partida.incrementarTiempo(1);
      }
    }, 1000);
  }

  /** Indica si es el turno del jugador local. */
  private esMiTurno(): boolean {
    return this.partida?.turnoActual === this.miColor;
  }

  /** Recalcula los recuentos de piezas capturadas por color a partir del tablero actual. */
  private actualizarPiezasEliminadas(): void {
    if (!this.tablero) return;

    this.piezasEliminadasBlancas.clear();
    this.piezasEliminadasNegras.clear();

    for (const pieza of this.tablero.piezas) {
      if (pieza.eliminada) {
        const map = pieza.color === 'Blanca' ? this.piezasEliminadasBlancas : this.piezasEliminadasNegras;
        map.set(pieza.tipo, (map.get(pieza.tipo) || 0) + 1);
      }
    }
  }

  private actualizarMensajeTurno(): void {
    if (!this.partida) return;

    // Mostrar el nombre del jugador al que le toca mover (usar turnoActual)
    try {
      const nombreTurno = this.partida.turnoActual === 'Blanca'
        ? this.partida.jugadorBlancas.nombre
        : this.partida.jugadorNegras.nombre;

      this.mensajeTurno = `Turno de: ${nombreTurno ?? 'Jugador'}`;
    } catch (err) {
      this.mensajeTurno = 'Turno de: Jugador';
    }
  }

  /**
   * Restablece el estado del ViewModel y detiene el temporizador.
   *
   * @remarks
   * **No** restablece {@link PartidaVM.estaSaliendo} a propósito, para seguir ignorando
   * eventos que lleguen tras abandonar la partida.
   */
  reset(): void {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
    this.partida = null;
    this.tablero = null;
    this.miColor = null;
    this.nombreOponente = '';
    this.piezaSeleccionada = null;
    this.movimientosPosibles = [];
    this.movimientoPendiente = null;
    this.mostrarPromocion = false;
    this.mostrarFinPartida = false;
    this.mensajeTurno = null;
    this.mensajeJaque = null;
    this.error = null;
    this.tablasOfrecidas = false;
    this.solicitadasTablas = false;
    this.solicitadoReinicio = false;
    this.oponenteSolicitoReinicio = false;
    this.oponenteAbandono = false;
    this.piezasEliminadasBlancas.clear();
    this.piezasEliminadasNegras.clear();
    // Note: estaSaliendo is NOT reset here to prevent processing events after leaving
  }
}
