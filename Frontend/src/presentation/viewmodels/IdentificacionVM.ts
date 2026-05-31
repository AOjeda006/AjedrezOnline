/**
 * ViewModel (MobX) de la pantalla de identificación: captura y valida el nombre del jugador.
 *
 * @remarks
 * Estado observable consumido por la pantalla. No realiza navegación ni conexión; solo
 * gestiona el nombre, el estado de carga y los mensajes de error de validación.
 */

import { makeAutoObservable } from 'mobx';

export class IdentificacionVM {
  /** Nombre introducido por el usuario. */
  nombreJugador: string = '';
  /** Mensaje de error de validación, o `null` si no hay. */
  error: string | null = null;
  isLoading: boolean = false;

  constructor() {
    makeAutoObservable(this);
  }

  /** Actualiza el nombre y limpia el error previo. */
  setNombre(nombre: string): void {
    this.nombreJugador = nombre;
    this.error = null;
  }

  /**
   * Valida el nombre actual y, de no ser válido, fija {@link IdentificacionVM.error}.
   *
   * @remarks
   * Reglas: no vacío, entre 2 y 20 caracteres (tras recortar espacios).
   * @returns `true` si el nombre es válido.
   */
  validarYContinuar(): boolean {
    if (!this.nombreJugador || this.nombreJugador.trim().length === 0) {
      this.error = 'El nombre del jugador no puede estar vacío';
      return false;
    }

    if (this.nombreJugador.trim().length < 2) {
      this.error = 'El nombre debe tener al menos 2 caracteres';
      return false;
    }

    if (this.nombreJugador.trim().length > 20) {
      this.error = 'El nombre no puede exceder 20 caracteres';
      return false;
    }

    this.error = null;
    return true;
  }

  setLoading(loading: boolean): void {
    this.isLoading = loading;
  }

  reset(): void {
    this.nombreJugador = '';
    this.error = null;
    this.isLoading = false;
  }
}
