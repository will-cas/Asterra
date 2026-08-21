using System;

namespace Asterra.Core.World
{
    /// <summary>
    /// Designer-authored skirmish map (JSON via Unity JsonUtility).
    /// Built-in maps stay in C#; custom maps live under Shared/Maps and StreamingAssets.
    /// </summary>
    [Serializable]
    public class MapDefinition
    {
        public string id = "new_map";
        public string displayName = "New Map";
        public int formatVersion = 1;
        public float playableHalfExtent = 450f;
        public float cellSize = 10f;
        public ushort defaultTerrain = 1;
        public float cameraFocusX = -320f;
        public float cameraFocusZ = 0f;

        public MapTerrainPaint[] terrain = Array.Empty<MapTerrainPaint>();
        public MapBlockedRect[] blocked = Array.Empty<MapBlockedRect>();
        public MapKeepSpawn[] keeps = Array.Empty<MapKeepSpawn>();
        public MapUnitSpawn[] units = Array.Empty<MapUnitSpawn>();
        public MapBuildingSpawn[] buildings = Array.Empty<MapBuildingSpawn>();
        public MapResourceNode[] resources = Array.Empty<MapResourceNode>();
        public MapTerritory[] territories = Array.Empty<MapTerritory>();
        public MapDestructible[] destructibles = Array.Empty<MapDestructible>();
        public MapTraversalLink[] traversalLinks = Array.Empty<MapTraversalLink>();

        public void EnsureArrays()
        {
            terrain ??= Array.Empty<MapTerrainPaint>();
            blocked ??= Array.Empty<MapBlockedRect>();
            keeps ??= Array.Empty<MapKeepSpawn>();
            units ??= Array.Empty<MapUnitSpawn>();
            buildings ??= Array.Empty<MapBuildingSpawn>();
            resources ??= Array.Empty<MapResourceNode>();
            territories ??= Array.Empty<MapTerritory>();
            destructibles ??= Array.Empty<MapDestructible>();
            traversalLinks ??= Array.Empty<MapTraversalLink>();
        }
    }

    [Serializable]
    public class MapTerrainPaint
    {
        /// <summary>"rect" or "disk".</summary>
        public string shape = "rect";
        public float minX;
        public float minZ;
        public float maxX;
        public float maxZ;
        /// <summary>Disk center (also used when shape=disk).</summary>
        public float x;
        public float z;
        public float radius = 10f;
        /// <summary>DefaultTerrainCatalog index (preferred when terrainId empty).</summary>
        public ushort terrainIndex = 1;
        /// <summary>Optional def id e.g. terrain_water_deep — resolved at load.</summary>
        public string terrainId = string.Empty;
    }

    [Serializable]
    public class MapBlockedRect
    {
        public float minX;
        public float minZ;
        public float maxX;
        public float maxZ;
        public bool blocked = true;
    }

    [Serializable]
    public class MapKeepSpawn
    {
        /// <summary>0 = west/seat A, 1 = east/seat B.</summary>
        public int seatIndex;
        public float x;
        public float z;
    }

    [Serializable]
    public class MapUnitSpawn
    {
        public int seatIndex;
        /// <summary>builder | basic | ranged | cavalry | siege | boat | pathfinder | leader</summary>
        public string role = "basic";
        public float x;
        public float z;
    }

    [Serializable]
    public class MapBuildingSpawn
    {
        public int seatIndex;
        /// <summary>tower | wall | producer | outpost</summary>
        public string role = "tower";
        public float x;
        public float z;
        public float yawDegrees;
    }

    [Serializable]
    public class MapResourceNode
    {
        /// <summary>gold | timber</summary>
        public string type = "gold";
        public int amount = 500;
        public float x;
        public float z;
    }

    [Serializable]
    public class MapTerritory
    {
        public float x;
        public float z;
        public float radius = 40f;
        public int goldPerSecond = 8;
    }

    [Serializable]
    public class MapDestructible
    {
        /// <summary>tree | rock | bridge</summary>
        public string catalogId = "tree";
        public float x;
        public float z;
        /// <summary>-1 = none. Index into map.traversalLinks when this prop disables a link on destroy.</summary>
        public int linkedTraversalLinkId = -1;
    }

    [Serializable]
    public class MapTraversalLink
    {
        public float startX;
        public float startZ;
        public float endX;
        public float endZ;
        /// <summary>bridge | jump | shore | ford</summary>
        public string type = "bridge";
        public float durationSeconds = 1.25f;
        public float approachRadius = 8f;
        public bool enabled = true;
    }

    /// <summary>Menu entry for built-in or custom maps.</summary>
    [Serializable]
    public class MapManifestEntry
    {
        public string id;
        public string displayName;
        public bool builtin;
        public int builtinIndex = -1;
    }

    [Serializable]
    public class MapManifest
    {
        public MapManifestEntry[] maps = Array.Empty<MapManifestEntry>();
    }
}
