namespace CefDetector.Net
{
    /// <summary>
    /// 更加美观的输出
    /// </summary>
    public static class PrettyPrinter
    {
        public static void WriteMessage(ConsoleColor color, string title, string contentFmt, params object?[]? args)
        {
            var originalForeground = Console.ForegroundColor;
            var originalBackground = Console.BackgroundColor;

            Console.ForegroundColor = color;
            Console.Write("[{0}]\t", title);
            Console.ForegroundColor = originalForeground;

            Console.WriteLine(contentFmt, args);
        }

        public static void WriteInfo(string title, string contentFmt, params object?[]? args)
        {
            WriteMessage(ConsoleColor.Green, title, contentFmt, args);
        }

        public static void WriteWarning(string title, string contentFmt, params object?[]? args)
        {
            WriteMessage(ConsoleColor.Yellow, title, contentFmt, args);
        }

        public static void WriteError(string title, string contentFmt, params object?[]? args)
        {
            WriteMessage(ConsoleColor.Red, title, contentFmt, args);
        }
    }
}

