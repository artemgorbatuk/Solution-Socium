using Datasource.Socium.Ef.Models;
using Services.Socium.Models;
using Services.Socium.Ports;
using Services.Socium.Texts;

namespace Services.Socium.Mapping;

public static class MessageMapper
{
    public static void Apply(Message model, string text)
    {
        model.Text = text;
    }

    public static MessageCreatePageResponse ToCreatePageResponse(Message model)
    {
        return new MessageCreatePageResponse
        {
            ChatId = model.ChatId,
            Text = model.Text,
        };
    }

    public static MessageUpdatePageResponse ToUpdatePageResponse(Message model)
    {
        return new MessageUpdatePageResponse
        {
            Id = model.Id,
            ChatId = model.ChatId,
            Text = model.Text,
            CreatedAt = model.CreatedAt,
        };
    }

    public static MessageInfoPageResponse ToInfoPageResponse(Message model)
    {
        return new MessageInfoPageResponse
        {
            Id = model.Id,
            ChatId = model.ChatId,
            Text = model.Text,
            CreatedAt = model.CreatedAt,
        };
    }

    public static MessageListModel ToListModel(Message model, UserPortModel? sender)
    {
        return new MessageListModel
        {
            Id = model.Id,
            Text = model.Text,
            CreatedAt = model.CreatedAt,
            SenderUserId = model.Sender.UserId,
            SenderName = sender?.Name ?? UserPortTexts.DeletedUserName,
        };
    }

    public static MessageDeletePageResponse ToDeletePageResponse(Message model)
    {
        return new MessageDeletePageResponse
        {
            Id = model.Id,
            ChatId = model.ChatId,
            Text = model.Text,
            CreatedAt = model.CreatedAt,
        };
    }
}
