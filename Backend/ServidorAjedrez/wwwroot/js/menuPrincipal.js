// menuPrincipal.js
const HUB_URL = 'https://localhost:7040/ajedrezHub';
let playerName = sessionStorage.getItem('playerName') || '';
let connection = null;

document.getElementById('playerNameEdit').value = playerName;

// Inicializar conexión SignalR
async function initializeConnection() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl(HUB_URL, {
            skipNegotiation: true,
            transport: signalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect()
        .build();

    connection.on('SalaCreada', function (sala) {
        console.log('Sala creada:', sala);
        alert(`Sala "${sala.nombre}" creada exitosamente. Esperando oponente...`);
        sessionStorage.setItem('currentSala', JSON.stringify(sala));
    });

    connection.on('PartidaIniciada', function (partida) {
        console.log('Partida iniciada:', partida);
        sessionStorage.setItem('currentPartida', JSON.stringify(partida));
        window.location.href = '/Partida?partidaId=' + partida.id;
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

// Crear Sala
document.getElementById('createRoomBtn').addEventListener('click', async function () {
    const roomName = document.getElementById('roomNameCreate').value.trim();
    
    if (!roomName) {
        alert('Por favor ingresa el nombre de la sala');
        return;
    }
    
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        alert('No estás conectado al servidor');
        return;
    }
    
    try {
        await connection.invoke('CrearSala', roomName);
        document.getElementById('roomNameCreate').value = '';
    } catch (err) {
        console.error('Error al crear sala:', err);
        alert('Error al crear sala: ' + err.message);
    }
});

// Unirse a Sala
document.getElementById('joinRoomBtn').addEventListener('click', async function () {
    const roomName = document.getElementById('roomNameJoin').value.trim();
    const playerNameInput = document.getElementById('playerNameEdit').value.trim();
    
    if (!roomName) {
        alert('Por favor ingresa el nombre de la sala');
        return;
    }
    
    if (!playerNameInput) {
        alert('Por favor ingresa tu nombre');
        return;
    }
    
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        alert('No estás conectado al servidor');
        return;
    }
    
    try {
        sessionStorage.setItem('playerName', playerNameInput);
        await connection.invoke('UnirseSala', roomName, playerNameInput);
        document.getElementById('roomNameJoin').value = '';
    } catch (err) {
        console.error('Error al unirse a sala:', err);
        alert('Error al unirse a sala: ' + err.message);
    }
});

// Inicializar cuando carga la página
window.addEventListener('load', function () {
    initializeConnection();
});

// Limpiar conexión al salir
window.addEventListener('beforeunload', function () {
    if (connection) {
        connection.stop();
    }
});
