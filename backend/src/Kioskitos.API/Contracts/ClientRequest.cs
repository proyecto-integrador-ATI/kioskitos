using System.ComponentModel.DataAnnotations;
using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Contracts;

/// <summary>Body to register or update a client.</summary>
public sealed record ClientRequest(
    [Required, MaxLength(Client.NameMaxLength)] string Name,
    [MaxLength(Client.IndustryMaxLength)] string? Industry,
    [MaxLength(Client.NotesMaxLength)] string? Notes,
    [Required, MinLength(1)] List<ClientContactRequest> Contacts);

public sealed record ClientContactRequest(
    [Required, MaxLength(ClientContact.NameMaxLength)] string Name,
    [Required, EmailAddress, MaxLength(ClientContact.EmailMaxLength)] string Email,
    [Required, MaxLength(ClientContact.PhoneMaxLength)] string Phone);