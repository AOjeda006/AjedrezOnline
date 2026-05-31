namespace ServidorAjedrez.Domain.Exceptions
{
    /// <summary>
    /// Excepción base de los errores de regla de negocio del dominio de ajedrez.
    /// </summary>
    /// <remarks>
    /// Permite distinguir los fallos esperables del dominio (sala llena, partida inexistente,
    /// etc.) de los errores técnicos. La capa de presentación (el hub de SignalR
    /// <c>AjedrezHub</c>) captura los tipos derivados para responder al cliente con un mensaje
    /// controlado en lugar de propagar un fallo genérico.
    /// </remarks>
    public class DomainException : Exception
    {
        /// <summary>Crea la excepción con un mensaje descriptivo del fallo de dominio.</summary>
        /// <param name="message">Texto explicativo, apto para registrar o mostrar al usuario.</param>
        public DomainException(string message) : base(message) { }

        /// <summary>Crea la excepción conservando la causa subyacente.</summary>
        /// <param name="message">Texto explicativo del fallo de dominio.</param>
        /// <param name="innerException">Excepción original que provocó este fallo.</param>
        public DomainException(string message, Exception innerException) : base(message, innerException) { }
    }
}
