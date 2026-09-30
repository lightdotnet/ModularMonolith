using Microsoft.AspNetCore.Identity;
using StarterKit.Modules.Identity.Extensions;
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
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void ToApplicationResult_ShouldReturnFailure_WhenIdentityResultHasErrors()
        {
            // Arrange
            var identityResult = IdentityResult.Failed(new IdentityError { Description = "Error" });

            // Act
            var result = identityResult.ToResult();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Error", result.Message);
        }
    }
}