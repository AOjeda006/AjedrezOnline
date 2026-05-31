/**
 * Entidad de dominio que representa una sala de juego donde se emparejan dos jugadores.
 *
 * @remarks
 * Una sala nace en estado `Esperando` (solo con su creador), pasa a `EnCurso` cuando
 * se une un oponente y a `Finalizada` al terminar la partida.
 */

import { ID, EstadoPartida } from '../../core/types';
import { Jugador } from './Jugador';

/** Datos de construcción de una {@link Sala}. */
export interface SalaProps {
  id: ID;
  nombre: string;
  creador: Jugador;
  /** Segundo jugador; `null` mientras la sala espera oponente. */
  oponente?: Jugador | null;
  /** Estado inicial. @defaultValue 'Esperando' */
  estado?: EstadoPartida;
}

export class Sala {
  id: ID;
  nombre: string;
  creador: Jugador;
  oponente: Jugador | null;
  estado: EstadoPartida;

  constructor(props: SalaProps) {
    this.id = props.id;
    this.nombre = props.nombre;
    this.creador = props.creador;
    this.oponente = props.oponente ?? null;
    this.estado = props.estado ?? 'Esperando';
  }

  /** Asigna el oponente de la sala. */
  agregarOponente(jugador: Jugador): void {
    this.oponente = jugador;
  }

  /** Pasa la sala a `EnCurso`, pero **solo si ya hay un oponente**; si no, no hace nada. */
  iniciarPartida(): void {
    if (this.oponente) {
      this.estado = 'EnCurso';
    }
  }

  /** Marca la sala como `Finalizada`. */
  finalizarPartida(): void {
    this.estado = 'Finalizada';
  }

  /** Serializa la entidad a un objeto plano (jugadores incluidos vía {@link Jugador.toPlain}). */
  toPlain(): object {
    return {
      id: this.id,
      nombre: this.nombre,
      creador: this.creador.toPlain(),
      oponente: this.oponente?.toPlain() ?? null,
      estado: this.estado,
    };
  }

  /**
   * Construye una {@link Sala} a partir de un DTO del servidor.
   *
   * @remarks
   * Tolera camelCase y PascalCase y delega en {@link Jugador.createFromDTO} para los
   * jugadores. Si `creador`/`oponente` ya son instancias de {@link Jugador}, los reutiliza.
   *
   * @returns La entidad de dominio equivalente.
   * @throws Error si faltan `id`, `nombre` o `creador`.
   */
  static createFromDTO(dto: {
    id?: ID;
    Id?: ID;
    nombre?: string;
    Nombre?: string;
    creador?: any;
    Creador?: any;
    oponente?: any | null;
    Oponente?: any | null;
    estado?: EstadoPartida;
    Estado?: EstadoPartida;
  }): Sala {
    if (!dto) {
      throw new Error('DTO incompleto para crear Sala');
    }

    // Aceptar tanto PascalCase como camelCase
    const id = dto.id ?? dto.Id ?? null;
    const nombre = dto.nombre ?? dto.Nombre ?? null;
    const estado = dto.estado ?? dto.Estado ?? null;
    const creadorDto = dto.creador ?? dto.Creador ?? null;
    const oponenteDto = dto.oponente ?? dto.Oponente ?? null;

    if (!id || !nombre || !creadorDto) {
      throw new Error('DTO incompleto para crear Sala');
    }

    // Usar Jugador.createFromDTO para robustez y consistencia
    const creador = creadorDto instanceof Jugador
      ? creadorDto
      : Jugador.createFromDTO(creadorDto);

    const oponente = oponenteDto
      ? (oponenteDto instanceof Jugador ? oponenteDto : Jugador.createFromDTO(oponenteDto))
      : null;

    return new Sala({
      id: String(id),
      nombre: String(nombre),
      creador,
      oponente,
      estado: (estado as EstadoPartida) ?? 'Esperando',
    });
  }
}
