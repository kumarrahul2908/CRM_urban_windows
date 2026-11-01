using System.Text.Json.Serialization;

namespace Udc.Client.Models;

/// <summary>One row of <c>GET /api/mobile/customers_list</c>.</summary>
public sealed class CustomerSummary
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("customer_code")]
    public string CustomerCode { get; set; } = "";

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = "";

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("area")]
    public string? Area { get; set; }

    [JsonPropertyName("price_list_name")]
    public string? PriceListName { get; set; }

    [JsonPropertyName("discount_percent")]
    public decimal DiscountPercent { get; set; }

    [JsonPropertyName("home_delivery")]
    public bool HomeDelivery { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
}

public sealed class CustomerListResult
{
    [JsonPropertyName("results")]
    public List<CustomerSummary> Results { get; set; } = new();

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }
}

/// <summary><c>GET /api/mobile/customers_detail</c>.</summary>
public sealed class CustomerDetail
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("customer_code")]
    public string CustomerCode { get; set; } = "";

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = "";

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("alt_mobile")]
    public string? AltMobile { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("area")]
    public string? Area { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("pincode")]
    public string? Pincode { get; set; }

    [JsonPropertyName("membership_id")]
    public string? MembershipId { get; set; }

    [JsonPropertyName("birthday")]
    public string? Birthday { get; set; }

    [JsonPropertyName("anniversary")]
    public string? Anniversary { get; set; }

    [JsonPropertyName("price_list_name")]
    public string? PriceListName { get; set; }

    [JsonPropertyName("discount_percent")]
    public decimal DiscountPercent { get; set; }

    [JsonPropertyName("communication_preference")]
    public string? CommunicationPreference { get; set; }

    [JsonPropertyName("gstin")]
    public string? Gstin { get; set; }

    [JsonPropertyName("referral_code")]
    public string? ReferralCode { get; set; }

    [JsonPropertyName("home_delivery")]
    public bool HomeDelivery { get; set; }

    [JsonPropertyName("registration_source")]
    public string? RegistrationSource { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("wallet_balance")]
    public decimal WalletBalance { get; set; }

    [JsonPropertyName("outstanding_balance")]
    public decimal OutstandingBalance { get; set; }
}

public sealed class CustomerDetailResult
{
    [JsonPropertyName("customer")]
    public CustomerDetail Customer { get; set; } = new();
}

/// <summary>Body for <c>POST /api/mobile/customers_create</c> (see customers.ts formFrom()).</summary>
public sealed class CreateCustomerRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "Mr";

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = "";

    [JsonPropertyName("mobile")]
    public string Mobile { get; set; } = "";

    [JsonPropertyName("alt_mobile")]
    public string AltMobile { get; set; } = "";

    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("address")]
    public string Address { get; set; } = "";

    [JsonPropertyName("area")]
    public string Area { get; set; } = "";

    [JsonPropertyName("city")]
    public string City { get; set; } = "";

    [JsonPropertyName("state")]
    public string State { get; set; } = "";

    [JsonPropertyName("pincode")]
    public string Pincode { get; set; } = "";

    [JsonPropertyName("home_delivery")]
    public bool HomeDelivery { get; set; }

    [JsonPropertyName("discount_percent")]
    public decimal DiscountPercent { get; set; }

    [JsonPropertyName("communication_preference")]
    public string CommunicationPreference { get; set; } = "SMS";

    [JsonPropertyName("registration_source")]
    public string RegistrationSource { get; set; } = "Counter";

    [JsonPropertyName("remarks")]
    public string Remarks { get; set; } = "";

    [JsonPropertyName("gstin")]
    public string Gstin { get; set; } = "";

    /// <summary>Resend the same submission with this set to retry past a "very similar name"
    /// duplicate warning (see <see cref="ApiException"/>.Code == "duplicate_requires_confirmation").</summary>
    [JsonPropertyName("confirm_different_person")]
    public bool ConfirmDifferentPerson { get; set; }
}

public sealed class CreateCustomerResult
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("customer_code")]
    public string CustomerCode { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
}

// ---- Edit --------------------------------------------------------------------------------

