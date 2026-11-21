using Kioskitos.Application.Tags;
using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Tests;

public sealed class TagServiceTests
{
    [Fact]
    public async Task List_ReturnsOnlyUserTagsSortedByName()
    {
        var userId = Guid.NewGuid();
        var tags = new InMemoryTagRepository(
            new Tag(userId, "web"),
            new Tag(Guid.NewGuid(), "Foreign"),
            new Tag(userId, "Branding"));

        var result = await new TagService(tags).ListAsync(userId, CancellationToken.None);

        Assert.Equal(["Branding", "web"], result.Select(t => t.Name));
    }
}
