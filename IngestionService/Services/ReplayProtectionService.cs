using System.Collections.Concurrent;

namespace IngestionService.Services
{
    public class ReplayProtectionService
    {
        private readonly ConcurrentDictionary<string, long> _lastMessageIds = new();

        public bool Accept(string sensorId, long messageId)
        {
            if (messageId == 1)
            {
                _lastMessageIds[sensorId] = 1;
                return true;
            }

            return _lastMessageIds.AddOrUpdate(
                sensorId,
                addValueFactory: _ => messageId,
                updateValueFactory: (_, lastId) =>
                {
                    if (messageId <= lastId)
                        return lastId;
                    return messageId;
                }) == messageId;
        }

        public IReadOnlyDictionary<string, long> GetState()
            => _lastMessageIds;
    }
}