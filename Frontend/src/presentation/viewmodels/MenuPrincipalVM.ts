/**
 * ViewModel (MobX) del menú principal: conexión, creación y unión a salas.
 *
 * @remarks
 * Mantiene campos **separados** para "crear" y "unirse" (escribir en uno no afecta al
 * otro). Al recibir la partida, la guarda como pendiente ({@link setPendingPartida}) para
 * que la pantalla de partida la consuma al montar, evitando una race condition de
 * navegación (ver {@link module:core/gameState}).
 */

import { logger } from '../../core/logger';
import { makeAutoObservable, runInAction } from 'mobx';
import { ConnectionState } from '../../core/types';
import { Partida } from '../../domain/entities/Partida';
import { Sala } from '../../domain/entities/Sala';
import { IAjedrezUseCase } from '../../domain/interfaces/IAjedrezUseCase';
// ← FIX: importar setPendingPartida y clearPendingPartida para resolver la race condition con PartidaScreen
import { setPendingPartida, clearPendingPartida } from '../../core/gameState';

export class MenuPrincipalVM {
  // Identificación
  nombreJugador: string = '';

  // Campos separados para evitar que escribir en uno afecte al otro
  nombreSalaCrear: string = '';
  nombreSalaUnirse: string = '';

  // (opcional) campo histórico/compatibilidad
  nombreSala: string = '';

  // UI / estado
  error: string | null = null;
  isLoading: boolean = false;
  connectionState: ConnectionState = 'Disconnected';
  salaCreada: Sala | null = null;
  esperandoOponente: boolean = false;
  partida: Partida | null = null;
  seCreaLaSala: boolean = false; // Flag to distinguish between creating and joining

  private ajedrezUseCase: IAjedrezUseCase;

  constructor(useCase: IAjedrezUseCase) {
    this.ajedrezUseCase = useCase;
    makeAutoObservable(this, {}, { autoBind: true });
  }

  // Setters
  setNombreJugador(nombre: string): void {
    this.nombreJugador = nombre;
  }

  setNombreSalaCrear(nombre: string): void {
    this.nombreSalaCrear = nombre;
    this.nombreSala = nombre;
  }

  setNombreSalaUnirse(nombre: string): void {
    this.nombreSalaUnirse = nombre;
    this.nombreSala = nombre;
  }

  /**
   * Conecta al servidor con el nombre actual y registra las suscripciones del menú
   * (sala creada, jugador unido, partida iniciada y errores).
   *
   * @remarks
   * Limpia la partida pendiente de una sesión anterior antes de conectar.
   * @throws Error si el nombre del jugador está vacío (se propaga al llamador).
   */
  async conectar(url: string): Promise<void> {
    try {
      runInAction(() => {
        this.isLoading = true;
        this.error = null;
        // Clear partida from previous game to prevent modal persistence
        this.partida = null;
      });

      // Clear pending partida from previous game
      clearPendingPartida();

      if (!this.nombreJugador || this.nombreJugador.trim().length === 0) {
        throw new Error('Debes ingresar tu nombre de jugador');
      }

      await this.ajedrezUseCase.conectarJugador(url, this.nombreJugador);

      runInAction(() => {
        this.connectionState = 'Connected';
      });

      // Suscripciones
      this.ajedrezUseCase.subscribeSalaCreada(this.handleSalaCreada.bind(this));
      this.ajedrezUseCase.subscribeJugadorUnido(this.handleJugadorUnido.bind(this));
      this.ajedrezUseCase.subscribePartidaIniciada(this.handleJugadorUnido.bind(this));
      this.ajedrezUseCase.subscribeError(this.handleError.bind(this));
    } catch (error: any) {
      runInAction(() => {
        this.error = error?.message ?? 'Error al conectar';
        this.connectionState = 'Disconnected';
      });
      logger.error('Error en conectar:', error);
      throw error;
    } finally {
      runInAction(() => {
        this.isLoading = false;
      });
    }
  }

