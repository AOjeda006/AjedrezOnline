/**
 * Entidad de dominio que representa un movimiento sobre el tablero.
 *
 * @remarks
 * Un movimiento puede ser **provisional** (recién enviado, pendiente de confirmar) o
 * **confirmado**. Cubre además casos especiales mediante flags: enroque y promoción.
 */

import { ID, Posicion } from '../../core/types';

/** Datos de construcción de un {@link Movimiento}. */
export interface MovimientoProps {
  id: ID;
  piezaId: ID;
  origen: Posicion;
  destino: Posicion;
  /** Id de la pieza capturada, o `null` si no hay captura. */
  piezaCapturada?: ID | null;
  /** `true` si el movimiento es un enroque (el rey se desplaza dos columnas). */
  esEnroque?: boolean;
  /** `true` si el movimiento corona un peón. */
  esPromocion?: boolean;
  /** `true` si el movimiento ya está confirmado. @defaultValue false */
  confirmado?: boolean;
}

export class Movimiento {
  id: ID;
  piezaId: ID;
  origen: Posicion;
  destino: Posicion;
  piezaCapturada: ID | null;
  esEnroque: boolean;
  esPromocion: boolean;
  confirmado: boolean;

  constructor(props: MovimientoProps) {
    this.id = props.id;
    this.piezaId = props.piezaId;
    this.origen = props.origen;
    this.destino = props.destino;
    this.piezaCapturada = props.piezaCapturada ?? null;
    this.esEnroque = props.esEnroque ?? false;
    this.esPromocion = props.esPromocion ?? false;
    this.confirmado = props.confirmado ?? false;
  }

  /** Marca el movimiento como confirmado (deja de ser provisional). */
  confirmar(): void {
    this.confirmado = true;
  }

  /** Serializa la entidad a un objeto plano (apto para enviar como DTO). */
  toPlain(): object {
    return {
      id: this.id,
      piezaId: this.piezaId,
      origen: this.origen,
      destino: this.destino,
      piezaCapturada: this.piezaCapturada,
      esEnroque: this.esEnroque,
      esPromocion: this.esPromocion,
      confirmado: this.confirmado,
    };
  }

  /**
   * Normaliza una posición que puede llegar como `{ fila, columna }` o
   * `{ Fila, Columna }` (el servidor serializa en PascalCase).
   *
   * @remarks
   * Sin esta normalización, accesos como `origen.fila` devolverían `undefined` y
   * romperían, por ejemplo, la detección de captura al paso.
   *
   * @returns La posición con claves en camelCase.
   * @throws Error si la posición es nula/ausente.
   */
  private static normalizarPosicion(pos: any): Posicion {
    if (!pos) {
      throw new Error('Movimiento sin posición de origen/destino');
    }
    return {
      fila: pos.fila ?? pos.Fila,
      columna: pos.columna ?? pos.Columna,
    };
  }

  /**
   * Construye un {@link Movimiento} a partir de un DTO del servidor.
   *
   * @remarks
   * Tolera camelCase y PascalCase, y normaliza las posiciones con
   * {@link Movimiento.normalizarPosicion}.
   *
   * @param dto - DTO de movimiento recibido por SignalR.
   * @returns La entidad de dominio equivalente.
   * @throws Error si faltan `id`, `piezaId`, `origen` o `destino`.
   */
  static createFromDTO(dto: any): Movimiento {
    // Aceptar PascalCase o camelCase
    const id = dto.id ?? dto.Id;
    const piezaId = dto.piezaId ?? dto.PiezaId;
    const origen = Movimiento.normalizarPosicion(dto.origen ?? dto.Origen);
    const destino = Movimiento.normalizarPosicion(dto.destino ?? dto.Destino);
    const piezaCapturada = dto.piezaCapturada ?? dto.PiezaCapturada ?? null;
    const esEnroque = dto.esEnroque ?? dto.EsEnroque ?? false;
    const esPromocion = dto.esPromocion ?? dto.EsPromocion ?? false;
    const confirmado = dto.confirmado ?? dto.Confirmado ?? false;

    if (!id || !piezaId || !origen || !destino) {
      throw new Error('DTO incompleto para crear Movimiento');
    }

    return new Movimiento({
      id,
      piezaId,
      origen,
      destino,
      piezaCapturada,
      esEnroque,
      esPromocion,
      confirmado,
    });
  }
}
