namespace Shared;

public class Post(params string[] paths) : HttpAttribute(paths)
{
}