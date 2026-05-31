// src/core/registrations.ts
/**
 * @module core/registrations
 *
 * Conecta y registra como singletons el grafo de dependencias de la app en el
 * {@link container}.
 *
 * @remarks
 * Construye la cadena `SignalRAjedrezDataSource → AjedrezRepositorySignalR →
 * AjedrezUseCase` (cada eslabón recibe el anterior) y los registra. Que sean
 * singletons es **esencial**: el cableado de eventos SignalR depende de compartir
 * una única instancia de cada capa.
 *
 * Importar este módulo tiene **efectos secundarios** (registra al evaluarse), por lo
 * que debe importarse **una sola vez** desde el punto de entrada (`app/_layout.tsx`
 * o `app/index.tsx`). Los guardas `container.has(...)` lo hacen idempotente.
 */

import { container } from './container';
import { SignalRAjedrezDataSource } from '../data/datasources/SignalRAjedrezDataSource';
import { AjedrezRepositorySignalR } from '../data/repositories/AjedrezRepositorySignalR';
import { AjedrezUseCase } from '../domain/usecases/AjedrezUseCase';
import { IAjedrezRepository } from '../domain/repositories/IAjedrezRepository';
import { IAjedrezUseCase } from '../domain/interfaces/IAjedrezUseCase';

// Registrar SignalRAjedrezDataSource si no existe
if (!container.has('SignalRAjedrezDataSource')) {
  const ds = new SignalRAjedrezDataSource();
  container.register('SignalRAjedrezDataSource', ds);
}

// Registrar AjedrezRepository (usa la instancia del data source)
if (!container.has('AjedrezRepository')) {
  const ds = container.resolve<SignalRAjedrezDataSource>('SignalRAjedrezDataSource');
  const repo = new AjedrezRepositorySignalR(ds);
  container.register('AjedrezRepository', repo as IAjedrezRepository);
}

// Registrar AjedrezUseCase (usa la instancia del repo)
if (!container.has('AjedrezUseCase')) {
  const repo = container.resolve<IAjedrezRepository>('AjedrezRepository');
  const usecase = new AjedrezUseCase(repo);
  container.register('AjedrezUseCase', usecase as IAjedrezUseCase);
}

export { container };
export default container;
