namespace AncientLightStudios.Nanoject.Tests
{
    using AncientLightStudios.Nanoject;

    public class Janitor
    {
        public House House { get; private set; }
        
        private Janitor()
        {
        }

        public static Janitor MakeJanitor()
        {
            return new Janitor();
        }

        [LateInit]
        public void LateInit(House house)
        {
            House = house;
        }
        
    }
}
