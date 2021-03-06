namespace LiteNetLib
{
    public enum SendOptions
    {
        Unreliable,
        ReliableUnordered,
        Sequenced,
        ReliableOrdered
    }

    public static class NetConstants
    {
        public const int HeaderSize = 1;
        public const int SequencedHeaderSize = 3;
        public const int FragmentHeaderSize = 10;
        public const int DefaultWindowSize = 64;
        public const ushort MaxSequence = 65535;
        public const ushort HalfMaxSequence = MaxSequence / 2;

        //protocol
        public const int MaxUdpHeaderSize = 68;
        public const int PacketSizeLimit = ushort.MaxValue - MaxUdpHeaderSize;
        public const int MinPacketSize = 576 - MaxUdpHeaderSize;
        public const int MinPacketDataSize = MinPacketSize - HeaderSize;
        public const int MinSequencedPacketDataSize = MinPacketSize - SequencedHeaderSize;

        public static readonly int[] PossibleMtu =
        {
            576 - MaxUdpHeaderSize,  //Internet Path MTU for X.25 (RFC 879)
            1492 - MaxUdpHeaderSize, //Ethernet with LLC and SNAP, PPPoE (RFC 1042)
            1500 - MaxUdpHeaderSize  //Ethernet II (RFC 1191)
        };

        //peer specific
        public const int FlowUpdateTime = 1000;
        public const int FlowIncreaseThreshold = 4;
        public const int PacketsPerSecondMax = 65535;
        public const int DefaultPingInterval = 1000;
    }
}
