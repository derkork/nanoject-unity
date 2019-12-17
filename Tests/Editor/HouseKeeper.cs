namespace AncientLightStudios.Nanoject.Tests
{
    using System.Collections.Generic;

    [DependencyComponent]
    public class HouseKeeper
    {
        public IReadOnlyCollection<House> Houses { get; }

        public HouseKeeper(IReadOnlyCollection<House> houses)
        {
            Houses = houses;
        }
    }
}
