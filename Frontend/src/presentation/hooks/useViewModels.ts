/**
 * @module presentation/hooks/useViewModels
 *
 * Hooks de React que crean y exponen los ViewModels a las pantallas.
 *
 * @remarks
 * Cada hook devuelve `{ viewModel/state, actions }`, donde `actions` son métodos del VM ya
 * enlazados (ver {@link safeBind}). Notas de diseño relevantes:
 *
 * - `usePartida` lee la partida pendiente al montar ({@link getPendingPartida}) para
 *   evitar la race condition del evento `PartidaIniciada` (ver {@link module:core/gameState}).
 * - `usePartida` **no** llama a `unsubscribeAll()` al desmontar: las suscripciones deben
 *   sobrevivir durante toda la partida; la limpieza la hace el VM en `volverAlMenu()`.
 */

import { logger } from '../../core/logger';
import { useEffect, useMemo } from 'react';
import { container } from '../../core/container';
import { clearPendingPartida, getPendingPartida } from '../../core/gameState';
import { Color } from '../../core/types';
import { Partida } from '../../domain/entities/Partida';
import { IAjedrezUseCase } from '../../domain/interfaces/IAjedrezUseCase';
import { IdentificacionVM } from '../viewmodels/IdentificacionVM';
import { MenuPrincipalVM } from '../viewmodels/MenuPrincipalVM';
import { PartidaVM } from '../viewmodels/PartidaVM';

/**
 * Devuelve el método `fnName` de `obj` enlazado a `obj`, o una función vacía si no existe.
 *
 * @remarks
 * Evita que la UI rompa si un método no está disponible (p. ej. por un VM mal inicializado);
 * en ese caso devuelve un *noop* y deja un aviso por consola.
 */
const safeBind = (obj: any, fnName: string) => {
  if (!obj) {
    logger.warn(`[safeBind] ${fnName}: obj es null/undefined`);
    return () => {};
  }
  const fn = (obj as any)[fnName];
  if (typeof fn === 'function') {
    return fn.bind(obj);
  } else {
    logger.error(`[safeBind] ${fnName}: NO es una función! tipo:`, typeof fn);
    return () => {};
  }
};

/**
 * Determina el color (Blanca/Negra) del jugador local.
 * Preferimos el connectionId (único por conexión) y, como respaldo, el nombre
 * (que puede fallar si ambos jugadores usan el mismo nombre).
 */
const determinarColorLocal = (partida: Partida, miNombre: string, miConnectionId: string | null): Color => {
  try {
    // 1) Identificación robusta por connectionId
    if (miConnectionId) {
      if (partida.jugadorBlancas?.connectionId === miConnectionId) return 'Blanca';
      if (partida.jugadorNegras?.connectionId === miConnectionId) return 'Negra';
    }

    // 2) Respaldo por nombre (insensible a mayúsculas/espacios)
    const miNombreNorm = miNombre?.trim().toLowerCase() ?? '';
    const nbNorm = (partida.jugadorBlancas?.nombre ?? '').trim().toLowerCase();
    const nnNorm = (partida.jugadorNegras?.nombre ?? '').trim().toLowerCase();

    if (miNombreNorm && nnNorm && miNombreNorm === nnNorm) return 'Negra';
    if (miNombreNorm && nbNorm && miNombreNorm === nbNorm) return 'Blanca';

    logger.warn('[WARN determinarColorLocal] No se pudo identificar el color (connectionId/nombre); usando fallback Blanca');
    return 'Blanca';
  } catch (err) {
    logger.error('[ERROR determinarColorLocal]', err);
    return 'Blanca';
  }
};

/** Hook de la pantalla de identificación: crea un {@link IdentificacionVM} estable y expone sus acciones. */
export const useIdentificacion = () => {
  const viewModel = useMemo(() => new IdentificacionVM(), []);
  return {
    viewModel,
    actions: {
      setNombre: safeBind(viewModel, 'setNombre'),
      validarYContinuar: safeBind(viewModel, 'validarYContinuar'),
      setLoading: safeBind(viewModel, 'setLoading'),
      reset: safeBind(viewModel, 'reset'),
    },
  };
};

/** Hook del menú principal: crea un {@link MenuPrincipalVM} (con el caso de uso del contenedor) y expone sus acciones. */
export const useMenuPrincipal = () => {
  const useCase = container.resolve<IAjedrezUseCase>('AjedrezUseCase');
  const viewModel = useMemo(() => new MenuPrincipalVM(useCase), [useCase]);

  return {
    viewModel,
    actions: {
      setNombreJugador: safeBind(viewModel, 'setNombreJugador'),
      setNombreSalaCrear: safeBind(viewModel, 'setNombreSalaCrear'),
      setNombreSalaUnirse: safeBind(viewModel, 'setNombreSalaUnirse'),
      conectar: safeBind(viewModel, 'conectar'),
      crearSala: safeBind(viewModel, 'crearSala'),
      unirseSala: safeBind(viewModel, 'unirseSala'),
      reset: safeBind(viewModel, 'reset'),
    },
  };
};

