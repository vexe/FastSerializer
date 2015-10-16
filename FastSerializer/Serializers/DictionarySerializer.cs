using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using Vexe.Fast.Serializer.Internal;

namespace Vexe.Fast.Serializer.Serializers
{
    public class DictionarySerializer : IBaseSerializer
    {
        public override bool CanHandle(Type type)
        {
            return type.IsImplementerOfRawGeneric(typeof(IDictionary<,>));
        }

        public override Type[] GetTypeDependency(Type type)
        {
            return type.GetGenericArgsInRawParentInterface(typeof(IDictionary<,>));
        }

        public override bool RequiresInheritance(Type type)
        {
            return true;
        }

        public override void EmitWrite(Type type)
        {
            // arg0: stream, arg1: value, arg2: ctx

            //WriteInt32(stream, value.Count);
            //var iter = value.GetEnumerator();
            //while(iter.MoveNext())
            //{
            //    var cur = iter.Current;
            //    Serialize(stream, cur.Key, ctx);
            //    Serialize(stream, cur.Value, ctx);
            //}

            EmitHelper.EmitSerializeRef();

            var genArgs = GetTypeDependency(type);
            var TKey = genArgs[0];
            var TValue = genArgs[1];
            var getEnumerator = type.GetMethod("GetEnumerator");
            var enumeratorType = getEnumerator.ReturnType;
            var moveNext = enumeratorType.GetMethod("MoveNext");
            if (moveNext == null)
                throw new Exception("Enumerator type " + enumeratorType
                        + " in dictionary type " + type + " doesn't have a MoveNext method defined "
                        + " If you're using a custom Enumerator, make sure the GetEnumerator that "
                        + " returns it is visible and all other GetEnumerators are not (expclitly implemented perhaps");

            var getCurrent = enumeratorType.GetMethod("get_Current");
            var getCurrentType = getCurrent.ReturnType;
            var getKey = getCurrentType.GetMethod("get_Key");
            var getValue = getCurrentType.GetMethod("get_Value");
            var iter = emit.declocal(enumeratorType);

            // WriteInt(stream, value.Count)
            var getCount = type.GetMethod("get_Count");
            EmitHelper.EmitWriteCount(getCount);

            // iter = value.GetEnumerator()
            emit.ldarg_1()
                .callvirt(getEnumerator)
                .stloc_s(iter);

            var BODY = emit.deflabel();
            var CHECK = emit.deflabel();

            emit.br_s(CHECK);

            emit.mark(BODY);

            // KeyValuePair<TKey, TValue> current = iter.Current;
            var current = emit.declocal(getCurrentType);
            emit.ldloca_s(iter)
                .call(getCurrent)
                .stloc_s(current);

            // Serialize(stream, current.Key, ctx)
            var serializeKey = ctx.GetWriteMethod(TKey);
            emit.ldarg_0()
                .ldloca_s(current)
                .call(getKey)
                .ldarg_2()
                .call(serializeKey);

            // Serialize(stream, current.Value)
            var serializeValue = ctx.GetWriteMethod(TValue);
            emit.ldarg_0()
                .ldloca_s(current)
                .call(getValue)
                .ldarg_2()
                .call(serializeValue);

            emit.mark(CHECK);

            // if (iter.MoveNext()) branch BODY
            emit.ldloca_s(iter)
                .call(moveNext)
                .brtrue_s(BODY);

            emit.ret();
        }

        public override void EmitRead(Type type)
        {
                        // arg0: stream, arg1: ctx

            //for(int i = 0; i < count; i++)
            //{
            //  TK key = <cast if necessary>Deserialize'TKey'(stream, ctx);
            //	TV value = <cast if necessary>Deserialize'TValue'(stream, ctx);
            //	instance.Add(key, value);
            //}

            var id = EmitHelper.EmitDeserializeRef(type);

            // int count = ReadInt32(stream);
            var count = EmitHelper.EmitReadInt();

            // var instance = new Dictionary<K, V>(count);
            var instance = EmitHelper.EmitNewCollection(type, count);

            EmitHelper.EmitMarkRef(instance, id);

            var BODY = emit.deflabel();
            var CHECK = emit.deflabel();
            var i = emit.declocal(typeof(int));
            var genArgs = GetTypeDependency(type);
            var TKey = genArgs[0];
            var TValue = genArgs[1];

            // i = 0
            emit.ldc_i4_0()
                .stloc_s(i);

            emit.br_s(CHECK);

            emit.mark(BODY);

            // TKey key = Deserialize(stream, ctx);
            var key = Deserialize(TKey);

            // TValue value = Deserialize(stream, ctx);
            var value = Deserialize(TValue);

            // instance.Add(key, value)
            var add = type.GetMethod("Add", genArgs);
            emit.ldloc_s(instance)
                .ldloc_s(key)
                .ldloc_s(value)
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

        LocalBuilder Deserialize(Type type)
        {
            var inheritance = ctx.RequiresInheritance(type);
            var deserialize = ctx.GetReadMethod(type, inheritance);
            var local = emit.declocal(type);

            emit.ldarg_0()
                .ldarg_1()
                .call(deserialize);

            if (inheritance)
                emit.cast(type);

            emit.stloc_s(local);

            return local;
        }
    }
}
