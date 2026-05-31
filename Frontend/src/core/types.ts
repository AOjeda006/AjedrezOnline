/**
 * @module core/types
 *
 * Tipos, enumeraciones y utilidades de dominio compartidos por toda la aplicación.
 *
 * @remarks
 * Aquí viven tanto los **tipos de dominio** (como {@link Color} o {@link Posicion})
 * como los **DTOs** que viajan por SignalR desde el servidor. Los DTOs se declaran
 * con todas sus propiedades opcionales porque la deserialización del servidor puede
 * variar; las entidades de dominio (p. ej. {@link Pieza}, {@link Partida}) los
 * normalizan en sus respectivos `createFromDTO`.
 *
 * **Convención de coordenadas** (consistente en cliente y servidor): el tablero es
 * una matriz 8×8 donde `fila 0` es la primera fila de las negras (arriba) y `fila 7`
 * la de las blancas (abajo); `columna 0..7` corresponde a las columnas `a..h`.
 */

/** Identificador de entidad (GUID/cadena generado por el servidor). */
export type ID = string;

/** Estado del ciclo de vida de la conexión SignalR. */
export type ConnectionState = 'Connected' | 'Disconnected' | 'Connecting' | 'Reconnecting';

/** Color (bando) de una pieza o jugador. */
export type Color = 'Blanca' | 'Negra';

/** Tipo de pieza de ajedrez. */
export type TipoPieza = 'Peon' | 'Torre' | 'Caballo' | 'Alfil' | 'Reina' | 'Rey';

/** Estado de una partida a lo largo de su ciclo de vida. */
export type EstadoPartida = 'Esperando' | 'EnCurso' | 'Finalizada';

/**
 * Resultado de una partida desde un punto de vista **absoluto** (no relativo al
 * jugador local). `null` indica que la partida aún no ha terminado.
 *
 * @remarks
 * La UI traduce este valor a "¡Ganaste!" / "Perdiste" combinándolo con el color
 * del jugador local. Coincide con el enum `ResultadoPartida` del servidor.
 */
export type ResultadoPartida = 'VictoriaBlancas' | 'VictoriaNegras' | 'Empate' | null;

/** Motivo por el que finaliza una partida. */
export type TipoFinPartida = 'JaqueMate' | 'Tablas' | 'Rendicion' | 'Abandono';

/**
 * Coordenada de una casilla del tablero.
 *
 * @remarks
 * Ambos campos están en el rango `[0, 7]`. Ver la convención de coordenadas en la
 * documentación del módulo.
 */
export interface Posicion {
  /** Fila (0 = arriba/negras, 7 = abajo/blancas). */
  fila: number;
  /** Columna (0 = `a`, 7 = `h`). */
  columna: number;
}

/**
 * DTO de un movimiento tal como viaja por SignalR.
 *
 * @remarks
 * Las posiciones pueden llegar en camelCase o PascalCase según el serializador del
 * servidor; {@link Movimiento.createFromDTO} se encarga de normalizarlas.
 */
export interface MovimientoDTO {
  id?: ID;
  /** Id de la pieza que se mueve. */
  piezaId?: ID;
  origen?: { fila?: number; columna?: number };
  destino?: { fila?: number; columna?: number };
  /** Id de la pieza capturada, o `null`/ausente si el movimiento no captura. */
  piezaCapturada?: ID | null;
  esEnroque?: boolean;
  esPromocion?: boolean;
  /** `true` cuando el movimiento ha sido confirmado (deja de ser provisional). */
  confirmado?: boolean;
}

/** DTO de una pieza tal como viaja por SignalR. */
export interface PiezaDTO {
  id?: ID;
  tipo?: TipoPieza;
  color?: Color;
  posicion?: { fila?: number; columna?: number };
  /** `true` si la pieza ha sido capturada (no se renderiza en el tablero). */
  eliminada?: boolean;
}

/** DTO del estado del tablero: piezas vivas/capturadas e historial de movimientos. */
export interface TableroDTO {
  piezas?: PiezaDTO[];
  movimientos?: MovimientoDTO[];
}

