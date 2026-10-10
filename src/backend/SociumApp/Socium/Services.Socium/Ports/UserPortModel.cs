namespace Services.Socium.Ports;

public class UserPortModel
{
    public required Guid Id { get; init; }
    public required string Login { get; init; }
    public required string Name { get; init; }
}
