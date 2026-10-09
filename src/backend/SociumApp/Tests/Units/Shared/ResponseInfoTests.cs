using Services.Shared.Enums;
using Services.Shared.Models;

namespace Tests.Units.Shared;

public sealed class ResponseInfoTests
{
    private sealed class Payload
    {
        public required string Value { get; init; }
    }

    [Fact]
    public void Success_Create_WithoutType_ShouldReturnLoadedAndDefaultText()
    {
        var payload = new Payload { Value = "data" };

        var result = ResponseInfo.Success(payload);

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.False(string.IsNullOrWhiteSpace(result.MessageInfo.MessageText));
        Assert.Same(payload, result.Response);
    }

    [Theory]
    [InlineData(MessageType.LOADED)]
    [InlineData(MessageType.SAVED)]
    [InlineData(MessageType.WARNING)]
    public void Success_Create_WithSuccessType_ShouldKeepTypeAndText(int messageType)
    {
        var result = ResponseInfo.Success<Payload>(messageType, "text");

        Assert.Equal(messageType, result.MessageInfo.MessageType);
        Assert.Equal("text", result.MessageInfo.MessageText);
        Assert.Null(result.Response);
    }

    [Theory]
    [InlineData(MessageType.ERROR)]
    [InlineData(MessageType.INVALID)]
    [InlineData(MessageType.BAD_REQUEST)]
    [InlineData(MessageType.NOT_FOUND)]
    [InlineData(MessageType.UNAUTHORIZED)]
    [InlineData(MessageType.FORBIDDEN)]
    public void Success_Create_WithErrorType_ShouldThrow(int messageType)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ResponseInfo.Success<Payload>(messageType, "text"));
    }

    [Fact]
    public void Error_Create_WithMatchingType_ShouldReturnError()
    {
        var result = ResponseInfo.Error<Payload>(MessageType.ERROR, "text");

        Assert.Equal(MessageType.ERROR, result.MessageInfo.MessageType);
        Assert.Equal("text", result.MessageInfo.MessageText);
    }

    [Fact]
    public void Invalid_Create_WithMatchingType_ShouldReturnInvalid()
    {
        var result = ResponseInfo.Invalid<Payload>(MessageType.INVALID, "text");

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
    }

    [Fact]
    public void BadRequest_Create_WithMatchingType_ShouldReturnBadRequest()
    {
        var result = ResponseInfo.BadRequest<Payload>(MessageType.BAD_REQUEST, "text");

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
    }

    [Fact]
    public void NotFound_Create_WithMatchingType_ShouldReturnNotFound()
    {
        var result = ResponseInfo.NotFound<Payload>(MessageType.NOT_FOUND, "text");

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
    }

    [Fact]
    public void Unauthorized_Create_WithMatchingType_ShouldReturnUnauthorized()
    {
        var result = ResponseInfo.Unauthorized<Payload>(MessageType.UNAUTHORIZED, "text");

        Assert.Equal(MessageType.UNAUTHORIZED, result.MessageInfo.MessageType);
        Assert.Equal("text", result.MessageInfo.MessageText);
    }

    [Fact]
    public void Forbidden_Create_WithMatchingType_ShouldReturnForbidden()
    {
        var result = ResponseInfo.Forbidden<Payload>(MessageType.FORBIDDEN, "text");

        Assert.Equal(MessageType.FORBIDDEN, result.MessageInfo.MessageType);
        Assert.Equal("text", result.MessageInfo.MessageText);
    }

    [Fact]
    public void Unauthorized_Create_WithForbiddenType_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ResponseInfo.Unauthorized<Payload>(MessageType.FORBIDDEN, "text"));
    }

    [Fact]
    public void Forbidden_Create_WithUnauthorizedType_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ResponseInfo.Forbidden<Payload>(MessageType.UNAUTHORIZED, "text"));
    }

    [Fact]
    public void Invalid_Create_WithBadRequestType_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ResponseInfo.Invalid<Payload>(MessageType.BAD_REQUEST, "text"));
    }

    [Fact]
    public void Error_Create_WithNonErrorType_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ResponseInfo.Error<Payload>(MessageType.INVALID, "text"));
    }
}
