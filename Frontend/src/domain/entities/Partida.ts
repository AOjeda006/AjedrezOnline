/**
 * Entidad de dominio que representa una partida completa: tablero, jugadores, turno y
 * estado de finalización.
 *
 * @remarks
 * En **juego en línea**, la autoridad es el servidor: la mayoría de transiciones de
 * estado llegan como eventos SignalR y el ViewModel actualiza esta entidad. Los métodos
 * que mutan el tablero localmente ({@link Partida.realizarMovimiento},
 * {@link Partida.deshacerMovimiento}) implementan la lógica de juego local y no se usan
 * en la ruta en línea principal.
 *
 * El campo {@link Partida.resultado | resultado} es **absoluto** (`VictoriaBlancas` /
 * `VictoriaNegras` / `Empate`), no relativo al jugador local.
 */

import { Color, EstadoPartida, ID, ResultadoPartida, TipoFinPartida, TipoPieza } from '../../core/types';
import { Jugador } from './Jugador';
import { Movimiento } from './Movimiento';
import { Tablero } from './Tablero';

/** Datos de construcción de una {@link Partida}. Los campos opcionales toman valores por defecto. */
export interface PartidaProps {
  id: ID;
  salaId: ID;
  tablero: Tablero;
  jugadorBlancas: Jugador;
  jugadorNegras: Jugador;
  /** @defaultValue 'Blanca' */
  turnoActual?: Color;
  /** @defaultValue 0 */
  numeroTurnos?: number;
  /** Segundos jugados. @defaultValue 0 */
  tiempoTranscurrido?: number;
  /** @defaultValue 'Esperando' */
  estado?: EstadoPartida;
  /** @defaultValue null */
  resultado?: ResultadoPartida;
  /** @defaultValue null */
  tipoFin?: TipoFinPartida | null;
  tablasBlancas?: boolean;
  tablasNegras?: boolean;
  hayJaque?: boolean;
  hayJaqueMate?: boolean;
}

export class Partida {
  id: ID;
  salaId: ID;
  tablero: Tablero;
  jugadorBlancas: Jugador;
  jugadorNegras: Jugador;
  turnoActual: Color;
  numeroTurnos: number;
  tiempoTranscurrido: number;
  estado: EstadoPartida;
  resultado: ResultadoPartida;
  tipoFin: TipoFinPartida | null;
  tablasBlancas: boolean;
  tablasNegras: boolean;
  hayJaque: boolean;
  hayJaqueMate: boolean;

  constructor(props: PartidaProps) {
    this.id = props.id;
    this.salaId = props.salaId;
    this.tablero = props.tablero;
    this.jugadorBlancas = props.jugadorBlancas;
    this.jugadorNegras = props.jugadorNegras;
    this.turnoActual = props.turnoActual ?? 'Blanca';
    this.numeroTurnos = props.numeroTurnos ?? 0;
    this.tiempoTranscurrido = props.tiempoTranscurrido ?? 0;
    this.estado = props.estado ?? 'Esperando';
    this.resultado = props.resultado ?? null;
    this.tipoFin = props.tipoFin ?? null;
    this.tablasBlancas = props.tablasBlancas ?? false;
    this.tablasNegras = props.tablasNegras ?? false;
    this.hayJaque = props.hayJaque ?? false;
    this.hayJaqueMate = props.hayJaqueMate ?? false;
  }

  /**
   * Cambia el turno al jugador opuesto
   */
  cambiarTurno(): void {
    this.turnoActual = this.turnoActual === 'Blanca' ? 'Negra' : 'Blanca';
    this.numeroTurnos++;
  }

  /**
   * Incrementa el tiempo transcurrido en la partida
   */
  incrementarTiempo(segundos: number): void {
    this.tiempoTranscurrido += segundos;
  }

  /**
   * Un jugador solicita tablas
   */
  solicitarTablas(color: Color): void {
    if (color === 'Blanca') {
      this.tablasBlancas = true;
    } else {
      this.tablasNegras = true;
    }
  }

  /**
   * Un jugador retira su solicitud de tablas
   */
  retirarTablas(color: Color): void {
    if (color === 'Blanca') {
      this.tablasBlancas = false;
    } else {
      this.tablasNegras = false;
    }
  }

