using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Vexe.Fast.Serializer.Internal;

namespace Vexe.Fast.Serializer.Serializers
{
    public class ListSerializer : IBaseSerializer
    {
        public override bool CanHandle(Type type)
        {
            return type.IsImplementerOfRawGeneric(typeof(IList<>));
        }

        public override Type[] GetTypeDependency(Type type)
        {
            return type.GetGenericArgsInRawParentInterface(typeof(IList<>));
        }

        public override bool RequiresInheritance(Type type)
        {
            return true;
        }

        private Type GetElementType(Type listType)
        {
            return GetTypeDependency(listType)[0];
        }

        public override void EmitWrite(Type type)
        {
            // arg0: stream, arg1: value, arg2: ctx

            //int count = value.Count;
            //WriteInt(stream, count);
            //for(int i = 0; i < count; i++)
                //SerializeT(stream, value[i], ctx);

            EmitHelper.EmitSerializeRef();

            // int count = value.Count
            var getCount = type.GetMethod("get_Count");
            var count = emit.declocal<int>();
            emit.ldarg_1()
                .call(getCount)
                .stloc_s(count);

            // WriteInt(stream, count)
            EmitHelper.EmitWriteCount(count);

            var elementType = GetElementType(type);
            var serialize = ctx.GetWriteMethod(elementType);
            var getItem = type.GetMethod("get_Item");

            var i = emit.declocal(typeof(int));
            var BODY = emit.deflabel();
            var CHECK = emit.deflabel();

            // i = 0
            emit.ldc_i4_0()
                .stloc_s(i);

            emit.br_s(CHECK);

            emit.mark(BODY);

            // Serialize(stream, value[i], ctx);
            emit.ldarg_0()
                .ldarg_1()
                .ldloc_s(i)
                .call(getItem)
                .ldarg_2()
                .call(serialize);

            // i++
            emit.increment_s(i);

            emit.mark(CHECK);

            // if (i < count) branch BODY
            emit.ldloc_s(i)
                .ldloc_s(count)
                .blt(BODY);

            emit.ret();
        }

        public override void EmitRead(Type type)
        {
            // arg0: stream, arg1: ctx

            //for(int i = 0; i < count; i++)
            //  instance[i] = <cast if necessary>DeserializeT(stream, ctx);
            //return instance;

            var id = EmitHelper.EmitDeserializeRef(type);

            // int count = ReadInt32(stream);
            var count = EmitHelper.EmitReadInt();

            // var instance = new List<T>(count);
            bool hasCapacityCtor;
            var instance = EmitHelper.EmitNewCollection(type, count, out hasCapacityCtor);
            if (!hasCapacityCtor)
            {
                var setCapacity = type.GetMethod("set_Capacity");
                emit.ldloc_s(instance)
                    .ldloc_s(count)
                    .call(setCapacity);
            }

            EmitHelper.EmitMarkRef(instance, id);

            var BODY = emit.deflabel();
            var CHECK = emit.deflabel();
            var i = emit.declocal<int>();
            var elementType = GetElementType(type);
            var inheritance = ctx.RequiresInheritance(elementType);
            var deserialize = ctx.GetReadMethod(elementType, inheritance);
            var add = type.GetMethod("Add");

            // i = 0
            emit.ldc_i4_0()
                .stloc_s(i);

            emit.br_s(CHECK);

            emit.mark(BODY);

            // T element = <cast if necessary>DeserializeT(stream, ctx);
            var element = emit.declocal(elementType);
            emit.ldarg_0()
                .ldarg_1()
                .call(deserialize);

            if (inheritance)
                emit.cast(elementType);

            emit.stloc_s(element);

            // value.Add(element);
            emit.ldloc_s(instance)
                .ldloc_s(element)
                .call(add);

            // i++
            emit.increment_s(i);

            emit.mark(CHECK);

            // if (i < count) branch BODY
            emit.ldloc_s(i)
                .ldloc_s(count)
                .blt(BODY);

            // return instance;
            emit.ret_s(instance);
        }
    }
}
