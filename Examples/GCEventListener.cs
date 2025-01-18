using Light.Transmit;
using System.Diagnostics.Tracing;

namespace Examples
{
    internal class GCEventListener : EventListener
    {
        private readonly ILogger<GCEventListener> logger = LoggerProvider.CreateLogger<GCEventListener>();


        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // Listen for.NET Runtime GC events
            if (eventSource.Name == "Microsoft-Windows-DotNETRuntime")
            {
                // Enable GC-related event listening (0x1 = GC-related event)
                EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)0x1);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventId == 13) //   eventData.EventName == "GCFinalizersEnd_V1"
            {
                logger.LogInformation("GCEvent GCFinalizersEnd_V1 Count = {0}", eventData.Payload[0]);
            }
        }
    }
}
