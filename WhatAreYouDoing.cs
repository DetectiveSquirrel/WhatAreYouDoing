using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Color = SharpDX.Color;
using Map = ExileCore.PoEMemory.Elements.Map;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace WhatAreYouDoing
{
    public class WhereAreYouGoing : BaseSettingsPlugin<WhatAreYouDoingSettings>
    {
        private CachedValue<SharpDX.RectangleF> _mapRect;
        private CachedValue<float> _diag;
        private Camera Camera => GameController.Game.IngameState.Camera;
        private Map MapWindow => GameController.Game.IngameState.IngameUi.Map;
        private SharpDX.RectangleF CurrentMapRect => _mapRect?.Value ?? (_mapRect = new TimeCache<SharpDX.RectangleF>(() => MapWindow.GetClientRect(), 100)).Value;

        private Vector2 ScreenCenter =>
            new Vector2(CurrentMapRect.Width / 2, (CurrentMapRect.Height / 2) - 20) + new Vector2(CurrentMapRect.X, CurrentMapRect.Y) +
            new Vector2(MapWindow.LargeMapShiftX, MapWindow.LargeMapShiftY);

        private IngameUIElements ingameStateIngameUi;
        private Vector2 ScreenCenterCache;
        private bool largeMap;
        private float scale;
        private float k;
        private TimedVector2ContainerList trapShotContainer;

        private float Diagonal =>
            _diag?.Value ?? (_diag = new TimeCache<float>(() =>
            {
                if (ingameStateIngameUi.Map.SmallMiniMap.IsVisibleLocal)
                {
                    var mapRect = ingameStateIngameUi.Map.SmallMiniMap.GetClientRect();
                    return (float)(Math.Sqrt((mapRect.Width * mapRect.Width) + (mapRect.Height * mapRect.Height)) / 2f);
                }

                return (float)Math.Sqrt((Camera.Width * Camera.Width) + (Camera.Height * Camera.Height));
            }, 100)).Value;

        public override bool Initialise()
        {
            trapShotContainer = new TimedVector2ContainerList();
            return true;
        }

        public WAYDConfig SettingMenu(WAYDConfig setting, string prefix)
        {
            var settings = setting;
            if (ImGui.CollapsingHeader($@"{prefix}##{prefix}", ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.DefaultOpen))
            {
                // Start Indent
                ImGui.Indent();

                settings.Enable = ImGuiExtension.Checkbox($@"{prefix}(s) Enabled", settings.Enable);
                if (ImGui.TreeNode($@"Colors##{prefix}"))
                {
                    settings.Colors.MapColor = ImGuiExtension.ColorPicker("Map Color", settings.Colors.MapColor);
                    settings.Colors.MapAttackColor = ImGuiExtension.ColorPicker("Map Attack Color", settings.Colors.MapAttackColor);
                    settings.Colors.WorldColor = ImGuiExtension.ColorPicker("World Color", settings.Colors.WorldColor);
                    settings.Colors.WorldAttackColor = ImGuiExtension.ColorPicker("World Attack Color", settings.Colors.WorldAttackColor);
                    ImGui.Spacing();
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode($@"Map##{prefix}"))
                {
                    settings.Map.Enable = ImGuiExtension.Checkbox("Map Drawing Enabled", settings.Map.Enable);
                    settings.Map.DrawAttack = ImGuiExtension.Checkbox("Draw Attacks", settings.Map.DrawAttack);
                    settings.Map.DrawDestination = ImGuiExtension.Checkbox("Draw Destinations", settings.Map.DrawDestination);
                    settings.Map.LineThickness = ImGuiExtension.IntDrag("Line Thickness", settings.Map.LineThickness, 1, 500, 0.1f);
                    ImGui.Spacing();
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode($@"World##{prefix}"))
                {
                    settings.World.Enable = ImGuiExtension.Checkbox("World Drawing Enabled", settings.World.Enable);
                    settings.World.DrawAttack = ImGuiExtension.Checkbox("World Attacks", settings.World.DrawAttack);
                    settings.World.DrawAttackEndPoint = ImGuiExtension.Checkbox("World Attack Endpoint", settings.World.DrawAttackEndPoint);
                    settings.World.DrawDestinationEndPoint = ImGuiExtension.Checkbox("World Destination Endpoint", settings.World.DrawDestinationEndPoint);
                    settings.World.DrawLine = ImGuiExtension.Checkbox("Draw Line", settings.World.DrawLine);
                    settings.World.AlwaysRenderCircle = ImGuiExtension.Checkbox("Always Render Entity Circle", settings.World.AlwaysRenderCircle);
                    settings.World.RenderCircleThickness = ImGuiExtension.IntDrag("Entity Circle Thickness", settings.World.RenderCircleThickness, 1, 100, 0.1f);
                    settings.World.LineThickness = ImGuiExtension.IntDrag("Line Thickness", settings.World.LineThickness, 1, 500, 0.1f);
                    ImGui.Spacing();
                    ImGui.TreePop();
                }

                // End Indent
                ImGui.Unindent();
            }

            // Reapply new settings
            return settings;
        }

        public override void DrawSettings()
        {
            base.DrawSettings();

            Settings.MovingTraps = SettingMenu(Settings.MovingTraps, "Moving Traps");
            Settings.DartTraps = SettingMenu(Settings.DartTraps, "Dart Traps");
        }

        public override Job Tick()
        {
            ingameStateIngameUi = GameController.Game.IngameState.IngameUi;
            k = Camera.Width < 1024f ? 1120f : 1024f;

            if (ingameStateIngameUi.Map.SmallMiniMap.IsVisibleLocal)
            {
                var mapRect = ingameStateIngameUi.Map.SmallMiniMap.GetClientRectCache;
                ScreenCenterCache = new Vector2(mapRect.X + (mapRect.Width / 2), mapRect.Y + (mapRect.Height / 2));
                largeMap = false;
            }
            else if (ingameStateIngameUi.Map.LargeMap.IsVisibleLocal)
            {
                ScreenCenterCache = ScreenCenter;
                largeMap = true;
            }

            scale = k / Camera.Height * Camera.Width * 3f / 4f / MapWindow.LargeMapZoom;

            trapShotContainer.RemoveInvalidObjects();
            return null;
        }

        public override void Render()
        {
            //Any Imgui or Graphics calls go here. This is called after Tick
            if (!Settings.Enable.Value || !GameController.InGame) return;

            var playerPositioned = GameController?.Player?.GetComponent<Positioned>();
            if (playerPositioned == null) return;
            var playerPos = playerPositioned.GridPosNum;

            var playerRender = GameController?.Player?.GetComponent<Render>();
            if (playerRender == null) return;
            var posZ = GameController.Player.PosNum.Z;

            if (MapWindow == null) return;
            var mapWindowLargeMapZoom = MapWindow.LargeMapZoom;

            var baseList = (GameController?.EntityListWrapper.ValidEntitiesByType[EntityType.Terrain].ToList() ?? new List<Entity>())
                                        .Concat(GameController?.EntityListWrapper.ValidEntitiesByType[EntityType.Monster].ToList() ?? new List<Entity>())
                                        .ToList();

            if (baseList == null) return;

            foreach (var entity in baseList)
            {
                if (entity == null) continue;

                var drawSettings = new WAYDConfig();

                switch (entity.Type)
                {
                    case EntityType.Terrain:
                        // Find a better way to sort this without hard coded paths
                        var pathToType = new Dictionary<string, TrapType>()
                        {
                            { "Metadata/Terrain/Labyrinth/Traps/LabyrinthRoomba", TrapType.GroundMover},
                            { "Metadata/Terrain/Labyrinth/Traps/LabyrinthFlyingRoomba", TrapType.GroundMover },
                            { "Metadata/Terrain/Labyrinth/Traps/LabyrinthSawblade", TrapType.GroundMover },
                            { "Metadata/Terrain/Labyrinth/Traps/LabyrinthSawbladeSlow", TrapType.GroundMover },
                            { "Metadata/Terrain/Labyrinth/Traps/LabyrinthRoomba_Slow", TrapType.GroundMover },
                            { "Metadata/Terrain/Labyrinth/Traps/LabyrinthSpinner", TrapType.GroundMover },
                            { "Metadata/Terrain/Labyrinth/Traps/LabyrinthArrowTrap_Single", TrapType.Darts },
                            { "Metadata/Terrain/Labyrinth/Traps/AlternateArrowTraps/LabyrinthArrowTrapTwo90DegreeArrows", TrapType.Darts }
                        };

                        var entityTypeCheck = pathToType.ContainsKey(entity.Path) ? pathToType[entity.Path] : TrapType.None;

                        switch (entityTypeCheck)
                        {
                            case TrapType.None:
                                break;
                            case TrapType.GroundMover:
                                //LogMessage(@$"entityTypeCheck = {entityTypeCheck}", 3);
                                drawSettings = Settings.MovingTraps;
                                drawSettings.TrapType = entityTypeCheck;
                                break;
                            case TrapType.Darts:
                                //LogMessage(@$"entityTypeCheck = {entityTypeCheck}", 3);
                                drawSettings = Settings.DartTraps;
                                drawSettings.TrapType = entityTypeCheck;
                                break;
                        }
                        break;

                    case EntityType.Monster:
                        // Find a better way to sort this without hard coded paths
                        var pathToTypeMonster = new Dictionary<string, TrapType>()
                        {
                            { "Metadata/Monsters/InvisibleFire/InvisibleFireOrionDeathZoneStationary", TrapType.Sirus },
                            { "Metadata/Monsters/InvisibleFire/InvisibleFireOrionDeathZone", TrapType.Sirus },
                        };

                        var entityTypeCheckMonster = pathToTypeMonster.ContainsKey(entity.Metadata) ? pathToTypeMonster[entity.Metadata] : TrapType.None;

                        switch (entityTypeCheckMonster)
                        {
                            case TrapType.None:
                                break;
                            case TrapType.Sirus:
                                drawSettings = Settings.MovingTraps;
                                drawSettings.TrapType = entityTypeCheckMonster;
                                break;
                        }
                        break;
                }

                if (!drawSettings.Enable) continue;

                var component = entity?.GetComponent<Render>();
                if (component == null) continue;
                //LogMessage(@$"drawSettings.TrapType = {drawSettings.TrapType}", 3);

                switch (drawSettings.TrapType)
                {
                    case TrapType.None:
                        // No u.
                        break;
                    case TrapType.Sirus:
                        {
                            var entityToSize = new Dictionary<string, float>()
                                {
                                    { "Metadata/Monsters/InvisibleFire/InvisibleFireOrionDeathZoneStationary", 790F },
                                    { "Metadata/Monsters/InvisibleFire/InvisibleFireOrionDeathZone", 790F },
                                };

                            var circleSize = entityToSize.ContainsKey(entity.Metadata) ? entityToSize[entity.Metadata] : 30f;

                            entity.TryGetComponent<Stats>(out var statsComp);
                            if (statsComp != null)
                            {
                                foreach ( var stats in statsComp.StatDictionary)
                                {
                                    if (stats.Key == GameStat.ActorScalePct)
                                    {
                                        circleSize = circleSize * (1 + stats.Value/100F);
                                    }
                                }
                            }
                            var color = drawSettings.Colors.WorldColor;
                            DrawCircleInWorldPosition(entity.PosNum, circleSize, drawSettings.World.RenderCircleThickness, drawSettings.Colors.MapAttackColor);

                            if (drawSettings.Map.Enable && drawSettings.Map.DrawAttack)
                            {
                                DrawCircleInMapPosition(entity.GridPosNum, circleSize/10, drawSettings.Map.LineThickness, drawSettings.Colors.MapAttackColor);
                            }
                        }
                        break;
                    case TrapType.GroundMover:
                        {

                            var movementComp = entity?.GetComponent<Movement>();
                            if (movementComp == null) continue;

                            var shouldDrawCircle = entity.DistancePlayer < Settings.MaxCircleDrawDistance;

                            if (drawSettings.World.Enable)
                            {
                                var entityToSize = new Dictionary<string, float>()
                                {
                                    { "Metadata/Terrain/Labyrinth/Traps/LabyrinthRoomba", 120f },
                                    { "Metadata/Terrain/Labyrinth/Traps/LabyrinthRoomba_Slow", 120f },
                                    { "Metadata/Terrain/Labyrinth/Traps/LabyrinthFlyingRoomba", 90f },
                                    { "Metadata/Terrain/Labyrinth/Traps/LabyrinthSawblade", 50f },
                                    { "Metadata/Terrain/Labyrinth/Traps/LabyrinthSawbladeSlow", 50f },
                                    { "Metadata/Terrain/Labyrinth/Traps/LabyrinthSpinner", 60f },
                                    { "Metadata/Monsters/InvisibleFire/InvisibleFireOrionDeathZoneStationary", 200f }
                                };
                                var circleSize = entityToSize.ContainsKey(entity.Path) ? entityToSize[entity.Path] : 30f;

                                var pathingNodes = movementComp.MovingToGridPosNum;
                                var color = drawSettings.Colors.WorldColor;

                                if (drawSettings.World.DrawLine && pathingNodes != new Vector2(0, 0))
                                {
                                    var entityGridPosNum = Camera.WorldToScreen(entity.PosNum); // Could use grid for true placement, but the jitter of switching height/grid is annoying.
                                    var nextPosition = QueryWorldScreenPositionWithTerrainHeight(pathingNodes);

                                    var entityValue = entity.GridPosNum.Distance(pathingNodes);

                                    var colorScaler = new ColorScaler();
                                    float maxValue = 60;
                                    var startColor = Color.Red;
                                    var endColor = Color.Green;
                                    byte alpha = 170; // Optional alpha value
                                    color = colorScaler.GetColor(entityValue, maxValue, startColor, endColor, alpha);

                                    Graphics.DrawLine(entityGridPosNum, nextPosition, drawSettings.World.LineThickness, color);

                                    if (drawSettings.World.DrawDestinationEndPoint && shouldDrawCircle)
                                    {
                                        var queriedWorldPos = QueryGridPositionToWorldWithTerrainHeight(pathingNodes);
                                        DrawCircleInWorldPosition(queriedWorldPos, circleSize / 4, drawSettings.World.RenderCircleThickness, color);
                                    }
                                }

                                if (drawSettings.World.AlwaysRenderCircle && shouldDrawCircle)
                                    DrawCircleInWorldPosition(entity.PosNum, circleSize, drawSettings.World.RenderCircleThickness, color);
                            }
                        }
                        break;
                    case TrapType.Darts:
                        {
                            var actorComp = entity?.GetComponent<Actor>();
                            if (actorComp == null) continue;

                            // Based on hand picked values on a single dart trap
                            var arrowDuration = 1.5f;
                            var shouldDrawCircle = entity.DistancePlayer < Settings.MaxCircleDrawDistance;

                            if (drawSettings.World.Enable)
                            {
                                var entityToSize = new Dictionary<string, float>()
                                {
                                    { "Metadata/Terrain/Labyrinth/Traps/AlternateArrowTraps/LabyrinthArrowTrapTwo90DegreeArrows", 20f },
                                    { "Metadata/Terrain/Labyrinth/Traps/LabyrinthArrowTrap_Single", 20f }
                                };
                                var circleSize = entityToSize.ContainsKey(entity.Path) ? entityToSize[entity.Path] : 30f;

                                var currentAction = actorComp?.CurrentAction;

                                if (currentAction != null)
                                {
                                    // Add to containerList if not already present
                                    var attackingGridPosNum = actorComp.CurrentAction.Destination.ToVector2Num();
                                    if (trapShotContainer.FindItem(entity.Address, currentAction.Address) == null)
                                    {
                                        trapShotContainer.Add(entity.Address, currentAction.Address, attackingGridPosNum, TimeSpan.FromSeconds(arrowDuration));
                                    }
                                }

                                // Draw the item if found
                                var itemToDraw = trapShotContainer.FindItem(entity.Address);
                                if (itemToDraw != null)
                                {
                                    // Perform the drawing logic for the item

                                    var color = drawSettings.Colors.WorldColor;

                                    if (drawSettings.World.DrawLine && itemToDraw.Position != new Vector2(0, 0))
                                    {
                                        var entityGridPosNum = Camera.WorldToScreen(entity.PosNum); // Could use grid for true placement, but the jitter of switching height/grid is annoying.
                                        var nextPosition = QueryWorldScreenPositionWithTerrainHeight(itemToDraw.Position);

                                        var entityValue = entity.GridPosNum.Distance(itemToDraw.Position);

                                        var colorScaler = new ColorScalerFromTime();
                                        var duration = arrowDuration; // Duration in seconds
                                        var startColor = Color.Red;
                                        byte maxAlpha = 200; // Maximum alpha value
                                        var elapsedSeconds = DateTime.Now - itemToDraw.StartTime;
                                        color = colorScaler.GetColor((float)elapsedSeconds.TotalSeconds, duration, startColor, maxAlpha);

                                        Graphics.DrawLine(entityGridPosNum, nextPosition, drawSettings.World.LineThickness, color);

                                        if (drawSettings.World.DrawDestinationEndPoint && shouldDrawCircle)
                                        {
                                            var queriedWorldPos = QueryGridPositionToWorldWithTerrainHeight(itemToDraw.Position);
                                            DrawCircleInWorldPosition(queriedWorldPos, circleSize / 4, drawSettings.World.RenderCircleThickness, color);
                                        }
                                    }

                                    if (drawSettings.World.AlwaysRenderCircle && shouldDrawCircle)
                                        DrawCircleInWorldPosition(entity.PosNum, circleSize, drawSettings.World.RenderCircleThickness, color);
                                }
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Queries the world screen position with terrain height for the given grid position.
        /// </summary>
        /// <param name="gridPosition">The grid position to query.</param>
        /// <returns>The world screen position with terrain height.</returns>
        private Vector2 QueryWorldScreenPositionWithTerrainHeight(Vector2 gridPosition)
        {
            // Query the world screen position with terrain height for the given grid position
            return Camera.WorldToScreen(QueryGridPositionToWorldWithTerrainHeight(gridPosition));
        }

        /// <summary>
        /// Queries the world screen positions with terrain height for the given grid positions.
        /// </summary>
        /// <param name="gridPositions">The grid positions to query.</param>
        /// <returns>The world screen positions with terrain height.</returns>
        private List<Vector2> QueryWorldScreenPositionsWithTerrainHeight(List<Vector2> gridPositions)
        {
            // Query the world screen positions with terrain height for the given grid positions
            return gridPositions.Select(gridPos => Camera.WorldToScreen(QueryGridPositionToWorldWithTerrainHeight(gridPos))).ToList();
        }

        /// <summary>
        /// Queries the grid position and extracts the corresponding terrain height.
        /// </summary>
        /// <param name="gridPosition">The grid position to query.</param>
        /// <returns>The world position with the extracted terrain height.</returns>
        private Vector3 QueryGridPositionToWorldWithTerrainHeight(Vector2 gridPosition)
        {
            // Query the grid position and extract the corresponding world position with terrain height
            return new Vector3(gridPosition.GridToWorld(), (float)GameController.IngameState.Data.GetTerrainHeightAt(gridPosition));
        }

        /// <summary>
        /// Draws a circle at the specified world position with the given radius, thickness, and color.
        /// </summary>
        /// <param name="position">The world position to draw the circle at.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="thickness">The thickness of the circle's outline.</param>
        /// <param name="color">The color of the circle.</param>
        private void DrawCircleInWorldPosition(Vector3 position, float radius, int thickness, Color color)
        {
            const int segments = 15;
            const float segmentAngle = 2f * MathF.PI / segments;

            for (var i = 0; i < segments; i++)
            {
                var angle = i * segmentAngle;
                var currentOffset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                var nextOffset = new Vector2(MathF.Cos(angle + segmentAngle), MathF.Sin(angle + segmentAngle)) * radius;

                var currentWorldPos = position + new Vector3(currentOffset, 0);
                var nextWorldPos = position + new Vector3(nextOffset, 0);

                Graphics.DrawLine(
                    Camera.WorldToScreen(currentWorldPos),
                    Camera.WorldToScreen(nextWorldPos),
                    thickness,
                    color
                );
            }
        }

        /// <summary>
        /// Draws a circle at the specified world position with the given radius, thickness, and color.
        /// </summary>
        /// <param name="position">The world position to draw the circle at.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="thickness">The thickness of the circle's outline.</param>
        /// <param name="color">The color of the circle.</param>
        private void DrawCircleInMapPosition(Vector2 position, float radius, int thickness, Color color)
        {
            const int segments = 15;
            const float segmentAngle = 2f * MathF.PI / segments;

            for (var i = 0; i < segments; i++)
            {
                var angle = i * segmentAngle;
                var currentOffset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                var nextOffset = new Vector2(MathF.Cos(angle + segmentAngle), MathF.Sin(angle + segmentAngle)) * radius;

                var currentWorldPos = position + currentOffset;
                var nextWorldPos = position + nextOffset;

                Graphics.DrawLine(
                    GameController.IngameState.Data.GetGridMapScreenPosition(currentWorldPos),
                    GameController.IngameState.Data.GetGridMapScreenPosition(nextWorldPos),
                    thickness,
                    color
                );
            }
        }
    }
    public class ColorScalerFromTime
    {
        /// <summary>
        /// Gets the interpolated color based on a given value and maximum value.
        /// </summary>
        /// <param name="value">The value to interpolate.</param>
        /// <param name="maxValue">The maximum value to interpolate against.</param>
        /// <param name="startColor">The starting color.</param>
        /// <param name="startAlpha">The starting alpha value.</param>
        /// <returns>The interpolated color.</returns>
        public Color GetColor(float value, float maxValue, Color startColor, byte startAlpha)
        {
            // Calculate the interpolated alpha value based on time
            var interpolatedAlpha = GetInterpolatedAlpha(value, maxValue, startAlpha);

            return new Color(startColor.R, startColor.G, startColor.B, interpolatedAlpha);
        }

        private byte GetInterpolatedAlpha(float value, float maxValue, byte startAlpha)
        {
            // Calculate the interpolated alpha value based on time
            var alphaValue = startAlpha - (startAlpha / maxValue * Math.Min(value, maxValue));
            return (byte)Math.Max(0, Math.Min(startAlpha, alphaValue));
        }
    }

    public class ColorScaler
    {
        /// <summary>
        /// Gets the interpolated color between the start and end colors based on a given value and maximum value.
        /// </summary>
        /// <param name="value">The value to interpolate.</param>
        /// <param name="maxValue">The maximum value to interpolate against.</param>
        /// <param name="startColor">The starting color of the gradient.</param>
        /// <param name="endColor">The ending color of the gradient.</param>
        /// <param name="alpha">The optional alpha value for the interpolated color.</param>
        /// <returns>The interpolated color.</returns>
        public Color GetColor(float value, float maxValue, Color startColor, Color endColor, byte? alpha = null)
        {
            // Normalize the value to a range between 0 and 1
            var normalizedValue = value / maxValue;

            // Interpolate the RGB components between the start and end colors
            var r = Interpolate(startColor.R, endColor.R, normalizedValue);
            var g = Interpolate(startColor.G, endColor.G, normalizedValue);
            var b = Interpolate(startColor.B, endColor.B, normalizedValue);

            var interpolatedAlpha = alpha ?? startColor.A;

            return new Color((int)r, (int)g, (int)b, interpolatedAlpha);
        }

        private float Interpolate(float startValue, float endValue, float t)
        {
            return startValue + ((endValue - startValue) * t);
        }
    }
    public class TimedVector2Container
    {
        public long EntityId { get; private set; }
        public long ActionId { get; private set; }
        public Vector2 Position { get; private set; }
        public DateTime StartTime { get; private set; }
        public TimeSpan Duration { get; private set; }

        public TimedVector2Container(long entityId, long positionAddress, Vector2 position, TimeSpan duration)
        {
            EntityId = entityId;
            ActionId = positionAddress;
            Position = position;
            Duration = duration;
            StartTime = DateTime.Now;
        }

        public bool IsWithinThreshold()
        {
            var currentTime = DateTime.Now;
            var elapsedTime = currentTime - StartTime;
            return elapsedTime <= Duration;
        }
    }

    public class TimedVector2ContainerList
    {
        private List<TimedVector2Container> containerList;

        public TimedVector2ContainerList()
        {
            containerList = new List<TimedVector2Container>();
        }

        public void Add(long entityId, long positionAddress, Vector2 position, TimeSpan duration)
        {
            var container = new TimedVector2Container(entityId, positionAddress, position, duration);
            containerList.Add(container);
        }

        public void RemoveInvalidObjects()
        {
            containerList.RemoveAll(container => !container.IsWithinThreshold());
        }

        public List<TimedVector2Container> GetAllContainers()
        {
            return containerList;
        }

        public TimedVector2Container FindItem(long entityId, long actionId)
        {
            return containerList.FirstOrDefault(container => container.EntityId == entityId && container.ActionId == actionId);
        }

        public TimedVector2Container FindItem(long entityId)
        {
            return containerList.FirstOrDefault(container => container.EntityId == entityId);
        }
    }
}