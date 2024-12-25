using System;
using System.Buffers;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            size += sizeof(UInt32);  // SerialNumber
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



        public static ParseResult Parse(SequenceReader<byte> reader, out GMessage message, out UInt32 packetLen)
        {
            packetLen = 0;
            message = default;
            reader.TryRead<UInt16>(out var header);
            if (header != Header) return ParseResult.Illicit;
            reader.TryRead<UInt32>(out var combineValue);
            GMessage.Split(combineValue, out GMFlags _flags, out packetLen);
            if (reader.Length < packetLen) return ParseResult.Partial;
            message = GMessage.Create();
            reader.TryRead<UInt32>(out message.SerialNumber);
            reader.TryRead<UInt32>(out message.Action);
            if ((_flags & GMFlags.HasTimestamp) == GMFlags.HasTimestamp)
            {
                reader.TryRead<UInt32>(out message.Timestamp);
            }
            if ((_flags & GMFlags.HasParams) == GMFlags.HasParams)
            {
                reader.TryRead<Byte>(out var paramsLen);
                message.Parameters.Alloc(paramsLen);
                for (int i = 0; i < paramsLen; i++)
                {
                    reader.TryRead<Int32>(out message.Parameters.Data[i]);
                }
            }
            if ((_flags & GMFlags.HasData) == GMFlags.HasData)
            {
                reader.TryRead<UInt32>(out var length);
                message.Payload.Alloc((Int32)length);
                var span = new Span<Byte>(message.Payload.Data, 0, (Int32)length);
                reader.TryCopyTo(span);
            }
            return ParseResult.Ok;
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
