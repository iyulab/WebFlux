using FluentAssertions;
using WebFlux.Core.Models;

namespace WebFlux.Tests.Models;

public class ProcessingErrorTests
{
    [Fact]
    public void Create_ShouldReturnCorrectValues()
    {
        var error = ProcessingError.Create(
            "TEST_ERROR",
            "Test message",
            ErrorSeverity.Warning,
            ErrorCategory.Validation,
            isRetryable: true);

        error.Code.Should().Be("TEST_ERROR");
        error.Message.Should().Be("Test message");
        error.Severity.Should().Be(ErrorSeverity.Warning);
        error.Category.Should().Be(ErrorCategory.Validation);
        error.IsRetryable.Should().BeTrue();
    }

    [Fact]
    public void Create_WithDefaults_ShouldUseDefaultSeverityAndCategory()
    {
        var error = ProcessingError.Create("ERR", "message");

        error.Severity.Should().Be(ErrorSeverity.Error);
        error.Category.Should().Be(ErrorCategory.General);
        error.IsRetryable.Should().BeFalse();
    }

    [Fact]
    public void NetworkError_ShouldSetCorrectValues()
    {
        var error = ProcessingError.NetworkError("Connection failed", "https://example.com");

        error.Code.Should().Be("NETWORK_ERROR");
        error.Category.Should().Be(ErrorCategory.Network);
        error.IsRetryable.Should().BeTrue();
        error.RelatedResource.Should().Be("https://example.com");
        error.UserFriendlyMessage.Should().NotBeNullOrEmpty();
        error.SuggestedActions.Should().NotBeEmpty();
    }

    [Fact]
    public void NetworkError_WithStatusCode_ShouldIncludeInDetails()
    {
        var error = ProcessingError.NetworkError("Server error", statusCode: 503);

        error.Details.Should().ContainKey("StatusCode");
        error.Details["StatusCode"].Should().Be(503);
    }

    [Fact]
    public void AuthenticationError_ShouldSetCorrectValues()
    {
        var error = ProcessingError.AuthenticationError("Invalid API key", "OpenAI");

        error.Code.Should().Be("AUTH_ERROR");
        error.Category.Should().Be(ErrorCategory.Authentication);
        error.IsRetryable.Should().BeFalse();
        error.RelatedResource.Should().Be("OpenAI");
        error.SuggestedActions.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidationError_ShouldSetCorrectValues()
    {
        var error = ProcessingError.ValidationError("Invalid URL");

        error.Code.Should().Be("VALIDATION_ERROR");
        error.Severity.Should().Be(ErrorSeverity.Warning);
        error.Category.Should().Be(ErrorCategory.Validation);
        error.IsRetryable.Should().BeFalse();
    }

    [Fact]
    public void ValidationError_WithFieldName_ShouldIncludeInDetails()
    {
        var error = ProcessingError.ValidationError("Required field missing", "email");

        error.Details.Should().ContainKey("Field");
        error.Details["Field"].Should().Be("email");
    }

    [Fact]
    public void ValidationError_WithoutFieldName_ShouldHaveEmptyDetails()
    {
        var error = ProcessingError.ValidationError("General error");
        error.Details.Should().BeEmpty();
    }

    [Fact]
    public void FromException_HttpRequestException_ShouldMapCorrectly()
    {
        var ex = new HttpRequestException("Connection refused");
        var error = ProcessingError.FromException(ex, "TestSource");

        error.Code.Should().Be("HTTPREQUEST");
        error.Message.Should().Be("Connection refused");
        error.Category.Should().Be(ErrorCategory.Network);
        error.Severity.Should().Be(ErrorSeverity.Error);
        error.IsRetryable.Should().BeTrue();
        error.Source.Should().Be("TestSource");
        error.InnerException.Should().Be(ex);
    }

    [Fact]
    public void FromException_TimeoutException_ShouldMapCorrectly()
    {
        var ex = new TimeoutException("Timed out");
        var error = ProcessingError.FromException(ex);

        error.Code.Should().Be("TIMEOUT");
        error.Category.Should().Be(ErrorCategory.Timeout);
        error.Severity.Should().Be(ErrorSeverity.Warning);
        error.IsRetryable.Should().BeTrue();
    }

    [Fact]
    public void FromException_UnauthorizedAccessException_ShouldMapCorrectly()
    {
        var ex = new UnauthorizedAccessException("Access denied");
        var error = ProcessingError.FromException(ex);

        error.Code.Should().Be("UNAUTHORIZEDACCESS");
        error.Category.Should().Be(ErrorCategory.Authentication);
        error.IsRetryable.Should().BeFalse();
    }

    [Fact]
    public void FromException_ArgumentException_ShouldMapCorrectly()
    {
        var ex = new ArgumentException("Bad argument");
        var error = ProcessingError.FromException(ex);

        error.Code.Should().Be("ARGUMENT");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Severity.Should().Be(ErrorSeverity.Warning);
        error.IsRetryable.Should().BeFalse();
    }

    [Fact]
    public void FromException_NotSupportedException_ShouldMapCorrectly()
    {
        var ex = new NotSupportedException("Not supported");
        var error = ProcessingError.FromException(ex);

        error.Code.Should().Be("NOTSUPPORTED");
        error.Category.Should().Be(ErrorCategory.NotSupported);
        error.Severity.Should().Be(ErrorSeverity.Error);
        error.IsRetryable.Should().BeTrue();
    }

    [Fact]
    public void FromException_GenericException_ShouldUseDefaults()
    {
        var ex = new InvalidOperationException("Something went wrong");
        var error = ProcessingError.FromException(ex);

        error.Code.Should().Be("INVALIDOPERATION");
        error.Category.Should().Be(ErrorCategory.Configuration);
        error.Severity.Should().Be(ErrorSeverity.Error);
        error.IsRetryable.Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        var error = ProcessingError.Create("NET_ERR", "Connection lost", ErrorSeverity.Critical);

        error.ToString().Should().Be("[Critical] NET_ERR: Connection lost");
    }
}

public class ErrorCategoryEnumTests
{
    [Fact]
    public void ShouldHaveElevenValues()
    {
        Enum.GetValues<ErrorCategory>().Should().HaveCount(11);
    }
}

public class ErrorSeverityEnumTests
{
    [Fact]
    public void ShouldHaveFourValues()
    {
        Enum.GetValues<ErrorSeverity>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(ErrorSeverity.Info, 0)]
    [InlineData(ErrorSeverity.Warning, 1)]
    [InlineData(ErrorSeverity.Error, 2)]
    [InlineData(ErrorSeverity.Critical, 3)]
    public void ShouldHaveExpectedIntValues(ErrorSeverity severity, int expected)
    {
        ((int)severity).Should().Be(expected);
    }
}
