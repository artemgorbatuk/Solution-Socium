using Datasource.Socium.Ef.Models;
using Services.Socium.Models;
using Services.Socium.Texts;
using Services.Socium.Validation;

namespace Tests.Units.Socium;

public sealed class MessageCrudValidatorsTests
{
    [Fact]
    public void AllRequests_ValidatePrimary_WithNullRequest_ShouldReturnRequestCannotBeNull()
    {
        string[] expected = [MessageCrudTexts.Messages.Validation.RequestCannotBeNull];

        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryCreatePageRequest(null));
        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryCreateRequest(null));
        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryListPageRequest(null));
        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryUpdatePageRequest(null));
        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryUpdateRequest(null));
        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryDeletePageRequest(null));
        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryDeleteRequest(null));
        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryInfoPageRequest(null));
    }

    [Fact]
    public void RequestsWithChatId_ValidatePrimary_WithEmptyChatId_ShouldReturnChatIdCannotBeEmpty()
    {
        string[] expected = [MessageCrudTexts.Messages.Validation.ChatIdCannotBeEmpty];

        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryCreatePageRequest(new MessageCreatePageRequest { ChatId = Guid.Empty }));
        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryCreateRequest(new MessageCreateRequest { ChatId = Guid.Empty, Text = "Text" }));
        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryListPageRequest(new MessageListPageRequest { ChatId = Guid.Empty }));
    }

    [Fact]
    public void RequestsWithId_ValidatePrimary_WithEmptyId_ShouldReturnIdCannotBeEmpty()
    {
        string[] expected = [MessageCrudTexts.Messages.Validation.IdCannotBeEmpty];

        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryUpdatePageRequest(new MessageUpdatePageRequest { Id = Guid.Empty }));
        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryUpdateRequest(new MessageUpdateRequest { Id = Guid.Empty, Text = "Text" }));
        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryDeletePageRequest(new MessageDeletePageRequest { Id = Guid.Empty }));
        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryDeleteRequest(new MessageDeleteRequest { Id = Guid.Empty }));
        Assert.Equal(expected, MessageCrudValidators.ValidatePrimaryInfoPageRequest(new MessageInfoPageRequest { Id = Guid.Empty }));
    }

    [Fact]
    public void AllRequests_ValidatePrimary_WithValidRequest_ShouldReturnNoErrors()
    {
        var id = Guid.CreateVersion7();
        var chatId = Guid.CreateVersion7();

        Assert.Empty(MessageCrudValidators.ValidatePrimaryCreatePageRequest(new MessageCreatePageRequest { ChatId = chatId }));
        Assert.Empty(MessageCrudValidators.ValidatePrimaryCreateRequest(new MessageCreateRequest { ChatId = chatId, Text = "" }));
        Assert.Empty(MessageCrudValidators.ValidatePrimaryListPageRequest(new MessageListPageRequest { ChatId = chatId }));
        Assert.Empty(MessageCrudValidators.ValidatePrimaryUpdatePageRequest(new MessageUpdatePageRequest { Id = id }));
        Assert.Empty(MessageCrudValidators.ValidatePrimaryUpdateRequest(new MessageUpdateRequest { Id = id, Text = "" }));
        Assert.Empty(MessageCrudValidators.ValidatePrimaryDeletePageRequest(new MessageDeletePageRequest { Id = id }));
        Assert.Empty(MessageCrudValidators.ValidatePrimaryDeleteRequest(new MessageDeleteRequest { Id = id }));
        Assert.Empty(MessageCrudValidators.ValidatePrimaryInfoPageRequest(new MessageInfoPageRequest { Id = id }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t ")]
    public void CreateRequest_ValidateDomain_WithBlankText_ShouldReturnTextNotEmpty(string text)
    {
        var request = new MessageCreateRequest { ChatId = Guid.CreateVersion7(), Text = text };

        var errors = MessageCrudValidators.ValidateDomainCreateRequest(request);

        Assert.Equal([MessageCrudTexts.Messages.Validation.TextNotEmpty], errors);
    }

    [Fact]
    public void CreateRequest_ValidateDomain_WithMillionCharacters_ShouldReturnNoErrors()
    {
        var request = new MessageCreateRequest { ChatId = Guid.CreateVersion7(), Text = new string('a', 1_000_000) };

        Assert.Empty(MessageCrudValidators.ValidateDomainCreateRequest(request));
    }

    [Fact]
    public void UpdateRequest_ValidateDomain_WithBlankText_ShouldReturnTextNotEmpty()
    {
        var request = new MessageUpdateRequest { Id = Guid.CreateVersion7(), Text = " " };

        var errors = MessageCrudValidators.ValidateDomainUpdateRequest(request);

        Assert.Equal([MessageCrudTexts.Messages.Validation.TextNotEmpty], errors);
    }

    [Fact]
    public void UpdateRequest_ValidateDomain_WithMultilineText_ShouldReturnNoErrors()
    {
        var request = new MessageUpdateRequest { Id = Guid.CreateVersion7(), Text = "Строка 1\nСтрока 2" };

        Assert.Empty(MessageCrudValidators.ValidateDomainUpdateRequest(request));
    }

    [Fact]
    public void Message_ValidateAccessibility_WithMissingMessage_ShouldReturnMessageNotFoundById()
    {
        var errors = MessageCrudValidators.ValidateAccessibilityMessage<object>(null);

        Assert.Equal([MessageCrudTexts.Messages.Validation.MessageNotFoundById], errors);
    }

    [Fact]
    public void Chat_ValidateAccessibility_WithMissingChat_ShouldReturnChatNotFoundById()
    {
        var errors = MessageCrudValidators.ValidateAccessibilityChat<object>(null);

        Assert.Equal([MessageCrudTexts.Messages.Validation.ChatNotFoundById], errors);
    }

    [Fact]
    public void MessageAndChat_ValidateAccessibility_WithExistingModel_ShouldReturnNoErrors()
    {
        Assert.Empty(MessageCrudValidators.ValidateAccessibilityMessage(new object()));
        Assert.Empty(MessageCrudValidators.ValidateAccessibilityChat(new object()));
    }

    private static Participant CreateParticipant(Guid userId) => new()
    {
        Id = Guid.CreateVersion7(),
        ChatId = Guid.CreateVersion7(),
        UserId = userId,
    };

    private static Message CreateMessage(Guid senderUserId) => new()
    {
        Id = Guid.CreateVersion7(),
        ChatId = Guid.CreateVersion7(),
        Text = "Привет",
        CreatedAt = DateTime.UtcNow,
        Sender = new Sender { Id = Guid.CreateVersion7(), MessageId = Guid.CreateVersion7(), UserId = senderUserId },
    };

    [Fact]
    public void CurrentUser_Validate_WithMissingOrDeletedUser_ShouldReturnCurrentUserNotFound()
    {
        string[] expected = [MessageCrudTexts.Messages.Validation.CurrentUserNotFound];
        var deleted = new User { Id = Guid.CreateVersion7(), Login = "ivan", Name = "Иван", IsDeleted = true };

        Assert.Equal(expected, MessageCrudValidators.ValidateCurrentUser(null));
        Assert.Equal(expected, MessageCrudValidators.ValidateCurrentUser(deleted));
        Assert.Empty(MessageCrudValidators.ValidateCurrentUser(new User { Id = Guid.CreateVersion7(), Login = "ivan", Name = "Иван" }));
    }

    [Fact]
    public void Participant_ValidateAccess_WithAndWithoutParticipant_ShouldAllowOnlyParticipant()
    {
        Assert.Equal([MessageCrudTexts.Messages.Validation.NotParticipant], MessageCrudValidators.ValidateAccessParticipant(null));
        Assert.Empty(MessageCrudValidators.ValidateAccessParticipant(CreateParticipant(Guid.CreateVersion7())));
    }

    [Fact]
    public void Sender_ValidateAccess_WithSenderParticipant_ShouldReturnNoErrors()
    {
        var userId = Guid.CreateVersion7();

        Assert.Empty(MessageCrudValidators.ValidateAccessSender(CreateParticipant(userId), CreateMessage(userId)));
    }

    [Fact]
    public void Sender_ValidateAccess_WithOtherParticipant_ShouldReturnNotSender()
    {
        var errors = MessageCrudValidators.ValidateAccessSender(CreateParticipant(Guid.CreateVersion7()), CreateMessage(Guid.CreateVersion7()));

        Assert.Equal([MessageCrudTexts.Messages.Validation.NotSender], errors);
    }

    [Fact]
    public void Sender_ValidateAccess_WithoutParticipant_ShouldReturnNotParticipant()
    {
        var errors = MessageCrudValidators.ValidateAccessSender(null, CreateMessage(Guid.CreateVersion7()));

        Assert.Equal([MessageCrudTexts.Messages.Validation.NotParticipant], errors);
    }
}
