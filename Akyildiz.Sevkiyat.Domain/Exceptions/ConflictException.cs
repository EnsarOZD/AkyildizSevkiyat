using System;

namespace Akyildiz.Sevkiyat.Domain.Exceptions
{
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message)
        {
        }
    }
}
