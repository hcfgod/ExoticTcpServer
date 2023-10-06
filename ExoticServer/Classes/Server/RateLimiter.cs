using System;
using System.Collections.Concurrent;

namespace ExoticServer.Classes.Server
{
    public class RateLimiter
    {
        // Rate Limiting
        private readonly ConcurrentDictionary<string, int> rateLimits = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, DateTime> lastRequestTimes = new ConcurrentDictionary<string, DateTime>();

        private const int MaxRequestsPerMinute = 0; // Set your limit here

        private const int RateLimitDurationSeconds = 60;

        public bool IsRateLimited(string clientId)
        {
            if (!rateLimits.ContainsKey(clientId))
            {
                rateLimits.TryAdd(clientId, 0);
                lastRequestTimes.TryAdd(clientId, DateTime.Now);
            }

            // Check if the time window has passed and reset
            if ((DateTime.Now - lastRequestTimes[clientId]).TotalMinutes >= 1)
            {
                rateLimits[clientId] = 0;
                lastRequestTimes[clientId] = DateTime.Now;
            }

            // Check rate limit
            if (rateLimits[clientId] >= MaxRequestsPerMinute)
            {
                return true;
            }

            // Increment the rate limit counter
            rateLimits[clientId]++;
            return false;
        }

        public TimeSpan GetTimeUntilNextRequest(string clientId)
        {
            if (lastRequestTimes.TryGetValue(clientId, out DateTime lastRequestTime))
            {
                var timeSinceLastRequest = DateTime.Now - lastRequestTime;
                return TimeSpan.FromSeconds(RateLimitDurationSeconds) - timeSinceLastRequest;
            }

            return TimeSpan.Zero;
        }
    }
}
