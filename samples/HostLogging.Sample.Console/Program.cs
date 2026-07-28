using DevTools.HostLogging.Sharp;

#region Simulation constants

const int StartupStepCount = 4;
const int StepDelayMilliseconds = 150;

const string ProcessStartedMessage = "Console host process started";
const string ConfigurationLoadedMessage = "Configuration loaded";
const string ServicesRegisteredMessage = "Services registered";
const string BackgroundWorkersStartedMessage = "Background workers started";
const string ProcessReadyMessage = "Console host ready";
const string StartupCompleteMessage = "Startup complete";

#endregion

var bootArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
using var boot = HostLog.Open(ref bootArgs);

boot.BeginProgress(StartupStepCount);
boot.Write(ProcessStartedMessage);
await Task.Delay(StepDelayMilliseconds);
boot.CompleteStep(ConfigurationLoadedMessage);
await Task.Delay(StepDelayMilliseconds);
boot.CompleteStep(ServicesRegisteredMessage);
await Task.Delay(StepDelayMilliseconds);
boot.CompleteStep(BackgroundWorkersStartedMessage);
await Task.Delay(StepDelayMilliseconds);
boot.Write(ProcessReadyMessage);
boot.CompleteStep(StartupCompleteMessage);
boot.Close();
