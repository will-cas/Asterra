using Asterra.Core;
using Asterra.Core.World;

namespace Asterra.Gameplay.World
{
    /// <summary>
    /// Match-scoped world environment. Ticked by <see cref="SkirmishWorldSim"/>; not a MonoBehaviour singleton.
    /// </summary>
    public sealed class WorldEnvironmentSim : IWorldEnvironment
    {
        public WorldTerrainGrid Grid { get; }
        public ITerrainMap Terrain => Grid;
        public TraversalGraph TraversalGraph { get; }
        public ITraversalGraph Traversal => TraversalGraph;
        public INoEntryMap NoEntry => Grid;
        public WeatherSystem WeatherSim { get; }
        public IWeatherSystem Weather => WeatherSim;
        public TimeOfDaySystem TimeOfDaySim { get; }
        public ITimeOfDaySystem TimeOfDay => TimeOfDaySim;
        public EnvironmentFeatureIndex Features { get; }
        public PathDirtyTracker PathDirty { get; }

        public IPathfindingService Pathfinding { get; }

        public WorldEnvironmentSim(
            WorldTerrainGrid grid = null,
            uint weatherSeed = 42u,
            float dayLengthSeconds = 1200f,
            float startTime01 = 0.32f)
        {
            Grid = grid ?? DefaultTerrainCatalog.CreatePlayableGrid(MapBounds.PlayableHalfExtent, cellSize: 10f);
            TraversalGraph = new TraversalGraph(Grid);
            WeatherSim = new WeatherSystem(weatherSeed, Grid);
            TimeOfDaySim = new TimeOfDaySystem(dayLengthSeconds, startTime01);
            Pathfinding = new GridAStarPathfindingService(Grid, TraversalGraph);
            Features = new EnvironmentFeatureIndex(Grid);
            Features.Rebuild();
            PathDirty = new PathDirtyTracker();
        }

        public void RebuildFeatureIndex() => Features.Rebuild();

        public void Tick(float deltaSeconds)
        {
            TimeOfDaySim.Tick(deltaSeconds);
            WeatherSim.ExternalTemperatureBias = TimeOfDaySim.TemperatureBias;
            WeatherSim.Tick(deltaSeconds);
        }

        public float CombinedTemperature() => WeatherSim.EffectiveTemperature;

        public float CombinedVisibility() =>
            WeatherSim.EffectiveVisibility() * TimeOfDaySim.VisibilityModifier;

        public bool CanUnitEnter(float x, float z, TraversalCapability capabilities) =>
            Grid.IsTraversable(x, z, capabilities);

        public float MovementModifier(float x, float z, TraversalCapability capabilities)
        {
            float terrain = Grid.GetMovementModifier(x, z, capabilities);
            if (terrain <= 0f)
                return 0f;
            float night = TimeOfDaySim.IsNight ? 0.94f : 1f;
            return terrain * WeatherSim.EffectiveMovement() * night;
        }

        public bool CanPlaceBuilding(float x, float z) =>
            Grid.AllowsBuilding(x, z) && !Grid.IsBlocked(x, z);
    }
}
