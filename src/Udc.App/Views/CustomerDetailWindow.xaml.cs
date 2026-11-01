using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Udc.Client;

namespace Udc.App.Views;

public partial class CustomerDetailWindow : Window
{
    private readonly ApiClient _api;
    private readonly int _customerId;

    public CustomerDetailWindow(ApiClient api, int customerId)
    {
        InitializeComponent();
        _api = api;
        _customerId = customerId;
        Loaded += async (_, _) => await LoadAsync();
    }

    private static string Money(decimal amount) => "₹" + amount.ToString("N2", CultureInfo.InvariantCulture);

    private async Task LoadAsync()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            var result = await _api.GetCustomerDetailAsync(_customerId);
            var c = result.Customer;
            TitleText.Text = $"{c.Title} {c.FullName}".Trim();
            CodeText.Text = c.CustomerCode;
            WalletText.Text = Money(c.WalletBalance);
            OutstandingText.Text = Money(c.OutstandingBalance);

            FieldsPanel.Children.Clear();
            AddField("Mobile", c.Mobile);
            AddField("Alt. mobile", c.AltMobile);
            AddField("Email", c.Email);
            AddField("Address", string.Join(", ", new[] { c.Address, c.Area, c.City, c.State, c.Pincode }.Where(s => !string.IsNullOrWhiteSpace(s))));
            AddField("Price list", c.PriceListName);
            AddField("Discount", c.DiscountPercent > 0 ? $"{c.DiscountPercent}%" : null);
            AddField("Home delivery", c.HomeDelivery ? "Yes" : "No");
            AddField("Communication", c.CommunicationPreference);
            AddField("GSTIN", c.Gstin);
            AddField("Membership ID", c.MembershipId);
            AddField("Registration source", c.RegistrationSource);
            AddField("Status", c.IsActive ? "Active" : "Inactive");
        }
        catch (ApiException ex)
        {
            ErrorText.Text = $"Could not load this customer: {ex.Message}";
            ErrorBanner.Visibility = Visibility.Visible;
        }
    }

    private void AddField(string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var labelText = new TextBlock { Text = label, FontSize = 12.5, Foreground = (System.Windows.Media.Brush)FindResource("MutedBrush") };
        var valueText = new TextBlock { Text = value, FontSize = 13, TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(labelText, 0);
        Grid.SetColumn(valueText, 1);
        row.Children.Add(labelText);
        row.Children.Add(valueText);
        FieldsPanel.Children.Add(row);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
