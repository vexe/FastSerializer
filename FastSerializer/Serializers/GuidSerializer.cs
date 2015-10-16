using System;
using System.IO;

namespace Vexe.Fast.Serializer.Serializers
{
    public class GuidSerializer : IStaticSerializer
    {
        public override bool CanHandle(Type type)
        {
            return type == typeof(Guid);
        }

        public override Type[] GetTypeDependency(Type type)
        {
            return new Type[] { typeof(string) };
        }

        public static void SerializeGuid(Stream stream, Guid value)
        {
            var bytes = value.ToByteArray();
            Basic.WriteByteArray(stream, bytes);
        }

        public static Guid DeserializeGuid(Stream stream)
        {
            byte[] bytes = Basic.ReadByteArray(stream);
            return new Guid(bytes);
        }
    }
}
