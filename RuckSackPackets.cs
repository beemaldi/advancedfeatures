
using ProtoBuf;

namespace RuckSack
{
    public enum RuckSackAttachKind
    {
        Bedroll = 1,
        Quartz = 2
    }

    [ProtoContract]
    public sealed class RuckSackAttachRequestPacket
    {
        [ProtoMember(1)]
        public int X;

        [ProtoMember(2)]
        public int Y;

        [ProtoMember(3)]
        public int Z;

        [ProtoMember(4)]
        public int Kind;
        [ProtoMember(5)]
        public string? Variant;
    }

    [ProtoContract]
    public sealed class RuckSackDetachRequestPacket
    {
        [ProtoMember(1)]
        public int X;

        [ProtoMember(2)]
        public int Y;

        [ProtoMember(3)]
        public int Z;
        [ProtoMember(4)]
        public int Kind;
    }

    [ProtoContract]
    public sealed class RuckSackQuartzLightStatePacket
    {
        [ProtoMember(1)]
        public long EntityId;

        [ProtoMember(2)]
        public bool Active;
        [ProtoMember(3)]
        public int H;

        [ProtoMember(4)]
        public int S;

        [ProtoMember(5)]
        public int V;
    }
}
