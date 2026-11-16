using Kioskitos.Application.GoogleCalendar;
using Microsoft.AspNetCore.DataProtection;

namespace Kioskitos.Infrastructure.GoogleCalendar;

public sealed class DataProtectionTokenProtector(IDataProtectionProvider provider) : ITokenProtector
{
    private readonly IDataProtector protector =
        provider.CreateProtector("Kioskitos.GoogleCalendar.RefreshToken");

    public string Protect(string plainText) => protector.Protect(plainText);

    public string Unprotect(string protectedText) => protector.Unprotect(protectedText);
}