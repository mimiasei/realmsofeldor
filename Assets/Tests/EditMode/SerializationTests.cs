using NUnit.Framework;
using Newtonsoft.Json;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Serialization;

namespace RealmsOfEldor.Tests
{
    public class SerializationTests
    {
        [Test]
        public void Position_SerializesToJson_Correctly()
        {
            var position = new Position(10, 25);

            var json = JsonConvert.SerializeObject(position);

            Assert.IsTrue(json.Contains("\"x\":10"));
            Assert.IsTrue(json.Contains("\"y\":25"));
        }

        [Test]
        public void Position_DeserializesFromJson_Correctly()
        {
            var json = "{\"x\":10,\"y\":25}";

            var position = JsonConvert.DeserializeObject<Position>(json);

            Assert.AreEqual(10, position.X);
            Assert.AreEqual(25, position.Y);
        }

        [Test]
        public void ResourceSet_SerializesToJson_Correctly()
        {
            var resources = new ResourceSet(
                gold: 1000,
                wood: 20,
                ore: 15,
                crystal: 5,
                gems: 3,
                sulfur: 2,
                mercury: 1
            );

            var json = JsonConvert.SerializeObject(resources);

            Assert.IsTrue(json.Contains("\"gold\":1000"));
            Assert.IsTrue(json.Contains("\"wood\":20"));
            Assert.IsTrue(json.Contains("\"ore\":15"));
            Assert.IsTrue(json.Contains("\"crystal\":5"));
        }

        [Test]
        public void ResourceSet_DeserializesFromJson_Correctly()
        {
            var json = "{\"gold\":1000,\"wood\":20,\"ore\":15,\"crystal\":5,\"gems\":3,\"sulfur\":2,\"mercury\":1}";

            var resources = JsonConvert.DeserializeObject<ResourceSet>(json);

            Assert.AreEqual(1000, resources.Gold);
            Assert.AreEqual(20, resources.Wood);
            Assert.AreEqual(15, resources.Ore);
            Assert.AreEqual(5, resources.Crystal);
            Assert.AreEqual(3, resources.Gems);
            Assert.AreEqual(2, resources.Sulfur);
            Assert.AreEqual(1, resources.Mercury);
        }

        [Test]
        public void GameState_SerializesToJson_WithDictionaries()
        {
            var gameState = new GameState { GameName = "Test Game" };
            gameState.Initialize(playerCount: 2, isHuman: new[] { true, false });

            // Add a hero with position
            var hero = gameState.AddHero(heroTypeId: 1, ownerId: 0, position: new Position(5, 10));

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            var json = JsonConvert.SerializeObject(gameState, settings);

            // Verify JSON contains expected data
            Assert.IsTrue(json.Contains("Test Game"));
            Assert.IsNotEmpty(json);

            // Should contain hero data
            Assert.IsTrue(json.Contains("heroes") || json.Contains("Heroes"));
        }

        [Test]
        public void GameState_DeserializesFromJson_WithDictionaries()
        {
            // Create and serialize a game state
            var originalState = new GameState { GameName = "Saved Game" };
            originalState.Initialize(playerCount: 2, isHuman: new[] { true, false });
            var hero = originalState.AddHero(heroTypeId: 1, ownerId: 0, position: new Position(15, 20));
            hero.GainExperience(500);

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            var json = JsonConvert.SerializeObject(originalState, settings);

            // Deserialize
            var loadedState = JsonConvert.DeserializeObject<GameState>(json, settings);

            Assert.IsNotNull(loadedState);
            Assert.AreEqual("Saved Game", loadedState.GameName);
            Assert.AreEqual(2, loadedState.GetActivePlayers().Count());

            var loadedHero = loadedState.GetHero(hero.Id);
            Assert.IsNotNull(loadedHero);
            Assert.AreEqual(15, loadedHero.Position.X);
            Assert.AreEqual(20, loadedHero.Position.Y);
            Assert.AreEqual(500, loadedHero.Experience);
        }

        [Test]
        public void Hero_WithArmy_SerializesCorrectly()
        {
            var gameState = new GameState();
            gameState.Initialize(1, new[] { true });
            var hero = gameState.AddHero(1, 0, new Position(0, 0));

            // Add creatures to army
            hero.Army.AddCreature(creatureId: 1, count: 10, slotIndex: 0);
            hero.Army.AddCreature(creatureId: 2, count: 5, slotIndex: 1);

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            var json = JsonConvert.SerializeObject(gameState, settings);
            var loadedState = JsonConvert.DeserializeObject<GameState>(json, settings);

            var loadedHero = loadedState.GetHero(hero.Id);
            Assert.AreEqual(10, loadedHero.Army.GetSlot(0).Count);
            Assert.AreEqual(5, loadedHero.Army.GetSlot(1).Count);
        }

        [Test]
        public void Player_WithResources_SerializesCorrectly()
        {
            var gameState = new GameState();
            gameState.Initialize(1, new[] { true });
            var player = gameState.GetPlayer(0);

            player.Resources = new ResourceSet(5000, 100, 75, 20, 15, 10, 5);

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            var json = JsonConvert.SerializeObject(gameState, settings);
            var loadedState = JsonConvert.DeserializeObject<GameState>(json, settings);

            var loadedPlayer = loadedState.GetPlayer(0);
            Assert.AreEqual(5000, loadedPlayer.Resources.Gold);
            Assert.AreEqual(100, loadedPlayer.Resources.Wood);
            Assert.AreEqual(75, loadedPlayer.Resources.Ore);
        }
    }
}
