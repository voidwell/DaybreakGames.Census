using System;

namespace DaybreakGames.Census.Exceptions
{
    public class CensusException : Exception
    {
        public CensusException()
        {
        }

        public CensusException(string message) : base(message)
        {
        }

        public CensusException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
