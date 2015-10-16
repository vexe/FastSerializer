using System;
using System.IO;

namespace Vexe.Fast.Serializer.Serializers
{
    public class TypeSerializer : IStaticSerializer
    {
        public override bool RequiresInheritance(Type type)
        {
            return true;
        }

        public override bool CanHandle(Type type)
        {
            return typeof(Type).IsAssignableFrom(type);
        }

        public override Type[] GetTypeDependency(Type type)
        {
            return new Type[] { typeof(string) };
        }

        public static void SerializeType(Stream stream, Type type)
        {
            var name = type.AssemblyQualifiedName;
            Basic.WriteString(stream, name);
        }

        public static Type DeserializeType(Stream stream)
        {
            string name = Basic.ReadString(stream);
            return Type.GetType(name);
        }
    }
}
