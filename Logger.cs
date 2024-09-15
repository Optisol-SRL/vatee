using System;

namespace Vatee;

public class Logger
{
    private static readonly Lazy<bool> IsDebug =
        new(() => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VATEE_DEBUG_PATH")));

    public static void WriteLine(string line)
    {
        if (!IsDebug.Value)
        {
            return;
        }

        Console.WriteLine();
    }
}
