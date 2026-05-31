/**
 * Entidad de dominio que mantiene el estado del tablero e implementa las reglas de
 * ajedrez (movimientos legales, jaque y jaque mate), incluidos enroque y captura al paso.
 *
 * @remarks
 * El motor está organizado en dos niveles para evitar recursión infinita:
 *
 * - **Movimientos pseudo-legales** (`calcular*`): qué casillas alcanza una pieza sin
 *   tener en cuenta el jaque propio.
 * - **Movimientos legales** ({@link Tablero.obtenerMovimientosPosibles}): los anteriores
 *   filtrados por {@link Tablero.movimientoDejaEnJaque}, que simula la jugada y descarta
 *   las que dejan al rey en jaque.
 *
 * La detección de jaque ({@link Tablero.hayJaque}) usa **generación de ataques en crudo**
 * ({@link Tablero.estaCasillaAtacada}), que no aplica el filtro de jaque; así se evita el
 * ciclo "¿es legal? → ¿hay jaque? → ¿es legal?…".
 *
 * Coordenadas: ver la convención en {@link module:core/types}.
 */

import { Color, ID, Posicion, esPosicionValida, posicionesIguales } from '../../core/types';
import { Movimiento } from './Movimiento';
import { Pieza } from './Pieza';

export class Tablero {
  /** Todas las piezas, incluidas las capturadas (`eliminada === true`). */
  piezas: Pieza[];
  /** Historial de movimientos confirmados (el último habilita la captura al paso). */
  movimientos: Movimiento[];

  constructor(piezas: Pieza[] = [], movimientos: Movimiento[] = []) {
    this.piezas = piezas;
    this.movimientos = movimientos;
  }

  /**
   * Devuelve la pieza **viva** que ocupa una casilla.
   *
   * @returns La pieza en esa posición, o `null` si está vacía (las capturadas se ignoran).
   */
  obtenerPieza(posicion: Posicion): Pieza | null {
    return this.piezas.find(
      p => !p.eliminada && p.posicion.fila === posicion.fila && p.posicion.columna === posicion.columna
    ) || null;
  }

  /**
   * Obtiene todas las piezas vivas de un color
   */
  obtenerPiezasPorColor(color: Color): Pieza[] {
    return this.piezas.filter(p => p.color === color && !p.eliminada);
  }

  /**
   * Obtiene el rey de un color específico
   */
  obtenerRey(color: Color): Pieza | null {
    return this.piezas.find(p => p.color === color && p.tipo === 'Rey' && !p.eliminada) || null;
  }

  /**
   * Agrega una pieza al tablero
   */
  agregarPieza(pieza: Pieza): void {
    this.piezas.push(pieza);
  }

  /**
   * Remueve una pieza del tablero
   */
  removerPieza(piezaId: ID): void {
    const pieza = this.piezas.find(p => p.id === piezaId);
    if (pieza) {
      pieza.eliminar();
    }
  }

  /**
   * Registra un movimiento en el historial
   */
  registrarMovimiento(movimiento: Movimiento): void {
    this.movimientos.push(movimiento);
  }

  /** Devuelve el último movimiento del historial, o `null` si no hay ninguno. */
  obtenerUltimoMovimiento(): Movimiento | null {
    return this.movimientos.length > 0 ? this.movimientos[this.movimientos.length - 1] : null;
  }

  /**
   * Calcula los movimientos **legales** de una pieza.
   *
   * @remarks
   * Genera los movimientos pseudo-legales según el tipo de pieza y descarta los que
   * dejarían al propio rey en jaque (clavadas, no responder a un jaque, etc.). Es el
   * método que usa la UI para resaltar destinos válidos.
   *
   * @returns Lista de casillas destino legales (puede estar vacía).
   * @see {@link Tablero.movimientoDejaEnJaque}
   */
  obtenerMovimientosPosibles(pieza: Pieza): Posicion[] {
    const movimientos: Posicion[] = [];

    switch (pieza.tipo) {
      case 'Peon':
        this.calcularMovimientosPeon(pieza, movimientos);
        break;
      case 'Torre':
        this.calcularMovimientosTorre(pieza, movimientos);
        break;
      case 'Caballo':
        this.calcularMovimientosCaballo(pieza, movimientos);
        break;
      case 'Alfil':
        this.calcularMovimientosAlfil(pieza, movimientos);
        break;
      case 'Reina':
        this.calcularMovimientosReina(pieza, movimientos);
        break;
      case 'Rey':
        this.calcularMovimientosRey(pieza, movimientos);
        break;
    }

    // Filtra movimientos que dejarían al rey en jaque
    return movimientos.filter(destino => !this.movimientoDejaEnJaque(pieza, destino));
  }

