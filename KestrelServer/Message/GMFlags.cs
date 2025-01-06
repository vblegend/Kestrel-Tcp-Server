using System;

namespace KestrelServer.Message
{
    [Flags]
    public enum GMFlags : Byte
    {
        None = 0b00000000,             // Kind 1字节 | len 2字节
        Kind2 = 0b00000001,            // Kind 2字节
        Flag7 = 0b00000010,            // 预留标志位
        Flag6 = 0b00000100,            // 预留标志位
        Flag5 = 0b00001000,            // 预留标志位
        Flag4 = 0b00010000,            // 预留标志位
        Flag3 = 0b00100000,            // 预留标志位
        Flag2 = 0b01000000,            // 预留标志位
        Flag1 = 0b10000000,            // 预留标志位
    }


}
