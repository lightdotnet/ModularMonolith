using System.Net;
using StarterKit.WebMvc.Services.Http;
using StarterKit.WebMvc.Tests.TestSupport;
using Xunit;

namespace StarterKit.WebMvc.Tests.Services.Http;

/// <summary>
/// Covers <see cref="ApiResponseReader"/> through <see cref="ApiClientBase"/>, so the envelope
/// handling is exercised on real <see cref="HttpResponseMessage"/>s from a fake handler.
/// </summary>
public class ApiResponseReaderTests
{
    [Fact]
    public async Task SuccessEnvelopeWithData_ShouldReturnSuccessWithData()
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(
            HttpStatusCode.OK,
            """{"requestId":"req-1","code":"success","isSuccess":true,"message":"ok","data":{"id":7,"name":"Seven"}}"""));

        // Act
        var result = await client.GetSampleAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ApiResultCodes.Success, result.Code);
        Assert.Equal("ok", result.Message);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("req-1", result.RequestId);
        Assert.Equal(new SampleDto(7, "Seven"), result.Data);
        Assert.False(result.HasValidationErrors);
    }

    [Fact]
    public async Task SuccessEnvelopeWithoutIsSuccessFlag_ShouldFallBackToSuccessCode()
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(
            HttpStatusCode.OK,
            """{"code":"success","data":{"id":1,"name":"One"}}"""));

        // Act
        var result = await client.GetSampleAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.Id);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.NoContent)]
    public async Task EmptyBody_OnCallWithoutData_ShouldBeSuccess(HttpStatusCode statusCode)
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(statusCode));

        // Act
        var result = await client.DeleteSampleAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ApiResultCodes.Success, result.Code);
        Assert.Equal((int)statusCode, result.StatusCode);
    }

    [Fact]
    public async Task SuccessEnvelope_OnCallWithoutData_ShouldBeSuccess()
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(
            HttpStatusCode.OK,
            """{"code":"success","isSuccess":true,"message":"Deleted"}"""));

        // Act
        var result = await client.DeleteSampleAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Deleted", result.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyBody_OnCallExpectingData_ShouldBeFailureNotCodedSuccess(string body)
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(
            HttpStatusCode.OK,
            body));

        // Act
        var result = await client.GetSampleAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ApiResultCodes.Error, result.Code);
        Assert.NotEqual(ApiResultCodes.Success, result.Code);
        Assert.Equal(200, result.StatusCode);
        Assert.False(string.IsNullOrEmpty(result.Message));
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task UnreadableBody_OnSuccessStatus_ShouldBeFailureNotCodedSuccess()
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(
            HttpStatusCode.OK,
            "<html><body>proxy page</body></html>"));

        // Act
        var result = await client.GetSampleAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ApiResultCodes.Error, result.Code);
    }

    [Fact]
    public async Task UnreadableBody_OnCallWithoutData_ShouldBeFailure()
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(
            HttpStatusCode.OK,
            "not json"));

        // Act
        var result = await client.DeleteSampleAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ApiResultCodes.Error, result.Code);
    }

    [Fact]
    public async Task FailureEnvelopeCodedSuccess_OnSuccessStatus_ShouldNeverBeCodedSuccess()
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(
            HttpStatusCode.OK,
            """{"code":"success","isSuccess":false,"message":"Something is off"}"""));

        // Act
        var result = await client.GetSampleAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ApiResultCodes.Error, result.Code);
        Assert.Equal("Something is off", result.Message);
    }

    [Fact]
    public async Task ErrorEnvelope_ShouldCarryCodeMessageAndRequestId()
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(
            HttpStatusCode.NotFound,
            """{"requestId":"req-9","code":"not_found","isSuccess":false,"message":"User not found"}"""));

        // Act
        var result = await client.GetSampleAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ApiResultCodes.NotFound, result.Code);
        Assert.Equal("User not found", result.Message);
        Assert.Equal("req-9", result.RequestId);
        Assert.Equal(404, result.StatusCode);
        Assert.False(result.HasValidationErrors);
        Assert.False(result.IsTransient);
    }

    [Fact]
    public async Task ErrorEnvelope_WithServerError_ShouldBeTransient()
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(
            HttpStatusCode.InternalServerError,
            """{"code":"error","isSuccess":false,"message":"Boom"}"""));

        // Act
        var result = await client.GetSampleAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ApiResultCodes.Error, result.Code);
        Assert.True(result.IsTransient);
    }

    [Fact]
    public async Task NonJsonBody_OnFailureStatus_ShouldUseStatusCode()
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(
            HttpStatusCode.Conflict,
            "<html>conflict</html>"));

        // Act
        var result = await client.GetSampleAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ApiResultCodes.Conflict, result.Code);
        Assert.Equal("Backend request failed with status 409.", result.Message);
    }

    [Fact]
    public async Task VendorExceptionEnvelope_ShouldMapNumericCodeAndParseValidationMessage()
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(
            HttpStatusCode.BadRequest,
            """{"code":"400","isSuccess":false,"message":"Model.Email: Email is required,Email is invalid|Model.Name: Name is too long"}"""));

        // Act
        var result = await client.GetSampleAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ApiResultCodes.BadRequest, result.Code);
        Assert.True(result.HasValidationErrors);
        Assert.Equal(2, result.ValidationErrors.Count);

        // Messages are kept whole (not split on ',') since a message may itself contain commas.
        Assert.Equal(["Email is required,Email is invalid"], result.ValidationErrors["Model.Email"]);
        Assert.Equal(["Name is too long"], result.ValidationErrors["Model.Name"]);

        // Keys are case-insensitive.
        Assert.True(result.ValidationErrors.ContainsKey("model.email"));
    }

    [Fact]
    public async Task VendorExceptionEnvelope_WithPlainMessage_ShouldNotProduceValidationErrors()
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(
            HttpStatusCode.BadRequest,
            """{"code":"400","isSuccess":false,"message":"Request failed: the user is locked"}"""));

        // Act
        var result = await client.GetSampleAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ApiResultCodes.BadRequest, result.Code);
        Assert.False(result.HasValidationErrors);
        Assert.Equal("Request failed: the user is locked", result.Message);
    }

    [Fact]
    public async Task ValidationProblemDetails_ShouldMapErrorsAndTitle()
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(
            HttpStatusCode.BadRequest,
            """
            {
              "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
              "title": "One or more validation errors occurred.",
              "status": 400,
              "errors": {
                "Email": ["The Email field is required."],
                "PageSize": ["Too big.", "Must be even."]
              }
            }
            """));

        // Act
        var result = await client.GetSampleAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ApiResultCodes.BadRequest, result.Code);
        Assert.Equal("One or more validation errors occurred.", result.Message);
        Assert.Equal(["The Email field is required."], result.ValidationErrors["Email"]);
        Assert.Equal(["Too big.", "Must be even."], result.ValidationErrors["PageSize"]);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task AuthFailure_ShouldThrowApiAuthorizationException(HttpStatusCode statusCode)
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(
            statusCode,
            """{"code":"x","isSuccess":false,"message":"Denied by backend"}"""));

        // Act
        var exception = await Assert.ThrowsAsync<ApiAuthorizationException>(() => client.GetSampleAsync());

        // Assert
        Assert.Equal((int)statusCode, exception.StatusCode);
        Assert.Equal("Denied by backend", exception.Message);
    }

    [Fact]
    public async Task AuthFailure_WithoutBody_ShouldThrowWithStatusMessage()
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(HttpStatusCode.Unauthorized));

        // Act
        var exception = await Assert.ThrowsAsync<ApiAuthorizationException>(() => client.DeleteSampleAsync());

        // Assert
        Assert.Equal(401, exception.StatusCode);
        Assert.Equal("Backend returned 401.", exception.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, ApiResultCodes.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, ApiResultCodes.Forbidden)]
    public async Task AuthFailure_WithOptOut_ShouldReturnFailedResult(
        HttpStatusCode statusCode,
        string expectedCode)
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Returning(
            statusCode,
            """{"code":"401","isSuccess":false,"message":"Invalid credentials"}"""));

        // Act
        var result = await client.GetSampleAsync(new ApiRequest
        {
            ThrowOnAuthFailure = false,
        });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Code);
        Assert.Equal("Invalid credentials", result.Message);
        Assert.Equal((int)statusCode, result.StatusCode);
    }

    [Fact]
    public async Task NetworkFailure_ShouldReturnTransientFailedResult()
    {
        // Arrange
        var client = TestApiClient.Create(FakeHttpMessageHandler.Throwing(new HttpRequestException("Connection refused")));

        // Act
        var result = await client.GetSampleAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ApiResultCodes.Error, result.Code);
        Assert.Null(result.StatusCode);
        Assert.True(result.IsTransient);
        Assert.Equal("The server could not be reached. Please try again.", result.Message);
    }

    [Fact]
    public async Task Timeout_ShouldReturnTransientFailedResult()
    {
        // Arrange: the handler never answers, so HttpClient.Timeout fires.
        var handler = new FakeHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(
                Timeout.Infinite,
                cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = TestApiClient.Create(
            handler,
            TimeSpan.FromMilliseconds(50));

        // Act
        var result = await client.DeleteSampleAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ApiResultCodes.Error, result.Code);
        Assert.Null(result.StatusCode);
        Assert.True(result.IsTransient);
        Assert.Equal("The server did not respond in time. Please try again.", result.Message);
    }

    [Fact]
    public async Task CallerCancellation_ShouldPropagate()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(
                Timeout.Infinite,
                cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = TestApiClient.Create(handler);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act + Assert: a cancellation requested by the caller is not swallowed as a timeout.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetSampleAsync(cancellationToken: cts.Token));
    }
}
