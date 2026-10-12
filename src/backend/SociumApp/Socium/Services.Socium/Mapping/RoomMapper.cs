using Datasource.Socium.Ef.Models;
using Repositories.Socium.Ef.Options;
using Services.Socium.Enums;
using Services.Socium.Events;
using Services.Socium.Models;
using Services.Socium.Ports;

namespace Services.Socium.Mapping;

public static class RoomMapper
{
    public static void Apply(Room model, string name)
    {
        model.Name = name;
    }

    public static RoomQueryOptions ToQueryOptions(
        string? name = null,
        Guid? excludeId = null,
        IReadOnlyCollection<Guid>? ids = null)
    {
        return new RoomQueryOptions
        {
            Name = name,
            ExcludeId = excludeId,
            Ids = ids,
        };
    }

    public static RoomCreatedEvent ToCreatedEvent(Guid roomId, Guid creatorUserId)
    {
        return new RoomCreatedEvent
        {
            RoomId = roomId,
            CreatorUserId = creatorUserId,
        };
    }

    public static RoomChangedEvent ToChangedEvent(RoomChangedKind kind, Guid roomId, string? name = null)
    {
        return new RoomChangedEvent
        {
            Kind = kind,
            RoomId = roomId,
            Name = name,
        };
    }

    public static ChatNotifyRoomChangedModel ToNotifyRoomChanged(RoomChangedEvent domainEvent)
    {
        return new ChatNotifyRoomChangedModel
        {
            Kind = ToNotifyKind(domainEvent.Kind),
            RoomId = domainEvent.RoomId,
            Name = domainEvent.Name,
        };
    }

    private static string ToNotifyKind(RoomChangedKind kind) => kind switch
    {
        RoomChangedKind.Created => "created",
        RoomChangedKind.Updated => "updated",
        RoomChangedKind.Deleted => "deleted",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    public static RoomCreatePageResponse ToCreatePageResponse(Room model, bool canCreate)
    {
        return new RoomCreatePageResponse
        {
            Name = model.Name,
            CanCreate = canCreate,
        };
    }

    public static RoomUpdatePageResponse ToUpdatePageResponse(Room model, bool canUpdate)
    {
        return new RoomUpdatePageResponse
        {
            Id = model.Id,
            Name = model.Name,
            CanUpdate = canUpdate,
        };
    }

    public static RoomInfoPageResponse ToInfoPageResponse(Room model, bool canUpdate, bool canDelete)
    {
        return new RoomInfoPageResponse
        {
            Id = model.Id,
            Name = model.Name,
            CanUpdate = canUpdate,
            CanDelete = canDelete,
        };
    }

    public static RoomListModel ToListModel(Room model, bool canUpdate, bool canDelete)
    {
        return new RoomListModel
        {
            Id = model.Id,
            Name = model.Name,
            CanUpdate = canUpdate,
            CanDelete = canDelete,
        };
    }

    public static RoomDeletePageResponse ToDeletePageResponse(Room model, int chatCount, bool canDelete)
    {
        return new RoomDeletePageResponse
        {
            Id = model.Id,
            Name = model.Name,
            ChatCount = chatCount,
            CanDelete = canDelete,
        };
    }

    public static RoomDeleteResponse ToDeleteResponse(bool isDeleted = true)
    {
        return new RoomDeleteResponse
        {
            IsDeleted = isDeleted,
        };
    }

    public static RoomListPageResponse ToListPageResponse(ICollection<RoomListModel> rows)
    {
        return new RoomListPageResponse
        {
            Rows = rows,
            RowExists = rows.Count > 0,
            RowCount = rows.Count,
        };
    }
}
