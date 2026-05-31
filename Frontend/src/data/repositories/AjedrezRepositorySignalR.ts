/**
 * Implementación de {@link IAjedrezRepository} sobre {@link SignalRAjedrezDataSource}.
 *
 * @remarks
 * Dos responsabilidades:
 *
 * - **Acciones**: traduce cada método a una invocación del hub (`invoke`).
 * - **Listeners** (`on*`): registra los handlers en el data source y **mapea los DTOs
 *   entrantes a entidades de dominio** (vía los `*DomainMapper`). Algunos eventos
 *   convierten además enums que pueden llegar como número o como texto
 *   (ver {@link AjedrezRepositorySignalR.onTurnoActualizado} y
 *   {@link AjedrezRepositorySignalR.onPartidaFinalizada}).
 */

import { Color, ConnectionState, ResultadoPartida, TipoFinPartida, TipoPieza } from '../../core/types';
import { Movimiento } from '../../domain/entities/Movimiento';
import { Partida } from '../../domain/entities/Partida';
import { Sala } from '../../domain/entities/Sala';
import { Tablero } from '../../domain/entities/Tablero';
import {
  MovimientoDomainMapper,
  PartidaDomainMapper,
  SalaDomainMapper,
  TableroDomainMapper,
} from '../../domain/mappers/DomainMappers';
import { IAjedrezRepository } from '../../domain/repositories/IAjedrezRepository';
import { SignalRAjedrezDataSource } from '../datasources/SignalRAjedrezDataSource';

export class AjedrezRepositorySignalR implements IAjedrezRepository {
  private dataSource: SignalRAjedrezDataSource;
  private currentState: ConnectionState = 'Disconnected';

  constructor(dataSource: SignalRAjedrezDataSource) {
    this.dataSource = dataSource;
  }

  async connect(url: string, jugadorNombre: string): Promise<void> {
    console.log('[TRACE repo] connect -> iniciando conexión a', url, 'con nombre', jugadorNombre);
    await this.dataSource.start(url, jugadorNombre);
    this.currentState = 'Connected';
    console.log('[TRACE repo] connect -> estado Connected');
  }

  async disconnect(): Promise<void> {
    console.log('[TRACE repo] disconnect -> deteniendo dataSource');
    await this.dataSource.stop();
    this.currentState = 'Disconnected';
    console.log('[TRACE repo] disconnect -> estado Disconnected');
  }

  getConnectionState(): ConnectionState {
    return this.dataSource.getState();
  }

  getConnectionId(): string | null {
    return this.dataSource.getConnectionId();
  }

  async crearSala(nombreSala: string): Promise<void> {
    await this.dataSource.invoke('CrearSala', nombreSala);
  }

  async unirseSala(nombreSala: string, nombreJugador: string): Promise<void> {
    try {
      console.log('[TRACE repo] Invocando UnirseSala ->', { nombreSala, nombreJugador });
      await this.dataSource.invoke('UnirseSala', nombreSala, nombreJugador);
      console.log('[TRACE repo] UnirseSala invocado correctamente');
    } catch (err) {
      console.error('[ERROR repo] Error invocando UnirseSala:', err);
      throw err;
    }
  }

  async abandonarSala(): Promise<void> {
    await this.dataSource.invoke('AbandonarSala');
  }

  async realizarMovimiento(movimiento: Movimiento): Promise<void> {
    await this.dataSource.invoke('RealizarMovimiento', movimiento.toPlain());
  }

  async confirmarMovimiento(): Promise<void> {
    await this.dataSource.invoke('ConfirmarMovimiento');
  }

  async deshacerMovimiento(): Promise<void> {
    await this.dataSource.invoke('DeshacerMovimiento');
  }

  async solicitarTablas(): Promise<void> {
    await this.dataSource.invoke('SolicitarTablas');
  }

  async retirarTablas(): Promise<void> {
    await this.dataSource.invoke('RetirarTablas');
  }

  async rendirse(): Promise<void> {
    console.log('[TRACE repo] rendirse() - invocando Rendirse en el servidor');
    try {
      await this.dataSource.invoke('Rendirse');
      console.log('[TRACE repo] rendirse() - Rendirse invocado exitosamente');
    } catch (error) {
      console.error('[ERROR repo] rendirse() - Error al invocar Rendirse:', error);
      throw error;
    }
  }

