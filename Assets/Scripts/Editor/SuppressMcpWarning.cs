using UnityEngine;

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
static class SuppressMcpWarning
{
    private class FilteredLogHandler : ILogHandler
    {
        private readonly ILogHandler _default;
        public FilteredLogHandler(ILogHandler defaultHandler) => _default = defaultHandler;

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            if (logType == LogType.Warning)
            {
                string msg = args.Length > 0 ? string.Format(format, args) : format;
                if (msg.Contains("WebSocket is not initialised"))
                    return;
            }
            _default.LogFormat(logType, context, format, args);
        }

        public void LogException(System.Exception exception, Object context)
        {
            _default.LogException(exception, context);
        }
    }

    static SuppressMcpWarning()
    {
        Debug.unityLogger.logHandler = new FilteredLogHandler(Debug.unityLogger.logHandler);
    }
}
#endif
