// partida.js
const HUB_URL = 'https://localhost:7040/ajedrezHub';
let currentPartida = null;
let connection = null;
let selectedSquare = null;

// Inicializar conexión SignalR
async function initializeConnection() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl(HUB_URL, {
            skipNegotiation: true,
            transport: signalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect()
        .build();

    connection.on('MovimientoRealizado', function (movimiento, tablero) {
        console.log('Movimiento realizado:', movimiento);
        renderBoard(tablero);
    });

    connection.on('TurnoActualizado', function (turno, numeroTurnos) {
        console.log('Turno actualizado:', turno);
        document.getElementById('currentTurn').textContent = turno === 0 ? 'Blancas' : 'Negras';
        document.getElementById('turnCount').textContent = numeroTurnos;
    });

    connection.on('TablasActualizadas', function (blancas, negras) {
        console.log('Tablas actualizadas:', blancas, negras);
    });

    connection.on('PartidaFinalizada', function (resultado, tipoFin, ganador) {
        console.log('Partida finalizada:', resultado, tipoFin);
        mostrarModalFinPartida(resultado, tipoFin, ganador);
    });

    connection.on('JaqueActualizado', function (hayJaque) {
        console.log('Jaque:', hayJaque);
        if (hayJaque) {
            alert('¡JAQUE!');
        }
    });

    connection.on('PromocionRequerida', function () {
        console.log('Promoción de peón requerida');
        mostrarModalPromocion();
    });

    connection.on('Error', function (message) {
        console.error('Error del servidor:', message);
        alert('Error: ' + message);
    });

    try {
        await connection.start();
        console.log('Conectado al hub de SignalR');
    } catch (err) {
        console.error('Error al conectar:', err);
        setTimeout(() => initializeConnection(), 5000);
    }
}

// Renderizar el tablero
function renderBoard(tablero) {
    const board = document.getElementById('board');
    board.innerHTML = '';
    
    // Crear 64 cuadrados
    for (let fila = 7; fila >= 0; fila--) {
        for (let col = 0; col < 8; col++) {
            const square = document.createElement('div');
            const isWhite = (fila + col) % 2 === 0;
            square.style.backgroundColor = isWhite ? '#f0d9b5' : '#b58863';
            square.style.aspectRatio = '1';
            square.style.display = 'flex';
            square.style.alignItems = 'center';
            square.style.justifyContent = 'center';
            square.style.cursor = 'pointer';
            square.style.fontSize = '24px';
            square.dataset.fila = fila;
            square.dataset.columna = col;
            
            // Encontrar pieza en esta posición
            if (tablero && tablero.piezas) {
                const pieza = tablero.piezas.find(p => p.posicion.fila === fila && p.posicion.columna === col && !p.eliminada);
                if (pieza) {
                    square.textContent = getPiezaSymbol(pieza.tipo, pieza.color);
                }
            }
            
            square.addEventListener('click', function () {
                handleSquareClick(fila, col, tablero);
            });
            
            board.appendChild(square);
        }
    }
}

// Obtener símbolo de pieza
function getPiezaSymbol(tipo, color) {
    const symbols = {
        0: color === 0 ? '?' : '?', // Peon
        1: color === 0 ? '?' : '?', // Torre
        2: color === 0 ? '?' : '?', // Caballo
        3: color === 0 ? '?' : '?', // Alfil
        4: color === 0 ? '?' : '?', // Reina
        5: color === 0 ? '?' : '?'  // Rey
    };
    return symbols[tipo] || '';
}

// Manejar clic en cuadrado
function handleSquareClick(fila, columna, tablero) {
    if (!selectedSquare) {
        selectedSquare = { fila, columna };
        console.log(`Cuadrado seleccionado: ${fila},${columna}`);
    } else {
        // Intentar mover
        const movimiento = {
            piezaId: null, // Necesitaría obtener de la pieza
            origen: { fila: selectedSquare.fila, columna: selectedSquare.columna },
            destino: { fila, columna }
        };
        
        console.log('Intentando mover:', movimiento);
        selectedSquare = null;
    }
}

// Modales
function mostrarModalPromocion() {
    const modal = new bootstrap.Modal(document.getElementById('promotionModal'));
    modal.show();
}

function mostrarModalFinPartida(resultado, tipoFin, ganador) {
    const mensaje = document.getElementById('gameEndMessage');
    let text = '';
    
    if (resultado === 0) text = 'Victoria de Blancas';
    else if (resultado === 1) text = 'Victoria de Negras';
    else if (resultado === 2) text = 'Empate';
    
    mensaje.textContent = text + ' (' + tipoFin + ')';
    
    const modal = new bootstrap.Modal(document.getElementById('gameEndModal'));
    modal.show();
}

// Botones de acción
document.getElementById('undoBtn').addEventListener('click', async function () {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        try {
            await connection.invoke('DeshacerMovimiento');
        } catch (err) {
            console.error('Error:', err);
        }
    }
});

document.getElementById('drawBtn').addEventListener('click', async function () {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        try {
            await connection.invoke('SolicitarTablas');
        } catch (err) {
            console.error('Error:', err);
        }
    }
});

document.getElementById('surrenderBtn').addEventListener('click', async function () {
    if (confirm('¿Estás seguro que quieres rendirte?')) {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            try {
                await connection.invoke('Rendirse');
            } catch (err) {
                console.error('Error:', err);
            }
        }
    }
});

document.getElementById('backToMenuBtn').addEventListener('click', function () {
    sessionStorage.removeItem('currentPartida');
    window.location.href = '/MenuPrincipal';
});

// Inicializar cuando carga la página
window.addEventListener('load', function () {
    const partidaJson = sessionStorage.getItem('currentPartida');
    if (partidaJson) {
        currentPartida = JSON.parse(partidaJson);
        console.log('Partida cargada:', currentPartida);
        
        // Actualizar UI
        document.getElementById('playerColor').textContent = currentPartida.turnoActual === 0 ? 'Blancas' : 'Negras';
        document.getElementById('opponentName').textContent = 'Oponente';
        document.getElementById('currentTurn').textContent = currentPartida.turnoActual === 0 ? 'Blancas' : 'Negras';
        document.getElementById('turnCount').textContent = currentPartida.numeroTurnos;
        
        // Renderizar tablero
        renderBoard(currentPartida.tablero);
    }
    
    initializeConnection();
});

// Limpiar conexión al salir
window.addEventListener('beforeunload', function () {
    if (connection) {
        connection.stop();
    }
});
