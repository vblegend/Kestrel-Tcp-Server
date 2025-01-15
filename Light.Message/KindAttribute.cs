using System;

namespace Light.Message
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
