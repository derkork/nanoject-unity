namespace AncientLightStudios.Nanoject.Tests
{
    using System.Collections.Generic;

    public class Guard
    {
        public IReadOnlyCollection<House> Houses { get; private set; }
        
        private Guard()
        {
            
        }

        public static Guard MakeGuard()
        {
            return new Guard();
        }
        
        [LateInit]
        public void LateInit(IReadOnlyCollection<House> houses)
        {
            Houses = houses;
        }
    }
}
