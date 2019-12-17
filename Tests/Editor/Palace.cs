namespace AncientLightStudios.Nanoject.Tests
{
    public class Palace : House
    {
        public Palace([Qualifier("goldenDoor")] Door door) : base(door)
        {
        }
    }
}