  /**
   * Calcula movimientos posibles para un peón
   */
  private calcularMovimientosPeon(pieza: Pieza, movimientos: Posicion[]): void {
    const direccion = pieza.color === 'Blanca' ? -1 : 1;
    const filaPrimerMovimiento = pieza.color === 'Blanca' ? 6 : 1;

    // Avance de una casilla
    const pos1 = { fila: pieza.posicion.fila + direccion, columna: pieza.posicion.columna };
    if (esPosicionValida(pos1) && !this.obtenerPieza(pos1)) {
      movimientos.push(pos1);

      // Avance de dos casillas (solo en primer movimiento)
      if (pieza.posicion.fila === filaPrimerMovimiento) {
        const pos2 = { fila: pieza.posicion.fila + 2 * direccion, columna: pieza.posicion.columna };
        if (!this.obtenerPieza(pos2)) {
          movimientos.push(pos2);
        }
      }
    }

    // Capturas diagonales
    for (let offset of [-1, 1]) {
      const posCaptura = {
        fila: pieza.posicion.fila + direccion,
        columna: pieza.posicion.columna + offset,
      };
      if (esPosicionValida(posCaptura)) {
        const piezaCaptura = this.obtenerPieza(posCaptura);
        if (piezaCaptura && piezaCaptura.color !== pieza.color) {
          movimientos.push(posCaptura);
        }
      }
    }

    // Captura al paso (en passant)
    this.agregarCapturasAlPaso(pieza, movimientos, direccion);
  }

  /**
   * Añade la captura al paso si procede.
   *
   * @remarks
   * Solo es legal **inmediatamente** después de que un peón rival adyacente avance dos
   * casillas: se comprueba que el último movimiento del historial sea justo eso. La
   * casilla destino es la que el peón rival "saltó".
   *
   * @param direccion - Sentido de avance del peón (`-1` blancas, `+1` negras).
   */
  private agregarCapturasAlPaso(pieza: Pieza, movimientos: Posicion[], direccion: number): void {
    if (pieza.tipo !== 'Peon') return;

    const ultimoMovimiento = this.obtenerUltimoMovimiento();
    if (!ultimoMovimiento) return;

    const piezaMovida = this.piezas.find(p => p.id === ultimoMovimiento.piezaId);
    if (!piezaMovida || piezaMovida.tipo !== 'Peon') return;

    const distancia = Math.abs(ultimoMovimiento.destino.fila - ultimoMovimiento.origen.fila);
    if (distancia !== 2) return;

    if (piezaMovida.posicion.fila === pieza.posicion.fila &&
        Math.abs(piezaMovida.posicion.columna - pieza.posicion.columna) === 1) {
      const posAlPaso = {
        fila: pieza.posicion.fila + direccion,
        columna: piezaMovida.posicion.columna,
      };
      if (esPosicionValida(posAlPaso)) {
        movimientos.push(posAlPaso);
      }
    }
  }

  /**
   * Calcula movimientos posibles para una torre
   */
  private calcularMovimientosTorre(pieza: Pieza, movimientos: Posicion[]): void {
    const direcciones = [
      { fila: -1, columna: 0 },
      { fila: 1, columna: 0 },
      { fila: 0, columna: -1 },
      { fila: 0, columna: 1 },
    ];
    for (const dir of direcciones) {
      this.agregarMovimientosEnLinea(pieza, dir, movimientos);
    }
  }

