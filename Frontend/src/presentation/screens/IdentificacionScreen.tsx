/**
 * @module presentation/screens/IdentificacionScreen
 *
 * Pantalla inicial donde el usuario introduce su nombre antes de entrar al menú.
 *
 * @remarks
 * Valida el nombre con {@link IdentificacionVM} y navega a `/menu-principal` pasando el
 * nombre por query param. Usa `expo-router` si está disponible y, como respaldo en web,
 * `window.location`.
 */

import React from 'react';
import { View, ScrollView, StyleSheet, Text } from 'react-native';
import { observer } from 'mobx-react-lite';
import { useIdentificacion } from '../hooks/useViewModels';
import { InputNombre, Boton } from '../components/AjedrezComponents';

// expo-router: usar router si está disponible (native). Fallback a window.location en web.
let useRouter: any = null;
try {
  // eslint-disable-next-line @typescript-eslint/no-var-requires
  useRouter = require('expo-router').useRouter;
} catch {
  useRouter = null;
}

const estilos = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    padding: 24,
    backgroundColor: '#fff',
  },
  titulo: {
    fontSize: 32,
    fontWeight: 'bold',
    marginBottom: 8,
    textAlign: 'center',
    color: '#2196F3',
  },
  subtitulo: {
    fontSize: 16,
    marginBottom: 24,
    textAlign: 'center',
    color: '#666',
  },
});

const IdentificacionScreenInner: React.FC = () => {
  const { actions, viewModel } = useIdentificacion();
  const router = useRouter ? useRouter() : null;

  /** Valida el nombre y, si es correcto, navega al menú principal con el nombre en la URL. */
  const handleContinuar = async () => {
    if (!actions.validarYContinuar()) return;
    actions.setLoading(true);

    const nombre = viewModel.nombreJugador.trim();
    const path = `/menu-principal?nombreJugador=${encodeURIComponent(nombre)}`;

    try {
      if (router && typeof router.push === 'function') {
        // preferimos router.push en native/SPA
        router.push(path);
      } else if (typeof window !== 'undefined') {
        // forzamos recarga completa en web para asegurar que la ruta se cargue
        window.location.assign(path);
      } else {
        console.warn('No hay router disponible para navegar.');
      }
    } finally {
      setTimeout(() => actions.setLoading(false), 500);
    }
  };


  return (
    <ScrollView contentContainerStyle={estilos.container} keyboardShouldPersistTaps="handled">
      <View>
        <View style={{ marginBottom: 40 }}>
          <Text style={{ fontSize: 64, textAlign: 'center', marginBottom: 20 }}>♔ ♕ ♖ ♗ ♘ ♙</Text>
        </View>

        <View style={{ marginBottom: 32 }}>
          <Text style={estilos.titulo}>⚔️ ClienteAjedrez</Text>
          <Text style={estilos.subtitulo}>Ingresa tu nombre para comenzar a jugar</Text>
        </View>

        <InputNombre
          value={viewModel.nombreJugador}
          onChangeText={actions.setNombre}
          placeholder="Tu nombre"
          error={viewModel.error || undefined}
          isLoading={viewModel.isLoading}
        />

        <Boton
          title="Continuar"
          onPress={handleContinuar}
          loading={viewModel.isLoading}
          disabled={!viewModel.nombreJugador.trim()}
        />
      </View>
    </ScrollView>
  );
};

export default observer(IdentificacionScreenInner);
