# ClienteAjedrez - Documentación

## Descripción

ClienteAjedrez es un cliente móvil de ajedrez desarrollado en React Native + TypeScript que se conecta a un servidor SignalR para jugar partidas en tiempo real entre dos jugadores.

## Características Implementadas

### ✅ Funcionalidad Core
- **Identificación**: Pantalla inicial para ingresar nombre de jugador
- **Menú Principal**: Crear o unirse a salas de juego
- **Partida**: Interfaz completa de juego con tablero interactivo
- **Conexión SignalR**: Comunicación en tiempo real con el servidor

### ✅ Lógica de Ajedrez
- **Movimientos Legales**: Validación completa para todas las piezas
  - Peón: movimiento lineal, captura diagonal, captura al paso
  - Torre: movimiento lineal (horizontal/vertical)
  - Caballo: movimiento en L
  - Alfil: movimiento diagonal
  - Reina: combinación de torre y alfil
  - Rey: una casilla en cualquier dirección

- **Enroque**: Implementado con validaciones
  - Enroque corto y largo
  - Verifica que rey y torre no hayan movido
  - Verifica casillas intermedias vacías
  - Verifica que el rey no esté en jaque ni pase por jaque

- **Captura al Paso**: Implementada para peones

- **Promoción de Peón**: Modal para elegir pieza (Torre, Caballo, Alfil, Reina)

- **Jaque y Jaque Mate**: Detección automática

### ✅ Interfaz de Usuario
- Tablero 8x8 interactivo
- Resaltado de movimientos posibles
- Información de turno y jaque
- Contadores de piezas eliminadas
- Botones de tablas y rendición
- Modal de promoción
- Modal de fin de partida

### ✅ Arquitectura
- Clean Architecture: `core`, `domain`, `data`, `presentation`
- TypeScript estricto
- MobX para state management
- Inyección de dependencias con contenedor simple
- Mappers para DTOs ↔ Entidades

## Instalación y Configuración

### Requisitos
- Node.js v18+
- npm o yarn
- Expo CLI

### Pasos

1. **Instalar dependencias**
```bash
npm install
# o
yarn install
```

2. **Configurar URL del servidor**

Edita el archivo `src/presentation/screens/MenuPrincipalScreen.tsx`:

```typescript
const HUB_URL = 'http://localhost:5000/ajedrezHub'; // Cambiar según tu servidor
```

El URL debe apuntar a tu servidor SignalR con el hub "ajedrezHub".

3. **Ejecutar la aplicación**

```bash
# Iniciar Expo
npm start
# o
yarn start

# Luego escoge:
# - 'i' para iOS
# - 'a' para Android
# - 'w' para Web
```

## Estructura del Proyecto

```
src/
├── core/
│   ├── types.ts              # Tipos globales y constantes
│   └── container.ts          # Contenedor DI simple
├── domain/
│   ├── entities/             # Entidades del dominio
│   │   ├── Jugador.ts
│   │   ├── Pieza.ts
│   │   ├── Movimiento.ts
│   │   ├── Tablero.ts        # Lógica de movimientos y validaciones
│   │   ├── Sala.ts
│   │   └── Partida.ts
│   ├── dto/                  # Data Transfer Objects (DTOs)
│   ├── repositories/         # Interfaz IAjedrezRepository
│   ├── interfaces/           # Interfaz IAjedrezUseCase
│   ├── usecases/             # AjedrezUseCase
│   └── mappers/              # Conversión DTO ↔ Entidad
├── data/
│   ├── datasources/          # SignalRAjedrezDataSource
│   ├── repositories/         # AjedrezRepositorySignalR
│   └── mappers/              # Mappers de datos
└── presentation/
    ├── viewmodels/           # MobX ViewModels
    ├── hooks/                # React hooks
    ├── components/           # Componentes UI
    └── screens/              # Pantallas
```

## API del Servidor (SignalR)

### Métodos que invoca el cliente

