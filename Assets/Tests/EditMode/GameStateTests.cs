using NUnit.Framework;
using RealmsOfEldor.Core;

namespace RealmsOfEldor.Tests
{
    [TestFixture]
    public class GameStateTests
    {
        [Test]
        public void GameState_Initialize_CreatesPlayers()
        {
            // Arrange
            var gameState = new GameState();

            // Act
            gameState.Initialize(playerCount: 2, isHuman: new[] { true, false });

            // Assert
            Assert.AreEqual(2, gameState.GetActivePlayers().Count());
        }

        [Test]
        public void GameState_Initialize_SetsFirstPlayerTurn()
        {
            // Arrange
            var gameState = new GameState();

            // Act
            gameState.Initialize(playerCount: 2, isHuman: new[] { true, false });

            // Assert
            Assert.AreEqual(0, gameState.GetCurrentPlayer().Id);
        }

        [Test]
        public void GameState_NextTurn_AdvancesToNextPlayer()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Initialize(playerCount: 2, isHuman: new[] { true, false });

            // Act
            gameState.NextTurn();

            // Assert
            Assert.AreEqual(1, gameState.GetCurrentPlayer().Id);
        }

        [Test]
        public void GameState_NextTurn_WrapsAroundToFirstPlayer()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Initialize(playerCount: 2, isHuman: new[] { true, false });

            // Act
            gameState.NextTurn(); // Player 1
            gameState.NextTurn(); // Back to Player 0

            // Assert
            Assert.AreEqual(0, gameState.GetCurrentPlayer().Id);
        }

        [Test]
        public void GameState_NextTurn_AdvancesDayAfterAllPlayersTurn()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Initialize(playerCount: 2, isHuman: new[] { true, false });
            int startDay = gameState.CurrentDay;

            // Act
            gameState.NextTurn(); // Player 1
            gameState.NextTurn(); // Player 0, new day

            // Assert
            Assert.AreEqual(startDay + 1, gameState.CurrentDay);
        }

        [Test]
        public void GameState_AddHero_CreatesHero()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Initialize(playerCount: 1, isHuman: new[] { true });

            // Act
            var hero = gameState.AddHero(typeId: 1, owner: 0, position: new Position(5, 5));

            // Assert
            Assert.IsNotNull(hero);
            Assert.AreEqual(1, hero.TypeId);
            Assert.AreEqual(0, hero.Owner);
            Assert.AreEqual(new Position(5, 5), hero.Position);
        }

        [Test]
        public void GameState_AddHero_AddsToPlayerHeroList()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Initialize(playerCount: 1, isHuman: new[] { true });

            // Act
            var hero = gameState.AddHero(typeId: 1, owner: 0, position: new Position(5, 5));
            var player = gameState.GetPlayer(0);

            // Assert
            Assert.Contains(hero.Id, player.HeroIds);
        }

        [Test]
        public void GameState_GetHero_ReturnsCorrectHero()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Initialize(playerCount: 1, isHuman: new[] { true });
            var hero = gameState.AddHero(typeId: 1, owner: 0, position: new Position(5, 5));

            // Act
            var retrievedHero = gameState.GetHero(hero.Id);

            // Assert
            Assert.AreEqual(hero, retrievedHero);
        }

        [Test]
        public void GameState_RemoveHero_RemovesHero()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Initialize(playerCount: 1, isHuman: new[] { true });
            var hero = gameState.AddHero(typeId: 1, owner: 0, position: new Position(5, 5));

            // Act
            gameState.RemoveHero(hero.Id);

            // Assert
            Assert.IsNull(gameState.GetHero(hero.Id));
        }

        [Test]
        public void GameState_RemoveHero_RemovesFromPlayerList()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Initialize(playerCount: 1, isHuman: new[] { true });
            var hero = gameState.AddHero(typeId: 1, owner: 0, position: new Position(5, 5));
            var player = gameState.GetPlayer(0);

            // Act
            gameState.RemoveHero(hero.Id);

            // Assert
            Assert.IsFalse(player.HeroIds.Contains(hero.Id));
        }

        [Test]
        public void GameState_EliminatePlayer_DeactivatesPlayer()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Initialize(playerCount: 2, isHuman: new[] { true, false });

            // Act
            gameState.EliminatePlayer(0);

            // Assert
            Assert.IsFalse(gameState.GetPlayer(0).IsActive);
        }

        [Test]
        public void GameState_IsGameOver_ReturnsTrueWithOnePlayer()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Initialize(playerCount: 2, isHuman: new[] { true, false });

            // Act
            gameState.EliminatePlayer(0);

            // Assert
            Assert.IsTrue(gameState.IsGameOver());
        }

        [Test]
        public void GameState_IsGameOver_ReturnsFalseWithMultiplePlayers()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Initialize(playerCount: 2, isHuman: new[] { true, false });

            // Act & Assert
            Assert.IsFalse(gameState.IsGameOver());
        }

        [Test]
        public void GameState_GetWinner_ReturnsLastActivePlayer()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Initialize(playerCount: 2, isHuman: new[] { true, false });

            // Act
            gameState.EliminatePlayer(0);
            var winner = gameState.GetWinner();

            // Assert
            Assert.IsNotNull(winner);
            Assert.AreEqual(1, winner.Id);
        }
    }
}
