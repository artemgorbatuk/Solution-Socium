using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Shared.Enums;
using Services.Shared.Models;
using WebApi.Controllers.Shared;

namespace Tests.Units.WebApi;

public sealed class ApiControllerBaseTests
{
    private sealed class TestController : ApiControllerBase
    {
        public IActionResult Map<T>(ResponseInfo<T> responseInfo) where T : class => FromResponse(responseInfo);
    }

    private sealed class Payload
    {
        public required string Value { get; init; }
    }

    private sealed class FileDownload : IFileDownload
    {
        public Stream Stream { get; } = new MemoryStream([1, 2, 3]);
        public string FileName => "report.xlsx";
        public string ContentType => "application/octet-stream";
    }

    private static ResponseInfo<T> Response<T>(int messageType, T? response = null) where T : class => new()
    {
        Response = response,
        MessageInfo = new MessageInfo { MessageType = messageType, MessageText = "text" }
    };

    [Theory]
    [InlineData(MessageType.LOADED)]
    [InlineData(MessageType.SAVED)]
    [InlineData(MessageType.WARNING)]
    public void Response_Convert_WithSuccessType_ShouldReturn200WithSuccessBody(int messageType)
    {
        var payload = new Payload { Value = "data" };

        var result = new TestController().Map(Response(messageType, payload));

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ApiSuccessResponse<Payload>>(ok.Value);
        Assert.Same(payload, body.Response);
        Assert.Equal(messageType, body.MessageInfo.MessageType);
        Assert.Equal("text", body.MessageInfo.MessageText);
        Assert.Equal(MessageType.GetAlertClass(messageType), body.MessageAlert);
    }

    [Theory]
    [InlineData(MessageType.BAD_REQUEST, StatusCodes.Status400BadRequest)]
    [InlineData(MessageType.UNAUTHORIZED, StatusCodes.Status401Unauthorized)]
    [InlineData(MessageType.FORBIDDEN, StatusCodes.Status403Forbidden)]
    [InlineData(MessageType.NOT_FOUND, StatusCodes.Status404NotFound)]
    [InlineData(MessageType.INVALID, StatusCodes.Status422UnprocessableEntity)]
    [InlineData(MessageType.ERROR, StatusCodes.Status500InternalServerError)]
    [InlineData(MessageType.NONE, StatusCodes.Status500InternalServerError)]
    public void Response_Convert_WithErrorType_ShouldReturnProblemDetailsWithStatus(int messageType, int expectedStatus)
    {
        var result = new TestController().Map(Response<Payload>(messageType));

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal("text", problem.Detail);
        Assert.Equal(MessageType.GetAlertClass(messageType), problem.Extensions["messageAlert"]);
    }

    [Fact]
    public void Response_Convert_WithFileDownload_ShouldReturnFile()
    {
        var download = new FileDownload();

        var result = new TestController().Map(Response<FileDownload>(MessageType.LOADED, download));

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal(download.FileName, file.FileDownloadName);
        Assert.Equal(download.ContentType, file.ContentType);
    }
}