/// <summary><c>GET /api/mobile/customers_edit_detail</c> - the editable field set. Narrower than
/// <see cref="CustomerDetail"/> (no customer_code/price_list_name/wallet/outstanding) but includes
/// several fields <c>CustomerFormWindow</c> doesn't show on screen (State, MembershipId, Birthday,
/// Anniversary, MarketingSource, PriceListId, ReferralCode) - those must be carried through
/// unchanged into <see cref="UpdateCustomerRequest"/> on save, since the backend's formFrom()
/// treats every field in the update payload as the new value for that field, not "leave
/// unchanged if omitted" (see customers.ts).</summary>
public sealed class CustomerEditDetail
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = "";

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("alt_mobile")]
    public string? AltMobile { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("area")]
    public string? Area { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("pincode")]
    public string? Pincode { get; set; }

    [JsonPropertyName("membership_id")]
    public string? MembershipId { get; set; }

    [JsonPropertyName("birthday")]
    public string? Birthday { get; set; }

    [JsonPropertyName("anniversary")]
    public string? Anniversary { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }

    [JsonPropertyName("home_delivery")]
    public bool HomeDelivery { get; set; }

    [JsonPropertyName("marketing_source")]
    public string? MarketingSource { get; set; }

    [JsonPropertyName("registration_source")]
    public string? RegistrationSource { get; set; }

    [JsonPropertyName("discount_percent")]
    public decimal DiscountPercent { get; set; }

    [JsonPropertyName("price_list_id")]
    public int? PriceListId { get; set; }

    [JsonPropertyName("communication_preference")]
    public string? CommunicationPreference { get; set; }

    [JsonPropertyName("gstin")]
    public string? Gstin { get; set; }

    [JsonPropertyName("referral_code")]
    public string? ReferralCode { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
}

public sealed class CustomerEditDetailResult
{
    [JsonPropertyName("customer")]
    public CustomerEditDetail Customer { get; set; } = new();
}

/// <summary>Body for <c>POST /api/mobile/customers_update</c> - same field set as
/// <see cref="CreateCustomerRequest"/> plus Id/IsActive. See <see cref="CustomerEditDetail"/>'s
/// doc comment: every field not shown in the Edit form is still sent, filled from the loaded
/// record, so saving an edit never silently blanks a field the UI doesn't surface.</summary>
public sealed class UpdateCustomerRequest
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = "";

    [JsonPropertyName("mobile")]
    public string Mobile { get; set; } = "";

    [JsonPropertyName("alt_mobile")]
    public string AltMobile { get; set; } = "";

    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("address")]
    public string Address { get; set; } = "";

    [JsonPropertyName("area")]
    public string Area { get; set; } = "";

    [JsonPropertyName("city")]
    public string City { get; set; } = "";

    [JsonPropertyName("state")]
    public string State { get; set; } = "";

    [JsonPropertyName("pincode")]
    public string Pincode { get; set; } = "";

    [JsonPropertyName("membership_id")]
    public string MembershipId { get; set; } = "";

    [JsonPropertyName("birthday")]
    public string Birthday { get; set; } = "";

    [JsonPropertyName("anniversary")]
    public string Anniversary { get; set; } = "";

    [JsonPropertyName("remarks")]
    public string Remarks { get; set; } = "";

    [JsonPropertyName("home_delivery")]
    public bool HomeDelivery { get; set; }

    [JsonPropertyName("marketing_source")]
    public string MarketingSource { get; set; } = "";

    [JsonPropertyName("registration_source")]
    public string RegistrationSource { get; set; } = "";

    [JsonPropertyName("discount_percent")]
    public decimal DiscountPercent { get; set; }

    [JsonPropertyName("price_list_id")]
    public int? PriceListId { get; set; }

    [JsonPropertyName("communication_preference")]
    public string CommunicationPreference { get; set; } = "";

    [JsonPropertyName("gstin")]
    public string Gstin { get; set; } = "";

    [JsonPropertyName("referral_code")]
    public string ReferralCode { get; set; } = "";

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>Resend with this set to retry past a "very similar name" duplicate warning, same
    /// as <see cref="CreateCustomerRequest.ConfirmDifferentPerson"/>.</summary>
    [JsonPropertyName("confirm_different_person")]
    public bool ConfirmDifferentPerson { get; set; }
}

public sealed class UpdateCustomerResult
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
}
