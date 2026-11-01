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
