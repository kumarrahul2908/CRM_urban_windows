using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace Udc.Client;

/// <summary>
/// Stand-in for the server deploy's <c>deploy/crontab</c> - there's no system cron on a
/// shop-counter Windows PC, so while the app is open this runs the same jobs on its own timers,
/// spawning them under the same bundled portable Node runtime <see cref="BackendProcessManager"/>
/// runs the backend itself under. Mirrors the earlier Electron prototype's jobs.cjs.
///
/// A failing job is logged and skipped - it never takes the app down. <c>nightlyBackup.js</c>
/// shells out to <c>mysqldump</c>; if that isn't on this PC's PATH, the nightly backup will fail
/// every night, same pre-existing limitation the backend's own DatabaseBackupService has.
/// </summary>
public sealed class BackgroundJobRunner : IDisposable
{
    private readonly AppConfig _config;
    private readonly Action<string> _log;
    private readonly List<Timer> _timers = new();

    public BackgroundJobRunner(AppConfig config, Action<string> log)
    {
        _config = config;
        _log = log;
    }

    public void Start()
    {
        ScheduleEveryMinute("whatsappWorker.js", "whatsapp-worker");
        ScheduleDaily("photoCleanup.js", "photo-cleanup", 3, 15);
        ScheduleDaily("nightlyBackup.js", "nightly-backup", 2, 30);
        ScheduleDaily("whatsappMarketing.js", "whatsapp-marketing", 9, 30);
    }

    public void Stop()
    {
        foreach (var timer in _timers) timer.Dispose();
        _timers.Clear();
    }

    public void Dispose() => Stop();

    /// <summary>How long until the next occurrence of hour:minute on the local clock - today if
    /// that time hasn't passed yet, otherwise tomorrow. Pure/testable: takes "now" as a
    /// parameter instead of reading the clock itself.</summary>
    public static TimeSpan TimeUntil(DateTime now, int hour, int minute)
    {
        var next = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0, now.Kind);
        if (next <= now) next = next.AddDays(1);
        return next - now;
    }

    private void ScheduleEveryMinute(string script, string label)
    {
        RunJob(script, label); // once right away, then on the interval
        _timers.Add(new Timer(_ => RunJob(script, label), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)));
    }

    private void ScheduleDaily(string script, string label, int hour, int minute)
    {
        var due = TimeUntil(DateTime.Now, hour, minute);
        _timers.Add(new Timer(_ => RunJob(script, label), null, due, TimeSpan.FromDays(1)));
    }

    private void RunJob(string script, string label)
    {
        try
        {
            var scriptPath = Path.Combine(BackendProcessManager.BackendDir, "dist", "jobs", script);
            var psi = new ProcessStartInfo
            {
                FileName = BackendProcessManager.NodeExePath,
                WorkingDirectory = BackendProcessManager.BackendDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.ArgumentList.Add(scriptPath);
            foreach (var (key, value) in BackendProcessManager.BuildEnvironment(_config))
                psi.Environment[key] = value;

            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) _log($"[{label}] {e.Data}"); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) _log($"[{label}:err] {e.Data}"); };
            process.Exited += (_, _) =>
            {
                try { if (process.ExitCode != 0) _log($"[jobs] {label} exited with code {process.ExitCode}"); }
                finally { process.Dispose(); }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            _log($"[jobs] {label} failed to start: {ex.Message}");
        }
    }
}
