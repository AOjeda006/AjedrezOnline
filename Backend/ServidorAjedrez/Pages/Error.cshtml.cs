using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServidorAjedrez.Pages
{
    /// <summary>
    /// Modelo de la página de error, que muestra el identificador de la petición fallida para
    /// facilitar el diagnóstico.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        /// <summary>Identificador de la petición que falló (de la actividad actual o del trace de HTTP).</summary>
        public string? RequestId { get; set; }

        /// <summary>Indica si hay un <see cref="RequestId"/> que mostrar.</summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        /// <summary>Captura el identificador de la petición actual para mostrarlo en la página de error.</summary>
        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }

}
