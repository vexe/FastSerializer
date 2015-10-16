using System;
using System.IO;
using System.Reflection;
using Vexe.Fast.Serializer.Internal;
using Vexe.Fast.Serializer.Serializers;

namespace Vexe.Fast.Serializer
{
    public abstract class IBaseSerializer
    {
        public EmitContext ctx;
        public static readonly ILEmitter emit = EmitHelper.emit;

        internal static readonly FieldInfo ContextField = typeof(SerializationContext).GetField("Context");

        public abstract bool CanHandle(Type type);
        public abstract Type[] GetTypeDependency(Type type);
        public abstract void EmitWrite(Type type);
        public abstract void EmitRead(Type type);

        public virtual bool RequiresInheritance(Type type)
        {
            return false;
        }

        public static void Write(Stream stream, int value)
        {
            Basic.WriteInt32(stream, value);
        }

        public static void Write(Stream stream, string value)
        {
            Basic.WriteString(stream, value);
        }

        public static void Write(Stream stream, float value)
        {
            Basic.WriteSingle(stream, value);
        }

        public static void Write(Stream stream, bool value)
        {
            Basic.WriteBoolean(stream, value);
        }

        public static void Write(Stream stream, Type type)
        {
            TypeSerializer.SerializeType(stream, type);
        }

        public static int ReadInt(Stream stream)
        {
            return Basic.ReadInt32(stream);
        }

        public static string ReadString(Stream stream)
        {
            return Basic.ReadString(stream);
        }

        public static float ReadFloat(Stream stream)
        {
            return Basic.ReadSingle(stream);
        }

        public static bool ReadBool(Stream stream)
        {
            return Basic.ReadBoolean(stream);
        }

        public static Type ReadType(Stream stream)
        {
            return TypeSerializer.DeserializeType(stream);
        }
    }
}