  async promocionarPeon(tipoPieza: TipoPieza): Promise<void> {
    await this.dataSource.invoke('PromocionarPeon', tipoPieza);
  }

  async solicitarReinicio(): Promise<void> {
    await this.dataSource.invoke('SolicitarReinicio');
  }

  async retirarReinicio(): Promise<void> {
    await this.dataSource.invoke('RetirarReinicio');
  }

  // ───────────────────────────────────────────────
  // EVENTOS DEL SERVIDOR
  // ───────────────────────────────────────────────

  onSalaCreada(callback: (sala: Sala) => void): void {
    this.dataSource.on('SalaCreada', (dto: any) => {
      console.log('[TRACE repo] onSalaCreada recibido en repo:', dto);
      try {
        const sala = SalaDomainMapper.toDomain(dto);
        console.log('[TRACE repo] Sala mapeada en repo:', { id: sala.id, nombre: sala.nombre });
        callback(sala);
      } catch (error) {
        console.error('[ERROR repo] Error mapeando SalaCreada:', error, 'DTO crudo:', dto);
      }
    });
  }

  onJugadorUnido(callback: (partida: Partida) => void): void {
    this.dataSource.on('JugadorUnido', (dto: any) => {
      console.log('[TRACE repo] onJugadorUnido recibido en repo:', dto);
      try {
        const partida = PartidaDomainMapper.toDomain(dto);
        console.log('[TRACE repo] JugadorUnido mapeado en repo:', { id: partida.id, salaId: partida.salaId });
        callback(partida);
      } catch (error) {
        console.error('[ERROR repo] Error mapeando JugadorUnido:', error, 'DTO crudo:', dto);
      }
    });
  }

  onPartidaIniciada(callback: (partida: Partida) => void): void {
    this.dataSource.on('PartidaIniciada', (dto: any) => {
      console.log('[TRACE repo] onPartidaIniciada recibido en repo:', dto);
      try {
        const partida = PartidaDomainMapper.toDomain(dto);
        console.log('[TRACE repo] Partida mapeada en repo:', { id: partida.id, salaId: partida.salaId });
        callback(partida);
      } catch (error) {
        console.error('[ERROR repo] Error mapeando PartidaIniciada:', error, 'DTO crudo:', dto);
      }
    });
  }

  onMovimientoRealizado(callback: (movimiento: Movimiento, tablero: Tablero) => void): void {
    this.dataSource.on('MovimientoRealizado', (movDTO: any, tableroDTO: any) => {
      console.log('[TRACE repo] MovimientoRealizado recibido en repo:', movDTO, tableroDTO);
      try {
        const movimiento = MovimientoDomainMapper.toDomain(movDTO);
        const tablero = TableroDomainMapper.toDomain(tableroDTO);
        callback(movimiento, tablero);
      } catch (error) {
        console.error('[ERROR repo] Error mapeando MovimientoRealizado:', error, 'DTOs:', movDTO, tableroDTO);
      }
    });
  }

  /**
   * Suscribe al cambio de turno.
   *
   * @remarks
   * Normaliza el color del turno, que puede llegar como texto (`'Blanca'`/`'Negra'`) o
   * como número (`0`/`1`) según la configuración del serializador del servidor.
   */
  onTurnoActualizado(callback: (turno: Color, numeroTurno: number) => void): void {
    this.dataSource.on('TurnoActualizado', (turnoRaw: any, numeroTurno: number) => {
      console.log('[TRACE repo] TurnoActualizado recibido en repo:', turnoRaw, numeroTurno);

      // FIX: Convert number to Color string (0 -> 'Blanca', 1 -> 'Negra')
      let turno: Color;
      if (typeof turnoRaw === 'number') {
        turno = turnoRaw === 0 ? 'Blanca' : 'Negra';
      } else {
        turno = turnoRaw as Color;
      }

      console.log('[TRACE repo] Turno convertido:', turno);
      callback(turno, numeroTurno);
    });
  }

  onTablasActualizadas(callback: (blancas: boolean, negras: boolean) => void): void {
    this.dataSource.on('TablasActualizadas', (blancas: boolean, negras: boolean) => {
      console.log('[TRACE repo] TablasActualizadas recibido en repo:', blancas, negras);
      callback(blancas, negras);
    });
  }

