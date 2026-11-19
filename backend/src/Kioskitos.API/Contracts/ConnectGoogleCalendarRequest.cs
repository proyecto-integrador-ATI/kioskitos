using System.ComponentModel.DataAnnotations;

namespace Kioskitos.API.Contracts;

public sealed record ConnectGoogleCalendarRequest([Required] string Code);

public sealed record ConnectGoogleCalendarResponse(string CalendarId, bool Connected);