- `SetNombreJugador(nombre: string)` - Registra el nombre del jugador
- `CrearSala(nombreSala: string)` - Crea una nueva sala
- `UnirseSala(nombreSala: string)` - Se une a una sala existente
- `AbandonarSala()` - Abandona la sala actual
- `RealizarMovimiento(movimiento: object)` - Realiza un movimiento
- `ConfirmarMovimiento()` - Confirma un movimiento
- `DeshacerMovimiento()` - Deshace un movimiento
- `SolicitarTablas()` - Solicita tablas
- `RetirarTablas()` - Retira solicitud de tablas
- `Rendirse()` - Se rinde de la partida
- `PromocionarPeon(tipoPieza: string)` - Promociona un peón
- `SolicitarReinicio()` - Solicita reinicio de partida
- `RetirarReinicio()` - Retira solicitud de reinicio

### Eventos que recibe el cliente

- `SalaCreada(sala: SalaDTO)` - Notifica cuando se crea una sala
- `JugadorUnido(partida: PartidaDTO)` - Notifica cuando se une un oponente
- `PartidaIniciada(partida: PartidaDTO)` - Notifica cuando inicia la partida
- `MovimientoRealizado(movimiento: MovimientoDTO, tablero: TableroDTO)` - Movimiento del oponente
- `TurnoActualizado(turno: Color, numeroTurno: number)` - Actualización de turno
- `TablasActualizadas(blancas: boolean, negras: boolean)` - Estado de solicitudes de tablas
- `PartidaFinalizada(resultado: string, tipo: string, ganador?: string)` - Fin de partida
- `JaqueActualizado(hayJaque: boolean)` - Notifica jaque
- `PromocionRequerida()` - Solicita selección de promoción
- `ReinicioActualizado(blancas: boolean, negras: boolean)` - Estado de reinicio
- `JugadorAbandonado(nombreJugador: string)` - Jugador abandonó
- `Error(error: string)` - Notifica errores

## Tests

Ejecutar tests:
```bash
npm test
# o
yarn test
```

Tests skeleton incluidos para:
- Validación de movimientos en Tablero
- Comportamiento de PartidaVM

## Limitaciones y Mejoras Futuras

### Limitaciones Actuales
- ❌ Sin reloj de juego (sin tiempo por turno)
- ❌ Sin interfaz de chat
- ❌ Sin historial de partidas
- ❌ Sin estadísticas de jugador
- ❌ Sin revancha automática
- ❌ Sin notaciones algebraicas en movimientos

### Mejoras Recomendadas
1. **Optimización Visual**
   - Animaciones de movimientos
   - Efectos de sonido
   - Temas personalizables

2. **Funcionalidad Extendida**
   - Sistema de rating Elo
   - Ligas/Torneos
   - Análisis de partidas
   - Movimientos sugeridos

3. **Rendimiento**
   - Memoización de cálculos de movimientos
   - Caché de posiciones
   - Optimización de re-renders

4. **Testing**
   - Tests unitarios completos
   - Tests de integración
   - Tests e2e con Detox

5. **Seguridad**
   - Autenticación OAuth2
   - Validación de movimientos en servidor
   - Anti-cheat

## Notas de Implementación

### Decisiones Algorítmicas
- **Detección de Jaque**: Se realiza simulando el movimiento y verificando si el rey sigue en jaque
- **Movimientos Posibles**: Se calculan para todas las piezas y se filtran aquellos que dejarían al rey en jaque
- **Enroque**: Se valida completamente (rey/torre sin mover, casillas vacías, no pasa por jaque)

### Puntos Complejos
- La detección de jaque mate verifica que haya jaque Y que no haya movimientos legales disponibles
- La captura al paso requiere verificar el movimiento anterior del oponente
- El enroque modifica la posición de dos piezas (rey y torre)

### Manejo de Errores
- Todos los métodos async usan try/catch
- Los errores se propagann al ViewModel y se muestran al usuario
- Se incluye logging en consola para debugging

## Variables de Entorno

```typescript
// En MenuPrincipalScreen.tsx
const HUB_URL = 'http://localhost:5000/ajedrezHub';
```

Para modo producción, cambiar a:
```typescript
const HUB_URL = 'https://tu-servidor.com/ajedrezHub';
```

## Contribuir

Para agregar funcionalidad:
1. Identificar la capa (domain/data/presentation)
2. Crear la entidad/clase necesaria
3. Actualizar interfaces si es necesario
4. Implementar lógica
5. Agregar tests
6. Actualizar documentación

## Licencia

Este proyecto es parte del curso de INTERFACES.
