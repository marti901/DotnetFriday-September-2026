namespace MonkeyBook.Shared;

public class Post
{
	public required Guid Id { get; set; }
	public required Guid MonkeyId { get; set; }
	public required string Message { get; set; }
	public required DateTime Created { get; set; }
}
