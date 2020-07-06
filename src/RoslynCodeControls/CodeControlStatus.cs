namespace RoslynCodeControls
{
    public enum CodeControlStatus
    {
        Idle = 0,
        Rendering = 1,
        Rendered = 2,
        Reading,
        InputHandling
    }
}