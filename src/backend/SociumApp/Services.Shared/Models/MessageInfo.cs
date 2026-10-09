namespace Services.Shared.Models;

public struct MessageInfo
{
    public MessageInfo() { }

    public required int MessageType { get; set; } = 0; // MessageType.NONE
    public required string MessageText { get; set; }
}
