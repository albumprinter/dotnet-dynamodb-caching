using System;

namespace Albelli.Extensions.Caching.DynamoDb
{
    public interface ISystemClock
    {
        DateTimeOffset UtcNow { get; }
    }

    public sealed class DefaultSystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}