  /**
   * Calcula movimientos posibles para un alfil
   */
  private calcularMovimientosAlfil(pieza: Pieza, movimientos: Posicion[]): void {
    const direcciones = [
      { fila: -1, columna: -1 },
      { fila: -1, columna: 1 },
      { fila: 1, columna: -1 },
      { fila: 1, columna: 1 },
    ];
    for (const dir of direcciones) {
      this.agregarMovimientosEnLinea(pieza, dir, movimientos);
    }
  }

  /**
   * Calcula movimientos posibles para una reina
   */
  private calcularMovimientosReina(pieza: Pieza, movimientos: Posicion[]): void {
    const direcciones = [
      { fila: -1, columna: 0 }, { fila: 1, columna: 0 },
      { fila: 0, columna: -1 }, { fila: 0, columna: 1 },
      { fila: -1, columna: -1 }, { fila: -1, columna: 1 },
      { fila: 1, columna: -1 }, { fila: 1, columna: 1 },
    ];
    for (const dir of direcciones) {
      this.agregarMovimientosEnLinea(pieza, dir, movimientos);
    }
  }

  /**
   * Recorre una dirección añadiendo casillas vacías hasta toparse con una pieza.
   *
   * @remarks
   * Si la pieza encontrada es rival, su casilla se añade (captura) y se detiene; si es
   * propia, se detiene sin añadirla. Es el núcleo de torre, alfil y reina.
   *
   * @param direccion - Vector de avance, p. ej. `{ fila: 1, columna: 0 }`.
   */
  private agregarMovimientosEnLinea(
    pieza: Pieza,
    direccion: { fila: number; columna: number },
    movimientos: Posicion[]
  ): void {
    let posActual = {
      fila: pieza.posicion.fila + direccion.fila,
      columna: pieza.posicion.columna + direccion.columna,
    };

    while (esPosicionValida(posActual)) {
      const piezaEnCasilla = this.obtenerPieza(posActual);

      if (!piezaEnCasilla) {
        movimientos.push({ ...posActual });
      } else {
        if (piezaEnCasilla.color !== pieza.color) {
          movimientos.push({ ...posActual });
        }
        break;
      }

      posActual = {
        fila: posActual.fila + direccion.fila,
        columna: posActual.columna + direccion.columna,
      };
    }
  }

  /**
   * Calcula movimientos posibles para un caballo
   */
  private calcularMovimientosCaballo(pieza: Pieza, movimientos: Posicion[]): void {
    const saltos = [
      { fila: -2, columna: -1 }, { fila: -2, columna: 1 },
      { fila: -1, columna: -2 }, { fila: -1, columna: 2 },
      { fila: 1, columna: -2 },  { fila: 1, columna: 2 },
      { fila: 2, columna: -1 },  { fila: 2, columna: 1 },
    ];

    for (const salto of saltos) {
      const posDestino = {
        fila: pieza.posicion.fila + salto.fila,
        columna: pieza.posicion.columna + salto.columna,
      };
      if (esPosicionValida(posDestino)) {
        const piezaEnCasilla = this.obtenerPieza(posDestino);
        if (!piezaEnCasilla || piezaEnCasilla.color !== pieza.color) {
          movimientos.push(posDestino);
        }
      }
    }
  }

  /** Calcula los movimientos del rey: las 8 casillas adyacentes más los enroques disponibles. */
  private calcularMovimientosRey(pieza: Pieza, movimientos: Posicion[]): void {
    const desplazamientos = [
      { fila: -1, columna: -1 }, { fila: -1, columna: 0 }, { fila: -1, columna: 1 },
      { fila: 0, columna: -1 },                             { fila: 0, columna: 1 },
      { fila: 1, columna: -1 },  { fila: 1, columna: 0 },  { fila: 1, columna: 1 },
    ];

    for (const despl of desplazamientos) {
      const posDestino = {
        fila: pieza.posicion.fila + despl.fila,
        columna: pieza.posicion.columna + despl.columna,
      };
      if (esPosicionValida(posDestino)) {
        const piezaEnCasilla = this.obtenerPieza(posDestino);
        if (!piezaEnCasilla || piezaEnCasilla.color !== pieza.color) {
          movimientos.push(posDestino);
        }
      }
    }

    this.agregarMovimientosEnroque(pieza, movimientos);
  }

