# AjedrezOnline

> Ajedrez multijugador en tiempo real, full-stack: un servidor **.NET 8 + SignalR** que arbitra las partidas y un cliente **React Native (Expo) + TypeScript**, ambos construidos sobre **Clean Architecture** con el mismo dominio espejado.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![SignalR](https://img.shields.io/badge/SignalR-Real--time-512BD4)](https://learn.microsoft.com/aspnet/core/signalr/introduction)
[![Expo](https://img.shields.io/badge/Expo-54-000020?logo=expo&logoColor=white)](https://expo.dev/)
[![React Native](https://img.shields.io/badge/React%20Native-0.81-61DAFB?logo=react&logoColor=black)](https://reactnative.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![MobX](https://img.shields.io/badge/MobX-State-FF9955?logo=mobx&logoColor=white)](https://mobx.js.org/)

---

## Tabla de contenidos

1. [Descripción](#descripción)
2. [Características](#características)
3. [Stack tecnológico](#stack-tecnológico)
4. [Arquitectura](#arquitectura)
5. [Estructura del proyecto](#estructura-del-proyecto)
6. [Modelo de dominio](#modelo-de-dominio)
7. [Protocolo en tiempo real (SignalR)](#protocolo-en-tiempo-real-signalr)
8. [Flujo de un movimiento de extremo a extremo](#flujo-de-un-movimiento-de-extremo-a-extremo)
9. [Cómo ejecutar el proyecto](#cómo-ejecutar-el-proyecto)
10. [Decisiones técnicas destacadas](#decisiones-técnicas-destacadas)
11. [Autor](#autor)

---

## Descripción

**AjedrezOnline** es una aplicación de ajedrez **1 contra 1 en tiempo real**. Un
jugador crea una sala, otro se une por su nombre y la partida arranca al instante;
a partir de ahí, cada movimiento, oferta de tablas, promoción o rendición viaja
entre ambos clientes a través de un servidor **SignalR** que actúa como árbitro
autoritativo.

El proyecto se ha construido como pieza de portfolio para demostrar el desarrollo
**full-stack end-to-end** de una aplicación con estado compartido y comunicación
bidireccional: un backend en **.NET 8** que contiene el motor de reglas del ajedrez
y un cliente móvil en **React Native / Expo**. Lo más interesante del diseño es que
**ambos lados aplican la misma Clean Architecture y comparten el mismo modelo de
dominio** (`Partida`, `Tablero`, `Pieza`, `Movimiento`, `Sala`, `Jugador`) y la misma
convención de coordenadas, lo que mantiene cliente y servidor perfectamente alineados.

El servidor es la **fuente de verdad**: valida cada jugada con su propio motor antes
de propagarla. El cliente reimplementa la lógica de movimientos únicamente para dar
respuesta visual inmediata (resaltar jugadas legales), pero nunca decide el resultado.

---

## Características

### Juego

- **Salas con emparejamiento por nombre**: un jugador crea la sala, otro se une y la
  partida se inicia automáticamente repartiendo los colores **al azar**.
- **Tablero interactivo 8×8** con resaltado de los movimientos legales de la pieza
  seleccionada e indicador de turno y de jaque.
- **Reglas completas de ajedrez**, validadas en el servidor:
  - Movimiento de las seis piezas, capturas y bloqueos.
  - **Enroque** corto y largo (rey y torre sin mover, casillas intermedias libres,
    el rey no parte ni cruza ni cae en casilla atacada).
  - **Captura al paso** (*en passant*).
  - **Promoción de peón** con selección de pieza (dama, torre, alfil o caballo).
  - **Jaque** y detección automática de **jaque mate**.
- **Confirmación de jugada en dos fases**: el movimiento se aplica de forma
  provisional y el jugador puede **confirmarlo** (cede el turno) o **deshacerlo**
  antes de consolidarlo.
- **Tablas** por acuerdo (ambos jugadores las ofrecen), **rendición** y **revancha**
  (reinicio de la partida si los dos aceptan).
- **Gestión de abandono y desconexión**: si un jugador deja la sala o pierde la
  conexión durante una partida en curso, el rival es notificado y la partida finaliza
  por abandono en lugar de quedar colgada.

### Técnicas

- Comunicación **en tiempo real** sobre WebSockets con **reconexión automática**.
- Identificación robusta del jugador local por `connectionId` de SignalR (no por
  nombre), de modo que dos jugadores homónimos no se confunden.
- Enums serializados como **texto** (`"Blanca"`, `"JaqueMate"`…) para un contrato
  estable y legible con el cliente.
- Estado de juego reactivo en el cliente mediante **MobX** (patrón MVVM).

---

## Stack tecnológico

### Backend (`/Backend`)

| Categoría        | Tecnología                                                              |
| ---------------- | ---------------------------------------------------------------------- |
| Lenguaje         | C# 12 sobre **.NET 8**                                                 |
| Host web         | ASP.NET Core (Kestrel) + Razor Pages                                   |
| Tiempo real      | **SignalR** (WebSockets y Long Polling)                               |
| Arquitectura     | Clean Architecture: `Domain` · `Infrastructure` · `ServidorAjedrez`   |
| Casos de uso     | 12 *use cases* tras interfaces, uno por acción del juego               |
| Persistencia     | **En memoria**, repositorios protegidos con `SemaphoreSlim`           |
| Inyección de dep.| `Microsoft.Extensions.DependencyInjection`                            |
| Serialización    | `System.Text.Json` con `JsonStringEnumConverter`                      |
| Logging          | `Microsoft.Extensions.Logging` (consola)                              |

### Frontend (`/Frontend`)

| Categoría        | Tecnología                                                              |
| ---------------- | ---------------------------------------------------------------------- |
| Lenguaje         | **TypeScript** (modo estricto)                                         |
| Framework        | **React Native 0.81** + React 19 sobre **Expo 54**                    |
| Navegación       | **Expo Router** (rutas basadas en archivos)                           |
| Tiempo real      | Cliente **`@microsoft/signalr`** (WebSockets, reconexión automática)  |
| Estado / UI      | **MobX** + `mobx-react-lite` — ViewModels observables (MVVM)          |
| Arquitectura     | Clean Architecture: `core` · `domain` · `data` · `presentation`       |
| Inyección de dep.| Contenedor propio de *singletons* (`core/container.ts`)               |
| Mapeo            | *Mappers* DTO ↔ entidad de dominio                                     |

---

## Arquitectura

Dos aplicaciones independientes que se comunican exclusivamente por el **hub de
SignalR**. El servidor es autoritativo; el cliente refleja su estado.

```
        ┌───────────────────────────────┐         ┌───────────────────────────────┐
        │      CLIENTE (Expo / RN)      │         │       SERVIDOR (.NET 8)       │
        │                               │         │                               │
        │  presentation (MVVM + MobX)   │         │  ServidorAjedrez (Razor +     │
        │  data (SignalR DataSource)    │ ◄─────► │     SignalR Hub)              │
        │  domain (entidades + casos)   │ WebSock │  Domain (motor + casos de uso)│
        │  core (DI + tipos)            │  /Hub   │  Infrastructure (repos memoria)│
        └───────────────────────────────┘         └───────────────────────────────┘
                     mismo modelo de dominio espejado (Partida, Tablero, Pieza…)
```

Ambos lados siguen **Clean Architecture**: las dependencias apuntan siempre hacia el
dominio, que no conoce ni a SignalR ni a la UI.

### Servidor — capas

- **`Domain`** — núcleo puro, sin dependencias de ASP.NET:
  - **Entidades**: `Partida` (raíz del agregado), `Tablero` (el motor de reglas:
    generación de movimientos, jaque y jaque mate), `Pieza`, `Movimiento`, `Sala`,
    `Jugador`, el *value object* `Posicion` y los *enums* del juego.
  - **Interfaces** de los 12 casos de uso + repositorios (`IPartidaRepository`,
    `ISalaRepository`) e `IConnectionManager`: son la fuente de verdad del contrato.
  - **Casos de uso**: una clase por acción (crear/unirse a sala, mover, confirmar,
    deshacer, tablas, rendirse, promocionar, reinicio, abandonar), que orquestan el
    dominio y persisten el resultado.
  - **DTOs** con sus *mappers* `FromDomain`, para no exponer las entidades al cliente.
- **`Infrastructure`** — implementaciones: repositorios **en memoria** (thread-safe
  con `SemaphoreSlim`), `ConnectionManager` (diccionarios concurrentes conexión→sala
  y conexión→nombre) y el registro de dependencias (`AddInfrastructure`).
- **`ServidorAjedrez`** — capa web: el **`AjedrezHub`** de SignalR (la API en tiempo
  real), las Razor Pages y `Program.cs` (configuración de SignalR, CORS, sesión).

### Cliente — capas

- **`core`** — contenedor de inyección de dependencias propio (registro de
  *singletons*), tipos/DTOs compartidos y utilidades de coordenadas.
- **`domain`** — entidades espejo del servidor, `IAjedrezRepository`,
  `IAjedrezUseCase`, el caso de uso `AjedrezUseCase` y los *mappers* DTO ↔ entidad.
- **`data`** — `SignalRAjedrezDataSource` (envuelve el `HubConnection`) y
  `AjedrezRepositorySignalR`, que implementa el repositorio sobre esa fuente.
- **`presentation`** — patrón **MVVM**: un `ViewModel` MobX por pantalla
  (`IdentificacionVM`, `MenuPrincipalVM`, `PartidaVM`), *hooks* que los exponen y
  los componentes/pantallas que los consumen.

---

## Estructura del proyecto

```
AjedrezOnline/
├── Backend/                         # Solución .NET (ServidorAjedrez.sln)
│   ├── Domain/                      # Núcleo de negocio (sin dependencias web)
│   │   ├── Entities/               # Partida, Tablero, Pieza, Movimiento, Sala, Jugador
│   │   ├── ValueObjects/           # Posicion
│   │   ├── Enums/                  # Color, TipoPieza, EstadoPartida, ResultadoPartida…
│   │   ├── Exceptions/             # DomainException y derivadas
│   │   ├── DTOs/                   # *DTO + FromDomain (contrato con el cliente)
│   │   ├── Interfaces/             # I*UseCase, IConnectionManager
│   │   ├── Repositories/           # IPartidaRepository, ISalaRepository
│   │   └── UseCases/               # 12 casos de uso + MovimientoMapper
│   ├── Infrastructure/
│   │   ├── Persistence/            # InMemory*Repository (SemaphoreSlim)
│   │   ├── SignalR/                # ConnectionManager
│   │   └── DI/                     # DependencyInjection.AddInfrastructure
│   └── ServidorAjedrez/            # Proyecto web (host)
│       ├── Hubs/AjedrezHub.cs      # Hub de SignalR — API en tiempo real
│       ├── Pages/                  # Razor Pages
│       └── Program.cs              # Arranque, SignalR, CORS, sesión
│
└── Frontend/                        # Cliente Expo / React Native (ClienteAjedrez)
    ├── app/                         # Rutas de Expo Router
    │   ├── _layout.tsx             # Pila de navegación + registro de DI
    │   ├── index.tsx · identificacion.tsx
    │   └── menu-principal.tsx · partida.tsx
    └── src/
        ├── core/                   # container.ts, registrations.ts, types.ts, gameState.ts
        ├── domain/                 # entities, interfaces, usecases, repositories, mappers
        ├── data/                   # datasources (SignalR), repositories
        └── presentation/           # viewmodels (MobX), hooks, components, screens
```

---

## Modelo de dominio

El mismo grafo de entidades vive en cliente y servidor:

```
            ┌─────────┐        crea/contiene        ┌─────────┐
            │  Sala   │ 1 ───────────────────────► 1│ Partida │
            ├─────────┤                              ├─────────┤
            │ creador │── 1     ┌───────────────────┤ tablero │
            │ oponente│── 0..1  │                    │ turno   │
            │ estado  │         │                    │ estado  │
            └─────────┘         ▼                    │ resultado
                          ┌──────────┐               └────┬────┘
                          │ Jugador  │                    │ 1
                          ├──────────┤                    ▼
                          │ id       │              ┌──────────┐
                          │ nombre   │              │ Tablero  │
                          │ color?   │              ├──────────┤
                          │connection│              │ piezas[] │──► Pieza
                          └──────────┘              │ historial│──► Movimiento
                                                    └──────────┘
```

**Convenciones y decisiones de modelado relevantes:**

- **Coordenadas alineadas cliente/servidor**: el tablero es una matriz 8×8 donde la
  `fila 0` es el borde de las **negras** (arriba) y la `fila 7` el de las **blancas**
  (abajo); la `columna 0..7` corresponde a las columnas `a..h`.
- **Identidad estable frente a transporte**: cada `Jugador` tiene un `Id` de dominio
  (GUID) **distinto** de su `ConnectionId` de SignalR (que puede cambiar al
  reconectar). El servidor traduce el connectionId entrante al jugador de dominio.
- **Borrado lógico de piezas**: una pieza capturada no se elimina de la colección,
  se marca como `Eliminada`. Esto permite **simular y deshacer** movimientos sin
  perder información (clave para la detección de jaque y para el "deshacer").
- **Estado de reversión en el servidor**: al aplicar un movimiento, la `Partida`
  guarda lo necesario para deshacerlo (pieza capturada, indicador de "se ha movido"
  y desplazamiento de la torre en el enroque), **sin depender de los datos del
  cliente**.
- **Enums serializados por nombre**: viajan como texto, de modo que el cliente recibe
  valores estables y legibles y añadir un valor nuevo no rompe el contrato.

---

## Protocolo en tiempo real (SignalR)

El cliente **invoca** métodos del hub; el servidor **emite** eventos al grupo de la
sala (o solo al emisor). El endpoint del hub es `/ajedrezHub`.

### Cliente → Servidor (métodos del hub)

| Método                       | Acción                                                        |
| ---------------------------- | ------------------------------------------------------------ |
| `SetNombreJugador(nombre)`   | Registra el nombre antes de crear/unirse a una sala          |
| `CrearSala(nombreSala)`      | Crea una sala y entra en su grupo                            |
| `UnirseSala(sala, nombre)`   | Se une como oponente e inicia la partida                    |
| `RealizarMovimiento(dto)`    | Aplica un movimiento de forma provisional                   |
| `ConfirmarMovimiento()`      | Confirma el movimiento pendiente y cede el turno            |
| `DeshacerMovimiento()`       | Revierte el movimiento pendiente                            |
| `SolicitarTablas()` / `RetirarTablas()` | Ofrece o retira tablas                            |
| `Rendirse()`                 | Se rinde de la partida                                       |
| `PromocionarPeon(tipo)`      | Corona el peón al tipo elegido                              |
| `SolicitarReinicio()` / `RetirarReinicio()` | Ofrece o retira revancha                     |
| `AbandonarSala()`            | Abandona la sala explícitamente                            |

### Servidor → Cliente (eventos)

| Evento                  | Significado                                                       |
| ----------------------- | ---------------------------------------------------------------- |
| `SalaCreada`            | La sala se creó (al creador)                                     |
| `PartidaIniciada`       | Comienza (o se reinicia) la partida, con su estado completo      |
| `MovimientoRealizado`   | Se aplicó un movimiento; incluye el movimiento y el tablero      |
| `TurnoActualizado`      | Cambió el turno tras una confirmación                            |
| `TableroActualizado`    | El tablero cambió (deshacer, promoción)                          |
| `PromocionRequerida`    | El movimiento confirmado exige coronar un peón (al emisor)       |
| `TablasActualizadas`    | Cambió el estado de las ofertas de tablas                        |
| `ReinicioActualizado`   | Cambió el estado de las solicitudes de revancha                  |
| `PartidaFinalizada`     | Fin de la partida (mate, tablas, rendición o abandono)           |
| `JugadorAbandonado`     | Un jugador dejó la sala o se desconectó                          |
| `Error`                 | Error controlado dirigido al jugador que originó la acción       |

---

## Flujo de un movimiento de extremo a extremo

Mover una pieza recorre las dos aplicaciones y muestra el modelo de **dos fases**
(aplicar → confirmar):

```
 CLIENTE                                           SERVIDOR (.NET)
 ───────                                           ───────────────
 PartidaScreen  (toca origen y destino)
   └─ PartidaVM.seleccionarCasilla()
        └─ AjedrezUseCase.moverPieza(mov)
             └─ AjedrezRepositorySignalR
                  └─ SignalRDataSource.invoke ───►  AjedrezHub.RealizarMovimiento(dto)
                                                       └─ RealizarMovimientoUseCase
                                                            └─ Partida.RealizarMovimiento()
                                                                 (valida con Tablero,
                                                                  guarda estado de reversión,
                                                                  deja MovimientoPendiente)
                       ◄──── "MovimientoRealizado" (movimiento + tablero) ──── a la sala

 El jugador revisa la jugada provisional y decide:

   confirmar ─► AjedrezHub.ConfirmarMovimiento()
                  └─ Partida.ConfirmarMovimiento()  (cambia turno, evalúa jaque mate)
                       ◄── "TurnoActualizado"  (y "PartidaFinalizada" si hay mate,
                            o "PromocionRequerida" si toca coronar)

   deshacer  ─► AjedrezHub.DeshacerMovimiento()
                  └─ Partida.DeshacerMovimiento()  (restaura con el estado guardado)
                       ◄── "TableroActualizado"
```

La pantalla se actualiza **reactivamente**: el `PartidaVM` (MobX) recibe el evento de
SignalR, actualiza su estado observable y la UI se vuelve a renderizar sin recargas
manuales.

---

## Cómo ejecutar el proyecto

Necesitas el servidor en marcha **antes** de arrancar el cliente.

### 1. Backend (.NET 8)

**Requisitos:** SDK de **.NET 8** o superior.

```bash
cd AjedrezOnline/Backend
dotnet run --project ServidorAjedrez
```

El servidor levanta el hub en `/ajedrezHub` (por ejemplo,
`https://localhost:7049/ajedrezHub` en desarrollo; comprueba el puerto que muestre la
consola). El CORS está configurado para aceptar al cliente.

### 2. Frontend (Expo / React Native)

**Requisitos:** **Node.js 18+** y la app **Expo Go** (o un emulador Android/iOS).

```bash
cd AjedrezOnline/Frontend
npm install
cp .env.example .env        # y edita EXPO_PUBLIC_HUB_URL con la URL de tu hub
npm start
```

Configura en `.env` la URL del servidor:

```bash
EXPO_PUBLIC_HUB_URL=https://<tu-ip-o-host>:7049/ajedrezHub
```

> Desde un dispositivo físico, usa la **IP de tu máquina** en la red local (no
> `localhost`) para que el móvil alcance al servidor.

Después, abre la app con Expo Go escaneando el QR, o pulsa `a` (Android), `i` (iOS) o
`w` (web). Para probar una partida real, abre **dos clientes**: uno crea la sala y el
otro se une con el mismo nombre de sala.

---

## Decisiones técnicas destacadas

- **Servidor autoritativo con dominio espejado**: el motor de reglas
  (`Tablero`) vive en el servidor y valida cada jugada; el cliente reimplementa la
  lógica solo para feedback visual inmediato (resaltar movimientos legales), pero
  nunca decide. Compartir el modelo de dominio a ambos lados mantiene el contrato
  coherente sin acoplarlos.
- **Movimiento en dos fases con reversión en el servidor**: aplicar y confirmar/
  deshacer están separados. El servidor guarda su **propio** estado de reversión
  (captura, derecho de enroque, torre del enroque), de modo que "deshacer" no
  depende de datos que envíe el cliente — más robusto y a prueba de manipulación.
- **Identidad por `connectionId`, no por nombre**: el jugador local se reconoce por
  su connectionId de SignalR; el nombre es solo un respaldo. Así, dos jugadores que
  elijan el mismo nombre no se confunden.
- **Capa SignalR robusta en el cliente**: los *handlers* se guardan localmente y
  **sobreviven a las reconexiones**; se engancha **un único "puente" por evento**
  para evitar emisiones duplicadas; y se usa **solo WebSockets** con
  `skipNegotiation` y reintentos automáticos escalonados.
- **Race condition de navegación resuelta**: la partida puede llegar (`PartidaIniciada`)
  antes de que la pantalla monte; el cliente la guarda en un estado transitorio
  (`gameState`) y la pantalla la consume al montar, con suscripción de respaldo.
- **Concurrencia en memoria**: los repositorios serializan sus accesos con
  `SemaphoreSlim`, suficiente para un almacén en memoria compartido por varias
  conexiones simultáneas.
- **Enums como texto en el cable**: `JsonStringEnumConverter` hace que el contrato
  sea legible y estable frente a reordenaciones de los enums.
- **Desconexión = abandono**: `OnDisconnectedAsync` reutiliza el caso de uso de
  abandono para notificar al rival y cerrar la partida en curso, evitando partidas
  "zombi".

---

## Autor

**Andrés Ojeda Rodríguez**
[andresojedarodriguez@gmail.com](mailto:andresojedarodriguez@gmail.com)


---

## Licencia

Este proyecto está licenciado bajo la **PolyForm Noncommercial License 1.0.0**.
Puedes ver, ejecutar, estudiar y modificar el código con fines **no comerciales**
(estudio personal, educación, evaluación), pero **cualquier uso comercial requiere
permiso escrito del autor**. Consulta el archivo [LICENSE.md](LICENSE.md) para los
términos completos.

© 2026 Andrés Ojeda Rodríguez. Todos los derechos no concedidos expresamente quedan reservados.
