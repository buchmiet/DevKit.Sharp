using HostEventLauncher.Sharp;

var bootArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
using var boot = Startup.Open(ref bootArgs);

boot.BeginProgress(4);
boot.Write("Console host process started");
await Task.Delay(150);
boot.CompleteStep("Configuration loaded");
await Task.Delay(150);
boot.CompleteStep("Services registered");
await Task.Delay(150);
boot.CompleteStep("Background workers started");
await Task.Delay(150);
boot.Write("Console host ready");
boot.CompleteStep("Startup complete");
boot.Close();
