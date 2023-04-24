using System.IO;
using UnityEngine;

public class FileLogHandler : ILogHandler
{
    private StreamWriter writer;

    public FileLogHandler(string path)
    {
        writer = new StreamWriter(path, true);
    }

    public void LogFormat(LogType logType, Object context, string format, params object[] args)
    {
        writer.WriteLine("[{0}] ({1}) {2}", logType, context == null ? "NoContext" : context.name, string.Format(format, args));
        writer.Flush();
    }

    public void LogException(System.Exception exception, Object context)
    {
        writer.WriteLine("[Exception] ({0}) {1}\n{2}", context == null ? "NoContext" : context.name, exception.Message, exception.StackTrace);
        writer.Flush();
    }

    public void Close()
    {
        writer.Close();
    }
}