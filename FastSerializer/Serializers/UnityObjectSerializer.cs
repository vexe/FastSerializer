using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Vexe.Fast.Serializer.Internal;
//#if UNITY_EDITOR
using UnityObject = UnityEngine.Object;
//#else
//using UnityObject = Vexe.Fast.Serializer.Serializers.FakeUnityObject;
//#endif

namespace Vexe.Fast.Serializer.Serializers
{
    public class FakeUnityObject { }

    public class UnityObjectSerializer : IBaseSerializer
    {
        public override bool CanHandle(Type type)
        {
            return typeof(UnityObject).IsAssignableFrom(type);
        }

        public override Type[] GetTypeDependency(Type type)
        {
            if (typeof(MonoBehaviour).IsAssignableFrom(type) || typeof(ScriptableObject).IsAssignableFrom(type))
                return ReflectionHelper.GetSerializableMembersTypes(ctx.Predicates, type);
            return Type.EmptyTypes;
        }

        MethodInfo _serialize = typeof(UnityObjectSerializer).GetMethod("SerializeUnityObject", BindingFlags.Public | BindingFlags.Static);
        public override void EmitWrite(Type type)
        {
            //arg0: stream, arg1: value, arg2: ctx
            EmitHelper.EmitCall(_serialize, 3);
        }

        MethodInfo _deserialize = typeof(UnityObjectSerializer).GetMethod("DeserializeUnityObject", BindingFlags.Public | BindingFlags.Static);
        public override void EmitRead(Type type)
        {
            // arg0: stream, arg1: ctx

            //return (T)DeserializeUnityObject(stream, ctx);
            emit.ldarg_0()
                .ldarg_1()
                .call(_deserialize)
                .cast(type)
                .ret();
        }

        public static void SerializeUnityObject(Stream stream, UnityObject obj, SerializationContext ctx)
        {
            var container = (List<UnityObject>)ctx.Context;
            var idx = container.Count;
            Basic.WriteInt32(stream, idx);
            container.Add(obj);
        }

        public static UnityObject DeserializeUnityObject(Stream stream, SerializationContext ctx)
        {
            var container = (List<UnityObject>)ctx.Context;
            int idx = Basic.ReadInt32(stream);
            if (idx < 0 || idx >= container.Count)
                return null;
            return container[idx];
        }
    }
}
