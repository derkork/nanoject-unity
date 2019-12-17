namespace AncientLightStudios.Nanoject.Tests
{
    using System.Collections.Generic;

    public class DoorStorage
    {
        public IReadOnlyCollection<Door> GoldenDoors { get; }
        public IReadOnlyCollection<Door> SilverDoors { get; }
        public IReadOnlyCollection<Door> CopperDoors { get; }
        public IReadOnlyCollection<Door> AllDoors { get; }

        public DoorStorage(
            [Qualifier("goldenDoor")] IReadOnlyCollection<Door> goldenDoors,
            [Qualifier("silverDoor")] IReadOnlyCollection<Door> silverDoors,
            [Qualifier("copperDoor")] IReadOnlyCollection<Door> copperDoors,
            IReadOnlyCollection<Door> allDoors
        )
        {
            GoldenDoors = goldenDoors;
            SilverDoors = silverDoors;
            CopperDoors = copperDoors;
            AllDoors = allDoors;
        }
    }
}