  /**
   * Añade los enroques (corto y largo) que sean legales.
   *
   * @remarks
   * Requisitos previos comprobados aquí: la pieza es un rey que **no se ha movido** y
   * **no está en jaque**. El resto de condiciones (torre intacta, casillas intermedias
   * libres y casillas de paso/destino no atacadas) las valida {@link Tablero.intentarEnroqueLado}.
   */
  private agregarMovimientosEnroque(pieza: Pieza, movimientos: Posicion[]): void {
    if (pieza.tipo !== 'Rey' || !pieza.nunca_ha_movido) return;
    if (this.hayJaque(pieza.color)) return;

    const filaRey = pieza.posicion.fila;

    this.intentarEnroqueLado(pieza, filaRey, 7, movimientos, 'corto');
    this.intentarEnroqueLado(pieza, filaRey, 0, movimientos, 'largo');
  }

  /**
   * Valida un enroque concreto y, si es legal, añade la casilla destino del rey.
   *
   * @remarks
   * Comprueba que la torre del lado indicado siga sin moverse, que no haya piezas entre
   * rey y torre, y que el rey **ni cruce ni aterrice** en una casilla atacada (regla de
   * "no enrocar a través de jaque").
   *
   * @param columnaRey - Columna de la torre de ese lado (`7` corto, `0` largo).
   * @param tipo - `'corto'` (flanco de rey) o `'largo'` (flanco de dama).
   */
  private intentarEnroqueLado(
    rey: Pieza,
    filaRey: number,
    columnaRey: number,
    movimientos: Posicion[],
    tipo: 'corto' | 'largo'
  ): void {
    const torre = this.obtenerPieza({ fila: filaRey, columna: columnaRey });
    if (!torre || torre.tipo !== 'Torre' || torre.color !== rey.color || !torre.nunca_ha_movido) return;

    const inicio = Math.min(rey.posicion.columna, columnaRey);
    const fin = Math.max(rey.posicion.columna, columnaRey);

    // Todas las casillas entre el rey y la torre deben estar vacías
    for (let col = inicio + 1; col < fin; col++) {
      if (this.obtenerPieza({ fila: filaRey, columna: col })) return;
    }

    // El rey no puede cruzar ni aterrizar en una casilla atacada
    // (corto: cruza la 5 y aterriza en la 6; largo: cruza la 3 y aterriza en la 2)
    const columnaPaso = tipo === 'corto' ? 5 : 3;
    const columnaDestino = tipo === 'corto' ? 6 : 2;
    if (this.estaCasillaAtacada({ fila: filaRey, columna: columnaPaso }, rey.color)) return;
    if (this.estaCasillaAtacada({ fila: filaRey, columna: columnaDestino }, rey.color)) return;

    movimientos.push({ fila: filaRey, columna: columnaDestino });
  }

  /**
   * Simula un movimiento y comprueba si dejaría al **propio** rey en jaque.
   *
   * @remarks
   * Aplica la jugada en sitio (incluida la captura al paso, donde la pieza capturada no
   * está en la casilla destino), evalúa el jaque y **revierte** el estado: posición,
   * `nunca_ha_movido` (que {@link Pieza.mover} habría puesto a `false`) y la pieza
   * capturada. No muta el tablero de forma observable.
   *
   * @returns `true` si tras el movimiento el rey propio quedaría en jaque.
   */
  private movimientoDejaEnJaque(pieza: Pieza, destino: Posicion): boolean {
    const posicionOriginal = pieza.posicion;
    const nuncaHaMovidoOriginal = pieza.nunca_ha_movido;

    // Captura al paso: el peón capturado no está en la casilla destino, sino al lado
    const esAlPaso =
      pieza.tipo === 'Peon' &&
      posicionOriginal.columna !== destino.columna &&
      !this.obtenerPieza(destino);
    const piezaCapturada = esAlPaso
      ? this.obtenerPieza({ fila: posicionOriginal.fila, columna: destino.columna })
      : this.obtenerPieza(destino);

    pieza.mover(destino);
    if (piezaCapturada) piezaCapturada.eliminar();

    const hayJaque = this.hayJaque(pieza.color);

    // Restaurar posición y el flag de enroque (mover() pone nunca_ha_movido en false)
    pieza.posicion = posicionOriginal;
    pieza.nunca_ha_movido = nuncaHaMovidoOriginal;
    if (piezaCapturada) piezaCapturada.eliminada = false;

    return hayJaque;
  }

