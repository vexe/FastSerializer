using System;
using System.IO;
using System.Reflection;

namespace Vexe.Fast.Serializer.Serializers
{
    public class PropertyInfoSerializer : IStaticSerializer
    {
        public override bool CanHandle(Type type)
        {
            return typeof(PropertyInfo).IsAssignableFrom(type);
        }

        public override Type[] GetTypeDependency(Type type)
        {
            return new Type[] { typeof(string), typeof(Type) };
        }

        public static void SerializePropertyInfo(Stream stream, PropertyInfo field)
        {
            TypeSerializer.SerializeType(stream, field.DeclaringType);
            Basic.WriteString(stream, field.Name);
        }

        public static PropertyInfo DeserializePropertyInfo(Stream stream)
        {
            var declaringType = TypeSerializer.DeserializeType(stream);
            var propertyName = Basic.ReadString(stream);
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var property = declaringType.GetProperty(propertyName, flags);
            return property;
        }
    }

    public class FieldInfoSerializer : IStaticSerializer
    {
        public override bool CanHandle(Type type)
        {
            return typeof(FieldInfo).IsAssignableFrom(type);
        }

        public override Type[] GetTypeDependency(Type type)
        {
            return new Type[] { typeof(string), typeof(Type) };
        }

        public static void SerializeFieldInfo(Stream stream, FieldInfo field)
        {
            TypeSerializer.SerializeType(stream, field.DeclaringType);
            Basic.WriteString(stream, field.Name);
        }

        public static FieldInfo DeserializeFieldInfo(Stream stream)
        {
            var declaringType = TypeSerializer.DeserializeType(stream);
            var fieldName = Basic.ReadString(stream);
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var field = declaringType.GetField(fieldName, flags);
            return field;
        }
    }

    public class MethodInfoSerializer : IStaticSerializer
    {
        public override bool CanHandle(Type type)
        {
            return typeof(MethodInfo).IsAssignableFrom(type);
        }

        public override Type[] GetTypeDependency(Type type)
        {
            return new Type[] { typeof(string), typeof(Type) };
        }

        // we write the method declaring type, name and argument types
        public static void SerializeMethodInfo(Stream stream, MethodInfo method)
        {
            TypeSerializer.SerializeType(stream, method.DeclaringType);
            Basic.WriteString(stream, method.Name);
            var args = method.GetParameters();
            Basic.WriteInt32(stream, args.Length);
            for(int i = 0; i < args.Length; i++)
                TypeSerializer.SerializeType(stream, args[i].ParameterType);
        }

        public static MethodInfo DeserializeMethodInfo(Stream stream)
        {
            var declaringType = TypeSerializer.DeserializeType(stream);
            var methodName = Basic.ReadString(stream);
            var argCount = Basic.ReadInt32(stream);
            var paramTypes = new Type[argCount];
            for(int i = 0; i < argCount; i++)
                paramTypes[i] = TypeSerializer.DeserializeType(stream);

            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var method = declaringType.GetMethod(methodName, flags, null, paramTypes, null);
            return method;
        }
    }
}