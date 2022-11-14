using FluentAssertions;
using Xunit;

namespace HttpParrot.Tests
{
    public class JsonExtensionsTests
    {
        [Fact]
        public void NonDestructiveJsonPrettify()
        {
            // Arrange
            var input = @"{""foo"":1.000,""bar"":null,""baz"":""2022-11-10T11:15:00Z""}";
            
            // Act
            var actual = input.NonDestructiveJsonPrettify();
            
            // Assert
            actual.Should().Be(@"{
  ""foo"": 1.000,
  ""bar"": null,
  ""baz"": ""2022-11-10T11:15:00Z""
}");
        }
    }
}