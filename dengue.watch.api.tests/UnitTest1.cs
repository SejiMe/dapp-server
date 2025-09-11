namespace dengue.watch.api.tests.common;

/// <summary>
/// Sample test class - replace with actual tests
/// </summary>
public class SampleTests
{
    [Fact]
    public void HealthCheck_ShouldReturnHealthy()
    {
        // Arrange
        var expected = "healthy";

        // Act
        var result = expected;

        // Assert
        result.Should().Be("healthy");
    }
}
