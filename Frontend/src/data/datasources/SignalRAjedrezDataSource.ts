/**
 * Fuente de datos de bajo nivel sobre la conexión SignalR del juego.
 *
 * @remarks
 * Envuelve un `HubConnection` y añade robustez:
 *
 * - **Registro local de handlers** ({@link SignalRAjedrezDataSource.eventHandlers}): los
 *   suscriptores se guardan aunque aún no haya conexión, y se enganchan al crearla en
 *   {@link SignalRAjedrezDataSource.start}. Así no se pierden suscripciones hechas antes
 *   de conectar.
 * - **Un único "puente" por evento** ({@link SignalRAjedrezDataSource.attachedBridges}):
 *   `emitEvent` reparte a todos los handlers locales, así que basta un listener por
 *   evento en el hub; registrar más provocaría emisiones duplicadas.
 * - El nombre del jugador viaja como parámetro de query en la URL del hub.
 */

import { logger } from '../../core/logger';
import { HttpTransportType, HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { ConnectionState } from '../../core/types';

export class SignalRAjedrezDataSource {
  private hubConnection: HubConnection | null = null;
  private baseUrl: string | null = null;
  /** Handlers registrados por nombre de evento (sobreviven a reconexiones). */
  private eventHandlers: Map<string, Function[]> = new Map();
  /**
   * Nombres de evento con un puente ya enganchado a la conexión.
   *
   * @remarks
   * Evita registrar puentes duplicados, que harían que cada evento se emitiera N veces.
   */
  private attachedBridges: Set<string> = new Set();
  private connectionState: ConnectionState = 'Disconnected';

  /**
   * Crea el `HubConnection` con la política de transporte y reconexión del juego.
   *
   * @remarks
   * Usa **solo WebSockets** con `skipNegotiation` y reintentos automáticos
   * (`[0, 0, 0, 5000, 5000, 10000]` ms). Si `jugadorNombre` está presente, se añade a la
   * URL como query param `nombre`.
   */
  private buildConnection(url: string, jugadorNombre?: string): HubConnection {
    const urlWithName = jugadorNombre ? `${url}${url.includes('?') ? '&' : '?'}nombre=${encodeURIComponent(jugadorNombre)}` : url;

    return new HubConnectionBuilder()
      .withUrl(urlWithName, {
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect([0, 0, 0, 5000, 5000, 10000])
      .build();
  }

  /**
   * Abre (o reabre) la conexión con el hub y engancha los handlers ya registrados.
   *
   * @remarks
   * Cierra cualquier conexión previa, configura los callbacks de ciclo de vida
   * (reconexión/cierre, que emiten `connectionStateChanged`), engancha un puente por
   * cada evento ya suscrito y, una vez conectado, envía el nombre del jugador.
   *
   * @param url - URL del hub de SignalR.
   * @param jugadorNombre - Nombre del jugador; se manda en la query y vía `SetNombreJugador`.
   * @throws Error si no logra establecer la conexión.
   */
  async start(url: string, jugadorNombre?: string): Promise<void> {
    try {
      this.connectionState = 'Connecting';
      this.baseUrl = url;

      if (this.hubConnection) {
        try {
          await this.hubConnection.stop();
        } catch (err) {
          logger.warn('[SignalR DS] Error al detener conexión previa:', err);
        }
        this.hubConnection = null;
      }

      logger.log('[SignalR DS] buildConnection con nombre:', jugadorNombre);
      this.hubConnection = this.buildConnection(url, jugadorNombre);

      // Re-attach global lifecycle handlers
      this.hubConnection.onreconnecting(() => {
        this.connectionState = 'Reconnecting';
        this.emitEvent('connectionStateChanged', 'Reconnecting');
        logger.log('[SignalR DS] onreconnecting');
      });

      this.hubConnection.onreconnected(() => {
        this.connectionState = 'Connected';
        this.emitEvent('connectionStateChanged', 'Connected');
        logger.log('[SignalR DS] onreconnected');
      });

      this.hubConnection.onclose(() => {
        this.connectionState = 'Disconnected';
        this.emitEvent('connectionStateChanged', 'Disconnected');
        logger.log('[SignalR DS] onclose');
      });

      // IMPORTANT: si ya había handlers registrados antes de crear la conexión,
      // los atamos ahora al hubConnection para que SignalR los invoque.
      // Como es una conexión nueva, partimos sin puentes y enganchamos uno por evento.
      this.attachedBridges.clear();
      if (this.eventHandlers.size > 0) {
        for (const eventName of this.eventHandlers.keys()) {
          try {
            (this.hubConnection as any).on(eventName, (...args: any[]) => {
              this.emitEvent(eventName, ...args);
            });
            this.attachedBridges.add(eventName);
          } catch (err) {
            logger.warn(`[SignalR DS] No se pudo attach event ${eventName} al hub:`, err);
          }
        }
      }

      logger.log('[SignalR DS] iniciando hubConnection.start()');
      await this.hubConnection.start();
      this.connectionState = 'Connected';
      this.emitEvent('connectionStateChanged', 'Connected');
      logger.log('[SignalR DS] Conexión establecida (Connected)');

      // Set player name after connecting
      if (jugadorNombre) {
        await this.invoke('SetNombreJugador', jugadorNombre);
        logger.log('[SignalR DS] Nombre de jugador establecido:', jugadorNombre);
      }
    } catch (error) {
      this.connectionState = 'Disconnected';
      logger.error('[SignalR DS] Error conectando a SignalR:', error);
      throw new Error(`No se pudo conectar a ${url}: ${error}`);
    }
  }

  async stop(): Promise<void> {
    try {
      if (this.hubConnection) {
        this.connectionState = 'Disconnected';
        await this.hubConnection.stop();
        this.hubConnection = null;
        // Los puentes desaparecen con la conexión; se reengancharán en el próximo start()
        this.attachedBridges.clear();
        logger.log('[SignalR DS] Conexión detenida');
      }
    } catch (error) {
      logger.error('[SignalR DS] Error deteniendo conexión SignalR:', error);
    }
  }

  /** ConnectionId actual de SignalR (o null si no hay conexión). */
  getConnectionId(): string | null {
    return this.hubConnection?.connectionId ?? null;
  }

  /**
   * Invoca un método del hub en el servidor.
   *
   * @remarks
   * Si la invocación se cancela porque la conexión se está cerrando, se ignora
   * silenciosamente (caso esperado al navegar/desconectar); cualquier otro error se
   * propaga.
   *
   * @param method - Nombre del método del hub.
   * @param args - Argumentos a enviar.
   * @throws Error si no hay conexión establecida o si la invocación falla por otra causa.
   */
  async invoke(method: string, ...args: any[]): Promise<void> {
    try {
      if (!this.hubConnection) {
        throw new Error('Conexión no inicializada');
      }

      if (this.hubConnection.state !== HubConnectionState.Connected) {
        throw new Error('Conexión no está establecida');
      }

      await (this.hubConnection as any).invoke(method, ...args);
    } catch (error: any) {
      const msg = error?.message ?? '';
      if (msg.includes('connection being closed') || msg.includes('Invocation canceled')) {
        logger.log(`[SignalR DS] Invocación de ${method} cancelada (conexión cerrada)`);
        return;
      }
      logger.error(`[SignalR DS] Error invocando ${method}:`, error);
      throw error;
    }
  }

  /**
   * Suscribe un handler a un evento del servidor.
   *
   * @remarks
   * El handler se guarda en el registro local (sobrevive a reconexiones) y, si ya hay
   * conexión, se engancha **un único** puente por evento. Pueden coexistir varios
   * handlers para el mismo evento: `emitEvent` los invoca a todos.
   */
  on(eventName: string, handler: Function): void {
    // Guardamos el handler localmente siempre
    if (!this.eventHandlers.has(eventName)) {
      this.eventHandlers.set(eventName, []);
    }
    this.eventHandlers.get(eventName)!.push(handler);

    // Si la hubConnection ya existe, atamos UN único puente por evento.
    // emitEvent ya reparte a todos los handlers locales, así que un solo puente
    // basta; registrar más provocaría emisiones duplicadas.
    if (this.hubConnection && !this.attachedBridges.has(eventName)) {
      try {
        (this.hubConnection as any).on(eventName, (...args: any[]) => {
          this.emitEvent(eventName, ...args);
        });
        this.attachedBridges.add(eventName);
      } catch (err) {
        logger.warn(`[SignalR DS] Error al registrar handler en hubConnection para ${eventName}:`, err);
      }
    }
  }

  /** Elimina todos los handlers (y el puente) de un evento concreto. */
  off(eventName: string): void {
    if (this.hubConnection) {
      try {
        (this.hubConnection as any).off(eventName);
      } catch {
        // noop
      }
    }
    this.attachedBridges.delete(eventName);
    this.eventHandlers.delete(eventName);
  }

  /** Elimina todos los handlers de todos los eventos y limpia los puentes. */
  offAll(): void {
    if (this.hubConnection) {
      try {
        (this.hubConnection as any).offAll?.();
      } catch {
        this.eventHandlers.forEach((_, eventName) => {
          try { (this.hubConnection as any).off(eventName); } catch {}
        });
      }
    }
    this.attachedBridges.clear();
    this.eventHandlers.clear();
  }

  /** Estado actual de la conexión. */
  getState(): ConnectionState {
    return this.connectionState;
  }

  /**
   * Reparte un evento a todos los handlers locales registrados para él.
   *
   * @remarks
   * Aísla los errores de cada handler (los registra y continúa) para que un fallo en uno
   * no impida ejecutar el resto.
   */
  private emitEvent(eventName: string, ...args: any[]): void {
    const handlers = this.eventHandlers.get(eventName);
    if (handlers) {
      handlers.forEach(handler => {
        try {
          handler(...args);
        } catch (error) {
          logger.error(`[SignalR DS] Error en handler de ${eventName}:`, error);
        }
      });
    }
  }
}
