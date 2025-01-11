using System;


namespace LightNet.Message
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class KindAttribute : Attribute
    {

        public KindAttribute()
        {

        }
        public KindAttribute(String description)
        {

        }
    }
}
