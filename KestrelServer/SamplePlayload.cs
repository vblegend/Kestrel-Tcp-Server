using KestrelServer.Message;
using System.Buffers;
using System.Text;
using System;
using System.Runtime.InteropServices;


namespace KestrelServer
{





    [StructLayout(LayoutKind.Auto)]
    public struct StringPayload : IMessagePayload
    {
        public String Text = "Hello";

        public StringPayload(String text)
        {
            this.Text = text;
        }


        public void Read(SequenceReader<byte> reader)
        {
            reader.TryReadString(out Text);
        }


        public void Write(IBufferWriter<byte> writer)
        {
            writer.Write(Text, Encoding.UTF8);
        }


        public override string ToString()
        {
            return Text;
        }

    }



    public struct SamplePlayload : IMessagePayload
    {
        public Int64 X = 123;

        public SamplePlayload(Int64 X)
        {
            this.X = X;
        }

        public void Read(SequenceReader<byte> reader)
        {
            reader.TryRead<Int64>(out X);
        }


        public void Write(IBufferWriter<byte> writer)
        {
            writer.Write(X);
        }
    }
}
