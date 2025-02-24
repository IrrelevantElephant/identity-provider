namespace Database;

public record DatabaseSettings
{
    public required string ConnectionString { get; init; }
}