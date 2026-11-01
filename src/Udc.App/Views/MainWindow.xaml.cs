using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class MainWindow : Window
{
    private enum CloseReason { UserQuit, LoggedOut, SettingsRequested }

    private readonly ApiClient _api;
    private CloseReason _closeReason = CloseReason.UserQuit;

    /// <summary>Fired when the user picks File &gt; Log out.</summary>
    public event Action? LoggedOut;

    /// <summary>Fired when the user picks File &gt; Settings...</summary>
    public event Action? SettingsRequested;

    /// <summary>Fired when this window closes for any other reason (Exit menu, Alt+F4, the X
    /// button) - the app should quit.</summary>
    public event Action? UserQuit;

    public MainWindow(ApiClient api, LoginResult login)
    {
        InitializeComponent();
        _api = api;
        Title = $"Urban Dry Clean CRM - {login.User.FullName} ({login.User.RoleName})";
        StatusBarText.Text = $"Signed in as {login.User.Username} - local server http://127.0.0.1:{App.CurrentConfig?.Port ?? 3100}";
        ShowDashboard();

        Closed += (_, _) =>
        {
            switch (_closeReason)
            {
                case CloseReason.LoggedOut: LoggedOut?.Invoke(); break;
                case CloseReason.SettingsRequested: SettingsRequested?.Invoke(); break;
                default: UserQuit?.Invoke(); break;
            }
        };
    }

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _closeReason = CloseReason.SettingsRequested;
        Close();
    }

    private void LogoutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _ = _api.LogoutAsync(); // best-effort; the session is gone client-side either way
        TokenStore.Clear();
        _closeReason = CloseReason.LoggedOut;
        Close();
    }

    private void OpenLogsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Paths.EnsureAppDataRoot();
            Process.Start(new ProcessStartInfo { FileName = Paths.LogDir, UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception) { /* nothing more we can do here */ }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e) => Close();

    private void RefreshMenuItem_Click(object sender, RoutedEventArgs e) => ShowDashboard();

    private void NavDashboard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => ShowDashboard();
    private void NavCustomers_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => ShowCustomers();
    private void NavOrders_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => ShowOrders();
    private void NavWorkshop_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => ShowWorkshop();
    private void NavBarcode_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => ShowBarcode();

    private void ShowDashboard()
    {
        SelectNav(NavDashboard);
        ContentArea.Content = new DashboardView(_api); // fresh instance each time - re-fetches on Loaded
    }

    private void ShowCustomers()
    {
        SelectNav(NavCustomers);
        ContentArea.Content = new CustomersView(_api);
    }

    private void ShowOrders()
    {
        SelectNav(NavOrders);
        ContentArea.Content = new OrdersView(_api);
    }

    private void ShowWorkshop()
    {
        SelectNav(NavWorkshop);
        ContentArea.Content = new WorkshopView(_api);
    }

    private void ShowBarcode()
    {
        SelectNav(NavBarcode);
        ContentArea.Content = new BarcodeLookupView(_api);
    }

    private void SelectNav(Border active)
    {
        foreach (var (border, text) in new[]
                 {
                     (NavDashboard, NavDashboardText), (NavCustomers, NavCustomersText), (NavOrders, NavOrdersText),
                     (NavWorkshop, NavWorkshopText), (NavBarcode, NavBarcodeText),
                 })
        {
            var isActive = border == active;
            border.Background = isActive ? new SolidColorBrush(Color.FromRgb(0x12, 0x32, 0x5E)) : Brushes.Transparent;
            text.Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x5E, 0x6E, 0x93));
        }
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        MessageBox.Show(
            $"Urban Dry Clean CRM\nVersion {version}\n\nLocal server: http://127.0.0.1:{App.CurrentConfig?.Port ?? 3100}",
            "About", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
