using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class DailyCollectionView : UserControl
{
    private readonly ApiClient _api;

    public DailyCollectionView(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        DatePickerBox.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await LoadAsync();
    }

    private static string Money(decimal amount) => "₹" + amount.ToString("N2", CultureInfo.InvariantCulture);

    private async Task LoadAsync()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            var date = DatePickerBox.SelectedDate?.ToString("yyyy-MM-dd") ?? "";
            var result = await _api.GetDailyCollectionAsync(date);

            NetCollectionText.Text = Money(result.NetCollection);
            TotalCollectedText.Text = Money(result.TotalCollections);
            TotalRefundedText.Text = Money(result.TotalRefunds);
            AdvanceReceivedText.Text = Money(result.AdvanceReceived);

            ByMethodPanel.Children.Clear();
            foreach (var c in result.Collections) AddMethodRow(c.TypeName, c.Total, isRefund: false);
            foreach (var rfd in result.Refunds) AddMethodRow(rfd.TypeName, rfd.Total, isRefund: true);
            if (result.Collections.Count == 0 && result.Refunds.Count == 0)
                ByMethodPanel.Children.Add(new TextBlock { Text = "No payments recorded for this date.", Foreground = (System.Windows.Media.Brush)FindResource("MutedBrush") });

            DetailsGrid.ItemsSource = result.Details;
        }
        catch (ApiException ex)
        {
            ErrorText.Text = $"Could not load daily collection: {ex.Message}";
            ErrorBanner.Visibility = Visibility.Visible;
        }
    }

    private void AddMethodRow(string typeName, decimal total, bool isRefund)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var label = new TextBlock { Text = isRefund ? $"{typeName} (refund)" : typeName, FontSize = 12.5 };
        var value = new TextBlock
        {
            Text = Money(total), FontWeight = FontWeights.SemiBold,
            Foreground = isRefund ? (System.Windows.Media.Brush)FindResource("ErrorBrush") : (System.Windows.Media.Brush)FindResource("TextBrush"),
        };
        Grid.SetColumn(label, 0);
        Grid.SetColumn(value, 1);
        row.Children.Add(label);
        row.Children.Add(value);
        ByMethodPanel.Children.Add(row);
    }

    private async void LoadButton_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void DatePickerBox_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        await LoadAsync();
    }
}
