using System;
using System.Collections.Generic;
using System.Linq;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Central game state - manages all game data
    /// Based on VCMI's CGameState
    /// Plain C# class (no Unity dependencies) for easy testing and serialization
    /// </summary>
    [Serializable]
    public class GameState
    {
        // Game metadata
        public string GameName { get; set; } = "New Game";
        public int CurrentDay { get; private set; } = 1;
        public int CurrentPlayerTurn { get; private set; } = 0;

        // Collections
        private Dictionary<int, Player> players = new Dictionary<int, Player>();
        private Dictionary<int, Hero> heroes = new Dictionary<int, Hero>();
        // private Dictionary<int, Town> towns = new Dictionary<int, Town>(); // TODO: Phase 6
        // public GameMap Map { get; private set; } // TODO: Phase 3

        // ID generators
        private int nextHeroId = 1;
        private int nextPlayerId = 0;

        /// <summary>
        /// Initialize a new game
        /// </summary>
        public void Initialize(int playerCount, bool[] isHuman)
        {
            if (playerCount is < 1 or > 8)
                throw new ArgumentException("Player count must be between 1 and 8");

            CurrentDay = 1;
            CurrentPlayerTurn = 0;

            // Create players
            for (var i = 0; i < playerCount; i++)
            {
                var player = new Player(
                    id: i,
                    color: (PlayerColor)i,
                    name: $"Player {i + 1}",
                    isHuman: i < isHuman.Length && isHuman[i]
                );
                players[i] = player;
            }
        }

        #region Player Management

        /// <summary>
        /// Get player by ID
        /// </summary>
        public Player GetPlayer(int playerId)
        {
            if (players.TryGetValue(playerId, out var player))
                return player;
            return null;
        }

        /// <summary>
        /// Get all active players
        /// </summary>
        public IEnumerable<Player> GetActivePlayers()
        {
            return players.Values.Where(p => p.IsActive);
        }

        /// <summary>
        /// Get current turn player
        /// </summary>
        public Player GetCurrentPlayer()
        {
            return GetPlayer(CurrentPlayerTurn);
        }

        /// <summary>
        /// Get player by hero ID
        /// </summary>
        public Player GetPlayerByHero(int heroId)
        {
            var hero = GetHero(heroId);
            if (hero != null)
            {
                return GetPlayer(hero.Owner);
            }
            return null;
        }

        #endregion

        #region Hero Management

        /// <summary>
        /// Get hero by ID
        /// </summary>
        public Hero GetHero(int heroId)
        {
            return heroes.GetValueOrDefault(heroId);
        }

        /// <summary>
        /// Get all heroes for a player
        /// </summary>
        public IEnumerable<Hero> GetPlayerHeroes(int playerId)
        {
            return heroes.Values.Where(h => h.Owner == playerId);
        }

        /// <summary>
        /// Add a hero to the game
        /// </summary>
        public Hero AddHero(int typeId, int owner, Position position)
        {
            var hero = new Hero
            {
                Id = nextHeroId++,
                TypeId = typeId,
                Owner = owner,
                Position = position,
                Level = 1,
                Experience = 0
            };

            heroes[hero.Id] = hero;

            // Add to player's hero list
            var player = GetPlayer(owner);
            player?.HeroIds.Add(hero.Id);

            return hero;
        }

        /// <summary>
        /// Remove a hero from the game (defeat)
        /// </summary>
        public void RemoveHero(int heroId)
        {
            if (!heroes.TryGetValue(heroId, out var hero)) return;
            var player = GetPlayer(hero.Owner);
            player?.HeroIds.Remove(heroId);
            heroes.Remove(heroId);
        }

        #endregion

        #region Turn Management

        /// <summary>
        /// Advance to next turn
        /// </summary>
        public void NextTurn()
        {
            var activePlayers = GetActivePlayers().ToList();
            if (activePlayers.Count == 0)
                return;

            CurrentPlayerTurn++;
            if (CurrentPlayerTurn >= activePlayers.Count)
            {
                CurrentPlayerTurn = 0;
                NextDay();
            }
        }

        /// <summary>
        /// Advance to next day
        /// </summary>
        private void NextDay()
        {
            CurrentDay++;

            // Process daily events for all players
            foreach (var player in players.Values.Where(p => p.IsActive))
            {
                ProcessDailyIncome(player);
                RefreshPlayerHeroes(player);
                // TODO: Process town growth, building construction, etc.
            }
        }

        /// <summary>
        /// Process daily income for a player
        /// </summary>
        private void ProcessDailyIncome(Player player)
        {
            var income = player.CalculateDailyIncome();
            player.AddResources(income);
        }

        /// <summary>
        /// Refresh all heroes' movement points for a player
        /// </summary>
        private void RefreshPlayerHeroes(Player player)
        {
            foreach (var heroId in player.HeroIds)
            {
                var hero = GetHero(heroId);
                if (hero != null)
                {
                    hero.RefreshMovement();
                    hero.RefreshMana();
                }
            }
        }

        /// <summary>
        /// End current player's turn
        /// </summary>
        public void EndTurn()
        {
            NextTurn();
        }

        #endregion

        #region Victory Conditions

        /// <summary>
        /// Check if the game is over
        /// </summary>
        public bool IsGameOver()
        {
            var activePlayers = GetActivePlayers().ToList();
            return activePlayers.Count <= 1;
        }

        /// <summary>
        /// Get the winning player (if any)
        /// </summary>
        public Player GetWinner()
        {
            var activePlayers = GetActivePlayers().ToList();
            return activePlayers.Count == 1 ? activePlayers[0] : null;
        }

        /// <summary>
        /// Eliminate a player from the game
        /// </summary>
        public void EliminatePlayer(int playerId)
        {
            var player = GetPlayer(playerId);
            if (player != null)
            {
                player.IsActive = false;

                // Remove all heroes
                foreach (var heroId in player.HeroIds.ToList())
                {
                    RemoveHero(heroId);
                }

                // TODO: Remove towns, transfer resources, etc.
            }
        }

        #endregion

        #region Serialization Helpers

        /// <summary>
        /// Get serializable state summary
        /// </summary>
        public string GetStateSummary()
        {
            return $"Day {CurrentDay} - {players.Count} players, {heroes.Count} heroes";
        }

        #endregion
    }
}
