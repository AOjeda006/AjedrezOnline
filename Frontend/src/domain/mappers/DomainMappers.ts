/**
 * @module domain/mappers
 *
 * Adaptadores DTO → entidad de dominio.
 *
 * @remarks
 * Cada mapper delega en el `createFromDTO` de su entidad. Su utilidad es dar un punto
 * único y estable (la capa de datos depende de los mappers, no de los métodos estáticos
 * de las entidades). La tolerancia a camelCase/PascalCase y la validación viven en cada
 * `createFromDTO` correspondiente, que **lanza** si el DTO está incompleto.
 */

import { Jugador } from '../entities/Jugador';
import { Pieza } from '../entities/Pieza';
import { Movimiento } from '../entities/Movimiento';
import { Tablero } from '../entities/Tablero';
import { Sala } from '../entities/Sala';
import { Partida } from '../entities/Partida';

/** Mapea un DTO a {@link Jugador} (delega en {@link Jugador.createFromDTO}). */
export class JugadorDomainMapper {
  static toDomain(dto: any): Jugador {
    return Jugador.createFromDTO(dto);
  }
}

/** Mapea un DTO a {@link Pieza} (delega en {@link Pieza.createFromDTO}). */
export class PiezaDomainMapper {
  static toDomain(dto: any): Pieza {
    return Pieza.createFromDTO(dto);
  }
}

/** Mapea un DTO a {@link Movimiento} (delega en {@link Movimiento.createFromDTO}). */
export class MovimientoDomainMapper {
  static toDomain(dto: any): Movimiento {
    return Movimiento.createFromDTO(dto);
  }
}

/** Mapea un DTO a {@link Tablero} (delega en {@link Tablero.createFromDTO}). */
export class TableroDomainMapper {
  static toDomain(dto: any): Tablero {
    return Tablero.createFromDTO(dto);
  }
}

/** Mapea un DTO a {@link Sala} (delega en {@link Sala.createFromDTO}). */
export class SalaDomainMapper {
  static toDomain(dto: any): Sala {
    return Sala.createFromDTO(dto);
  }
}

/** Mapea un DTO a {@link Partida} (delega en {@link Partida.createFromDTO}). */
export class PartidaDomainMapper {
  static toDomain(dto: any): Partida {
    return Partida.createFromDTO(dto);
  }
}
