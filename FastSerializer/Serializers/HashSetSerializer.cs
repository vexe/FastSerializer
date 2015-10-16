using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Vexe.Fast.Serializer.Internal;

namespace Vexe.Fast.Serializer.Serializers
{
    public class HashSetSerializer : IBaseSerializer
    {
        public override bool CanHandle(Type type)
        {
            return type.IsSubclassOfRawGeneric(typeof(HashSet<>));
        }

        public override Type[] GetTypeDependency(Type type)
        {
            return type.GetGenericArgsInRawParentClass(typeof(HashSet<>));
        }

        public override bool RequiresInheritance(Type type)
        {
            return true;
        }

        public override void EmitWrite(Type type)
        {
            //arg0: stream, arg1: value, arg2: ctx

            //  int count = value.Count;
            //  Write(stream, count);
            //  var iter = value.GetEnumerator();
            //  while(iter.MoveNext())
            //    Serialize(stream, iter.Current, ctx);

            EmitHelper.EmitSerializeRef();

            //int count = value.Count;
            //Write(stream, count);
            var intWriter = Basic.GetWriter<int>();
            var getCount = type.GetMethod("get_Count");
            emit.ldarg_0()
                .ldarg_1()
                .call(getCount)
                .call(intWriter);

            //var iter = value.GetEnumerator();
            var getEnumerator = type.GetMethod("GetEnumerator");
            var enumeratorType = getEnumerator.ReturnType;
            var iter = emit.declocal(enumeratorType);
            emit.ldarg_1()
                .call(getEnumerator)
                .stloc_s(iter);

            //while(iter.MoveNext())
            //  Serialize(stream, iter.Current, ctx);
            var BODY = emit.deflabel();
            var CHECK = emit.deflabel();
            var moveNext = enumeratorType.GetMethod("MoveNext");
            var getCurrent = enumeratorType.GetMethod("get_Current");
            var elementType = GetTypeDependency(type)[0];
            var serialize = ctx.GetWriteMethod(elementType);

            emit.br_s(CHECK);

            emit.mark(BODY);
            emit.ldarg_0()
                .ldloca_s(iter)
                .call(getCurrent)
                .ldarg_2()
                .call(serialize);

            emit.mark(CHECK);
            emit.ldloca_s(iter)
                .call(moveNext)
                .brtrue(BODY);

            emit.ret();
        }

        public override void EmitRead(Type type)
        {
            //arg0: stream, arg1: ctx

            //int count = ReadInt32(stream);
            //for(int i = 0; i < count; i++)
            //{
            //   var item = <cast if necessary>Deserialize(stream, ctx);
            //   instance.Add(item);
            //}
            //return instance;

            var id = EmitHelper.EmitDeserializeRef(type);

            //int count = ReadInt32(stream);
            var count = EmitHelper.EmitReadInt();

            //var instance = new HashSet<T>();
            var instance = EmitNewHashSet(type, count);

            EmitHelper.EmitMarkRef(instance, id);

            var BODY = emit.deflabel();
            var CHECK = emit.deflabel();
            var i = emit.declocal<int>();
            var elementType = GetTypeDependency(type)[0];
            var inheritance = ctx.RequiresInheritance(elementType);
            var deserialize = ctx.GetReadMethod(elementType, inheritance);
            var add = type.GetMethod("Add");

            // i = 0;
            emit.ldc_i4_0()
                .stloc_s(i);

            emit.br_s(CHECK);

            emit.mark(BODY);

            // T item = DeserializeT(stream, ctx);
            var item = emit.declocal(elementType);
            emit.ldarg_0()
                .ldarg_1()
                .call(deserialize);
            if (inheritance)
                emit.cast(elementType);

            emit.stloc_s(item);

            //instance.Add(item);
            emit.ldloc_s(instance)
                .ldloc_s(item)
                .call(add) // returns bool, since we're not using the value we need to pop the stack to keep the balance
                .pop();

            // i++;
            emit.increment_s(i);

            emit.mark(CHECK);

            // if (i < count) branch BODY
            emit.ldloc_s(i)
                .ldloc_s(count)
                .blt(BODY);

            // return instance;
            emit.ret_s(instance);
        }

        private LocalBuilder EmitNewHashSet(Type type, LocalBuilder count)
        {
            var instance = emit.declocal(type);
            var ctor = type.GetEmptyConstructor();
            emit.newobj(ctor)
                .stloc_s(instance);
            return instance;
        }
    }
}
