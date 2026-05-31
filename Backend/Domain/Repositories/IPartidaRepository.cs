using ServidorAjedrez.Domain.Entities;

namespace ServidorAjedrez.Domain.Repositories
{
    /// <summary>
    /// Almacén de partidas: persiste y recupera las <see cref="Partida"/> gestionadas por el servidor.
    /// </summary>
    public interface IPartidaRepository
    {
        /// <summary>Guarda una partida nueva.</summary>
        /// <param name="partida">Partida a almacenar.</param>
        /// <returns>La partida almacenada.</returns>
        Task<Partida> CrearAsync(Partida partida);

        /// <summary>Recupera una partida por su identificador.</summary>
        /// <param name="partidaId">Id de la partida.</param>
        /// <returns>La partida, o <c>null</c> si no existe.</returns>
        Task<Partida?> ObtenerPorIdAsync(string partidaId);

        /// <summary>Sustituye el estado almacenado de una partida por el de la instancia dada.</summary>
        /// <param name="partida">Partida con el estado actualizado.</param>
        /// <returns>La partida actualizada.</returns>
        Task<Partida> ActualizarAsync(Partida partida);

        /// <summary>Elimina una partida del almacén; no hace nada si no existe.</summary>
        /// <param name="partidaId">Id de la partida a eliminar.</param>
        Task EliminarAsync(string partidaId);

        /// <summary>Devuelve todas las partidas almacenadas.</summary>
        /// <returns>Lista (posiblemente vacía) con todas las partidas.</returns>
        Task<List<Partida>> ObtenerTodasAsync();
    }
}
