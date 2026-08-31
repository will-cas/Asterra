using Asterra.Core;
using Asterra.Gameplay.Sim;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay.Content
{
    /// <summary>Spawns built-in destructibles from <see cref="BuiltinMaps"/>.</summary>
    public static class SkirmishMapDestructibles
    {
        public static void Apply(SkirmishWorldSim world, IIdFactory ids, SkirmishMapId map)
        {
            if (world == null)
                return;
            SkirmishMapLoader.ApplyDestructibles(world, ids, BuiltinMaps.Definition(map));
        }
    }
}