  /**
   * Ambos jugadores aceptan tablas
   */
  aceptarTablas(): void {
    this.estado = 'Finalizada';
    this.resultado = 'Empate';
    this.tipoFin = 'Tablas';
  }

  /**
   * Finaliza la partida por rendición del color indicado.
   *
   * @param color - Color que se rinde (gana el contrario).
   */
  rendirse(color: Color): void {
    this.estado = 'Finalizada';
    const ganador = color === 'Blanca' ? 'Negra' : 'Blanca';
    this.resultado = ganador === 'Blanca' ? 'VictoriaBlancas' : 'VictoriaNegras';
    this.tipoFin = 'Rendicion';
  }

  /**
   * Establece si hay jaque
   */
  establecerJaque(valor: boolean): void {
    this.hayJaque = valor;
  }

  /**
   * Marca jaque mate y finaliza la partida, calculando el ganador.
   *
   * @remarks
   * El bando en jaque mate es el del {@link Partida.turnoActual | turno actual}; gana el
   * contrario.
   */
  establecerJaqueMate(): void {
    this.hayJaqueMate = true;
    this.estado = 'Finalizada';
    const ganador = this.turnoActual === 'Blanca' ? 'Negra' : 'Blanca';
    this.resultado = ganador === 'Blanca' ? 'VictoriaBlancas' : 'VictoriaNegras';
    this.tipoFin = 'JaqueMate';
  }

  /**
   * Finaliza la partida con un motivo y, si aplica, un ganador.
   *
   * @param tipo - Motivo del fin de partida.
   * @param ganador - Color ganador; necesario para jaque mate y rendición (en tablas se ignora).
   */
  finalizarPartida(tipo: TipoFinPartida, ganador?: Color): void {
    this.estado = 'Finalizada';
    this.tipoFin = tipo;

    if (tipo === 'JaqueMate' && ganador) {
      this.resultado = ganador === 'Blanca' ? 'VictoriaBlancas' : 'VictoriaNegras';
    } else if (tipo === 'Tablas') {
      this.resultado = 'Empate';
    } else if (tipo === 'Rendicion' && ganador) {
      this.resultado = ganador === 'Blanca' ? 'VictoriaBlancas' : 'VictoriaNegras';
    }
  }

  /**
   * Aplica un movimiento al tablero (lógica de juego **local**) y actualiza el estado.
   *
   * @remarks
   * Mueve la pieza, gestiona la captura y el enroque, cambia el turno y recalcula
   * jaque/jaque mate. En la ruta de juego en línea esta transición la dirige el servidor.
   *
   * @param movimiento - Movimiento a aplicar.
   * @param esEnroque - Si es `true`, además mueve la torre correspondiente.
   * @throws Error si la pieza no existe o si no es el turno de su color.
   */
  realizarMovimiento(movimiento: Movimiento, esEnroque: boolean = false): void {
    const pieza = this.tablero.piezas.find(p => p.id === movimiento.piezaId);
    if (!pieza) {
      throw new Error(`Pieza con ID ${movimiento.piezaId} no encontrada`);
    }

    // Verificar que la pieza pertenece al jugador actual
    if (pieza.color !== this.turnoActual) {
      throw new Error(`No es el turno de ${pieza.color}`);
    }

    // Aplicar el movimiento
    pieza.mover(movimiento.destino);

    // Si hay pieza capturada, eliminarla
    if (movimiento.piezaCapturada) {
      this.tablero.removerPieza(movimiento.piezaCapturada);
    }

    // Manejar enroque (mover la torre)
    if (esEnroque) {
      this.aplicarEnroque(movimiento);
    }

    // Registrar el movimiento
    this.tablero.registrarMovimiento(movimiento);

    // Actualizar estado de jaque/jaque mate
    this.cambiarTurno();
    this.hayJaque = this.tablero.hayJaque(this.turnoActual);
    this.hayJaqueMate = this.tablero.hayJaqueMate(this.turnoActual);

    if (this.hayJaqueMate) {
      this.establecerJaqueMate();
    }
  }

