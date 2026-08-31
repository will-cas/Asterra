using Asterra.Gameplay.World;

namespace Asterra.Gameplay.Content
{
    /// <summary>Applies built-in map terrain from <see cref="BuiltinMaps"/>.</summary>
    public static class SkirmishMapTerrain
    {
        public static void Apply(WorldEnvironmentSim environment, SkirmishMapId map)
        {
            if (environment == null)
                return;
            SkirmishMapLoader.ApplyTerrain(environment, BuiltinMaps.Definition(map));
        }
    }
}
