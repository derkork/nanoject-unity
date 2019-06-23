namespace AncientLightStudios.Nanoject.Tests
{
    using AncientLightStudios.Nanoject;

    public class Warden
    {
        public House House { get; private set; }
        
        private Warden()
        {
        }

        public static Warden MakeWarden()
        {
            return new Warden();
        }

        [LateInit]
        public void LateInit(House house)
        {
            House = house;
        }
        
    }
}