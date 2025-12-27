namespace Shared;

public class Patch(params string[] paths) : HttpAttribute(paths)
{
}