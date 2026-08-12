namespace Asterra.Core
{
    public sealed class SequentialIdFactory : IIdFactory
    {
        private uint _next = 1;

        public EntityId Next() => new EntityId(_next++);
    }
}
