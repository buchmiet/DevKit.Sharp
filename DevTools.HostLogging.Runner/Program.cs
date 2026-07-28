using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using DevTools.HostLogging.Sharp;

#region Runner constants

const string ReadyMessage = "DevTools.HostLogging.Runner ready.";
const string StartupCompleteMessage = "Startup complete, closing console.";
const string ClientNotFoundMessage = "Client executable not found. Pass the path as the first argument or place it in binaries/client.exe.";
const string AttachWaitFormat = "waiting for attach on '{0}'.";
const string StartingClientMessage = "starting client...";
const string ClientStartFailedMessage = "Client process could not be started.";
const string WaitingForAttachMessage = "waiting for client attach...";
const string AttachFailedFormat = "attach failed: {0}";
const string ClientAttachedMessage = "client attached.";
const string UnrecognizedPayloadFormat = "unrecognized payload: {0}";
const string ClientClosedSessionMessage = "client closed session.";
const string AttachChannelReadErrorFormat = "error while reading attach channel: {0}";
const string RunnerExitingMessage = "runner exiting.";
const string AttachChannelClosedExitedFormat = "attach channel closed; client exited with code {0}.";
const string AttachChannelClosedRunningMessage = "attach channel closed; client is still running.";

const string AttachNamePrefix = "devtools-hostlog-";
const string AttachNameGuidFormat = "N";
const string ClientBinariesFolder = "binaries";
const string ClientExecutableName = "client.exe";

const int SuccessExitCode = 0;
const int ClientStartFailureExitCode = 1;
const int AttachReadFailureExitCode = -1;
const int MinimumProgressSteps = 1;
const int AttachPipeMaxInstances = 1;

#endregion

using var view = new StartupConsoleView();
using var shutdownCts = new CancellationTokenSource();
view.OnProgressComplete += () =>
{
    WriteLine(StartupCompleteMessage);
    shutdownCts.Cancel();
};
WriteLine(ReadyMessage);

var clientPath = ResolveClientPath(args);
if (clientPath is null)
{
    WriteLine(ClientNotFoundMessage);
    return ClientStartFailureExitCode;
}

var clientDirectory = Path.GetDirectoryName(clientPath)!;
var attachName = AttachNamePrefix + Environment.ProcessId + "-" + Guid.NewGuid().ToString(AttachNameGuidFormat);
WriteLine(string.Format(AttachWaitFormat, attachName));

await using var attachChannel = new NamedPipeServerStream(
    attachName,
    PipeDirection.In,
    AttachPipeMaxInstances,
    PipeTransmissionMode.Byte,
    PipeOptions.Asynchronous);

var startInfo = CreateClientStartInfo(clientPath, clientDirectory, attachName);

WriteLine(StartingClientMessage);
using var clientProcess = Process.Start(startInfo);
if (clientProcess is null)
{
    WriteLine(ClientStartFailedMessage);
    return ClientStartFailureExitCode;
}

WriteLine(WaitingForAttachMessage);
view.StartSpinner();

try
{
    await attachChannel.WaitForConnectionAsync();
}
catch (Exception ex)
{
    view.StopSpinner();
    WriteLine(string.Format(AttachFailedFormat, ex.Message));
    return ClientStartFailureExitCode;
}

view.StopSpinner();
WriteLine(ClientAttachedMessage);

using var reader = new StreamReader(attachChannel, Encoding.UTF8, leaveOpen: true);
try
{
    while (true)
    {
        view.StartSpinner();
        var line = await reader.ReadLineAsync(shutdownCts.Token);
        view.StopSpinner();

        if (line is null)
        {
            WriteLine(DescribeDetach(clientProcess));
            break;
        }

        if (!StartupWireMessage.TryParse(line, out var message))
        {
            WriteLine(string.Format(UnrecognizedPayloadFormat, line));
            continue;
        }

        if (message.Kind.Equals(StartupWireMessage.ControlKind, StringComparison.OrdinalIgnoreCase) &&
            message.Payload.Equals(RemoteStartupSession.CloseControlCommand, StringComparison.OrdinalIgnoreCase))
        {
            WriteLine(ClientClosedSessionMessage);
            break;
        }

        if (message.Kind.Equals(StartupWireMessage.ProgressKind, StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(message.Payload, out var totalSteps) && totalSteps >= MinimumProgressSteps)
            {
                view.SetProgressTotal(totalSteps);
            }
            else if (message.Payload.Equals(StartupWireMessage.StepPayload, StringComparison.OrdinalIgnoreCase))
            {
                view.AdvanceStep();
            }

            continue;
        }

        if (message.Kind.Equals(StartupWireMessage.LogKind, StringComparison.OrdinalIgnoreCase))
        {
            WriteLine(message.Payload);
        }
    }
}
catch (OperationCanceledException)
{
    view.StopSpinner();
    return SuccessExitCode;
}
catch (Exception ex)
{
    view.StopSpinner();
    WriteLine(string.Format(AttachChannelReadErrorFormat, ex.Message));
    return AttachReadFailureExitCode;
}

WriteLine(RunnerExitingMessage);
return SuccessExitCode;

void WriteLine(string text) => view.WriteLine(text);

static string DescribeDetach(Process clientProcess)
{
    if (clientProcess.HasExited)
    {
        return string.Format(AttachChannelClosedExitedFormat, clientProcess.ExitCode);
    }

    return AttachChannelClosedRunningMessage;
}

static string? ResolveClientPath(string[] args)
{
    if (args.Length > 0 && File.Exists(args[0]))
    {
        return Path.GetFullPath(args[0]);
    }

    foreach (var directory in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory }.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        var candidate = Path.Combine(directory, ClientBinariesFolder, ClientExecutableName);
        if (File.Exists(candidate))
        {
            return candidate;
        }
    }

    return null;
}

static ProcessStartInfo CreateClientStartInfo(string clientPath, string clientDirectory, string attachName)
{
    return new ProcessStartInfo(clientPath)
    {
        UseShellExecute = false,
        WorkingDirectory = clientDirectory,
        Environment =
        {
            [HostLog.RemoteAttachVariable] = attachName
        }
    };
}
