
namespace AncientLightStudios.Nanoject.Tests
{
    using AncientLightStudios.Nanoject;
    using NUnit.Framework;

    [TestFixture]
    public class Tests
    {
        private DependencyContext _context;

        [SetUp]
        public void SetUp()
        {
            _context = new DependencyContext();
        }
        
        [Test]
        public void TestConstruction()
        {
            // setup
            _context.Declare<House>();
            _context.Declare<Door>();
            
            // when
            _context.Resolve();
            var house = _context.Get<House>();
            
            // then
            Assert.NotNull(house);
            Assert.NotNull(house.Door);
            Assert.IsTrue(house.Door.Locked);
        }

        [Test]
        public void TestPredefinedObjectIsUsed()
        {
            // setup
            _context.Declare(new Door {Locked = false});
            _context.Declare<House>();
            
            // when
            _context.Resolve();
            var house = _context.Get<House>();
            
            // then
            Assert.IsFalse(house.Door.Locked);
        }

        [Test]
        public void TestLateInitWorks()
        {
            // setup
            _context.Declare<Door>();
            _context.Declare<House>();
            _context.Declare(Warden.MakeWarden());
            
            // when
            _context.Resolve();
            var warden = _context.Get<Warden>();
            
            // then
            Assert.NotNull(warden);
            Assert.NotNull(warden.House);
        }

        [Test]
        public void ScanningWorks()
        {
            // setup
            _context.DeclareAnnotatedComponents();
            
            // when
            _context.Resolve();
            var house = _context.Get<House>();
            var door = _context.Get<Door>();
            
            // then
            Assert.NotNull(house);
            Assert.NotNull(door);
            Assert.AreSame(door, house.Door);

        }
    }
}
