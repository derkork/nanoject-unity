namespace AncientLightStudios.Nanoject.Tests
{
    using Nanoject;

    [DependencyComponent]
    public class Door
    {
        public bool Locked { get; set; } = true;

    }
}