/**
 * Hook de la pantalla de partida: crea el {@link PartidaVM} e inicializa la partida.
 *
 * @remarks
 * Al montar, consume la partida pendiente ({@link getPendingPartida}) y, si no la hay, se
 * suscribe a `PartidaIniciada` como fallback. Determina el color local con
 * {@link determinarColorLocal}. No desuscribe al desmontar (ver nota del módulo).
 */
export const usePartida = () => {
  const useCase = container.resolve<IAjedrezUseCase>('AjedrezUseCase');
  const viewModel = useMemo(() => new PartidaVM(useCase), [useCase]);

  useEffect(() => {
    let mounted = true;

    // ── FIX PRINCIPAL: comprobar si la partida ya llegó antes de que este
    //    componente montase (race condition). MenuPrincipalVM la guardó via
    //    setPendingPartida() en gameState.ts justo al recibirla.
    const pending = getPendingPartida();
    if (pending) {
      clearPendingPartida();
      const miColor = determinarColorLocal(pending.partida, pending.miNombre, useCase.obtenerMiConnectionId());
      try {
        // ← CORRECCIÓN: pasar también miNombre al inicializar la VM
        viewModel.inicializarPartida(pending.partida, miColor, pending.miNombre);
        logger.log('[TRACE hook] VM inicializada desde pendingPartida:', {
          id: pending.partida.id,
          miColor,
          miNombre: pending.miNombre,
        });
      } catch (err) {
        logger.error('[ERROR hook] inicializarPartida (pending) falló:', err);
      }
      // Con la partida ya inicializada no es necesario suscribirse al evento.
      return;
    }

    // ── FALLBACK: la partida aún no llegó (caso poco probable, p.ej. navegación
    //    manual a la pantalla). Nos suscribimos por si acaso.
    logger.log('[TRACE hook] No hay pendingPartida, suscribiendo a subscribePartidaIniciada...');

    const onPartidaIniciada = (partida: Partida) => {
      logger.log('[TRACE hook] PartidaIniciada recibida en hook:', { id: partida.id });
      if (!mounted) {
        logger.warn('[TRACE hook] componente desmontado, ignorando partida');
        return;
      }

      // Intentar obtener nombre local desde container (respaldo; el color se
      // determina principalmente por connectionId)
      let miNombre = '';
      try {
        if (container.has('MenuPrincipalVM')) {
          const menuVM = container.resolve<MenuPrincipalVM>('MenuPrincipalVM');
          miNombre = (menuVM as any).nombreJugador ?? '';
        }
      } catch {
        // noop
      }

      const miColor = determinarColorLocal(partida, miNombre, useCase.obtenerMiConnectionId());
      try {
        // ← CORRECCIÓN: pasar miNombre al inicializar la VM
        viewModel.inicializarPartida(partida, miColor, miNombre);
        logger.log('[TRACE hook] VM inicializada (fallback):', { id: partida.id, miColor, miNombre });
      } catch (err) {
        logger.error('[ERROR hook] inicializarPartida (fallback) falló:', err);
      }
    };

    try {
      useCase.subscribePartidaIniciada(onPartidaIniciada);
    } catch (err) {
      logger.error('[ERROR hook] Error suscribiendo a PartidaIniciada:', err);
    }

    return () => {
      mounted = false;
      // ── FIX: NO llamar unsubscribeAll() aquí porque mataría todas las
      //    suscripciones de SignalR necesarias durante la partida (movimientos,
      //    turno, jaque, etc.). El VM se desuscribe en volverAlMenu() cuando
      //    el usuario realmente quiere salir.
    };
  }, [useCase, viewModel]);

  return {
    state: viewModel,
    actions: {
      inicializarPartida: safeBind(viewModel, 'inicializarPartida'),
      seleccionarCasilla: safeBind(viewModel, 'seleccionarCasilla'),
      confirmarMovimiento: safeBind(viewModel, 'confirmarMovimiento'),
      deshacerMovimiento: safeBind(viewModel, 'deshacerMovimiento'),
      solicitarTablas: safeBind(viewModel, 'solicitarTablas'),
      retirarTablas: safeBind(viewModel, 'retirarTablas'),
      rendirse: safeBind(viewModel, 'rendirse'),
      promocionarPeon: safeBind(viewModel, 'promocionarPeon'),
      solicitarReinicio: safeBind(viewModel, 'solicitarReinicio'),
      retirarReinicio: safeBind(viewModel, 'retirarReinicio'),
      cerrarModalFinPartida: safeBind(viewModel, 'cerrarModalFinPartida'),
      volverAlMenu: safeBind(viewModel, 'volverAlMenu'),
    },
  };
};
