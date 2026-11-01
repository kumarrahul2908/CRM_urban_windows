using System.IO;
using System.Windows;
using Udc.App.Views;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App;

public partial class App : System.Windows.Application
{
    public static readonly BackendProcessManager Backend = new();
    public static AppConfig? CurrentConfig { get; private set; }
    public static ApiClient? Api { get; private set; }
    private static BackgroundJobRunner? _jobs;

    private static StreamWriter? _logWriter;

    public static void Log(string line)
    {
        try
        {
            if (_logWriter is null)
            {
                Paths.EnsureAppDataRoot();
                Directory.CreateDirectory(Paths.LogDir);
                _logWriter = new StreamWriter(Paths.LogFile, append: true) { AutoFlush = true };
            }
            _logWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {line}");
        }
        catch (IOException) { /* best-effort logging only */ }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        Paths.EnsureAppDataRoot();
        Log("app starting");

        var config = ConfigStore.Load();
        if (config is null)
        {
            ShowSetupWindow(existing: null);
            return;
        }
        await BootAsync(config);
    }

    private static void ShowSetupWindow(AppConfig? existing)
    {
        var window = new SetupWindow(existing);
        window.Saved += async savedConfig => await BootAsync(savedConfig);
        window.Cancelled += () =>
        {
            if (existing is not null) _ = BootAsync(existing); // Settings reopened then cancelled: resume what was already running
            else Current.Shutdown(); // first run, nothing to fall back to
        };
        window.Show();
    }

    public static async Task BootAsync(AppConfig config)
    {
        CurrentConfig = config;
        var splash = new SplashWindow();
        splash.Show();
        splash.SetStatus("Starting local server...");
        Log($"[backend] starting on port {config.Port}");

        Backend.Start(config, Log);
        var healthy = await Backend.WaitForHealthAsync(config.Port, TimeSpan.FromSeconds(30));
        if (!healthy)
        {
            splash.SetStatus("Still waiting for the database...");
            healthy = await Backend.WaitForHealthAsync(config.Port, TimeSpan.FromSeconds(20));
        }

        if (!healthy)
        {
            splash.Close();
            Backend.Stop();
            var choice = MessageBox.Show(
                "Urban Dry Clean CRM could not reach its local server.\n\n" +
                "This usually means the database settings are wrong, or the database server is unreachable.\n" +
                $"Check the log at {Paths.LogFile} for details.\n\n" +
                "Click OK to reopen Setup, or Cancel to quit.",
                "Could not start", MessageBoxButton.OKCancel, MessageBoxImage.Error);
            if (choice == MessageBoxResult.OK) ShowSetupWindow(config);
            else Current.Shutdown();
            return;
        }

        Api = new ApiClient($"http://127.0.0.1:{config.Port}");

        _jobs?.Stop();
        _jobs = new BackgroundJobRunner(config, Log);
        _jobs.Start();

        var savedToken = TokenStore.Load();
        if (!string.IsNullOrEmpty(savedToken))
        {
            splash.SetStatus("Restoring your session...");
            Api.BearerToken = savedToken;
            try
            {
                var me = await Api.GetMeAsync();
                splash.Close();
                ShowMainWindow(SessionRestore.FromMe(savedToken, me));
                return;
            }
            catch (ApiException)
            {
                // expired/revoked token (or a first run against a different database) - fall
                // through to a normal login, same as if nothing had been saved at all
                TokenStore.Clear();
                Api.BearerToken = null;
            }
        }

        splash.Close();
        ShowLoginWindow();
    }

    private static void ShowMainWindow(LoginResult loginResult)
    {
        var main = new MainWindow(Api!, loginResult);
        main.LoggedOut += ShowLoginWindow;
        main.SettingsRequested += () =>
        {
            _jobs?.Stop();
            Backend.Stop();
            ShowSetupWindow(CurrentConfig);
        };
        main.UserQuit += () => Current.Shutdown();
        Current.MainWindow = main;
        main.Show();
    }

    private static void ShowLoginWindow()
    {
        var login = new LoginWindow(Api!);
        login.LoggedIn += ShowMainWindow;
        login.Cancelled += () => Current.Shutdown();
        login.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _jobs?.Stop();
        Backend.Stop();
        _logWriter?.Dispose();
        base.OnExit(e);
    }
}
