
using System;
using System.Buffers;

namespace KestrelServer
{

    public enum ParseResult
    {
        Illicit = 0,
        Partial = 1,
        Ok = 2,
    }




    public partial class GMessage
    {
        public static readonly UInt32 MinimumSize = CalcMinimumSize();
        private static UInt32 CalcMinimumSize()
        {
            UInt32 size = 0;
            size += sizeof(UInt16);  //HEADER
            size += sizeof(UInt32);  // FLAGES + TOTALLength
            size += sizeof(UInt32);  // Action
            return size;
        }




        public static UInt32 ReadLength(SequenceReader<byte> reader)
        {
            reader.TryRead<UInt16>(out var header);
            if (header != Header) return UInt32.MaxValue;
            reader.TryRead<UInt32>(out var combineValue);
            GMessage.Split(combineValue, out Byte _, out var packetLen);
            return packetLen;
        }



        private static void Split(uint combineValue, out GMFlags flags, out uint remainingBytes)
        {
            flags = (GMFlags)((combineValue >> 24) & 0xFF);
            remainingBytes = (combineValue & 0x00FFFFFF) ^ 0xFFFFFF;
        }

        private static void Split(uint combineValue, out byte firstByte, out uint remainingBytes)
        {
            firstByte = (byte)((combineValue >> 24) & 0xFF);
            remainingBytes = (combineValue & 0x00FFFFFF) ^ 0xFFFFFF;
        }
    }
}
