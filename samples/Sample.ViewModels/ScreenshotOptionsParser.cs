namespace Sample.ViewModels;

public static class ScreenshotOptionsParser
{
    public static ScreenshotOptions Parse(string[] args)
    {
        string? outputPath = null;
        var exitAfterCapture = false;
        var delayMs = 150;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--screenshot" when i + 1 < args.Length:
                    outputPath = args[++i];
                    break;
                case "--exit":
                    exitAfterCapture = true;
                    break;
                case "--delay" when i + 1 < args.Length && int.TryParse(args[++i], out var delay):
                    delayMs = delay;
                    break;
            }
        }

        return new ScreenshotOptions
        {
            OutputPath = outputPath,
            ExitAfterCapture = exitAfterCapture,
            DelayMs = delayMs,
        };
    }
}
