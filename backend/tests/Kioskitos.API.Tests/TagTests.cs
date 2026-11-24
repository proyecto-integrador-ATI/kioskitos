using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Tests;

public sealed class TagTests
{
    [Fact]
    public void Tag_TrimsName()
    {
        var userId = Guid.NewGuid();

        var tag = new Tag(userId, "  Web  ");

        Assert.NotEqual(Guid.Empty, tag.Id);
        Assert.Equal(userId, tag.UserId);
        Assert.Equal("Web", tag.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Tag_WithoutName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Tag(Guid.NewGuid(), name!));
    }

    [Fact]
    public void Tag_WithoutUser_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Tag(Guid.Empty, "Web"));
    }

    [Fact]
    public void StageName_TrimsName()
    {
        var stageName = new StageName(Guid.NewGuid(), "  Design  ");

        Assert.Equal("Design", stageName.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void StageName_WithoutName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() => new StageName(Guid.NewGuid(), name!));
    }

    [Fact]
    public void StageName_WithoutUser_Throws()
    {
        Assert.Throws<ArgumentException>(() => new StageName(Guid.Empty, "Design"));
    }
}
