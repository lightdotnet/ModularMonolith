using StarterKit.Shared;
using System.Text.Json;
using Xunit;

namespace Framework.Tests.Shared;

public class BaseDtoTests
{
    private sealed class SampleDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void Serialize_ShouldPlaceIdFirst_RegardlessOfDeclarationOrder()
    {
        // Arrange
        var dto = new SampleDto { Id = "abc-123", Name = "Sample" };

        // Act
        var json = JsonSerializer.Serialize(dto);
        using var document = JsonDocument.Parse(json);
        var firstProperty = document.RootElement.EnumerateObject().First();

        // Assert
        Assert.Equal("Id", firstProperty.Name);
        Assert.Equal("abc-123", firstProperty.Value.GetString());
    }
}
