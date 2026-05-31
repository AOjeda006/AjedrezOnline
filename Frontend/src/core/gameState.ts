/**
 * @module core/gameState
 *
 * Almacén singleton en memoria para la partida "pendiente" de inicializar.
 *
 * @remarks
 * Resuelve una **race condition** de navegación: el evento `PartidaIniciada` puede
 * llegar mientras el usuario sigue en el menú. {@link MenuPrincipalVM} lo recibe y
 * navega a la pantalla de partida, pero para cuando esa pantalla monta y se suscribe,
 * el evento ya se disparó → el ViewModel nunca se inicializaría y el tablero quedaría vacío.
 *
 * La solución: el menú guarda la partida con {@link setPendingPartida} justo antes de
 * navegar, y `usePartida()` la consume con {@link getPendingPartida} en su efecto de
 * montaje, inicializando el ViewModel sin esperar a otro evento.
 */

import { Partida } from '../domain/entities/Partida';

/** Partida recibida pendiente de ser consumida por la pantalla de partida. */
interface PendingPartida {
  partida: Partida;
  /** Nombre del jugador local, necesario para resolver su color al inicializar. */
  miNombre: string;
}

let _pending: PendingPartida | null = null;

/**
 * Guarda la partida recibida para que la pantalla de partida la consuma al montar.
 *
 * @param partida - Partida ya mapeada a dominio.
 * @param miNombre - Nombre del jugador local.
 */
export const setPendingPartida = (partida: Partida, miNombre: string): void => {
  _pending = { partida, miNombre };
};

/**
 * Devuelve la partida pendiente sin consumirla.
 *
 * @returns La partida pendiente, o `null` si no hay ninguna.
 */
export const getPendingPartida = (): PendingPartida | null => _pending;

/** Limpia la partida pendiente. Debe llamarse tras consumirla con {@link getPendingPartida}. */
export const clearPendingPartida = (): void => {
  _pending = null;
};
