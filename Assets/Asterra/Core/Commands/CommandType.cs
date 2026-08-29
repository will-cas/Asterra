namespace Asterra.Core
{
    public enum CommandType : byte
    {
        Move = 1,
        Attack = 2,
        PlaceBuilding = 3,
        TrainUnit = 4,
        CaptureTerritory = 5,
        ChooseUpgrade = 6,
        SetStance = 7,
        Gather = 8,
        SetRally = 9,
        CancelProduction = 10,
        AttackMove = 11,
        Stop = 12,
        Patrol = 13,
        ActivateCommanderAbility = 14,
        EnterGarrison = 15,
        ExitGarrison = 16,
        ApplyUnitUpgrade = 17,
        UnlockPower = 18,
        AttachBuilding = 19,
        DigTrench = 20,
        DemolishBuilding = 21,
        TerrainWork = 22,
        RepairBridge = 23,
        UpgradeBuilding = 24,
    }
}
