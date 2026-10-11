using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Services.Shared.Enums;
using Services.Socium.Models;
using Tests.Integrations.Socium;
using WebApi.Controllers.Shared;
using static Tests.Infrastructure.ApiAssert;

namespace Tests.Integrations.Files;

[Collection(SociumApiCollection.Name)]
public sealed class FileApiTests(SociumApiFixture fixture)
{
    private const string FileUrl = "/api/file";
    private const string MessageUrl = "/api/message";
    private const string ChatUrl = "/api/chat";
    private const string RoomUrl = "/api/room";

    private HttpClient Client => fixture.Client;

    private async Task<Guid> CreateChatAsync()
    {
        var roomName = $"Room {Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync(RoomUrl, new RoomCreateRequest { Name = roomName }, TestContext.Current.CancellationToken)).StatusCode);
        var rooms = await ReadSuccessAsync<RoomListPageResponse>(await Client.GetAsync(RoomUrl, TestContext.Current.CancellationToken));
        var roomId = rooms.Response!.Rows.Single(row => row.Name == roomName).Id;

        var chatName = $"Chat {Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync(ChatUrl, new ChatCreateRequest { RoomId = roomId, Name = chatName }, TestContext.Current.CancellationToken)).StatusCode);
        var chats = await ReadSuccessAsync<ChatListPageResponse>(await Client.GetAsync($"{ChatUrl}?roomId={roomId}", TestContext.Current.CancellationToken));
        return chats.Response!.Rows.Single(row => row.Name == chatName).Id;
    }

    private static MultipartFormDataContent CreateTextFileContent(string fileName, string text, string contentType = "text/plain")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(text));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    [Fact]
    public async Task UploadAndDownload_WithTextFile_ShouldRoundTrip()
    {
        using var uploadBody = CreateTextFileContent("note.txt", "hello-file");
        var uploaded = await ReadSuccessAsync<FileUploadDto>(await Client.PostAsync(FileUrl, uploadBody, TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.SAVED, uploaded.MessageInfo.MessageType);
        Assert.Equal("note.txt", uploaded.Response!.FileName);
        Assert.Equal("text/plain", uploaded.Response.ContentType);

        var download = await Client.GetAsync($"{FileUrl}/{uploaded.Response.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("hello-file", await download.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Upload_POST_WithCsharpAndOctetStream_ShouldResolveContentTypeByExtension()
    {
        using var uploadBody = CreateTextFileContent("Program.cs", "class A {}", "application/octet-stream");
        var uploaded = await ReadSuccessAsync<FileUploadDto>(await Client.PostAsync(FileUrl, uploadBody, TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.SAVED, uploaded.MessageInfo.MessageType);
        Assert.Equal("Program.cs", uploaded.Response!.FileName);
        Assert.Equal("text/x-csharp", uploaded.Response.ContentType);
    }

    [Fact]
    public async Task Upload_POST_WithDocxAndOctetStream_ShouldResolveOfficeContentType()
    {
        using var uploadBody = CreateTextFileContent(
            "report.docx",
            "PK-fake-docx",
            "application/octet-stream");
        var uploaded = await ReadSuccessAsync<FileUploadDto>(await Client.PostAsync(FileUrl, uploadBody, TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.SAVED, uploaded.MessageInfo.MessageType);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            uploaded.Response!.ContentType);
    }

    [Fact]
    public async Task Message_POST_WithFileIds_ShouldListAttachments()
    {
        var chatId = await CreateChatAsync();
        using var uploadBody = CreateTextFileContent("attach.txt", "payload");
        var uploaded = await ReadSuccessAsync<FileUploadDto>(await Client.PostAsync(FileUrl, uploadBody, TestContext.Current.CancellationToken));

        var created = await Client.PostAsJsonAsync(
            MessageUrl,
            new MessageCreateRequest
            {
                ChatId = chatId,
                Text = "",
                FileIds = [uploaded.Response!.Id],
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var list = await ReadSuccessAsync<MessageListPageResponse>(
            await Client.GetAsync($"{MessageUrl}?chatId={chatId}", TestContext.Current.CancellationToken));
        var row = Assert.Single(list.Response!.Rows);
        var attachment = Assert.Single(row.Attachments);
        Assert.Equal(uploaded.Response.Id, attachment.Id);
        Assert.Equal("attach.txt", attachment.FileName);
    }

    [Fact]
    public async Task Download_GET_WithNonParticipant_ShouldReturn403()
    {
        var chatId = await CreateChatAsync();
        using var uploadBody = CreateTextFileContent("secret.txt", "secret");
        var uploaded = await ReadSuccessAsync<FileUploadDto>(await Client.PostAsync(FileUrl, uploadBody, TestContext.Current.CancellationToken));
        Assert.Equal(
            HttpStatusCode.OK,
            (await Client.PostAsJsonAsync(
                MessageUrl,
                new MessageCreateRequest { ChatId = chatId, Text = "with file", FileIds = [uploaded.Response!.Id] },
                TestContext.Current.CancellationToken)).StatusCode);

        var outsiderId = await fixture.CreateUserAsync($"outsider-{Guid.NewGuid():N}", "Чужой");
        using var outsider = fixture.CreateClientAs(outsiderId);
        var response = await outsider.GetAsync($"{FileUrl}/{uploaded.Response.Id}", TestContext.Current.CancellationToken);

        await ReadProblemAsync(response, HttpStatusCode.Forbidden);
    }

    private sealed class FileUploadDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "";
        public long SizeBytes { get; set; }
    }
}
