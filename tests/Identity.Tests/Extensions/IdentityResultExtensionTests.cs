using Microsoft.AspNetCore.Identity;
using Monolith.Identity.Extensions;
using Xunit;

namespace Identity.Tests.Extensions
{
    public class IdentityResultExtensionTests
    {
        [Fact]
        public void ToApplicationResult_ShouldReturnSuccess_WhenIdentityResultIsSuccessful()
        {
            // Arrange
            var identityResult = IdentityResult.Success;

            // Act
            var result = identityResult.ToResult();

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public void ToApplicationResult_ShouldReturnFailure_WhenIdentityResultHasErrors()
        {
            // Arrange
            var identityResult = IdentityResult.Failed(new IdentityError { Description = "Error" });

            // Act
            var result = identityResult.ToResult();

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("Error", result.Message);
        }
    }
}