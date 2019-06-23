namespace AncientLightStudios.Nanoject.Tests
{
    using AncientLightStudios.Nanoject;

    [DependencyComponent]
    public class Door
    {
        public bool Locked { get; set; } = true;

    }
}