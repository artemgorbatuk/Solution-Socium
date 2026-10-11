using Microsoft.AspNetCore.Mvc;
using Services.Files.Stored.Api;
using Services.Shared.Enums;
using Services.Shared.Models;
using WebApi.Controllers.Shared;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController : ApiControllerBase
{
    private readonly IServiceStoredFile serviceStoredFile;

    public FileController(IServiceStoredFile serviceStoredFile)
    {
        this.serviceStoredFile = serviceStoredFile;
    }

    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return FromResponse(ResponseInfo.Unauthorized<FileUploadResponse>(
                MessageType.UNAUTHORIZED,
                "Текущий пользователь не выбран или не найден."));
        }

        if (file == null || file.Length == 0)
        {
            return FromResponse(ResponseInfo.Invalid<FileUploadResponse>(
                MessageType.INVALID,
                "Файл не передан."));
        }

        await using var stream = file.OpenReadStream();
        var responseInfo = await serviceStoredFile.UploadAsync(
            new FileUploadRequest
            {
                UserId = userId,
                FileName = file.FileName,
                ContentType = file.ContentType ?? string.Empty,
                SizeBytes = file.Length,
                Content = stream,
            },
            cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return FromResponse(ResponseInfo.Unauthorized<FileDownloadResponse>(
                MessageType.UNAUTHORIZED,
                "Текущий пользователь не выбран или не найден."));
        }

        var responseInfo = await serviceStoredFile.DownloadAsync(
            new FileDownloadRequest { FileId = id, UserId = userId },
            cancellationToken);
        return FromResponse(responseInfo);
    }
}
