using System;

namespace Akyildiz.Sevkiyat.Domain.Exceptions
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message)
        {
        }
    }
}
