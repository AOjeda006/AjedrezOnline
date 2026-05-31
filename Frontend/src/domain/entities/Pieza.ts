/**
 * Entidad de dominio que representa una pieza de ajedrez en el tablero.
 *
 * @remarks
 * Su `createFromDTO` es tolerante: acepta camelCase y PascalCase, y mapea los enums
 * `tipo`/`color` tanto si llegan como texto como si llegan numéricos.
 */

import { Color, ID, Posicion, TipoPieza } from '../../core/types';

/** Datos de construcción de una {@link Pieza}. */
export interface PiezaProps {
  id: ID;
  tipo: TipoPieza;
  color: Color;
  posicion: Posicion;
  /** `true` si la pieza ya fue capturada. @defaultValue false */
  eliminada?: boolean;
}

export class Pieza {
  id: ID;
  tipo: TipoPieza;
  color: Color;
  posicion: Posicion;
  eliminada: boolean;

  /**
   * Indica si la pieza no se ha movido todavía (necesario para validar el **enroque**).
   *
   * @remarks
   * Se **deriva de la posición inicial** (blancas en fila 7, negras en fila 0) al
   * construir la pieza, ya que el servidor no envía este dato. Por tanto, al
   * reconstruir el tablero desde un DTO el valor se recalcula a partir de la posición
   * actual de la pieza. {@link Pieza.mover} lo pone a `false`.
   */
  nunca_ha_movido: boolean;

  /**
   * @remarks
   * Además de copiar las props, deriva {@link Pieza.nunca_ha_movido} comprobando si la
   * pieza está en su fila de salida según su color.
   */
  constructor(props: PiezaProps) {
    this.id = props.id;
    this.tipo = props.tipo;
    this.color = props.color;
    this.posicion = props.posicion;
    this.eliminada = props.eliminada ?? false;

    if (props.color === 'Blanca' && props.posicion.fila === 7) {
      this.nunca_ha_movido = true;
    } else if (props.color === 'Negra' && props.posicion.fila === 0) {
      this.nunca_ha_movido = true;
    } else {
      this.nunca_ha_movido = false;
    }
  }

  /**
   * Mueve la pieza a una nueva posición.
   *
   * @remarks
   * Marca además {@link Pieza.nunca_ha_movido} como `false` (la pieza pierde el derecho
   * a enrocar). Al simular movimientos para detectar jaque hay que restaurar ese flag
   * manualmente (ver {@link Tablero}).
   */
  mover(nuevaPosicion: Posicion): void {
    this.posicion = nuevaPosicion;
    this.nunca_ha_movido = false;
  }

  /** Marca la pieza como capturada. */
  eliminar(): void {
    this.eliminada = true;
  }

  /**
   * Promociona un peón a otro tipo de pieza.
   *
   * @param nuevoTipo - Tipo al que se corona el peón.
   * @throws Error si la pieza no es un peón.
   */
  promocionar(nuevoTipo: TipoPieza): void {
    if (this.tipo !== 'Peon') {
      throw new Error('Solo los peones pueden ser promocionados');
    }
    this.tipo = nuevoTipo;
  }

  /** Serializa la entidad a un objeto plano (incluye `nunca_ha_movido`). */
  toPlain(): object {
    return {
      id: this.id,
      tipo: this.tipo,
      color: this.color,
      posicion: this.posicion,
      eliminada: this.eliminada,
      nunca_ha_movido: this.nunca_ha_movido,
    };
  }

  /**
   * Construye una {@link Pieza} a partir de un DTO del servidor.
   *
   * @remarks
   * Es tolerante a la forma del DTO: acepta camelCase y PascalCase, normaliza la
   * posición (`fila`/`Fila`) y mapea `tipo`/`color` tanto si llegan como texto
   * (`'Peon'`, `'Blanca'`) como si llegan numéricos (`0..5`, `0..1`).
   *
   * @param dto - DTO de pieza recibido por SignalR.
   * @returns La entidad de dominio equivalente.
   * @throws Error si el DTO es nulo o le faltan `id`, `tipo`, `color` o una posición completa.
   */
  static createFromDTO(dto: any): Pieza {
    if (!dto) throw new Error('DTO nulo para crear Pieza');

    // Leer id con tolerancia a ambas convenciones
    const id: ID = dto.id ?? dto.Id;
    const tipoRaw = dto.tipo ?? dto.Tipo;
    const colorRaw = dto.color ?? dto.Color;
    const eliminada: boolean = dto.eliminada ?? dto.Eliminada ?? false;

    // Mapear tipo si es numérico
    let tipo: TipoPieza;
    if (typeof tipoRaw === 'number') {
      const tipoMap: Record<number, TipoPieza> = {
        0: 'Peon',
        1: 'Torre',
        2: 'Caballo',
        3: 'Alfil',
        4: 'Reina',
        5: 'Rey'
      };
      tipo = tipoMap[tipoRaw];
    } else {
      tipo = tipoRaw;
    }

    // Mapear color si es numérico
    let color: Color;
    if (typeof colorRaw === 'number') {
      const colorMap: Record<number, Color> = {
        0: 'Blanca',
        1: 'Negra'
      };
      color = colorMap[colorRaw];
    } else {
      color = colorRaw;
    }

    // Normalizar posición (puede venir como { fila, columna } o { Fila, Columna })
    const posicionRaw = dto.posicion ?? dto.Posicion;
    if (!posicionRaw) {
      throw new Error(`DTO de Pieza sin posición: ${JSON.stringify(dto)}`);
    }
    const posicion: Posicion = {
      fila: posicionRaw.fila ?? posicionRaw.Fila,
      columna: posicionRaw.columna ?? posicionRaw.Columna,
    };

    if (id === undefined || id === null) throw new Error(`DTO de Pieza sin id: ${JSON.stringify(dto)}`);
    if (tipo === undefined || tipo === null) throw new Error(`DTO de Pieza sin tipo: ${JSON.stringify(dto)}`);
    if (color === undefined || color === null) throw new Error(`DTO de Pieza sin color: ${JSON.stringify(dto)}`);
    if (posicion.fila === undefined || posicion.columna === undefined) {
      throw new Error(`Posición incompleta en Pieza: ${JSON.stringify(posicionRaw)}`);
    }

    return new Pieza({ id, tipo, color, posicion, eliminada });
  }
}