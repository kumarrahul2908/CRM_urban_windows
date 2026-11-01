using System.Windows;
using System.Windows.Controls;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class CustomerFormWindow : Window
{
    private readonly ApiClient _api;
    private readonly int? _customerId;

    /// <summary>Fields the Edit form doesn't surface - carried through unchanged from the loaded
    /// record into the update payload (see CustomerEditDetail's doc comment). Unused in create
    /// mode, where there's nothing yet to preserve.</summary>
    private CustomerEditDetail? _existing;

    public CustomerFormWindow(ApiClient api) : this(api, null)
    {
    }

    public CustomerFormWindow(ApiClient api, int? customerId)
    {
        InitializeComponent();
        _api = api;
        _customerId = customerId;
        if (customerId is int id)
        {
            Title = "Edit Customer";
            HeaderText.Text = "Edit Customer";
            SaveButton.Content = "Save Changes";
            ActiveBox.Visibility = Visibility.Visible;
            Loaded += async (_, _) => await LoadExistingAsync(id);
        }
    }

    private async Task LoadExistingAsync(int id)
    {
        try
        {
            var result = await _api.GetCustomerEditDetailAsync(id);
            var c = result.Customer;
            _existing = c;

            SetComboText(TitleBox, c.Title);
            FullNameBox.Text = c.FullName;
            MobileBox.Text = c.Mobile ?? "";
            AltMobileBox.Text = c.AltMobile ?? "";
            EmailBox.Text = c.Email ?? "";
            AddressBox.Text = c.Address ?? "";
            AreaBox.Text = c.Area ?? "";
            CityBox.Text = c.City ?? "";
            PincodeBox.Text = c.Pincode ?? "";
            DiscountBox.Text = c.DiscountPercent.ToString();
            SetComboText(CommunicationBox, c.CommunicationPreference);
            HomeDeliveryBox.IsChecked = c.HomeDelivery;
            RemarksBox.Text = c.Remarks ?? "";
            ActiveBox.IsChecked = c.IsActive;
        }
        catch (ApiException ex)
        {
            ShowError($"Could not load this customer: {ex.Message}");
        }
    }

    private static string ComboText(ComboBox box) => (box.SelectedItem as ComboBoxItem)?.Content as string ?? "";

    private static void SetComboText(ComboBox box, string? value)
    {
        foreach (var item in box.Items)
        {
            if (item is ComboBoxItem { Content: string content } && string.Equals(content, value, StringComparison.OrdinalIgnoreCase))
            {
                box.SelectedItem = item;
                return;
            }
        }
    }

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

        SaveButton.IsEnabled = false;
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            if (_customerId is int id)
            {
                await _api.UpdateCustomerAsync(new UpdateCustomerRequest
                {
                    Id = id,
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
                    IsActive = ActiveBox.IsChecked == true,
                    // not shown on screen - keep exactly as loaded so saving never blanks them out
                    State = _existing?.State ?? "",
                    MembershipId = _existing?.MembershipId ?? "",
                    Birthday = _existing?.Birthday ?? "",
                    Anniversary = _existing?.Anniversary ?? "",
                    MarketingSource = _existing?.MarketingSource ?? "",
                    RegistrationSource = _existing?.RegistrationSource ?? "",
                    PriceListId = _existing?.PriceListId,
                    Gstin = _existing?.Gstin ?? "",
                    ReferralCode = _existing?.ReferralCode ?? "",
                });
            }
            else
            {
                await _api.CreateCustomerAsync(new CreateCustomerRequest
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
                });
            }
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
