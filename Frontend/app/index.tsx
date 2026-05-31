// app/index.tsx
/**
 * @module app/index
 *
 * Ruta raíz (`/`): redirige a la pantalla de identificación.
 *
 * @remarks
 * Importa {@link module:core/registrations} para garantizar que los singletons del
 * contenedor DI estén registrados antes de renderizar.
 */
import '../src/core/registrations'; // registrar singletons antes de renderizar
import React from 'react';
import { Redirect } from 'expo-router';

/** Punto de entrada: redirige de `/` a `/identificacion`. */
export default function Index() {
  return <Redirect href="/identificacion" />;
}
