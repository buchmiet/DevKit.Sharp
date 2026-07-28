using DevTools.HostLogging.Sharp;

#region Simulation constants

const int StartupStepCount = 4;
const int UiFrameworkDelayMilliseconds = 250;
const int DiContainerDelayMilliseconds = 350;
const int MainWindowDelayMilliseconds = 450;
const int ViewModelDelayMilliseconds = 200;

const string ProcessStartedMessage = "GUI host process started (no real window — boot simulation only)";
const string UiFrameworkInitializedMessage = "UI framework initialized";
const string DiContainerBuiltMessage = "DI container built";
const string MainWindowOpenedMessage = "Main window opened";
const string ViewModelReadyMessage = "Main view model ready";

#endregion

var bootArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
using var boot = HostLog.Open(ref bootArgs);

boot.BeginProgress(StartupStepCount);
boot.Write(ProcessStartedMessage);
await Task.Delay(UiFrameworkDelayMilliseconds);
boot.CompleteStep(UiFrameworkInitializedMessage);
await Task.Delay(DiContainerDelayMilliseconds);
boot.CompleteStep(DiContainerBuiltMessage);
await Task.Delay(MainWindowDelayMilliseconds);
boot.CompleteStep(MainWindowOpenedMessage);
await Task.Delay(ViewModelDelayMilliseconds);
boot.CompleteStep(ViewModelReadyMessage);
boot.Close();
