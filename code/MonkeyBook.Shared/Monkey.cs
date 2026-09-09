namespace MonkeyBook.Shared;

public class Monkey
{
	public required Guid Id { get; set; }
	public required string Name { get; set; }
	public required List<Post> Posts { get; set; }
}