  /**
   * Crea una sala con {@link MenuPrincipalVM.nombreSalaCrear} y queda a la espera de oponente.
   * @throws Error si el nombre de la sala está vacío (se propaga al llamador).
   */
  async crearSala(): Promise<void> {
    try {
      runInAction(() => {
        this.isLoading = true;
        this.error = null;
      });

      if (!this.nombreSalaCrear || this.nombreSalaCrear.trim().length === 0) {
        throw new Error('El nombre de la sala no puede estar vacío');
      }

      await this.ajedrezUseCase.crearNuevaSala(this.nombreSalaCrear);

      runInAction(() => {
        this.esperandoOponente = true;
        this.seCreaLaSala = true; // Mark that we created the room
      });
    } catch (error: any) {
      runInAction(() => {
        this.error = error?.message ?? 'Error al crear sala';
      });
      logger.error('Error en crearSala:', error);
      throw error;
    } finally {
      runInAction(() => { this.isLoading = false; });
    }
  }

  /**
   * Se une a la sala {@link MenuPrincipalVM.nombreSalaUnirse} con el nombre actual.
   * @throws Error si el nombre de la sala o del jugador están vacíos (se propaga al llamador).
   */
  async unirseSala(): Promise<void> {
    try {
      runInAction(() => {
        this.isLoading = true;
        this.error = null;
      });

      if (!this.nombreSalaUnirse || this.nombreSalaUnirse.trim().length === 0) {
        throw new Error('El nombre de la sala no puede estar vacío');
      }

      if (!this.nombreJugador || this.nombreJugador.trim().length === 0) {
        throw new Error('Debes ingresar tu nombre de jugador antes de unirte a una sala');
      }

      await this.ajedrezUseCase.unirseASala(this.nombreSalaUnirse, this.nombreJugador);

      runInAction(() => {
        this.esperandoOponente = true;
        this.seCreaLaSala = false; // Mark that we joined the room
      });
    } catch (error: any) {
      runInAction(() => {
        this.error = error?.message ?? 'Error al unirse a sala';
      });
      logger.error('Error en unirseSala:', error);
      throw error;
    } finally {
      runInAction(() => { this.isLoading = false; });
    }
  }

  // Handlers de eventos
  handleSalaCreada(sala: Sala): void {
    runInAction(() => {
      this.salaCreada = sala;
    });
  }

  /**
   * Maneja el inicio de partida: actualiza el estado y la deja **pendiente** para que la
   * pantalla de partida la consuma al montar.
   *
   * @remarks
   * Fijar `this.partida` dispara la navegación en la pantalla del menú (vía observer).
   */
  handleJugadorUnido(partida: Partida): void {
    runInAction(() => {
      this.partida = partida;
      this.esperandoOponente = false;
    });

    // ← FIX RACE CONDITION: guardamos la partida ANTES de navegar a PartidaScreen.
    // Cuando PartidaScreen monte, usePartida() leerá getPendingPartida() e inicializará
    // el VM inmediatamente, sin necesitar esperar a un nuevo evento PartidaIniciada.
    setPendingPartida(partida, this.nombreJugador);
    logger.log('[MenuPrincipalVM] Partida guardada como pendiente para PartidaScreen, jugador:', this.nombreJugador);
  }

  handleError(error: string): void {
    runInAction(() => {
      this.error = error;
    });
  }

  reset(): void {
    runInAction(() => {
      this.nombreJugador = '';
      this.nombreSalaCrear = '';
      this.nombreSalaUnirse = '';
      this.nombreSala = '';
      this.error = null;
      this.isLoading = false;
      this.connectionState = 'Disconnected';
      this.salaCreada = null;
      this.esperandoOponente = false;
      this.partida = null;
      this.seCreaLaSala = false;
    });
  }
}

export default MenuPrincipalVM;