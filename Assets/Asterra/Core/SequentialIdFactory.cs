namespace Asterra.Core
{
    public sealed class SequentialIdFactory : IIdFactory
    {
        private uint _next = 1;

        public uint PeekNext => _next;

        public SimEntityId Next() => new SimEntityId(_next++);

        public void Seek(uint nextId)
        {
            _next = nextId < 1u ? 1u : nextId;
        }
    }
}