  /**
   * Suscribe al fin de partida.
   *
   * @remarks
   * Normaliza `resultado` y `tipo`, que pueden llegar como texto o como índice numérico
   * del enum del servidor (`ResultadoPartida`: 0=VictoriaBlancas, 1=VictoriaNegras,
   * 2=Empate; `TipoFinPartida`: 0=JaqueMate, 1=Tablas, 2=Rendicion, 3=Abandono).
   *
   * @param callback - Recibe el resultado, el motivo y el id del jugador ganador (si aplica).
   */
  onPartidaFinalizada(callback: (resultado: ResultadoPartida, tipo: TipoFinPartida, ganador?: string) => void): void {
    this.dataSource.on('PartidaFinalizada', (resultadoRaw: any, tipoRaw: any, ganador?: string) => {
      console.log('[TRACE repo] PartidaFinalizada recibido en repo (raw):', resultadoRaw, tipoRaw, ganador);

      // FIX: Convert enum numbers to strings
      // ResultadoPartida: 0 = VictoriaBlancas, 1 = VictoriaNegras, 2 = Empate
      let resultado: ResultadoPartida;
      if (typeof resultadoRaw === 'number') {
        const resultados: ResultadoPartida[] = ['VictoriaBlancas', 'VictoriaNegras', 'Empate'];
        resultado = resultados[resultadoRaw] ?? 'Empate';
      } else {
        resultado = resultadoRaw as ResultadoPartida;
      }

      // TipoFinPartida: 0 = JaqueMate, 1 = Tablas, 2 = Rendicion, 3 = Abandono
      let tipo: TipoFinPartida;
      if (typeof tipoRaw === 'number') {
        const tipos: TipoFinPartida[] = ['JaqueMate', 'Tablas', 'Rendicion', 'Abandono'];
        tipo = tipos[tipoRaw] ?? 'Abandono';
      } else {
        tipo = tipoRaw as TipoFinPartida;
      }

      console.log('[TRACE repo] PartidaFinalizada convertido:', resultado, tipo, ganador);
      callback(resultado, tipo, ganador);
    });
  }

  onJaqueActualizado(callback: (hayJaque: boolean) => void): void {
    this.dataSource.on('JaqueActualizado', (hayJaque: boolean) => {
      console.log('[TRACE repo] JaqueActualizado recibido en repo:', hayJaque);
      callback(hayJaque);
    });
  }

  onPromocionRequerida(callback: () => void): void {
    this.dataSource.on('PromocionRequerida', () => {
      console.log('[TRACE repo] PromocionRequerida recibido en repo');
      callback();
    });
  }

  onReinicioActualizado(callback: (blancas: boolean, negras: boolean) => void): void {
    this.dataSource.on('ReinicioActualizado', (blancas: boolean, negras: boolean) => {
      console.log('[TRACE repo] ReinicioActualizado recibido en repo:', blancas, negras);
      callback(blancas, negras);
    });
  }

  onJugadorAbandonado(callback: (nombreJugador: string) => void): void {
    this.dataSource.on('JugadorAbandonado', (nombreJugador: string) => {
      console.log('[TRACE repo] JugadorAbandonado recibido en repo:', nombreJugador);
      callback(nombreJugador);
    });
  }

  onError(callback: (error: string) => void): void {
    this.dataSource.on('Error', (error: string) => {
      console.error('[TRACE repo] Error recibido en repo:', error);
      callback(error);
    });
  }

  onTableroActualizado(callback: (tablero: Tablero) => void): void {
    this.dataSource.on('TableroActualizado', (tableroDTO: any) => {
      console.log('[TRACE repo] TableroActualizado recibido en repo:', tableroDTO);
      try {
        const tablero = TableroDomainMapper.toDomain(tableroDTO);
        callback(tablero);
      } catch (error) {
        console.error('[ERROR repo] Error mapeando TableroActualizado:', error, 'DTO:', tableroDTO);
      }
    });
  }

  offAllListeners(): void {
    console.log('[TRACE repo] offAllListeners -> removiendo listeners en dataSource');
    this.dataSource.offAll();
  }
}
