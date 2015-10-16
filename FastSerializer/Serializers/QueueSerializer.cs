using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Vexe.Fast.Serializer.Internal;

namespace Vexe.Fast.Serializer.Serializers
{
    public class QueueSerializer : IBaseSerializer
    {
        public override bool CanHandle(Type type)
        {
            return type.IsSubclassOfRawGeneric(typeof(Queue<>));
        }

        public override Type[] GetTypeDependency(Type type)
        {
            var elementType = GetElementType(type);
            return new Type[] { elementType.MakeArrayType() };
        }

        public override bool RequiresInheritance(Type type)
        {
            return true;
        }

        private Type GetElementType(Type queueType)
        {
            return queueType.GetGenericArgsInRawParentClass(typeof(Queue<>))[0];
        }

        public override void EmitWrite(Type type)
        {
            // arg0: stream, arg1: value, arg2: ctx

            EmitHelper.EmitSerializeRef();

            // no need to write count if we're not doing anything with when reading!
            if (type.HasConstructor<int>())
            {
                var getCount = type.GetMethod("get_Count");
                EmitHelper.EmitWriteCount(getCount);
            }

            // var array = value.ToArray();
            var elementType = GetElementType(type);
            var arrayType = elementType.MakeArrayType();
            var toArray = type.GetMethod("ToArray");
            var array = emit.declocal(arrayType);
            emit.ldarg_1()
                .call(toArray)
                .stloc_s(array);

            // Serialize(stream, array, ctx);
            var serialize = ctx.GetWriteStub(arrayType);
            emit.ldarg_0()
                .ldloc_s(array)
                .ldarg_2()
                .call(serialize);

            emit.ret();
        }

        public override void EmitRead(Type type)
        {
            //arg0: stream, arg1: ctx

            //var instance = get new queue
            //mark instance
            //deserialize from array

            var id = EmitHelper.EmitDeserializeRef(type);

            LocalBuilder instance;
            if (type.HasConstructor<int>())
            {
                var count = EmitHelper.EmitReadInt();
                instance = EmitHelper.EmitNewCollection(type, count);
            }
            else instance = EmitHelper.EmitNewCollection(type, null);

            EmitHelper.EmitMarkRef(instance, id);

            // T[] array = Deserialize(stream, ctx);
            var elementType = GetElementType(type);
            var arrayType = elementType.MakeArrayType();
            var array = emit.declocal(arrayType);
            var deserialize = ctx.GetReadStub(arrayType);
            emit.ldarg_0()
                .ldarg_1()
                .call(deserialize)
                .stloc_s(array);

            // iterate over array and enqueue
            EmitHelper.EmitArrayForLoop(arrayType, array, i =>
            {
                var enqueue = type.GetMethod("Enqueue");
                emit.ldloc_s(instance);
                EmitHelper.EmitLoadArrayElement(array, i, elementType);
                emit.call(enqueue);
            });

            emit.ret_s(instance);
        }
    }
}
