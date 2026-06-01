/**
 * Logger condicional del cliente.
 *
 * @remarks
 * Los mensajes de depuración (`log` y `warn`) solo se emiten cuando la variable de
 * entorno pública `EXPO_PUBLIC_DEBUG` vale `"true"`, de modo que en producción no
 * queda ruido en la consola. Los errores (`error`) se registran siempre, porque
 * interesan también en producción.
 *
 * El flag se documenta en `.env.example` (`EXPO_PUBLIC_DEBUG`). Las variables
 * `EXPO_PUBLIC_*` las inyecta Expo en tiempo de compilación.
 */
const DEBUG = process.env.EXPO_PUBLIC_DEBUG === 'true';

export const logger = {
  /** Traza de depuración; solo se emite con `EXPO_PUBLIC_DEBUG=true`. */
  log: (...args: unknown[]): void => {
    if (DEBUG) {
      console.log(...args);
    }
  },
  /** Aviso de depuración; solo se emite con `EXPO_PUBLIC_DEBUG=true`. */
  warn: (...args: unknown[]): void => {
    if (DEBUG) {
      console.warn(...args);
    }
  },
  /** Error; se registra siempre, también en producción. */
  error: (...args: unknown[]): void => {
    console.error(...args);
  },
};
