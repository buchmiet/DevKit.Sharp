using DevTools.HostLogging.Sharp;

var bootArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
using var boot = HostLog.Open(ref bootArgs);

boot.BeginProgress(4);
boot.Write("GUI host process started (no real window — boot simulation only)");
await Task.Delay(250);
boot.CompleteStep("UI framework initialized");
await Task.Delay(350);
boot.CompleteStep("DI container built");
await Task.Delay(450);
boot.CompleteStep("Main window opened");
await Task.Delay(200);
boot.CompleteStep("Main view model ready");
boot.Close();
