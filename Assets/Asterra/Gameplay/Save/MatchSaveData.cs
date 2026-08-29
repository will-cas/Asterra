using System;
using Asterra.Core;

namespace Asterra.Gameplay.Save
{
    /// <summary>Offline skirmish save (JsonUtility). FormatVersion bumps when fields change.</summary>
    [Serializable]
    public class MatchSaveData
    {
        public int formatVersion = 3;
        public string savedAtUtc;
        public uint matchSeed;
        public string mapKey;
        public int playerFaction;
        public int enemyFaction;
        /// <summary><see cref="Asterra.AI.AiDifficulty"/> as int. Present from formatVersion 2.</summary>
        public int aiDifficulty = 1;
        public uint tick;
        public uint nextEntityId;
        public float holdSecondsP0;
        public float holdSecondsP1;
        public float timeOfDay01;
        public int weatherKind;
        public float weatherIntensity;

        public WalletSave[] wallets = Array.Empty<WalletSave>();
        public string[] unlockedUpgrades = Array.Empty<string>(); // "player|upgradeId"
        public string[] unlockedPowers = Array.Empty<string>();   // "player|powerId"
        public AbilitySave[] abilities = Array.Empty<AbilitySave>();
        public UnitSave[] units = Array.Empty<UnitSave>();
        public BuildingSave[] buildings = Array.Empty<BuildingSave>();
        public TerritorySave[] territories = Array.Empty<TerritorySave>();
        public ResourceSave[] resources = Array.Empty<ResourceSave>();
        public DestructibleSave[] destructibles = Array.Empty<DestructibleSave>();
        /// <summary>Player-dug trenches / bridge decks etc. Present from formatVersion 3.</summary>
        public TerrainCellSave[] terrainCells = Array.Empty<TerrainCellSave>();
    }

    [Serializable]
    public class TerrainCellSave
    {
        public int cellX;
        public int cellZ;
        public ushort defIndex;
    }

    [Serializable]
    public class WalletSave
    {
        public byte player;
        public int gold;
        public int timber;
    }

    [Serializable]
    public class AbilitySave
    {
        public byte player;
        public string powerId;
        public float cooldownRemaining;
        public float buffRemaining;
        public float armorBonus;
        public float moveBonus;
        public float damageBonus;
        public float percentBonus;
        public float buildingMitigation;
        public int effect;
    }

    [Serializable]
    public class UnitSave
    {
        public uint id;
        public byte owner;
        public byte faction;
        public string definitionId;
        public float x;
        public float z;
        public float health;
        public float maxHealth;
        public int stance;
        public float attackCooldownRemaining;
        public float armor;
        public float attackDamage;
        public string equipment0;
        public string equipment1;
        public string equipment2;
        public string equipment3;
        public int equipmentCount;
        public float commanderArmorBonus;
        public float commanderMoveBonus;
        public float commanderDamageBonus;
        public int carryAmount;
        public int carryType;
        public bool hasCarry;
        public bool returningToDeposit;
        public bool attackMoving;
        public bool patrolling;
        public float patrolAX;
        public float patrolAZ;
        public float patrolBX;
        public float patrolBZ;
        public bool patrolToB;
        public float moveTargetX;
        public float moveTargetZ;
        public bool hasMoveTarget;
        public uint attackTargetId;
        public bool hasAttackTarget;
        public uint gatherTargetId;
        public bool hasGatherTarget;
        public uint garrisonBuildingId;
        public bool isGarrisoned;
    }

    [Serializable]
    public class BuildingSave
    {
        public uint id;
        public byte owner;
        public byte faction;
        public string definitionId;
        public float x;
        public float z;
        public int state;
        public float health;
        public float maxHealth;
        public float yawDegrees;
        public float buildSecondsRemaining;
        public float buildSecondsTotal;
        public string productionUnitDefId;
        public float productionSecondsRemaining;
        public float productionSecondsTotal;
        public string researchUpgradeDefId;
        public float researchSecondsRemaining;
        public float researchSecondsTotal;
        public string queue0;
        public string queue1;
        public string queue2;
        public string queue3;
        public int queueCount;
        public float rallyX;
        public float rallyZ;
        public bool hasRally;
        public float attackCooldownRemaining;
        public byte wallLinks;
        public uint linkedDestructibleId;
        public int linkedTraversalLinkId = -1;
        public uint parentBuildingId;
        public bool hasParent;
        public byte attachmentSlotIndex;
        public uint attach0;
        public uint attach1;
        public uint attach2;
        public uint attach3;
        public uint garrison0;
        public uint garrison1;
        public uint garrison2;
        public uint garrison3;
        public uint garrison4;
        public uint garrison5;
        public uint garrison6;
        public uint garrison7;
        public int garrisonCount;
    }

    [Serializable]
    public class TerritorySave
    {
        public uint id;
        public float x;
        public float z;
        public float radius;
        public int goldPerSecond;
        public int state;
        public byte controller;
        public bool hasController;
        public float captureProgress;
    }

    [Serializable]
    public class ResourceSave
    {
        public uint id;
        public int type;
        public int amount;
        public float x;
        public float z;
    }

    [Serializable]
    public class DestructibleSave
    {
        public uint id;
        public string definitionId;
        public float x;
        public float z;
        public float health;
        public int state;
        public int linkedTraversalLinkId;
    }
}
