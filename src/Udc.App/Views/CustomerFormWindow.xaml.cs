using System.Windows;
using System.Windows.Controls;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class CustomerFormWindow : Window
{
    private readonly ApiClient _api;

    public CustomerFormWindow(ApiClient api)
    {
        InitializeComponent();
        _api = api;
    }

    private static string ComboText(ComboBox box) => (box.SelectedItem as ComboBoxItem)?.Content as string ?? "";

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var fullName = FullNameBox.Text.Trim();
        var mobile = MobileBox.Text.Trim();
        if (fullName.Length == 0 || mobile.Length == 0)
        {
            ShowError("Full name and mobile number are required.");
            return;
        }
        if (!decimal.TryParse(DiscountBox.Text, out var discount)) discount = 0;

        var request = new CreateCustomerRequest
        {
            Title = ComboText(TitleBox),
            FullName = fullName,
            Mobile = mobile,
            AltMobile = AltMobileBox.Text.Trim(),
            Email = EmailBox.Text.Trim(),
            Address = AddressBox.Text.Trim(),
            Area = AreaBox.Text.Trim(),
            City = CityBox.Text.Trim(),
            Pincode = PincodeBox.Text.Trim(),
            DiscountPercent = discount,
            CommunicationPreference = ComboText(CommunicationBox),
            HomeDelivery = HomeDeliveryBox.IsChecked == true,
            Remarks = RemarksBox.Text.Trim(),
        };

        SaveButton.IsEnabled = false;
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            await _api.CreateCustomerAsync(request);
            DialogResult = true;
            Close();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBanner.Visibility = Visibility.Visible;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
