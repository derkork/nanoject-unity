namespace AncientLightStudios.Nanoject.Tests
{
    using Nanoject;

    [DependencyComponent]
    public class House
    {
        public Door Door { get; }

        public House(Door door)
        {
            Door = door;
        }
        
    }
}
