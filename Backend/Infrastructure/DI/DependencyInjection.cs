using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServidorAjedrez.Domain.Interfaces;
using ServidorAjedrez.Domain.UseCases;
using ServidorAjedrez.Domain.Repositories;
using ServidorAjedrez.Infrastructure.Persistence;
using ServidorAjedrez.Infrastructure.SignalR;

namespace ServidorAjedrez.Infrastructure.DI
{
    /// <summary>
    /// Registro de los servicios de infraestructura en el contenedor de inyección de dependencias.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registra las implementaciones de infraestructura en el contenedor.
        /// </summary>
        /// <remarks>
        /// Los repositorios y el gestor de conexiones se registran como <c>Singleton</c> (mantienen
        /// el estado compartido en memoria de toda la aplicación), mientras que los casos de uso se
        /// registran como <c>Scoped</c>.
        /// </remarks>
        /// <param name="services">Colección de servicios sobre la que se registran las dependencias.</param>
        /// <param name="configuration">Configuración de la aplicación (disponible para futuros ajustes; actualmente no se usa).</param>
        /// <returns>La propia <paramref name="services"/>, para poder encadenar llamadas.</returns>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Repositorios - Singleton
            services.AddSingleton<ISalaRepository, InMemorySalaRepository>();
            services.AddSingleton<IPartidaRepository, InMemoryPartidaRepository>();

            // Connection Manager - Singleton
            services.AddSingleton<IConnectionManager, ConnectionManager>();

            // UseCases - Scoped
            services.AddScoped<ICrearSalaUseCase, CrearSalaUseCase>();
            services.AddScoped<IUnirseSalaUseCase, UnirseSalaUseCase>();
            services.AddScoped<IRealizarMovimientoUseCase, RealizarMovimientoUseCase>();
            services.AddScoped<IConfirmarMovimientoUseCase, ConfirmarMovimientoUseCase>();
            services.AddScoped<IDeshacerMovimientoUseCase, DeshacerMovimientoUseCase>();
            services.AddScoped<ISolicitarTablasUseCase, SolicitarTablasUseCase>();
            services.AddScoped<IRetirarTablasUseCase, RetirarTablasUseCase>();
            services.AddScoped<IRendirseUseCase, RendirseUseCase>();
            services.AddScoped<IPromocionarPeonUseCase, PromocionarPeonUseCase>();
            services.AddScoped<ISolicitarReinicioUseCase, SolicitarReinicioUseCase>();
            services.AddScoped<IRetirarReinicioUseCase, RetirarReinicioUseCase>();
            services.AddScoped<IAbandonarSalaUseCase, AbandonarSalaUseCase>();

            return services;
        }
    }
}
