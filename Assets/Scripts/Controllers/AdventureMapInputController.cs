using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private Cartographer cartographer;
        [SerializeField] private PathPreviewRenderer pathPreviewRenderer;

        private GameMap gameMap;

        [Header("Settings")]
        [SerializeField] private GameSettings gameSettings;

        private InputState currentState = InputState.Normal;
        private Hero selectedHero;
        private int castingSpellId = -1;
        private float lastTapTime;
        private Position lastTappedPosition;
        private Position previewedDestination;

        void OnEnable()
        {
            if (mapEvents != null)
            {
                mapEvents.OnTileSelected += HandleTileClicked;
                mapEvents.OnMapLoaded += HandleMapLoaded;
                mapEvents.OnObjectClicked += HandleObjectClicked;
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
                mapEvents.OnObjectClicked -= HandleObjectClicked;
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
            var settings = gameSettings ?? GameSettings.Instance;
            if (currentState == InputState.Disabled)
                return;

            // Handle mouse input (hero clicks/hovers) with manual raycasting
            HandleMouseInput();

            if (settings.enableKeyboardShortcuts)
            {
                HandleKeyboardShortcuts();
            }
        }

        /// <summary>
        /// Handles mouse input with manual raycasting and bounds checking.
        /// Replaces OnMouse* handlers to prevent Unity's internal SendMouseEvents
        /// from triggering raycast errors when mouse is outside screen bounds.
        /// </summary>
        private void HandleMouseInput()
        {
            var mousePos = Input.mousePosition;

            // Safety check: ensure mouse is within screen bounds
            if (mousePos.x < 0 || mousePos.x > Screen.width ||
                mousePos.y < 0 || mousePos.y > Screen.height)
            {
                return; // Mouse outside window, skip raycasting
            }

            // Get main camera
            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            // Manual raycast to detect hero and map object clicks
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = mainCamera.ScreenPointToRay(mousePos);
                RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

                if (hit.collider != null)
                {
                    // Check if we hit a HeroController
                    var heroController = hit.collider.GetComponent<HeroController>();
                    if (heroController != null && heroController.HeroData != null)
                    {
                        Debug.Log($"Hero clicked: {heroController.HeroData.CustomName}");

                        // If already selected, don't re-raise the event
                        if (!heroController.IsSelected)
                        {
                            uiEvents?.RaiseHeroSelected(heroController.HeroData);
                        }
                        return; // Don't process tile click
                    }

                    // Check if we hit a MapObjectView
                    var mapObjectView = hit.collider.GetComponent<MapObjectView>();
                    if (mapObjectView != null && mapObjectView.GetObject() != null)
                    {
                        var mapObject = mapObjectView.GetObject();
                        Debug.Log($"MapObjectView: Object clicked: {mapObject.Name} at {mapObject.Position}");
                        mapEvents?.RaiseObjectClicked(mapObject);
                        return; // Don't process tile click
                    }
                }
            }

            // TODO: Add hover detection for tooltips if needed
            // This would require tracking which hero is currently hovered
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
                if (selectedHero != null && cartographer != null)
                    cartographer.CenterOn(cartographer.MapToWorldPosition(selectedHero.Position.X, selectedHero.Position.Y));
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
            Debug.Log($"InputController: Tile clicked: {tilePos}");
            Debug.Log($"InputController: Current state: {currentState}");
            Debug.Log($"InputController: Selected hero: {selectedHero?.CustomName ?? "none"}");

            if (currentState == InputState.Disabled)
            {
                Debug.Log("InputController: State is Disabled, ignoring click");
                return;
            }

            if (currentState == InputState.SpellCasting)
            {
                CastSpellAtPosition(tilePos);
                return;
            }

            // Check for double-click (execute hero movement immediately)
            if (IsDoubleTap(tilePos))
            {
                Debug.Log("InputController: Double-click detected");
                // If we have a hero selected and previewed path, execute movement
                if (selectedHero != null && previewedDestination == tilePos &&
                    pathPreviewRenderer != null && pathPreviewRenderer.IsShowingPath)
                {
                    Debug.Log("InputController: Executing previewed movement on double-click");
                    ExecutePreviewedMove();
                    return;
                }
            }

            // Normal click handling
            Debug.Log("InputController: Processing as normal click");
            HandleNormalTileClick(tilePos);
        }

        private void HandleMapLoaded(GameMap map)
        {
            gameMap = map;
            Debug.Log($"AdventureMapInputController: Map loaded ({map.Width}x{map.Height})");
        }

        private void HandleNormalTileClick(Position tilePos)
        {
            Debug.Log($"InputController: HandleNormalTileClick({tilePos})");

            if (GameStateManager.Instance == null)
            {
                Debug.LogWarning("InputController: GameStateManager.Instance is null");
                return;
            }

            if (gameMap == null)
            {
                Debug.LogWarning("InputController: gameMap is null");
                return;
            }

            if (!gameMap.IsInBounds(tilePos))
            {
                Debug.LogWarning($"InputController: Position {tilePos} is out of bounds");
                return;
            }

            // Check for hero at position
            var heroAtPos = GetHeroAtPosition(tilePos);
            if (heroAtPos != null)
            {
                Debug.Log($"InputController: Found hero at position: {heroAtPos.CustomName}");
                SelectHero(heroAtPos);
                return;
            }

            // Check for map object
            var objectsAtPos = gameMap.GetObjectsAt(tilePos);
            if (objectsAtPos.Count > 0)
            {
                Debug.Log($"InputController: Found {objectsAtPos.Count} objects at position");
                HandleObjectClick(objectsAtPos[0], tilePos);
                return;
            }

            // Move selected hero to position (with path preview)
            if (selectedHero != null)
            {
                Debug.Log($"InputController: Moving hero {selectedHero.CustomName} to {tilePos}");
                HandleHeroMovementClick(tilePos);
            }
            else
            {
                Debug.Log("InputController: No hero selected, ignoring empty tile click");
            }
        }

        /// <summary>
        /// Handles hero movement click with path preview.
        /// First click: Show preview. Second click on same tile: Execute move.
        /// </summary>
        private void HandleHeroMovementClick(Position targetPos)
        {
            if (selectedHero == null || GameStateManager.Instance == null || gameMap == null)
                return;

            Debug.Log($"HandleHeroMovementClick: targetPos={targetPos}, previewedDestination={previewedDestination}, IsShowingPath={pathPreviewRenderer?.IsShowingPath}");

            // Check if clicking on already previewed destination
            if (previewedDestination.Equals(targetPos) && pathPreviewRenderer != null && pathPreviewRenderer.IsShowingPath)
            {
                Debug.Log("Second click detected - executing movement");
                // Second click - execute movement
                ExecutePreviewedMove();
                return;
            }

            // First click - show path preview
            Debug.Log("First click - showing preview");
            ShowPathPreview(targetPos);
        }

        /// <summary>
        /// Shows path preview to target position.
        /// Calculates per-step costs for accurate green/red split rendering.
        /// </summary>
        private void ShowPathPreview(Position targetPos)
        {
            if (selectedHero == null || gameMap == null)
                return;

            // Don't show path to current position
            if (selectedHero.Position.Equals(targetPos))
            {
                Debug.Log("Cannot move to current position");
                ClearPathPreview();
                return;
            }

            // Find path
            var path = BasicPathfinder.FindPath(gameMap, selectedHero.Position, targetPos);
            if (path == null)
            {
                ClearPathPreview();
                uiEvents?.RaiseShowStatusMessage("Cannot reach that tile!");
                return;
            }

            // Calculate per-step costs for accurate rendering
            var pathStepCosts = new List<int>();
            for (int i = 0; i < path.Count - 1; i++)
            {
                var stepCost = gameMap.GetMovementCost(path[i], path[i + 1]);
                pathStepCosts.Add(stepCost);
            }

            // Calculate total movement cost
            var totalMovementCost = pathStepCosts.Sum();

            // Show preview with per-step costs
            if (pathPreviewRenderer != null)
            {
                pathPreviewRenderer.ShowPath(path, pathStepCosts, totalMovementCost, selectedHero.Movement);
                previewedDestination = targetPos;

                // Show status message
                if (totalMovementCost > selectedHero.Movement)
                {
                    // Calculate how far we can actually go
                    var reachableIndex = pathPreviewRenderer.ReachablePathIndex;
                    if (reachableIndex > 0)
                    {
                        uiEvents?.RaiseShowStatusMessage($"Can move {reachableIndex}/{path.Count - 1} tiles. Click again to move as far as possible.");
                    }
                    else
                    {
                        uiEvents?.RaiseShowStatusMessage($"Not enough movement! Need {totalMovementCost}, have {selectedHero.Movement}.");
                    }
                }
                else
                {
                    uiEvents?.RaiseShowStatusMessage($"Movement cost: {totalMovementCost}/{selectedHero.Movement}. Click again to move.");
                }
            }
            else
            {
                // No preview renderer - execute immediately (fallback)
                MoveHeroToPosition(targetPos);
            }
        }

        /// <summary>
        /// Executes the currently previewed move.
        /// If insufficient movement, moves as far along path as possible (partial movement).
        /// </summary>
        private void ExecutePreviewedMove()
        {
            Debug.Log("ExecutePreviewedMove called");

            if (pathPreviewRenderer == null || !pathPreviewRenderer.IsShowingPath)
            {
                Debug.LogWarning("No path preview to execute!");
                return;
            }

            var fullPath = pathPreviewRenderer.CurrentPath;
            var reachablePath = pathPreviewRenderer.ReachablePath;
            var totalMovementCost = pathPreviewRenderer.CurrentMovementCost;

            Debug.Log($"Executing move: fullPathLength={fullPath?.Count}, reachablePathLength={reachablePath?.Count}, cost={totalMovementCost}, heroMovement={selectedHero?.Movement}");

            ClearPathPreview();

            if (selectedHero == null || fullPath == null)
            {
                Debug.LogWarning("Selected hero or path is null!");
                return;
            }

            // If full path is reachable, use it
            if (totalMovementCost <= selectedHero.Movement)
            {
                Debug.Log($"Full path reachable, moving to {fullPath[fullPath.Count - 1]}");
                MoveHeroAlongPath(fullPath, totalMovementCost);
            }
            // Otherwise, move as far as possible (partial movement)
            else if (reachablePath != null && reachablePath.Count > 1)
            {
                // Calculate cost of reachable portion
                var reachableCost = 0;
                for (int i = 0; i < reachablePath.Count - 1; i++)
                {
                    reachableCost += gameMap.GetMovementCost(reachablePath[i], reachablePath[i + 1]);
                }

                Debug.Log($"Partial movement: moving {reachablePath.Count - 1} tiles (cost={reachableCost}) to {reachablePath[reachablePath.Count - 1]}");
                uiEvents?.RaiseShowStatusMessage($"Moved as far as possible ({reachablePath.Count - 1} tiles)");
                MoveHeroAlongPath(reachablePath, reachableCost);
            }
            else
            {
                Debug.LogWarning("No reachable path available!");
                uiEvents?.RaiseShowStatusMessage($"Not enough movement! Need at least 1 tile.");
            }
        }

        /// <summary>
        /// Clears path preview.
        /// </summary>
        private void ClearPathPreview()
        {
            if (pathPreviewRenderer != null)
            {
                pathPreviewRenderer.ClearPath();
            }
            previewedDestination = default;
        }

        /// <summary>
        /// Immediately moves hero to position without preview (internal method).
        /// </summary>
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

            // Execute animated movement
            MoveHeroAlongPath(path, movementCost);
        }

        /// <summary>
        /// Moves hero along path with animation.
        /// Checks for object interactions when hero lands on destination.
        /// </summary>
        private async void MoveHeroAlongPath(System.Collections.Generic.List<Position> path, int movementCost)
        {
            Debug.Log($"MoveHeroAlongPath called - path has {path.Count} positions");

            var heroSpawner = FindFirstObjectByType<HeroSpawner>();
            if (heroSpawner == null)
            {
                Debug.LogError("HeroSpawner not found!");
                return;
            }

            var heroController = heroSpawner.GetHeroController(selectedHero.Id);
            if (heroController == null)
            {
                Debug.LogError($"HeroController not found for hero {selectedHero.Id}!");
                return;
            }

            Debug.Log($"Starting animation for {path.Count - 1} tiles");

            // Build waypoint array in 3D coordinates (skip first, it's current position)
            var waypoints = new Vector3[path.Count - 1];
            for (int i = 1; i < path.Count; i++)
            {
                // Convert map position to 3D world position (X,Z ground plane)
                var worldPos = new Vector3(
                    path[i].X + 0.5f,  // Center on tile X
                    0f,                 // Ground level
                    path[i].Y + 0.5f   // Center on tile Z
                );
                waypoints[i - 1] = worldPos;
            }

            // Smooth movement through all waypoints
            var settings = gameSettings ?? GameSettings.Instance;
            var totalDuration = (path.Count - 1) * settings.heroMovementTimePerTile;
            await heroController.MoveAlongPathAsync(waypoints, customSpeed: totalDuration);

            // Update hero data
            var finalPos = path[path.Count - 1];
            selectedHero.Position = finalPos;
            selectedHero.Movement -= movementCost;

            // Raise event
            gameEvents?.RaiseHeroMoved(selectedHero.Id, selectedHero.Position);

            // Update UI
            uiEvents?.RaiseShowHeroInfo(selectedHero);

            // Check if hero landed on an object's visitable position
            CheckForObjectInteraction(finalPos);
        }

        /// <summary>
        /// Checks if hero is at a visitable position for any object and triggers OnVisit if so.
        /// </summary>
        private void CheckForObjectInteraction(Position heroPos)
        {
            if (gameMap == null || selectedHero == null)
                return;

            // Get all objects on the map
            var allObjects = gameMap.GetAllObjects();

            foreach (var obj in allObjects)
            {
                // Check if hero is at a visitable position for this object
                if (obj.IsVisitableAt(heroPos))
                {
                    Debug.Log($"Hero arrived at visitable position for {obj.Name} - triggering OnVisit");
                    VisitObject(obj);
                    return; // Only visit one object per move
                }
            }
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

        /// <summary>
        /// Handles object clicks from MapObjectView colliders.
        /// Shows path preview to nearest visitable position around the object.
        /// </summary>
        private void HandleObjectClicked(MapObject mapObject)
        {
            if (selectedHero == null || gameMap == null)
            {
                Debug.Log("Cannot interact with object: no hero selected or no map");
                return;
            }

            Debug.Log($"Object clicked: {mapObject.Name} at {mapObject.Position}");

            // Check if hero is already at a visitable position
            if (mapObject.IsVisitableAt(selectedHero.Position))
            {
                Debug.Log("Hero is already at visitable position - visiting object now");
                VisitObject(mapObject);
                return;
            }

            // Find nearest visitable position
            var visitablePositions = mapObject.GetVisitablePositions();
            if (visitablePositions.Count == 0)
            {
                Debug.LogWarning($"Object {mapObject.Name} has no visitable positions!");
                uiEvents?.RaiseShowStatusMessage("Cannot interact with this object!");
                return;
            }

            // Find closest visitable position that's reachable
            Position nearestVisitablePos = default;
            var shortestPathLength = int.MaxValue;

            foreach (var visitablePos in visitablePositions)
            {
                if (!gameMap.IsInBounds(visitablePos))
                    continue;

                var path = BasicPathfinder.FindPath(gameMap, selectedHero.Position, visitablePos);
                if (path != null && path.Count < shortestPathLength)
                {
                    shortestPathLength = path.Count;
                    nearestVisitablePos = visitablePos;
                }
            }

            if (shortestPathLength == int.MaxValue)
            {
                Debug.LogWarning("No reachable visitable position found!");
                uiEvents?.RaiseShowStatusMessage("Cannot reach this object!");
                return;
            }

            Debug.Log($"Showing path preview to visitable position {nearestVisitablePos}");

            // Show path preview to nearest visitable position
            // This will trigger the standard path preview system
            ShowPathPreview(nearestVisitablePos);
        }

        private void HandleObjectClick(MapObject mapObject, Position clickPos)
        {
            // Delegate to HandleObjectClicked (unified handler)
            HandleObjectClicked(mapObject);
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

            // Show interaction message
            uiEvents?.RaiseShowStatusMessage($"Visited {mapObject.Name}");
        }

        // ===== Hero Selection =====

        private void SelectHero(Hero hero)
        {
            ClearPathPreview(); // Clear any existing preview
            selectedHero = hero;
            uiEvents?.RaiseHeroSelected(hero);

            // Center camera on hero
            if (cartographer != null)
            {
                cartographer.CenterOn(cartographer.MapToWorldPosition(hero.Position.X, hero.Position.Y));
            }
        }

        private void ClearSelection()
        {
            ClearPathPreview(); // Clear preview when deselecting
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
            Debug.Log($"AdventureMapInputController: Hero selected: {hero.CustomName}");
            selectedHero = hero;

            // Update visual selection on HeroController
            UpdateHeroSelectionVisuals(hero);
        }

        private void UpdateHeroSelectionVisuals(Hero hero)
        {
            // Find HeroSpawner and get HeroController
            var heroSpawner = FindFirstObjectByType<HeroSpawner>();
            if (heroSpawner == null)
                return;

            // Deselect all heroes first
            foreach (var heroController in heroSpawner.GetAllHeroControllers())
            {
                if (heroController != null)
                    heroController.SetSelected(false);
            }

            // Select the new hero
            var selectedHeroController = heroSpawner.GetHeroController(hero.Id);
            if (selectedHeroController != null)
            {
                selectedHeroController.SetSelected(true);
                Debug.Log($"Set hero {hero.CustomName} as selected visually");
            }
        }

        private void HandleSelectionCleared()
        {
            selectedHero = null;

            // Clear visual selection on all heroes
            var heroSpawner = FindFirstObjectByType<HeroSpawner>();
            if (heroSpawner != null)
            {
                foreach (var heroController in heroSpawner.GetAllHeroControllers())
                {
                    if (heroController != null)
                        heroController.SetSelected(false);
                }
            }
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
            var settings = gameSettings ?? GameSettings.Instance;
            var currentTime = Time.unscaledTime;
            var isDoubleTap = lastTappedPosition == pos && (currentTime - lastTapTime) < settings.doubleTapThreshold;

            lastTappedPosition = pos;
            lastTapTime = currentTime;

            return isDoubleTap;
        }

        public InputState CurrentState => currentState;
        public Hero SelectedHero => selectedHero;
    }
}
