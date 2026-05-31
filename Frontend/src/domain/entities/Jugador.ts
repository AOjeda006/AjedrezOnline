/**
 * Entidad de dominio que representa a un jugador de la partida.
 *
 * @remarks
 * El {@link Jugador.connectionId | connectionId} permite identificar de forma única al
 * jugador local (más fiable que el nombre, que puede repetirse).
 */

import { ID, Color } from '../../core/types';

export class Jugador {
  id: ID;
  nombre: string;
  /** Color asignado por el servidor, o `null` hasta que se asigne. */
  color: Color | null;
  /** ConnectionId de SignalR; `null` si no se conoce. Sirve para identificar al jugador local. */
  connectionId: string | null;

  constructor(id: ID, nombre: string, color: Color | null = null, connectionId: string | null = null) {
    this.id = id;
    this.nombre = nombre;
    this.color = color;
    this.connectionId = connectionId;
  }

  /** Asigna el color (bando) del jugador. */
  asignarColor(color: Color): void {
    this.color = color;
  }

  /**
   * Serializa la entidad a un objeto plano.
   *
   * @remarks
   * No incluye `connectionId` (dato de transporte, no de dominio).
   * @returns Objeto con `id`, `nombre` y `color`.
   */
  toPlain(): object {
    return {
      id: this.id,
      nombre: this.nombre,
      color: this.color,
    };
  }

  /**
   * Construye un {@link Jugador} a partir de un DTO del servidor.
   *
   * @remarks
   * Tolera claves en camelCase y PascalCase (`id`/`Id`/`ID`, `color`/`Color`, …).
   *
   * @param dto - DTO de jugador recibido por SignalR.
   * @returns La entidad de dominio equivalente.
   * @throws Error si el DTO es nulo o le faltan `id`/`nombre`.
   */
  static createFromDTO(dto: any): Jugador {
    if (!dto) {
      throw new Error('DTO de Jugador vacío');
    }

    // Aceptar tanto PascalCase como camelCase
    const id = dto.id ?? dto.Id ?? dto.ID ?? null;
    const nombre = dto.nombre ?? dto.Nombre ?? null;
    const color = dto.color ?? dto.Color ?? null;
    const connectionId = dto.connectionId ?? dto.ConnectionId ?? null;

    if (!id || !nombre) {
      throw new Error('DTO incompleto para crear Jugador');
    }

    return new Jugador(String(id), String(nombre), color ?? null, connectionId ?? null);
  }
}
