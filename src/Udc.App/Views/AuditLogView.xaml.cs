using System.Windows;
using System.Windows.Controls;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class AuditLogView : UserControl
{
    private readonly ApiClient _api;
    private int _page = 1;
    private int _totalPages = 1;
    private bool _optionsLoaded;

    public AuditLogView(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        DetailCard.Visibility = Visibility.Collapsed;
        try
        {
            var module = (ModuleBox.SelectedItem as string) ?? "";
            var action = (ActionBox.SelectedItem as string) ?? "";
            var from = FromDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? "";
            var to = ToDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? "";

            var result = await _api.GetAuditLogsAsync(module, action, from, to, _page);
            _totalPages = Math.Max(1, (int)Math.Ceiling(result.Total / (double)Math.Max(1, result.PerPage)));
            EntriesGrid.ItemsSource = result.Entries;
            SummaryText.Text = $"Page {result.Page} of {_totalPages} - {result.Total} entry/entries";

            if (!_optionsLoaded && result.Options is not null)
            {
                _optionsLoaded = true;
                var modules = new List<string> { "" };
                modules.AddRange(result.Options.Modules);
                var actions = new List<string> { "" };
                actions.AddRange(result.Options.Actions);
                ModuleBox.ItemsSource = modules;
                ActionBox.ItemsSource = actions;
                ModuleBox.SelectedIndex = 0;
                ActionBox.SelectedIndex = 0;
            }
        }
        catch (ApiException ex)
        {
            ErrorText.Text = $"Could not load the audit log: {ex.Message}";
            ErrorBanner.Visibility = Visibility.Visible;
        }
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        _page = 1;
        await LoadAsync();
    }

    private async void PrevButton_Click(object sender, RoutedEventArgs e)
    {
        if (_page <= 1) return;
        _page--;
        await LoadAsync();
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_page >= _totalPages) return;
        _page++;
        await LoadAsync();
    }

    private void EntriesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EntriesGrid.SelectedItem is not AuditLogEntry entry)
        {
            DetailCard.Visibility = Visibility.Collapsed;
            return;
        }
        OldValueText.Text = entry.OldValue ?? "(none)";
        NewValueText.Text = entry.NewValue ?? "(none)";
        DetailCard.Visibility = Visibility.Visible;
    }
}
