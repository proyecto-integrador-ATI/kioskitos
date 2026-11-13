using System.ComponentModel.DataAnnotations;

namespace Kioskitos.API.Contracts;

public sealed record GoogleLoginRequest([Required] string IdToken);