using System;

namespace DSA.Domain.Exceptions;

public sealed class DocumentoInvalidoException : Exception
{
    public DocumentoInvalidoException(string message) : base(message) { }
    
    public DocumentoInvalidoException(string message, Exception inner) 
        : base(message, inner) { }
}
