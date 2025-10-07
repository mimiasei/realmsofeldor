using System.Collections.Generic;
using UnityEngine;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;
using RealmsOfEldor.Core.Events;
using RealmsOfEldor.Core.Events.EventChannels;
using RealmsOfEldor.Utilities;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Central input controller for adventure map interactions.
    /// Handles tile clicks, keyboard shortcuts, and hero movement commands.
    /// Based on VCMI's AdventureMapInterface pattern.
    /// </summary>
    public class AdventureMapInputController : MonoBehaviour
    {
        public enum InputState
        {
            Normal,             // Default exploration mode
            SpellCasting,       // Selecting spell target
            Disabled            // Input disabled (e.g., AI turn)
        }

        [Header("Event Channels")]
        [SerializeField] private MapEventChannel mapEvents;
        [SerializeField] private GameEventChannel gameEvents;
        [SerializeField] private UIEventChannel uiEvents;

        [Header("References")]
        [SerializeField] private MapRenderer mapRenderer;
        [SerializeField] private CameraController cameraController;

        private GameMap gameMap;

        [Header("Input Settings")]
        [SerializeField] private bool enableKeyboardShortcuts = true;
        [SerializeField] private float doubleTapThreshold = 0.3f;

        private InputState currentState = InputState.Normal;
        private Hero selectedHero;
        private int castingSpellId = -1;
        private float lastTapTime;
        private Position lastTappedPosition;

        void OnEnable()
        {
            if (mapEvents != null)
            {
                mapEvents.OnTileSelected += HandleTileClicked;
                mapEvents.OnMapLoaded += HandleMapLoaded;
            }

            if (uiEvents != null)
            {
                uiEvents.OnHeroSelected += HandleHeroSelected;
                uiEvents.OnSelectionCleared += HandleSelectionCleared;
                uiEvents.OnEnterSpellCastingMode += HandleEnterSpellCasting;
                uiEvents.OnExitSpellCastingMode += HandleExitSpellCasting;
                uiEvents.OnEndTurnButtonClicked += HandleEndTurnClicked;
                uiEvents.OnNextHeroButtonClicked += HandleNextHeroClicked;
                uiEvents.OnSleepWakeButtonClicked += HandleSleepWakeClicked;
            }

            if (gameEvents != null)
            {
                gameEvents.OnTurnChanged += HandleTurnChanged;
            }
        }

        void OnDisable()
        {
            if (mapEvents != null)
            {
                mapEvents.OnTileSelected -= HandleTileClicked;
                mapEvents.OnMapLoaded -= HandleMapLoaded;
            }

            if (uiEvents != null)
            {
                uiEvents.OnHeroSelected -= HandleHeroSelected;
                uiEvents.OnSelectionCleared -= HandleSelectionCleared;
                uiEvents.OnEnterSpellCastingMode -= HandleEnterSpellCasting;
                uiEvents.OnExitSpellCastingMode -= HandleExitSpellCasting;
                uiEvents.OnEndTurnButtonClicked -= HandleEndTurnClicked;
                uiEvents.OnNextHeroButtonClicked -= HandleNextHeroClicked;
                uiEvents.OnSleepWakeButtonClicked -= HandleSleepWakeClicked;
            }

            if (gameEvents != null)
            {
                gameEvents.OnTurnChanged -= HandleTurnChanged;
            }
        }

        void Update()
        {
            if (!enableKeyboardShortcuts || currentState == InputState.Disabled)
                return;

            HandleKeyboardShortcuts();
        }

        /// <summary>
        /// Handles keyboard shortcuts (VCMI-style).
        /// </summary>
        private void HandleKeyboardShortcuts()
        {
            // End turn
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (CanEndTurn())
                    EndTurn();
            }

            // Next hero
            if (Input.GetKeyDown(KeyCode.H))
            {
                SelectNextHero();
            }

            // Sleep/wake hero
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (selectedHero != null)
                    ToggleHeroSleep();
            }

            // Spellbook
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (selectedHero != null)
                    OpenSpellbook();
            }

            // Center on selected hero
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (selectedHero != null && cameraController != null)
                    cameraController.CenterOn(mapRenderer.MapToWorldPosition(selectedHero.Position));
            }

            // Arrow key movement (8-directional)
            if (selectedHero != null)
            {
                var direction = Vector2Int.zero;

                if (Input.GetKeyDown(KeyCode.UpArrow))
                    direction += Vector2Int.up;
                if (Input.GetKeyDown(KeyCode.DownArrow))
                    direction += Vector2Int.down;
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                    direction += Vector2Int.left;
                if (Input.GetKeyDown(KeyCode.RightArrow))
                    direction += Vector2Int.right;

                if (direction != Vector2Int.zero)
                    MoveHeroInDirection(direction);
            }

            // Escape - cancel actions
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentState == InputState.SpellCasting)
                {
                    ExitSpellCastingMode();
                }
                else if (selectedHero != null)
                {
                    ClearSelection();
                }
            }
        }

        /// <summary>
        /// Handles tile click events from MapRenderer.
        /// </summary>
        private void HandleTileClicked(Position tilePos)
        {
            if (currentState == InputState.Disabled)
                return;

            if (currentState == InputState.SpellCasting)
            {
                CastSpellAtPosition(tilePos);
                return;
            }

            // Check for double-tap (center camera)
            if (IsDoubleTap(tilePos))
            {
                if (cameraController != null)
                    cameraController.CenterOn(mapRenderer.MapToWorldPosition(tilePos));
                return;
            }

            // Normal click handling
            HandleNormalTileClick(tilePos);
        }

        private void HandleMapLoaded(GameMap map)
        {
            gameMap = map;
            Debug.Log($"AdventureMapInputController: Map loaded ({map.Width}x{map.Height})");
        }

        private void HandleNormalTileClick(Position tilePos)
        {
            if (GameStateManager.Instance == null)
                return;

            if (gameMap == null || !gameMap.IsInBounds(tilePos))
                return;

            // Check for hero at position
            var heroAtPos = GetHeroAtPosition(tilePos);
            if (heroAtPos != null)
            {
                SelectHero(heroAtPos);
                return;
            }

            // Check for map object
            var objectsAtPos = gameMap.GetObjectsAt(tilePos);
            if (objectsAtPos.Count > 0)
            {
                HandleObjectClick(objectsAtPos[0], tilePos);
                return;
            }

            // Move selected hero to position
            if (selectedHero != null)
            {
                MoveHeroToPosition(tilePos);
            }
        }

        private void MoveHeroToPosition(Position targetPos)
        {
            if (selectedHero == null || GameStateManager.Instance == null)
                return;

            if (gameMap == null)
                return;

            // Use BasicPathfinder to find path
            var path = BasicPathfinder.FindPath(gameMap, selectedHero.Position, targetPos);
            if (path == null)
            {
                uiEvents?.RaiseShowStatusMessage("Cannot reach that tile!");
                return;
            }

            // Check movement cost
            var movementCost = BasicPathfinder.CalculatePathCost(gameMap, path);
            if (movementCost > selectedHero.Movement)
            {
                uiEvents?.RaiseShowStatusMessage($"Not enough movement! Need {movementCost}, have {selectedHero.Movement}");
                return;
            }

            // Execute movement
            var oldPos = selectedHero.Position;
            selectedHero.Position = targetPos;
            selectedHero.Movement -= movementCost;

            // Raise event
            gameEvents?.RaiseHeroMoved(selectedHero.Id, targetPos);

            // Update UI
            uiEvents?.RaiseShowHeroInfo(selectedHero);
        }

        private void MoveHeroInDirection(Vector2Int direction)
        {
            if (selectedHero == null)
                return;

            var targetPos = new Position(
                selectedHero.Position.X + direction.x,
                selectedHero.Position.Y + direction.y
            );

            MoveHeroToPosition(targetPos);
        }

        private void HandleObjectClick(MapObject mapObject, Position clickPos)
        {
            if (selectedHero == null)
                return;

            // Check if hero is adjacent to object
            if (!IsAdjacent(selectedHero.Position, clickPos))
            {
                // Move hero toward object
                MoveHeroToPosition(clickPos);
                return;
            }

            // Visit object
            VisitObject(mapObject);
        }

        private void VisitObject(MapObject mapObject)
        {
            if (selectedHero == null)
                return;

            Debug.Log($"{selectedHero.CustomName} visits {mapObject.ObjectType} at {mapObject.Position}");

            // Trigger object interaction
            mapObject.OnVisit(selectedHero);

            // Raise event
            mapEvents?.RaiseObjectVisited(selectedHero, mapObject);
        }

        // ===== Hero Selection =====

        private void SelectHero(Hero hero)
        {
            selectedHero = hero;
            uiEvents?.RaiseHeroSelected(hero);

            // Center camera on hero
            if (cameraController != null && mapRenderer != null)
            {
                cameraController.CenterOn(mapRenderer.MapToWorldPosition(hero.Position));
            }
        }

        private void ClearSelection()
        {
            selectedHero = null;
            uiEvents?.RaiseSelectionCleared();
        }

        private void SelectNextHero()
        {
            if (GameStateManager.Instance == null)
                return;

            var gameState = GameStateManager.Instance.State;
            var currentPlayer = gameState.GetCurrentPlayer();
            if (currentPlayer == null || currentPlayer.HeroIds.Count == 0)
                return;

            // Get actual hero objects from GameState
            var playerHeroes = new List<Hero>();
            foreach (var heroId in currentPlayer.HeroIds)
            {
                var hero = gameState.GetHero(heroId);
                if (hero != null)
                    playerHeroes.Add(hero);
            }

            if (playerHeroes.Count == 0)
                return;

            // Find next hero
            var currentIndex = selectedHero != null
                ? playerHeroes.FindIndex(h => h.Id == selectedHero.Id)
                : -1;

            var nextIndex = (currentIndex + 1) % playerHeroes.Count;
            SelectHero(playerHeroes[nextIndex]);
        }

        // ===== Spell Casting =====

        private void EnterSpellCastingMode(int spellId)
        {
            currentState = InputState.SpellCasting;
            castingSpellId = spellId;

            uiEvents?.RaiseShowStatusMessage("Select target for spell...");
        }

        private void ExitSpellCastingMode()
        {
            currentState = InputState.Normal;
            castingSpellId = -1;

            uiEvents?.RaiseExitSpellCastingMode();
        }

        private void CastSpellAtPosition(Position targetPos)
        {
            if (selectedHero == null || castingSpellId == -1)
                return;

            Debug.Log($"{selectedHero.CustomName} casts spell {castingSpellId} at {targetPos}");

            // TODO: Implement spell casting when spell system is ready

            ExitSpellCastingMode();
        }

        // ===== Button Handlers =====

        private void HandleHeroSelected(Hero hero)
        {
            selectedHero = hero;
        }

        private void HandleSelectionCleared()
        {
            selectedHero = null;
        }

        private void HandleEnterSpellCasting(int spellId)
        {
            EnterSpellCastingMode(spellId);
        }

        private void HandleExitSpellCasting()
        {
            ExitSpellCastingMode();
        }

        private void HandleEndTurnClicked()
        {
            EndTurn();
        }

        private void HandleNextHeroClicked()
        {
            SelectNextHero();
        }

        private void HandleSleepWakeClicked()
        {
            ToggleHeroSleep();
        }

        private void HandleTurnChanged(int playerId)
        {
            // Enable input when it's the human player's turn
            // For now, assume player 0 is human
            if (playerId == 0)
            {
                currentState = InputState.Normal;
            }
            else
            {
                currentState = InputState.Disabled;
                ClearSelection();
            }
        }

        // ===== Action Methods =====

        private void EndTurn()
        {
            if (!CanEndTurn())
                return;

            GameStateManager.Instance?.EndTurn();
        }

        private void ToggleHeroSleep()
        {
            if (selectedHero == null)
                return;

            // TODO: Implement hero sleep state when ready
            Debug.Log($"Toggle sleep for {selectedHero.CustomName}");
        }

        private void OpenSpellbook()
        {
            if (selectedHero == null)
                return;

            // TODO: Open spellbook UI when ready
            Debug.Log($"Open spellbook for {selectedHero.CustomName}");
        }

        // ===== Validation Methods =====

        private bool CanMoveHeroToPosition(Hero hero, Position targetPos)
        {
            if (GameStateManager.Instance == null || gameMap == null)
                return false;

            // Use BasicPathfinder for validation
            return BasicPathfinder.CanReachPosition(gameMap, hero, targetPos);
        }

        private bool CanEndTurn()
        {
            return currentState == InputState.Normal && GameStateManager.Instance != null;
        }

        // ===== Utility Methods =====

        private Hero GetHeroAtPosition(Position pos)
        {
            if (GameStateManager.Instance == null)
                return null;

            var gameState = GameStateManager.Instance.State;
            var currentPlayer = gameState.GetCurrentPlayer();
            if (currentPlayer == null)
                return null;

            // Find hero at position from player's hero IDs
            foreach (var heroId in currentPlayer.HeroIds)
            {
                var hero = gameState.GetHero(heroId);
                if (hero != null && hero.Position.Equals(pos))
                    return hero;
            }

            return null;
        }

        private bool IsAdjacent(Position pos1, Position pos2)
        {
            var dx = Mathf.Abs(pos1.X - pos2.X);
            var dy = Mathf.Abs(pos1.Y - pos2.Y);
            return dx <= 1 && dy <= 1 && (dx + dy) > 0;
        }

        private bool IsDoubleTap(Position pos)
        {
            var currentTime = Time.unscaledTime;
            var isDoubleTap = lastTappedPosition == pos && (currentTime - lastTapTime) < doubleTapThreshold;

            lastTappedPosition = pos;
            lastTapTime = currentTime;

            return isDoubleTap;
        }

        public InputState CurrentState => currentState;
        public Hero SelectedHero => selectedHero;
    }
}
