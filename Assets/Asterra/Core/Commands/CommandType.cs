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
    }
}