  /**
   * Indica si el rey de un color está en jaque.
   *
   * @returns `true` si la casilla del rey está atacada; `false` si no hay jaque (o no hay rey).
   */
  hayJaque(color: Color): boolean {
    const rey = this.obtenerRey(color);
    if (!rey) return false;
    return this.estaCasillaAtacada(rey.posicion, color);
  }

  /**
   * Indica si una casilla está atacada por alguna pieza del color contrario.
   *
   * @remarks
   * Usa generación de **ataques en crudo** ({@link Tablero.puedePiezaAtacarCasilla}), sin
   * el filtro de jaque, para no entrar en recursión al evaluar la legalidad de jugadas.
   *
   * @param colorDefensor - Color del bando que defiende la casilla.
   */
  private estaCasillaAtacada(casilla: Posicion, colorDefensor: Color): boolean {
    const colorAtacante = colorDefensor === 'Blanca' ? 'Negra' : 'Blanca';
    const piezasAtacantes = this.obtenerPiezasPorColor(colorAtacante);

    for (const pieza of piezasAtacantes) {
      if (this.puedePiezaAtacarCasilla(pieza, casilla)) return true;
    }
    return false;
  }

  /**
   * Indica si una pieza ataca una casilla concreta (sin filtrar por jaque).
   *
   * @remarks
   * Para peón y rey usa generadores de **ataque** específicos
   * ({@link Tablero.calcularAtaquesPeon}, {@link Tablero.calcularAtaquesRey}) en lugar de
   * sus movimientos: un peón ataca solo en diagonal (no de frente) y el rey no "ataca"
   * vía enroque. El resto de piezas atacan donde se mueven.
   */
  private puedePiezaAtacarCasilla(pieza: Pieza, casilla: Posicion): boolean {
    const movimientos: Posicion[] = [];

    switch (pieza.tipo) {
      case 'Peon':   this.calcularAtaquesPeon(pieza, movimientos); break;
      case 'Torre':  this.calcularMovimientosTorre(pieza, movimientos); break;
      case 'Caballo':this.calcularMovimientosCaballo(pieza, movimientos); break;
      case 'Alfil':  this.calcularMovimientosAlfil(pieza, movimientos); break;
      case 'Reina':  this.calcularMovimientosReina(pieza, movimientos); break;
      case 'Rey':    this.calcularAtaquesRey(pieza, movimientos); break;
    }

    return movimientos.some(m => posicionesIguales(m, casilla));
  }

  /** Casillas que un peón **ataca** (las dos diagonales delanteras), sin incluir el avance. */
  private calcularAtaquesPeon(pieza: Pieza, movimientos: Posicion[]): void {
    const direccion = pieza.color === 'Blanca' ? -1 : 1;
    for (let offset of [-1, 1]) {
      const posAtaque = {
        fila: pieza.posicion.fila + direccion,
        columna: pieza.posicion.columna + offset,
      };
      if (esPosicionValida(posAtaque)) movimientos.push(posAtaque);
    }
  }

