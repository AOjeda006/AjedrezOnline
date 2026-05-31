// src/core/container.ts
/**
 * @module core/container
 *
 * Contenedor de inyección de dependencias mínimo, basado en un registro
 * clave→instancia.
 *
 * @remarks
 * El registro concreto de las dependencias se hace en {@link module:core/registrations}
 * (y no aquí) para evitar importaciones circulares. Se expone una única instancia
 * compartida, {@link container}.
 */

/**
 * Contenedor DI sencillo que guarda y resuelve instancias por clave de cadena.
 *
 * @remarks
 * Es un registro de **singletons**: cada clave apunta a una única instancia ya creada.
 * No construye dependencias ni resuelve grafos; solo almacena lo que se le registra.
 */
class DIContainer {
  private instances: Map<string, any> = new Map();

  /**
   * Registra (o reemplaza) la instancia asociada a una clave.
   *
   * @typeParam T - Tipo de la instancia registrada.
   * @param key - Clave única de la dependencia.
   * @param instance - Instancia a almacenar.
   */
  public register<T>(key: string, instance: T): void {
    this.instances.set(key, instance);
  }

  /**
   * Resuelve la instancia registrada bajo una clave.
   *
   * @typeParam T - Tipo esperado de la instancia.
   * @param key - Clave de la dependencia.
   * @returns La instancia registrada.
   * @throws Error si no hay ninguna instancia registrada bajo esa clave.
   */
  public resolve<T>(key: string): T {
    if (!this.instances.has(key)) {
      throw new Error(`Dependencia "${key}" no registrada en el contenedor`);
    }
    return this.instances.get(key) as T;
  }

  /** Indica si existe una instancia registrada bajo la clave dada. */
  public has(key: string): boolean {
    return this.instances.has(key);
  }

  /** Elimina todas las instancias registradas. */
  public clear(): void {
    this.instances.clear();
  }
}

/** Instancia compartida del contenedor DI usada en toda la aplicación. */
export const container = new DIContainer();
export default container;
