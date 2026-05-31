// identificacion.js
document.getElementById('continueBtn').addEventListener('click', function () {
    const playerName = document.getElementById('playerName').value.trim();
    
    if (!playerName) {
        alert('Por favor ingresa tu nombre');
        return;
    }
    
    // Guardar en sessionStorage y redirigir
    sessionStorage.setItem('playerName', playerName);
    window.location.href = '/MenuPrincipal';
});

// Permitir Enter para continuar
document.getElementById('playerName').addEventListener('keypress', function (e) {
    if (e.key === 'Enter') {
        document.getElementById('continueBtn').click();
    }
});
