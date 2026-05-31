using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServidorAjedrez.Pages
{
    /// <summary>
    /// Modelo de la página de la partida; resuelve el identificador de la partida que se va a mostrar.
    /// </summary>
    public class PartidaModel : PageModel
    {
        /// <summary>
        /// Id de la partida a mostrar. Se enlaza desde la cadena de consulta (GET) y, si llega vacío,
        /// se recupera de la sesión en <see cref="OnGet"/>.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string PartidaId { get; set; } = "";

        /// <summary>Si no se recibió por la cadena de consulta, recupera el id de la partida de la sesión.</summary>
        public void OnGet()
        {
            // Obtener el ID de la partida de los parámetros de consulta o sesión
            if (string.IsNullOrEmpty(PartidaId))
            {
                PartidaId = HttpContext.Session.GetString("PartidaId") ?? "";
            }
        }
    }
}
