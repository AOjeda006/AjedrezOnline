// src/domain/repositories/IAjedrezRepository.ts
/**
 * Contrato del repositorio de ajedrez: abstrae el transporte (SignalR) del resto de capas.
 *
 * @remarks
 * La implementación concreta es `AjedrezRepositorySignalR` (capa de datos). Notas de diseño:
 *
 * - Las acciones de juego **sin argumentos** (`confirmarMovimiento`, `deshacerMovimiento`,
 *   `solicitarTablas`, `rendirse`, …) no envían el id de partida: el servidor resuelve la
 *   sala/partida a partir del `connectionId` del llamante.
 * - Los métodos `on*` registran **listeners persistentes** de eventos del servidor; los
 *   DTOs entrantes se mapean a entidades de dominio antes de invocar el callback.
 */

import { Color, ConnectionState, ResultadoPartida, TipoFinPartida, TipoPieza } from '../../core/types';
import { Movimiento } from '../entities/Movimiento';
import { Partida } from '../entities/Partida';
import { Sala } from '../entities/Sala';
import { Tablero } from '../entities/Tablero';

export interface IAjedrezRepository {
  // --- Conexión ---
  connect(url: string, jugadorNombre: string): Promise<void>;
  disconnect(): Promise<void>;
  getConnectionState(): ConnectionState;
  /** ConnectionId actual de SignalR (o null si no hay conexión). */
  getConnectionId(): string | null;

  // --- Acciones de sala / partida ---
  crearSala(nombreSala: string): Promise<void>;
  unirseSala(nombreSala: string, nombreJugador: string): Promise<void>;
  abandonarSala(): Promise<void>;

  // --- Movimientos ---
  realizarMovimiento(movimiento: Movimiento): Promise<void>;
  confirmarMovimiento(): Promise<void>;
  deshacerMovimiento(): Promise<void>;

  // --- Tablas / Rendición / Promoción / Reinicio ---
  solicitarTablas(): Promise<void>;
  retirarTablas(): Promise<void>;
  rendirse(): Promise<void>;
  promocionarPeon(tipoPieza: TipoPieza): Promise<void>;
  solicitarReinicio(): Promise<void>;
  retirarReinicio(): Promise<void>;

  // --- Listeners de eventos del servidor ---
  onSalaCreada(callback: (sala: Sala) => void): void;
  onJugadorUnido(callback: (partida: Partida) => void): void;
  onPartidaIniciada(callback: (partida: Partida) => void): void;
  onMovimientoRealizado(callback: (movimiento: Movimiento, tablero: Tablero) => void): void;
  onTableroActualizado(callback: (tablero: Tablero) => void): void;
  onTurnoActualizado(callback: (turno: Color, numeroTurno: number) => void): void;
  onTablasActualizadas(callback: (blancas: boolean, negras: boolean) => void): void;
  onPartidaFinalizada(callback: (resultado: ResultadoPartida, tipo: TipoFinPartida, ganador?: string) => void): void;
  onJaqueActualizado(callback: (hayJaque: boolean) => void): void;
  onPromocionRequerida(callback: () => void): void;
  onReinicioActualizado(callback: (blancas: boolean, negras: boolean) => void): void;
  onJugadorAbandonado(callback: (nombreJugador: string) => void): void;
  onError(callback: (error: string) => void): void;

  // --- Limpieza ---
  offAllListeners(): void;
}
