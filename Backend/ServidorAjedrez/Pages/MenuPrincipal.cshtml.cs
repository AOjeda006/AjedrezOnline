using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServidorAjedrez.Pages
{
    /// <summary>
    /// Modelo de la página del menú principal, donde el jugador indica su nombre antes de crear o
    /// unirse a una sala.
    /// </summary>
    public class MenuPrincipalModel : PageModel
    {
        /// <summary>Nombre del jugador, enlazado con el formulario y precargado desde la sesión.</summary>
        [BindProperty]
        public string NombreJugador { get; set; } = "";

        /// <summary>Carga en <see cref="NombreJugador"/> el nombre guardado en la sesión, si existe.</summary>
        public void OnGet()
        {
            // Obtener nombre del jugador de sesión o parámetros
            NombreJugador = HttpContext.Session.GetString("NombreJugador") ?? "";
        }
    }
}
