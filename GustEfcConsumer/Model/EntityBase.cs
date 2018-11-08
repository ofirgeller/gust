using NodaTime;
using System;

namespace GustEfcConsumer.Model
{
    /// <summary>
    /// Here to see that inheritance does not break the code
    /// </summary>
    public class EntityBase
    {
        public Instant CreatedAt { get; set; } = Instant.FromUtc(2000, 10, 8, 6, 4);
    }
}