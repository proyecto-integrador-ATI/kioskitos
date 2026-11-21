using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Tests;

public sealed class ClientTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void NewClient_TrimsValuesAndStoresOwner()
    {
        var client = new Client(UserId, "  Estudio Sur  ", "  Diseño  ", "  Cliente recurrente ");

        Assert.Equal(UserId, client.UserId);
        Assert.Equal("Estudio Sur", client.Name);
        Assert.Equal("Diseño", client.Industry);
        Assert.Equal("Cliente recurrente", client.Notes);
        Assert.NotEqual(Guid.Empty, client.Id);
    }

    [Fact]
    public void NewClient_WithBlankOptionalFields_StoresNull()
    {
        var client = new Client(UserId, "Estudio Sur", "   ", "");

        Assert.Null(client.Industry);
        Assert.Null(client.Notes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NewClient_WithoutName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => new Client(UserId, name, null, null));
    }

    [Fact]
    public void NewClient_WithoutOwner_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Client(Guid.Empty, "Estudio Sur", null, null));
    }

    [Fact]
    public void AddContact_LinksContactToClient()
    {
        var client = new Client(UserId, "Estudio Sur", null, null);

        var contact = client.AddContact(" Ana Pérez ", " ana@estudiosur.com ", " 099123456 ");

        Assert.Single(client.Contacts);
        Assert.Equal(client.Id, contact.ClientId);
        Assert.Equal("Ana Pérez", contact.Name);
        Assert.Equal("ana@estudiosur.com", contact.Email);
        Assert.Equal("099123456", contact.Phone);
    }

    [Theory]
    [InlineData("", "ana@estudiosur.com", "099123456")]
    [InlineData("Ana", "", "099123456")]
    [InlineData("Ana", "ana@estudiosur.com", " ")]
    public void AddContact_WithMissingData_Throws(string name, string email, string phone)
    {
        var client = new Client(UserId, "Estudio Sur", null, null);

        Assert.Throws<ArgumentException>(() => client.AddContact(name, email, phone));
        Assert.Empty(client.Contacts);
    }
}