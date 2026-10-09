using System.Diagnostics;
using System.Text;

namespace Tests.Infrastructure;

/// <summary>
/// Внешний процесс для E2E (WebApi, сборка и хостинг фронтенда): собирает вывод для диагностики
/// и завершается вместе с дочерними процессами на dispose.
/// </summary>
public sealed class TestProcess : IDisposable
{
    private readonly Process process;
    private readonly StringBuilder output = new();
    private readonly string commandLine;

    private TestProcess(Process process, string commandLine)
    {
        this.process = process;
        this.commandLine = commandLine;
        process.OutputDataReceived += (_, e) => Append(e.Data);
        process.ErrorDataReceived += (_, e) => Append(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    public bool HasExited => process.HasExited;

    public string Output
    {
        get
        {
            lock (output)
            {
                return output.ToString();
            }
        }
    }

    public static TestProcess Start(
        string fileName,
        string arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var (key, value) in environment ?? new Dictionary<string, string>())
        {
            startInfo.Environment[key] = value;
        }

        var commandLine = $"{fileName} {arguments}";
        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Не удалось запустить: {commandLine}");
        return new TestProcess(process, commandLine);
    }

    /// <summary>Запускает команду и ждёт успешного завершения; иначе — исключение с выводом команды.</summary>
    public static async Task RunAsync(string fileName, string arguments, string workingDirectory, TimeSpan timeout)
    {
        using var testProcess = Start(fileName, arguments, workingDirectory);
        using var cancellation = new CancellationTokenSource(timeout);

        try
        {
            await testProcess.process.WaitForExitAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Таймаут {timeout}: {testProcess.commandLine}{Environment.NewLine}{testProcess.Output}");
        }

        if (testProcess.process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Команда завершилась с кодом {testProcess.process.ExitCode}: {testProcess.commandLine}{Environment.NewLine}{testProcess.Output}");
        }
    }

    public void Dispose()
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(10_000);
            }
        }
        catch
        {
            // ignore shutdown races
        }

        process.Dispose();
    }

    private void Append(string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (output)
        {
            output.AppendLine(line);
        }
    }
}
