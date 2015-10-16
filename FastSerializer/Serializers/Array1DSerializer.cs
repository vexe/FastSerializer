using System;
using System.Reflection.Emit;
using Vexe.Fast.Serializer.Internal;

namespace Vexe.Fast.Serializer.Serializers
{
    public class Array1DSerializer : IBaseSerializer
    {
        public override bool CanHandle(Type type)
        {
            return type.IsArray && type.GetArrayRank() == 1;
        }

        public override Type[] GetTypeDependency(Type type)
        {
            return new Type[] { type.GetElementType() };
        }

        private LocalBuilder EmitNewArray(Type type, LocalBuilder count)
        {
            var elementType = type.GetElementType();
            var array = emit.declocal(type);
            emit.ldloc_s(count)
                .newarr(elementType)
                .stloc_s(array);
            return array;
        }

        public override void EmitWrite(Type type)
        {
            // arg0: stream, arg1: value, arg2: ctx

            //if (value == null)
            //{
            //    WriteInt32(stream, -1);
            //    return;
            //}
            //WriteInt32(stream, value.Length);
            //for(int i = 0; i < value.Length; i++)
            //    Serialize(stream, value[i], ctx);

            // if (value == null) WriteInt(stream, -1); return;
            EmitHelper.HandleNullWrite();

            // WriteInt(stream, value.Length);
            var writeInt = Basic.GetWriter<int>();
            emit.ldarg_0()
                .ldarg_1()
                .ldlen()
                .call(writeInt);

            EmitHelper.EmitSerializeRef();

            EmitHelper.EmitArrayForLoop(type, null, i =>
            {
                var elementType = type.GetElementType();
                var serialize = ctx.GetWriteMethod(elementType);

                // Serialize(stream, value[i], ctx)
                emit.ldarg_0()
                    .ldarg_1()
                    .ldloc_s(i)
                    .ldelem(elementType)
                    .ldarg_2()
                    .call(serialize);
            });

            emit.ret();
        }

        public override void EmitRead(Type type)
        {
            // arg0: stream, arg1: ctx

            //int count = ReadInt32(stream);
            //if (count == -1)
                //return null;
            //T[] array = new T[count];
            //for(int i = 0; i < count; i++)
                //array[i] = <cast if necessary>DeserializeT(stream, ctx);
            //return array;

            // int count = ReadInt32(stream)
            var count = EmitHelper.EmitReadInt();

            // if (count == -1) return null;
            EmitHelper.HandleNullRead(count);

            var id = EmitHelper.EmitDeserializeRef(type);

            // T[] instance = new T[count]
            var instance = EmitNewArray(type, count);

            EmitHelper.EmitMarkRef(instance, id);

            EmitHelper.EmitArrayForLoop(type, instance, i =>
            {
                var elementType = type.GetElementType();
                var inheritance = ctx.RequiresInheritance(elementType);
                var deserialize = ctx.GetReadMethod(elementType, inheritance);

                // instance[i] = <cast if necessary>Deserialize(stream, ctx);
                emit.ldloc_s(instance)
                    .ldloc_s(i);

                emit.ldarg_0()
                    .ldarg_1()
                    .call(deserialize);

                if (inheritance)
                    emit.cast(elementType);

                emit.stelem(elementType);
            });

            // return instance
            emit.ret_s(instance);
        }
    }
}
