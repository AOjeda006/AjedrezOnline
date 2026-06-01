/**
 * Implementación de {@link IAjedrezUseCase}: valida argumentos, delega en el repositorio
 * y reparte el evento de inicio de partida entre varios suscriptores.
 *
 * @remarks
 * Mantiene un **relay** propio para `PartidaIniciada`: el repositorio solo admite un
 * listener por evento, así que el caso de uso registra **un** listener interno
 * ({@link AjedrezUseCase.handlePartidaIniciadaFromRepo}) y lo reenvía a todos los
 * suscriptores de la UI, además de cachear la última partida en
 * {@link AjedrezUseCase.lastPartida} para reentregarla a quien se suscriba tarde.
 */

import { logger } from '../../core/logger';
import { IAjedrezRepository } from '../repositories/IAjedrezRepository';
import { IAjedrezUseCase } from '../interfaces/IAjedrezUseCase';
import { Movimiento } from '../entities/Movimiento';
import { Tablero } from '../entities/Tablero';
import { Sala } from '../entities/Sala';
import { Partida } from '../entities/Partida';
import { Color, ResultadoPartida, TipoFinPartida, TipoPieza } from '../../core/types';

export class AjedrezUseCase implements IAjedrezUseCase {
  private ajedrezRepository: IAjedrezRepository;

  /** Suscriptores de la UI al evento de inicio de partida. */
  private partidaIniciadaCallbacks: Array<(partida: Partida) => void> = [];
  /** Última partida recibida, reentregada a suscriptores tardíos. */
  private lastPartida: Partida | null = null;

  /**
   * @remarks
   * Registra el listener interno de `PartidaIniciada` en el repositorio (relay). Si el
   * registro falla, lo deja avisado por consola pero no propaga el error.
   */
  constructor(repository: IAjedrezRepository) {
    this.ajedrezRepository = repository;

    try {
      this.ajedrezRepository.onPartidaIniciada((p: Partida) => this.handlePartidaIniciadaFromRepo(p));
      logger.log('[TRACE usecase] Suscripción interna a onPartidaIniciada registrada en constructor');
    } catch (err) {
      logger.warn('[WARN usecase] No se pudo registrar suscripción interna a onPartidaIniciada:', err);
    }
  }

  /** Listener interno: cachea la partida y la reparte entre todos los suscriptores de la UI. */
  private handlePartidaIniciadaFromRepo(partida: Partida): void {
    logger.log('[TRACE usecase] evento PartidaIniciada recibido en usecase (distribuyendo):', { id: partida.id, salaId: partida.salaId });
    this.lastPartida = partida;
    for (const cb of this.partidaIniciadaCallbacks) {
      try {
        cb(partida);
      } catch (err) {
        logger.error('[ERROR usecase] callback PartidaIniciada falló:', err);
      }
    }
  }

  /**
   * Conecta al servidor SignalR con el nombre del jugador.
   * @throws Error si el nombre está vacío.
   */
  async conectarJugador(url: string, nombre: string): Promise<void> {
    if (!nombre || nombre.trim().length === 0) {
      throw new Error('Debes proporcionar un nombre de jugador para conectar');
    }
    logger.log('[TRACE usecase] conectarJugador ->', nombre, url);
    return this.ajedrezRepository.connect(url, nombre);
  }

  async desconectarJugador(): Promise<void> {
    logger.log('[TRACE usecase] desconectarJugador');
    return this.ajedrezRepository.disconnect();
  }

  /**
   * Crea una sala con el nombre dado.
   * @throws Error si el nombre de la sala está vacío.
   */
  async crearNuevaSala(nombreSala: string): Promise<void> {
    if (!nombreSala || nombreSala.trim().length === 0) {
      throw new Error('El nombre de la sala no puede estar vacío');
    }
    logger.log('[TRACE usecase] crearNuevaSala ->', nombreSala);
    return this.ajedrezRepository.crearSala(nombreSala);
  }

