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
        public ITimeOfDaySystem TimeOfDay { get; }
        public EnvironmentFeatureIndex Features { get; }
        public PathDirtyTracker PathDirty { get; }

        public DirectSteerPathfindingService Pathfinding { get; }

        public WorldEnvironmentSim(WorldTerrainGrid grid = null, uint weatherSeed = 42u)
        {
            Grid = grid ?? DefaultTerrainCatalog.CreatePlayableGrid(MapBounds.PlayableHalfExtent, cellSize: 10f);
            TraversalGraph = new TraversalGraph(Grid);
            WeatherSim = new WeatherSystem(weatherSeed, Grid);
            TimeOfDay = new StaticTimeOfDaySystem();
            Pathfinding = new DirectSteerPathfindingService(Grid, TraversalGraph);
            Features = new EnvironmentFeatureIndex(Grid);
            Features.Rebuild();
            PathDirty = new PathDirtyTracker();
        }

        public void RebuildFeatureIndex() => Features.Rebuild();

        public void Tick(float deltaSeconds)
        {
            WeatherSim.Tick(deltaSeconds);
            TimeOfDay.Tick(deltaSeconds);
        }

        public bool CanUnitEnter(float x, float z, TraversalCapability capabilities) =>
            Grid.IsTraversable(x, z, capabilities);

        public float MovementModifier(float x, float z, TraversalCapability capabilities)
        {
            float terrain = Grid.GetMovementModifier(x, z, capabilities);
            if (terrain <= 0f)
                return 0f;
            return terrain * WeatherSim.EffectiveMovement();
        }

        public bool CanPlaceBuilding(float x, float z) =>
            Grid.AllowsBuilding(x, z) && !Grid.IsBlocked(x, z);
    }
}
