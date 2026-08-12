namespace Asterra.Core
{
    /// <summary>Stable identity for networked / saved entities. Not a Unity InstanceID.</summary>
    public readonly struct EntityId : System.IEquatable<EntityId>
    {
        public readonly uint Value;

        public EntityId(uint value) => Value = value;

        public bool Equals(EntityId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => (int)Value;
        public override string ToString() => $"Id:{Value}";

        public static bool operator ==(EntityId a, EntityId b) => a.Value == b.Value;
        public static bool operator !=(EntityId a, EntityId b) => a.Value != b.Value;
    }

    public readonly struct PlayerId : System.IEquatable<PlayerId>
    {
        public readonly byte Value;

        public PlayerId(byte value) => Value = value;

        public bool Equals(PlayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"P{Value}";

        public static bool operator ==(PlayerId a, PlayerId b) => a.Value == b.Value;
        public static bool operator !=(PlayerId a, PlayerId b) => a.Value != b.Value;
    }

    public readonly struct FactionId : System.IEquatable<FactionId>
    {
        public readonly byte Value;

        public FactionId(byte value) => Value = value;

        public bool Equals(FactionId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is FactionId other && Equals(other);
        public override int GetHashCode() => Value;
        public static bool operator ==(FactionId a, FactionId b) => a.Value == b.Value;
        public static bool operator !=(FactionId a, FactionId b) => a.Value != b.Value;
    }

    public readonly struct Tick : System.IEquatable<Tick>
    {
        public readonly uint Value;

        public Tick(uint value) => Value = value;

        public Tick Next() => new Tick(Value + 1);
        public bool Equals(Tick other) => Value == other.Value;
        public override bool Equals(object obj) => obj is Tick other && Equals(other);
        public override int GetHashCode() => (int)Value;
        public static bool operator ==(Tick a, Tick b) => a.Value == b.Value;
        public static bool operator !=(Tick a, Tick b) => a.Value != b.Value;
    }
}