  /** Casillas que un rey **ataca** (las 8 adyacentes), sin incluir el enroque. */
  private calcularAtaquesRey(pieza: Pieza, movimientos: Posicion[]): void {
    const desplazamientos = [
      { fila: -1, columna: -1 }, { fila: -1, columna: 0 }, { fila: -1, columna: 1 },
      { fila: 0, columna: -1 },                             { fila: 0, columna: 1 },
      { fila: 1, columna: -1 },  { fila: 1, columna: 0 },  { fila: 1, columna: 1 },
    ];
    for (const despl of desplazamientos) {
      const posDestino = {
        fila: pieza.posicion.fila + despl.fila,
        columna: pieza.posicion.columna + despl.columna,
      };
      if (esPosicionValida(posDestino)) movimientos.push(posDestino);
    }
  }

  /**
   * Indica si un color está en **jaque mate**.
   *
   * @remarks
   * Es jaque mate si el color está en jaque y **ninguna** de sus piezas tiene algún
   * movimiento legal (los movimientos ya vienen filtrados contra el jaque).
   */
  hayJaqueMate(color: Color): boolean {
    if (!this.hayJaque(color)) return false;

    const piezas = this.obtenerPiezasPorColor(color);
    for (const pieza of piezas) {
      if (this.obtenerMovimientosPosibles(pieza).length > 0) return false;
    }
    return true;
  }

  /**
   * Convierte la entidad a un objeto plano
   */
  toPlain(): object {
    return {
      piezas: this.piezas.map(p => p.toPlain()),
      movimientos: this.movimientos.map(m => m.toPlain()),
    };
  }

  /**
   * Construye un {@link Tablero} a partir de un DTO del servidor.
   *
   * @remarks
   * Tolerante a la forma del DTO: las piezas pueden venir como `piezas`/`Piezas` y el
   * historial como `historialMovimientos` (lo que envía el servidor), `movimientos`, etc.
   * Delega en {@link Pieza.createFromDTO} y {@link Movimiento.createFromDTO}.
   *
   * @returns El tablero reconstruido.
   * @throws Error si alguna pieza o movimiento del DTO está incompleto (propagado desde
   * los `createFromDTO` de las sub-entidades).
   */
  static createFromDTO(dto: any): Tablero {
    // Piezas: acepta camelCase (piezas) y PascalCase (Piezas)
    const piezasRaw: any[] = dto?.piezas ?? dto?.Piezas ?? [];

    // Historial: el servidor envía "historialMovimientos"; también aceptamos "movimientos"
    // como fallback por compatibilidad con código local
    const movimientosRaw: any[] = dto?.historialMovimientos
      ?? dto?.HistorialMovimientos
      ?? dto?.movimientos
      ?? dto?.Movimientos
      ?? [];

    const piezas = piezasRaw.map((p: any) => Pieza.createFromDTO(p));
    const movimientos = movimientosRaw.map((m: any) => Movimiento.createFromDTO(m));
    return new Tablero(piezas, movimientos);
  }

  /**
   * Crea un tablero con la posición inicial estándar de ajedrez.
   *
   * @remarks
   * Pensado para juego local y tests: asigna ids secuenciales (`'1'`, `'2'`, …), no los
   * GUID del servidor. En partida en línea el tablero se reconstruye con
   * {@link Tablero.createFromDTO}.
   */
  static crearTableroInicial(): Tablero {
    const piezas: Pieza[] = [];
    let id = 1;

    const crearPieza = (tipo: any, color: Color, fila: number, columna: number) => {
      piezas.push(
        new Pieza({
          id: `${id++}`,
          tipo,
          color,
          posicion: { fila, columna },
        })
      );
    };

    for (let col = 0; col < 8; col++) crearPieza('Peon', 'Blanca', 6, col);
    for (let col = 0; col < 8; col++) crearPieza('Peon', 'Negra', 1, col);

    const ordenPiezas: TipoPieza[] = ['Torre', 'Caballo', 'Alfil', 'Reina', 'Rey', 'Alfil', 'Caballo', 'Torre'];
    for (let col = 0; col < 8; col++) {
      crearPieza(ordenPiezas[col], 'Negra', 0, col);
      crearPieza(ordenPiezas[col], 'Blanca', 7, col);
    }

    return new Tablero(piezas);
  }
}

type TipoPieza = 'Peon' | 'Torre' | 'Caballo' | 'Alfil' | 'Reina' | 'Rey';