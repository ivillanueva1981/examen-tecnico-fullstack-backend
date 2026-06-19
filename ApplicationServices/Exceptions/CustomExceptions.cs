using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationServices.Exceptions
{
    // Excepción Base
    public abstract class CustomException : Exception
    {
        protected CustomException(string message) : base(message) { }
    }

    // 400 - Validación
    public class BadRequestException : CustomException
    {
        public BadRequestException(string message) : base(message) { }
    }

    // 404 - No Encontrado
    public class NotFoundException : CustomException
    {
        public NotFoundException(string message) : base(message) { }
    }

    // 409 - Conflicto
    public class ConflictException : CustomException
    {
        public ConflictException(string message) : base(message) { }
    }
}
