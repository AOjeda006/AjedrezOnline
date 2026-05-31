// src/domain/interfaces/IAjedrezUseCase.ts
import { Movimiento } from '../entities/Movimiento';
import { Tablero } from '../entities/Tablero';
import { Sala } from '../entities/Sala';
import { Partida } from '../entities/Partida';
import { Color, ResultadoPartida, TipoFinPartida, TipoPieza } from '../../core/types';

/**
 * Caso de uso de ajedrez: fachada que consume la capa de presentación.
 *
 * @remarks
 * Agrupa las **acciones** (conectar, crear/unirse a sala, mover, etc.) y las
 * **suscripciones** a eventos del servidor (`subscribe*`). Cada acción valida sus
 * argumentos y delega en el repositorio; las suscripciones registran callbacks que se
 * invocan al llegar el evento correspondiente por SignalR.
 *
 * @see {@link IAjedrezRepository}
 */
export interface IAjedrezUseCase {
  // Conexión y salas
  conectarJugador(url: string, nombre: string): Promise<void>;
  desconectarJugador(): Promise<void>;
  crearNuevaSala(nombreSala: string): Promise<void>;
  unirseASala(nombreSala: string, nombreJugador: string): Promise<void>;
  salirDeSala(): Promise<void>;
  /** ConnectionId propio de SignalR, para identificar de forma única al jugador local. */
  obtenerMiConnectionId(): string | null;

  // Movimientos
  /**
   * Envía un movimiento al servidor. Queda **provisional** hasta confirmarlo o deshacerlo.
   * @see {@link IAjedrezUseCase.confirmarJugada}
   * @see {@link IAjedrezUseCase.deshacerJugada}
   */
  moverPieza(movimiento: Movimiento): Promise<void>;
  /** Confirma el movimiento provisional, cediendo el turno al rival. */
  confirmarJugada(): Promise<void>;
  /** Deshace el movimiento provisional aún no confirmado. */
  deshacerJugada(): Promise<void>;

  // Tablas / Rendición / Promoción / Reinicio
  pedirTablas(): Promise<void>;
  cancelarTablas(): Promise<void>;
  rendirsePartida(): Promise<void>;
  seleccionarPromocion(tipo: TipoPieza): Promise<void>;
  pedirReinicio(): Promise<void>;
  cancelarReinicio(): Promise<void>;

  // Suscripciones (UI)
  subscribeSalaCreada(callback: (sala: Sala) => void): void;
  subscribeJugadorUnido(callback: (partida: Partida) => void): void;
  /**
   * Se suscribe al inicio de partida.
   *
   * @remarks
   * Si ya se recibió una partida antes de suscribirse, el caso de uso la **reentrega**
   * de inmediato al nuevo suscriptor (evita perder el evento por una race condition de
   * montaje; ver {@link module:core/gameState}).
   */
  subscribePartidaIniciada(callback: (partida: Partida) => void): void;
  subscribeMovimiento(callback: (movimiento: Movimiento, tablero: Tablero) => void): void;
  subscribeTableroActualizado(callback: (tablero: Tablero) => void): void;
  subscribeTurno(callback: (turno: Color, numeroTurno: number) => void): void;
  subscribeTablas(callback: (blancas: boolean, negras: boolean) => void): void;
  subscribeFinPartida(callback: (resultado: ResultadoPartida, tipo: TipoFinPartida, ganador?: string) => void): void;
  subscribeJaque(callback: (hayJaque: boolean) => void): void;
  subscribePromocion(callback: () => void): void;
  subscribeReinicio(callback: (blancas: boolean, negras: boolean) => void): void;
  subscribeAbandono(callback: (nombreJugador: string) => void): void;
  subscribeError(callback: (error: string) => void): void;

  // Unsubscribe / cleanup
  /** Elimina todas las suscripciones y limpia la partida cacheada (al salir de la partida). */
  unsubscribeAll(): void;
}
