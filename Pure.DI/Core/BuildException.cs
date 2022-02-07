namespace Pure.DI.Core;

public class BuildException : Exception
{
    public BuildException(string id, string message, Location? location = default)
        : base(message)
    {
        Id = id;
        Location = location;
    }

    public string Id { get; }

    public Location? Location { get; }
}