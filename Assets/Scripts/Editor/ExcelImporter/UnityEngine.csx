#r "System.Console"

public class UnityEngine
{
    public class Application
    {
        public static string dataPath => GetCallerDirectory() + "/../../..";
    }

    public class Debug
    {
        public static void Log(string msg) => Console.WriteLine(msg);
        public static void LogError(string msg) => Console.Error.WriteLine(msg);
        public static void LogError(Exception e) => Console.Error.WriteLine(e.StackTrace);
    }
}

public static string GetCallerDirectory([System.Runtime.CompilerServices.CallerFilePath] string fileName = null) => Path.GetDirectoryName(fileName);