/** DTO de una sala de juego. */
export interface SalaDTO {
  id?: ID;
  nombre?: string;
  creador?: JugadorDTO;
  /** Segundo jugador, o `null` mientras la sala espera oponente. */
  oponente?: JugadorDTO | null;
  estado?: EstadoPartida;
}

/** DTO completo de una partida (estado de juego + jugadores + flags). */
export interface PartidaDTO {
  id?: ID;
  salaId?: ID;
  tablero?: TableroDTO;
  jugadorBlancas?: JugadorDTO;
  jugadorNegras?: JugadorDTO;
  /** Color al que le toca mover. */
  turnoActual?: Color;
  numeroTurnos?: number;
  /** Tiempo de juego transcurrido, en segundos. */
  tiempoTranscurrido?: number;
  estado?: EstadoPartida;
  resultado?: ResultadoPartida;
  tipoFin?: TipoFinPartida | null;
  /** `true` si las blancas han solicitado tablas. */
  tablasBlancas?: boolean;
  /** `true` si las negras han solicitado tablas. */
  tablasNegras?: boolean;
  hayJaque?: boolean;
  hayJaqueMate?: boolean;
}

/** DTO de un jugador. */
export interface JugadorDTO {
  id?: ID;
  nombre?: string;
  color?: Color;
  /**
   * ConnectionId de SignalR del jugador.
   *
   * @remarks
   * Se usa para identificar de forma **única** al jugador local (comparándolo con el
   * connectionId propio), evitando ambigüedades cuando dos jugadores comparten nombre.
   */
  connectionId?: string;
}

/** Número de filas del tablero. */
export const TABLERO_FILAS = 8;
/** Número de columnas del tablero. */
export const TABLERO_COLUMNAS = 8;

/**
 * Indica si una posición cae dentro de los límites del tablero.
 *
 * @param posicion - Casilla a comprobar.
 * @returns `true` si `fila` y `columna` están ambas en `[0, 7]`.
 */
export const esPosicionValida = (posicion: Posicion): boolean => {
  return posicion.fila >= 0 && posicion.fila < TABLERO_FILAS &&
         posicion.columna >= 0 && posicion.columna < TABLERO_COLUMNAS;
};

/**
 * Compara dos posiciones por valor (misma fila y columna).
 *
 * @returns `true` si ambas casillas son la misma.
 */
export const posicionesIguales = (pos1: Posicion, pos2: Posicion): boolean => {
  return pos1.fila === pos2.fila && pos1.columna === pos2.columna;
};

/**
 * Convierte una posición a notación algebraica (`a1`–`h8`).
 *
 * @remarks
 * La columna se mapea a letra (`0` → `a`) y la fila se invierte (`fila 7` → rank `1`,
 * `fila 0` → rank `8`) para coincidir con la notación estándar donde las blancas
 * ocupan la fila 1.
 *
 * @example
 * ```typescript
 * posicionAString({ fila: 7, columna: 4 }); // 'e1'
 * ```
 *
 * @returns La casilla en notación algebraica de dos caracteres.
 */
export const posicionAString = (posicion: Posicion): string => {
  const columna = String.fromCharCode(97 + posicion.columna);
  const fila = 8 - posicion.fila;
  return `${columna}${fila}`;
};

/**
 * Convierte notación algebraica (`a1`–`h8`) a una {@link Posicion}.
 *
 * @param notacion - Casilla de dos caracteres (letra de columna + número de fila).
 * @returns La posición equivalente, o `null` si la notación es inválida o cae fuera del tablero.
 * @see {@link posicionAString}
 */
export const stringAPosicion = (notacion: string): Posicion | null => {
  if (notacion.length !== 2) return null;
  const columna = notacion.charCodeAt(0) - 97;
  const fila = 8 - parseInt(notacion[1], 10);
  if (columna < 0 || columna >= TABLERO_COLUMNAS || fila < 0 || fila >= TABLERO_FILAS) {
    return null;
  }
  return { fila, columna };
};