  /**
   * Une al jugador a una sala existente.
   * @throws Error si el nombre de la sala o del jugador están vacíos.
   */
  async unirseASala(nombreSala: string, nombreJugador: string): Promise<void> {
    if (!nombreSala || nombreSala.trim().length === 0) {
      throw new Error('El nombre de la sala no puede estar vacío');
    }
    if (!nombreJugador || nombreJugador.trim().length === 0) {
      throw new Error('Debes ingresar tu nombre de jugador antes de unirte a una sala');
    }
    logger.log('[TRACE usecase] unirseASala -> sala:', nombreSala, 'jugador:', nombreJugador);
    try {
      await this.ajedrezRepository.unirseSala(nombreSala, nombreJugador);
    } catch (err) {
      logger.error('[ERROR usecase] unirseASala falló:', err);
      throw err;
    }
  }

  async salirDeSala(): Promise<void> {
    logger.log('[TRACE usecase] salirDeSala');
    return this.ajedrezRepository.abandonarSala();
  }

  obtenerMiConnectionId(): string | null {
    return this.ajedrezRepository.getConnectionId();
  }

  /**
   * Envía un movimiento al servidor (queda provisional hasta confirmar/deshacer).
   * @throws Error si el movimiento es nulo o no tiene `id`.
   */
  async moverPieza(movimiento: Movimiento): Promise<void> {
    if (!movimiento || !movimiento.id) {
      throw new Error('Movimiento inválido');
    }
    logger.log('[TRACE usecase] moverPieza ->', movimiento.id);
    return this.ajedrezRepository.realizarMovimiento(movimiento);
  }

  async confirmarJugada(): Promise<void> {
    logger.log('[TRACE usecase] confirmarJugada');
    return this.ajedrezRepository.confirmarMovimiento();
  }

  async deshacerJugada(): Promise<void> {
    logger.log('[TRACE usecase] deshacerJugada');
    return this.ajedrezRepository.deshacerMovimiento();
  }

  async pedirTablas(): Promise<void> {
    logger.log('[TRACE usecase] pedirTablas');
    return this.ajedrezRepository.solicitarTablas();
  }

  async cancelarTablas(): Promise<void> {
    logger.log('[TRACE usecase] cancelarTablas');
    return this.ajedrezRepository.retirarTablas();
  }

  async rendirsePartida(): Promise<void> {
    logger.log('[TRACE usecase] rendirsePartida');
    return this.ajedrezRepository.rendirse();
  }

  /**
   * Envía el tipo de pieza elegido para coronar un peón.
   * @throws Error si el tipo no es promocionable (debe ser Torre, Caballo, Alfil o Reina).
   */
  async seleccionarPromocion(tipo: TipoPieza): Promise<void> {
    if (!['Torre', 'Caballo', 'Alfil', 'Reina'].includes(tipo)) {
      throw new Error(`Tipo de pieza inválido para promoción: ${tipo}`);
    }
    logger.log('[TRACE usecase] seleccionarPromocion ->', tipo);
    return this.ajedrezRepository.promocionarPeon(tipo);
  }

  async pedirReinicio(): Promise<void> {
    logger.log('[TRACE usecase] pedirReinicio');
    return this.ajedrezRepository.solicitarReinicio();
  }

  async cancelarReinicio(): Promise<void> {
    logger.log('[TRACE usecase] cancelarReinicio');
    return this.ajedrezRepository.retirarReinicio();
  }

  subscribeSalaCreada(callback: (sala: Sala) => void): void {
    logger.log('[TRACE usecase] subscribeSalaCreada registrado');
    this.ajedrezRepository.onSalaCreada((sala) => {
      logger.log('[TRACE usecase] evento SalaCreada recibido en usecase:', { id: sala.id, nombre: sala.nombre });
      callback(sala);
    });
  }

  subscribeJugadorUnido(callback: (partida: Partida) => void): void {
    logger.log('[TRACE usecase] subscribeJugadorUnido registrado');
    this.ajedrezRepository.onJugadorUnido((partida) => {
      logger.log('[TRACE usecase] evento JugadorUnido recibido en usecase:', { id: partida.id, salaId: partida.salaId });
      callback(partida);
    });
  }