  /**
   * Mueve la torre que corresponde a un enroque ya aplicado sobre el rey.
   *
   * @remarks
   * Deduce corto/largo por la columna destino del rey (`6` corto → torre `7`→`5`; en
   * otro caso largo → torre `0`→`3`).
   */
  private aplicarEnroque(movimiento: Movimiento): void {
    const filaRey = movimiento.destino.fila;
    const esEnroqueCorto = movimiento.destino.columna === 6;

    if (esEnroqueCorto) {
      // Enroque corto: Torre se mueve de columna 7 a columna 5
      const torre = this.tablero.obtenerPieza({ fila: filaRey, columna: 7 });
      if (torre) {
        torre.mover({ fila: filaRey, columna: 5 });
      }
    } else {
      // Enroque largo: Torre se mueve de columna 0 a columna 3
      const torre = this.tablero.obtenerPieza({ fila: filaRey, columna: 0 });
      if (torre) {
        torre.mover({ fila: filaRey, columna: 3 });
      }
    }
  }

  /** Marca como confirmado el último movimiento del historial (si lo hay). */
  confirmarMovimiento(): void {
    const ultimoMovimiento = this.tablero.obtenerUltimoMovimiento();
    if (ultimoMovimiento) {
      ultimoMovimiento.confirmar();
    }
  }

  /**
   * Revierte el último movimiento (implementación **local**).
   *
   * @remarks
   * Devuelve la pieza a su origen, restaura la pieza capturada si la hubo, deshace el
   * cambio de turno y recalcula el jaque.
   *
   * @throws Error si no hay movimientos que deshacer.
   */
  deshacerMovimiento(): void {
    if (this.tablero.movimientos.length === 0) {
      throw new Error('No hay movimientos para deshacer');
    }

    const ultimoMovimiento = this.tablero.movimientos.pop();
    if (!ultimoMovimiento) return;

    // Deshace el movimiento de la pieza
    const pieza = this.tablero.piezas.find(p => p.id === ultimoMovimiento.piezaId);
    if (pieza) {
      pieza.mover(ultimoMovimiento.origen);
    }

    // Si hay pieza capturada, restaurarla
    if (ultimoMovimiento.piezaCapturada) {
      const piezaCapturada = this.tablero.piezas.find(p => p.id === ultimoMovimiento.piezaCapturada);
      if (piezaCapturada) {
        piezaCapturada.eliminada = false;
      }
    }

    // Deshace cambio de turno
    this.cambiarTurno();

    // Actualizar estado
    this.hayJaque = this.tablero.hayJaque(this.turnoActual);
    this.hayJaqueMate = false;
  }

  /**
   * Corona el peón indicado al tipo de pieza elegido.
   *
   * @throws Error si la pieza no existe o no es un peón.
   */
  promocionarPeon(piezaId: ID, nuevoTipo: TipoPieza): void {
    const pieza = this.tablero.piezas.find(p => p.id === piezaId);
    if (!pieza || pieza.tipo !== 'Peon') {
      throw new Error('La pieza a promocionar no es un peón');
    }

    pieza.promocionar(nuevoTipo);
  }

  /**
   * Convierte la entidad a un objeto plano
   */
  toPlain(): object {
    return {
      id: this.id,
      salaId: this.salaId,
      tablero: this.tablero.toPlain(),
      jugadorBlancas: this.jugadorBlancas.toPlain(),
      jugadorNegras: this.jugadorNegras.toPlain(),
      turnoActual: this.turnoActual,
      numeroTurnos: this.numeroTurnos,
      tiempoTranscurrido: this.tiempoTranscurrido,
      estado: this.estado,
      resultado: this.resultado,
      tipoFin: this.tipoFin,
      tablasBlancas: this.tablasBlancas,
      tablasNegras: this.tablasNegras,
      hayJaque: this.hayJaque,
      hayJaqueMate: this.hayJaqueMate,
    };
  }

