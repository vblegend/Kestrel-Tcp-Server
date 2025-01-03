using System;

namespace KestrelServer.Message
{
    [Flags]
    public enum GMFlags : Byte
    {
        None = 0b00000000,            // 普通的包
        SSSSS = 0b00000001,    // 小端序，否则大端序
        Kind2 = 0b00000010,      // 包含Action参数
        Kind3 = 0b00000100,       // 包含数据区
        Kind4 = 0b00001000,   // 数据区被压缩了
        LargePacket = 0b00010000,   // 包含时间戳
        HasTimestamp = 0b00100000,   // 大封包，超过256字节
        Flag1 = 0b01000000,     // 64
        Flag2 = 0b10000000,    // 128
    }


}
