using ServidorAjedrez.Domain.Entities;

namespace ServidorAjedrez.Domain.Repositories
{
    /// <summary>
    /// Almacén de salas: persiste y recupera las <see cref="Sala"/> y permite localizarlas por nombre.
    /// </summary>
    public interface ISalaRepository
    {
        /// <summary>Guarda una sala nueva.</summary>
        /// <param name="sala">Sala a almacenar.</param>
        /// <returns>La sala almacenada.</returns>
        Task<Sala> CrearAsync(Sala sala);

        /// <summary>Recupera una sala por su identificador.</summary>
        /// <param name="salaId">Id de la sala.</param>
        /// <returns>La sala, o <c>null</c> si no existe.</returns>
        Task<Sala?> ObtenerPorIdAsync(string salaId);

        /// <summary>Busca una sala abierta (todavía sin oponente) por su nombre, para poder unirse a ella.</summary>
        /// <param name="nombre">Nombre de la sala.</param>
        /// <returns>La sala libre con ese nombre, o <c>null</c> si no hay ninguna disponible.</returns>
        Task<Sala?> ObtenerPorNombreAsync(string nombre);

        /// <summary>Sustituye el estado almacenado de una sala por el de la instancia dada.</summary>
        /// <param name="sala">Sala con el estado actualizado.</param>
        /// <returns>La sala actualizada.</returns>
        Task<Sala> ActualizarAsync(Sala sala);

        /// <summary>Elimina una sala del almacén; no hace nada si no existe.</summary>
        /// <param name="salaId">Id de la sala a eliminar.</param>
        Task EliminarAsync(string salaId);

        /// <summary>Devuelve todas las salas almacenadas.</summary>
        /// <returns>Lista (posiblemente vacía) con todas las salas.</returns>
        Task<List<Sala>> ObtenerTodasAsync();
    }
}