  /**
   * Construye una {@link Partida} a partir de un DTO del servidor.
   *
   * @remarks
   * Tolerante a la forma del DTO: acepta camelCase y PascalCase y **normaliza los enums**
   * que puedan llegar como número (`turnoActual`, `estado`, `resultado`, `tipoFin`) o como
   * texto. Delega en los `createFromDTO` de {@link Tablero} y {@link Jugador}.
   *
   * @returns La partida reconstruida.
   * @throws Error si el DTO es nulo o faltan `id`, `salaId`, `tablero` o los jugadores.
   */
  static createFromDTO(dto: any): Partida {
    if (!dto) {
      throw new Error('DTO incompleto para crear Partida');
    }

    // Aceptar PascalCase o camelCase
    const id = dto.id ?? dto.Id ?? null;
    const salaId = dto.salaId ?? dto.SalaId ?? null;

    // Tablero y jugadores pueden venir en PascalCase o camelCase
    const tableroDto = dto.tablero ?? dto.Tablero ?? null;
    const jugadorBlancasDto = dto.jugadorBlancas ?? dto.JugadorBlancas ?? null;
    const jugadorNegrasDto = dto.jugadorNegras ?? dto.JugadorNegras ?? null;

    // Campos opcionales con nombres alternativos
    let turnoActualRaw = dto.turnoActual ?? dto.TurnoActual ?? dto.turno ?? dto.Turno ?? 'Blanca';
    const numeroTurnos = dto.numeroTurnos ?? dto.NumeroTurnos ?? dto.numeroTurno ?? 0;
    const tiempoTranscurrido = dto.tiempoTranscurrido ?? dto.TiempoTranscurrido ?? 0;
    const estado = dto.estado ?? dto.Estado ?? 'Esperando';
    const resultado = dto.resultado ?? dto.Resultado ?? null;
    const tipoFin = dto.tipoFin ?? dto.TipoFin ?? null;
    const tablasBlancas = dto.tablasBlancas ?? dto.TablasBlancas ?? false;
    const tablasNegras = dto.tablasNegras ?? dto.TablasNegras ?? false;
    const hayJaque = dto.hayJaque ?? dto.HayJaque ?? false;
    const hayJaqueMate = dto.hayJaqueMate ?? dto.HayJaqueMate ?? false;

    if (!id || !salaId || !tableroDto || !jugadorBlancasDto || !jugadorNegrasDto) {
      throw new Error('DTO incompleto para crear Partida');
    }

    // Normalizar turnoActual: aceptar string ('Blanca'|'Negra') o número (0 => Blanca, 1 => Negra)
    let turnoActual: Color;
    if (typeof turnoActualRaw === 'number') {
      turnoActual = turnoActualRaw === 1 ? 'Negra' : 'Blanca';
    } else {
      turnoActual = String(turnoActualRaw) as Color;
    }

    // Normalizar enums que pueden llegar como número (si el servidor no serializa
    // enums como texto) o como string.
    const estadoNorm = typeof estado === 'number'
      ? (['Esperando', 'EnCurso', 'Finalizada'][estado] ?? 'Esperando')
      : estado;
    const resultadoNorm = typeof resultado === 'number'
      ? (['VictoriaBlancas', 'VictoriaNegras', 'Empate'][resultado] ?? null)
      : resultado;
    const tipoFinNorm = typeof tipoFin === 'number'
      ? (['JaqueMate', 'Tablas', 'Rendicion', 'Abandono'][tipoFin] ?? null)
      : tipoFin;

    // Mapear subobjetos usando sus helpers
    const tablero = tableroDto instanceof Tablero ? tableroDto : Tablero.createFromDTO(tableroDto);
    const jugadorBlancas = jugadorBlancasDto instanceof Jugador ? jugadorBlancasDto : Jugador.createFromDTO(jugadorBlancasDto);
    const jugadorNegras = jugadorNegrasDto instanceof Jugador ? jugadorNegrasDto : Jugador.createFromDTO(jugadorNegrasDto);

    return new Partida({
      id: String(id),
      salaId: String(salaId),
      tablero,
      jugadorBlancas,
      jugadorNegras,
      turnoActual: turnoActual as Color,
      numeroTurnos: Number(numeroTurnos ?? 0),
      tiempoTranscurrido: Number(tiempoTranscurrido ?? 0),
      estado: estadoNorm as EstadoPartida,
      resultado: resultadoNorm as ResultadoPartida,
      tipoFin: tipoFinNorm as TipoFinPartida | null,
      tablasBlancas: Boolean(tablasBlancas),
      tablasNegras: Boolean(tablasNegras),
      hayJaque: Boolean(hayJaque),
      hayJaqueMate: Boolean(hayJaqueMate),
    });
  }
}
