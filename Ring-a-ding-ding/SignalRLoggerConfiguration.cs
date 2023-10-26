using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Ring_a_ding_ding
{
    public class SignalRLoggerConfiguration
    {
        public IHubContext<BlazorChatSampleHub> HubContext { get; set; }
        public string GroupName { get; set; } = "LogMonitor";
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
        public int EventId { get; set; } = 0;

        public SignalRLoggerConfiguration(IHubContext<BlazorChatSampleHub> hubContext)
        {
            HubContext = hubContext;
        }
    }
}
