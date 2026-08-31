using Asterra.Gameplay.World;

namespace Asterra.Gameplay.Content
{
    /// <summary>Applies built-in traversal links from <see cref="BuiltinMaps"/>.</summary>
    public static class SkirmishMapTraversal
    {
        public static void Apply(WorldEnvironmentSim environment, SkirmishMapId map)
        {
            if (environment == null)
                return;
            SkirmishMapLoader.ApplyTraversal(environment, BuiltinMaps.Definition(map));
        }
    }
}