  /**
   * Registra un suscriptor al inicio de partida.
   *
   * @remarks
   * Si ya hay una partida cacheada ({@link AjedrezUseCase.lastPartida}), invoca el callback
   * de inmediato para no perder el evento si la suscripción llega tarde.
   */
  subscribePartidaIniciada(callback: (partida: Partida) => void): void {
    logger.log('[TRACE usecase] subscribePartidaIniciada registrado (UI)');
    this.partidaIniciadaCallbacks.push(callback);
    if (this.lastPartida) {
      try {
        logger.log('[TRACE usecase] entregando lastPartida inmediatamente al nuevo suscriptor:', { id: this.lastPartida.id });
        callback(this.lastPartida);
      } catch (err) {
        logger.error('[ERROR usecase] callback inicial (lastPartida) falló:', err);
      }
    }
  }

  subscribeMovimiento(callback: (movimiento: Movimiento, tablero: Tablero) => void): void {
    logger.log('[TRACE usecase] subscribeMovimiento registrado');
    this.ajedrezRepository.onMovimientoRealizado((movimiento, tablero) => {
      callback(movimiento, tablero);
    });
  }

  subscribeTableroActualizado(callback: (tablero: Tablero) => void): void {
    logger.log('[TRACE usecase] subscribeTableroActualizado registrado');
    this.ajedrezRepository.onTableroActualizado((tablero) => {
      callback(tablero);
    });
  }

  subscribeTurno(callback: (turno: Color, numeroTurno: number) => void): void {
    logger.log('[TRACE usecase] subscribeTurno registrado');
    this.ajedrezRepository.onTurnoActualizado((turno, numeroTurno) => {
      callback(turno, numeroTurno);
    });
  }

  subscribeTablas(callback: (blancas: boolean, negras: boolean) => void): void {
    logger.log('[TRACE usecase] subscribeTablas registrado');
    this.ajedrezRepository.onTablasActualizadas((blancas, negras) => {
      callback(blancas, negras);
    });
  }

  subscribeFinPartida(callback: (resultado: ResultadoPartida, tipo: TipoFinPartida, ganador?: string) => void): void {
    logger.log('[TRACE usecase] subscribeFinPartida registrado');
    this.ajedrezRepository.onPartidaFinalizada((resultado, tipo, ganador) => {
      callback(resultado, tipo, ganador);
    });
  }

  subscribeJaque(callback: (hayJaque: boolean) => void): void {
    logger.log('[TRACE usecase] subscribeJaque registrado');
    this.ajedrezRepository.onJaqueActualizado((hayJaque) => {
      callback(hayJaque);
    });
  }

  subscribePromocion(callback: () => void): void {
    logger.log('[TRACE usecase] subscribePromocion registrado');
    this.ajedrezRepository.onPromocionRequerida(() => {
      callback();
    });
  }

  subscribeReinicio(callback: (blancas: boolean, negras: boolean) => void): void {
    logger.log('[TRACE usecase] subscribeReinicio registrado');
    this.ajedrezRepository.onReinicioActualizado((blancas, negras) => {
      callback(blancas, negras);
    });
  }

  subscribeAbandono(callback: (nombreJugador: string) => void): void {
    logger.log('[TRACE usecase] subscribeAbandono registrado');
    this.ajedrezRepository.onJugadorAbandonado((nombreJugador) => {
      callback(nombreJugador);
    });
  }

  subscribeError(callback: (error: string) => void): void {
    logger.log('[TRACE usecase] subscribeError registrado');
    this.ajedrezRepository.onError((error) => {
      callback(error);
    });
  }

  /**
   * Elimina todos los listeners y limpia el estado de suscripción.
   *
   * @remarks
   * Tras vaciar listeners y caché, **re-registra el relay interno** de `PartidaIniciada`
   * para que el evento siga funcionando en la siguiente partida/sesión.
   */
  unsubscribeAll(): void {
    logger.log('[TRACE usecase] unsubscribeAll -> delegando a repo.offAllListeners y limpiando callbacks');
    this.ajedrezRepository.offAllListeners();
    this.partidaIniciadaCallbacks = [];
    this.lastPartida = null;
    // Re-register the internal relay handler so PartidaIniciada events work in the next session
    this.ajedrezRepository.onPartidaIniciada((p: Partida) => this.handlePartidaIniciadaFromRepo(p));
  }
}
