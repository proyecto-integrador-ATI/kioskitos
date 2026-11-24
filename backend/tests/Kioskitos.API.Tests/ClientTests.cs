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

    [Fact]
    public void AddContact_AssignsPositionsInInsertionOrder()
    {
        var client = new Client(UserId, "Estudio Sur", null, null);

        var first = client.AddContact("Ana", "ana@estudiosur.com", "099123456");
        var second = client.AddContact("Luis", "luis@estudiosur.com", "098765432");

        Assert.Equal(0, first.Position);
        Assert.Equal(1, second.Position);
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

    [Theory]
    [InlineData("ss")]
    [InlineData("099-abc-456")]
    [InlineData("1234567")]
    [InlineData("++598 99 123 456")]
    [InlineData("1234567890123456")]
    public void AddContact_WithInvalidPhone_Throws(string phone)
    {
        var client = new Client(UserId, "Estudio Sur", null, null);

        Assert.Throws<ArgumentException>(() => client.AddContact("Ana", "ana@estudiosur.com", phone));
        Assert.Empty(client.Contacts);
    }

    [Fact]
    public void Update_TrimsValuesAndStoresBlankOptionalFieldsAsNull()
    {
        var client = new Client(UserId, "Estudio Sur", "Diseño", "Notas");

        client.Update("  Estudio Norte ", "  ", "");

        Assert.Equal("Estudio Norte", client.Name);
        Assert.Null(client.Industry);
        Assert.Null(client.Notes);
    }

    [Fact]
    public void Update_WithoutName_Throws()
    {
        var client = new Client(UserId, "Estudio Sur", null, null);

        Assert.Throws<ArgumentException>(() => client.Update(" ", null, null));
        Assert.Equal("Estudio Sur", client.Name);
    }

    [Fact]
    public void ReplaceContacts_ReplacesContactsAndRenumbersPositions()
    {
        var client = new Client(UserId, "Estudio Sur", null, null);
        client.AddContact("Ana", "ana@estudiosur.com", "099123456");

        client.ReplaceContacts([
            ("Luis", "luis@estudiosur.com", "098765432"),
            ("Marta", "marta@estudiosur.com", "097111222")
        ]);

        Assert.Equal(["Luis", "Marta"], client.Contacts.Select(c => c.Name));
        Assert.Equal([0, 1], client.Contacts.Select(c => c.Position));
    }

    [Fact]
    public void ReplaceContacts_WithInvalidContact_KeepsCurrentContacts()
    {
        var client = new Client(UserId, "Estudio Sur", null, null);
        client.AddContact("Ana", "ana@estudiosur.com", "099123456");

        Assert.Throws<ArgumentException>(() => client.ReplaceContacts([("Luis", "luis@estudiosur.com", "ss")]));
        Assert.Equal("Ana", Assert.Single(client.Contacts).Name);
    }

    [Fact]
    public void ReplaceContacts_WithoutContacts_Throws()
    {
        var client = new Client(UserId, "Estudio Sur", null, null);
        client.AddContact("Ana", "ana@estudiosur.com", "099123456");

        Assert.Throws<InvalidOperationException>(() => client.ReplaceContacts([]));
        Assert.Single(client.Contacts);
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("099 123 456")]
    [InlineData("2123-4567")]
    [InlineData("+598 (2) 123 4567")]
    [InlineData("123456789012345")]
    public void IsValidPhone_AcceptsCommonFormats(string phone)
    {
        Assert.True(ClientContact.IsValidPhone(phone));
    }
}