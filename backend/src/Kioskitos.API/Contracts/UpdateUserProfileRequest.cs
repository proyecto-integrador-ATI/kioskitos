using System.ComponentModel.DataAnnotations;

namespace Kioskitos.API.Contracts;

/// <summary>Only the editable profile fields. Identity data (Id, Email, ...) is never read from the request.</summary>
public sealed record UpdateUserProfileRequest(
    [Required, MaxLength(200)] string Name,
    [MaxLength(100)] string? Industry,
    [Range(typeof(decimal), "0", "9999999999.99",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "HourlyRate must be between 0 and 9999999999.99.")]
    decimal? HourlyRate);
