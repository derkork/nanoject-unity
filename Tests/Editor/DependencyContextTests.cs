namespace AncientLightStudios.Nanoject.Tests
{
    using System;
    using Nanoject;
    using NUnit.Framework;

    [TestFixture]
    public class DependencyContextTests
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
            _context.Declare(Janitor.MakeJanitor());
            
            // when
            _context.Resolve();
            var janitor = _context.Get<Janitor>();
            
            // then
            Assert.NotNull(janitor);
            Assert.NotNull(janitor.House);
        }

        [Test]
        public void LateInitWithCollectionsWorks()
        {
            // setup
            _context.Declare<Door>();
            _context.Declare<House>();
            _context.Declare<House>();
            _context.Declare(Guard.MakeGuard());
            
            // when
            _context.Resolve();
            var guard = _context.Get<Guard>();

            // then
            Assert.AreEqual(2, guard.Houses.Count);
        }

        [Test]
        public void PolymorphismWorks()
        {
            // setup
            _context.DeclareQualified<Door>("goldenDoor");
            _context.Declare<Palace>();
            _context.Declare(Janitor.MakeJanitor());
            
            // when
            _context.Resolve();
            var janitor = _context.Get<Janitor>();
            
            // then
            // a palace also is a house, so this janitor gets the palace
            Assert.IsNotNull(janitor.House);
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

        [Test]
        public void InjectingAnEmptyListWorks()
        {
            // setup
            _context.Declare<HouseKeeper>();
            
            // when
            _context.Resolve();
            var keeper = _context.Get<HouseKeeper>();
            
            // then
            Assert.IsEmpty(keeper.Houses);
        }

        [Test]
        public void DeclaringManyDependenciesWithoutQualifierWorks()
        {
            // setup
            _context.Declare<House>();
            _context.Declare<House>();
            _context.Declare<House>();
            // three houses share a door
            _context.Declare<Door>();

            // when
            _context.Resolve();
            
            // then
            Assert.AreEqual(3, _context.GetAll<House>().Count);
        }

        [Test]
        public void DeclaringManyDependenciesWithoutQualifierThrowsException()
        {
            // setup
            _context.Declare<House>();
            _context.Declare<Door>();
            _context.Declare<Door>();
            
            // expect
            Assert.Throws<InvalidOperationException>(() => _context.Resolve());
        }

        [Test]
        public void DeclaringManyDependenciesWithQualifierWorks()
        {
            var theGoldenDoor = new Door();
            _context.DeclareQualified("goldenDoor", theGoldenDoor);
            _context.Declare<Door>();
            _context.Declare<Palace>();
            
            // when
            _context.Resolve();
            
            // then
            var palace = _context.Get<Palace>();
            // the palace gets the golden door, by qualifier
            Assert.AreSame(theGoldenDoor, palace.Door);
        }

        [Test]
        public void InjectingAListWorks()
        {
            // setup
            // declare the keeper and two houses
            _context.Declare<HouseKeeper>();
            _context.DeclareQualified("palace", new House(new Door()));
            _context.DeclareQualified("hut", new House(new Door()));
            
            // when
            _context.Resolve();
            var keeper = _context.Get<HouseKeeper>();
            
            // then
            // the houses should be in the list of the keeper
            Assert.AreEqual(2, keeper.Houses.Count);
        }

        
        [Test]
        public void ListsObeyQualifiers()
        {
            _context.DeclareQualified<Door>("goldenDoor");
            _context.DeclareQualified<Door>("silverDoor");
            _context.DeclareQualified<Door>("copperDoor");
            _context.DeclareQualified<Door>("diamondDoor");
            _context.Declare<DoorStorage>();
            
            // when
            _context.Resolve();
            var storage = _context.Get<DoorStorage>();
            
            // then
            Assert.AreEqual(1, storage.GoldenDoors.Count);
            Assert.AreEqual(1, storage.SilverDoors.Count);
            Assert.AreEqual(1, storage.CopperDoors.Count);
            Assert.AreEqual(4, storage.AllDoors.Count);

        }
    }
}
