// app/_layout.tsx
/**
 * @module app/_layout
 *
 * Layout raíz de Expo Router: define la pila de navegación (`Stack`) y sus pantallas.
 *
 * @remarks
 * Importa {@link module:core/registrations} **una sola vez** al arrancar (efecto
 * secundario que registra los singletons del contenedor DI antes de renderizar nada).
 */

import '../src/core/registrations'; // <-- registra singletons UNA VEZ al arrancar
import React from 'react';
import { Stack } from 'expo-router';

/** Layout raíz: configura la barra de navegación y declara las rutas de la app. */
export default function RootLayout() {
  return (
    <Stack
      screenOptions={{
        headerStyle: {
          backgroundColor: '#2196F3',
        },
        headerTintColor: '#fff',
        headerTitleStyle: {
          fontWeight: 'bold',
        },
      }}
    >
      <Stack.Screen
        name="index"
        options={{
          headerShown: false
        }}
      />
      <Stack.Screen
        name="identificacion"
        options={{
          title: 'Identificación',
          headerBackVisible: false
        }}
      />
      <Stack.Screen
        name="menu-principal"
        options={{
          title: 'Menú Principal',
          headerBackVisible: false
        }}
      />
      <Stack.Screen
        name="partida"
        options={{
          headerBackVisible: false,
          gestureEnabled: false,
          title: 'Partida'
        }}
      />
    </Stack>
  );
}
