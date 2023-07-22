namespace Hikaria.ReverseFriendlyFire.Utils
{
    internal static class Logs
    {
        public static void LogDebug(object data)
        {
            EntryPoint.Instance.Log.LogDebug(data);
        }

        public static void LogError(object data)
        {
            EntryPoint.Instance.Log.LogError(data);
        }

        public static void LogFatal(object data)
        {
            EntryPoint.Instance.Log.LogFatal(data);
        }

        public static void LogInfo(object data)
        {
            EntryPoint.Instance.Log.LogInfo(data);
        }

        public static void LogMessage(object data)
        {
            EntryPoint.Instance.Log.LogMessage(data);
        }

        public static void LogWarning(object data)
        {
            EntryPoint.Instance.Log.LogWarning(data);
        }
    }
}
