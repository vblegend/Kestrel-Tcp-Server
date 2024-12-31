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



    public struct TestClass : IMessagePayload
    {
        public Int32 X = 123;
        public String Text = "Hello";
        public Int32 Y = 321;

        public TestClass()
        {
        }

        public void Read(SequenceReader<byte> reader)
        {
            reader.TryRead<Int32>(out X);
            reader.TryReadString(out Text);
            reader.TryRead<Int32>(out Y);
        }


        public void Write(IBufferWriter<byte> writer)
        {
            writer.Write(X);
            writer.Write(Text, Encoding.UTF8);
            writer.Write(Y);
        }
    }
}
