using System.Collections.Concurrent;

namespace Ring_a_ding_ding
{
    public class SignalRLoggerProvider : ILoggerProvider
    {
        private readonly SignalRLoggerConfiguration _config;
        private readonly ConcurrentDictionary<string, SignalRLogger> _loggers = new ConcurrentDictionary<string, SignalRLogger>();

        public SignalRLoggerProvider(SignalRLoggerConfiguration signalRLoggerConfiguration)
        {
            _config = signalRLoggerConfiguration;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new SignalRLogger(name, _config));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    public static class SignalRLoggerProviderExtensions
    {
        public static ILoggingBuilder AddSignalRLogger(this ILoggingBuilder builder)
        {
            builder.Services.AddSingleton<ILoggerProvider, SignalRLoggerProvider>();
            return builder;
        }
    }

}
