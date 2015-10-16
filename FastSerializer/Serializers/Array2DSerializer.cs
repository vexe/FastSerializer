using System;
using Vexe.Fast.Serializer.Internal;

namespace Vexe.Fast.Serializer.Serializers
{
    public class Array2DSerializer : IBaseSerializer
    {
        public override bool CanHandle(Type type)
        {
            return type.IsArray && type.GetArrayRank() == 2;
        }

        public override Type[] GetTypeDependency(Type type)
        {
            return new Type[] { type.GetElementType() };
        }

        public override void EmitWrite(Type type)
        {
            // arg0: stream, arg1: value, arg2: ctx

            //if (value == null)
            //{
                //Write(stream, -1);
                //return;
            //}
            //int L1 = value.GetLength(1);
            //Write(stream, L1);
            //int L2 = value.GetLength(2);
            //Write(stream, L2);
            //for(int i = 0; i < L1; i++)
                //for(int j = 0; j < L2; j++)
                    //Serialize(stream, value[i, j], ctx);

            // if (value == null) ...
            EmitHelper.HandleNullWrite();

            //int L1 = value.GetLength(1);
            var getLength = type.GetMethod("GetLength");
            var L1 = emit.declocal<int>();
            emit.ldarg_1()
                .ldc_i4_0()
                .call(getLength)
                .stloc_s(L1);

            //WriteInt32(stream, L1);
            var writeInt = Basic.GetWriter<int>();
            emit.ldarg_0()
                .ldloc_s(L1)
                .call(writeInt);

            EmitHelper.EmitSerializeRef();

            //int L2 = value.GetLength(2);
            var L2 = emit.declocal<int>();
            emit.ldarg_1()
                .ldc_i4_1()
                .call(getLength)
                .stloc_s(L2);

            //WriteInt32(stream, L2);
            emit.ldarg_0()
                .ldloc_s(L2)
                .call(writeInt);

            var elementType = type.GetElementType();
            var serialize = ctx.GetWriteMethod(elementType);
            var i = emit.declocal<int>();
            var BODY1 = emit.deflabel();
            var CHECK1 = emit.deflabel();

            emit.br_s(CHECK1);

            emit.mark(BODY1);
            {
                var get = type.GetMethod("Get");
                var j = emit.declocal<int>();
                var BODY2 =emit.deflabel();
                var CHECK2 = emit.deflabel();

                emit.br_s(CHECK2);

                emit.mark(BODY2);
                {
                    // Serialize(stream, value[i, j], ctx);
                    emit.ldarg_0()
                        .ldarg_1()
                        .ldloc_s(i)
                        .ldloc_s(j)
                        .call(get)
                        .ldarg_2()
                        .call(serialize);

                    emit.increment_s(j);
                }

                // if (j < L2) branch BODY2
                emit.mark(CHECK2);
                emit.ldloc_s(j)
                    .ldloc_s(L2)
                    .blt(BODY2);

                emit.increment_s(i);
            }

            // if (i < L1) branch BODY1
            emit.mark(CHECK1);
            emit.ldloc_s(i)
                .ldloc_s(L1)
                .blt(BODY1);

            emit.ret();
        }

        public override void EmitRead(Type type)
        {
            //arg0: stream, arg1: ctx

            //int L1 = ReadInt32(stream);
            //if (L1 == -1)
                //return null;
            //int L2;
            //Read(stream, out L2);
            //value = new T[L1, L2];
            //for(int i = 0; i < L1; i++)
                //for(int j = 0; j < L2; j++)
                    //value[i, j] = <cast if necessary>Deserialize(stream, ctx);

            // int L1 = ReadInt32(stream);
            var L1 = EmitHelper.EmitReadInt();

            // if (L1 == -1) return null;
            EmitHelper.HandleNullRead(L1);

            var id = EmitHelper.EmitDeserializeRef(type);

            // int L2 = ReadInt32(stream);
            var L2 = EmitHelper.EmitReadInt();

            // var instance = new T[L1, L2];
            var ctor = type.GetConstructor(new Type[] { typeof(int), typeof(int) });
            var instance = emit.declocal(type);
            emit.ldloc_s(L1)
                .ldloc_s(L2)
                .newobj(ctor)
                .stloc_s(instance);

            EmitHelper.EmitMarkRef(instance, id);

            var elementType = type.GetElementType();
            var inheritance = ctx.RequiresInheritance(elementType);
            var deserialize = ctx.GetReadMethod(elementType, inheritance);
            var i = emit.declocal<int>();
            var BODY1 = emit.deflabel();
            var CHECK1 = emit.deflabel();

            emit.br_s(CHECK1);

            emit.mark(BODY1);
            {
                var set = type.GetMethod("Set");
                var j = emit.declocal<int>();
                var BODY2 =emit.deflabel();
                var CHECK2 = emit.deflabel();

                emit.br_s(CHECK2);

                emit.mark(BODY2);
                {
                    // value[i, j] = <cast if necessary>Deserialize(stream, ctx);
                    emit.ldloc_s(instance)    // push array
                        .ldloc_s(i)         // push i
                        .ldloc_s(j)         // push j
                        .ldarg_0()          // push stream
                        .ldarg_1()          // push ctx
                        .call(deserialize); // pop stream, ctx. push deserialize instance

                    if (inheritance)
                        emit.cast(elementType);

                    emit.call(set);

                    emit.increment_s(j);
                }

                // if (j < L2) branch BODY2
                emit.mark(CHECK2);
                emit.ldloc_s(j)
                    .ldloc_s(L2)
                    .blt(BODY2);

                emit.increment_s(i);
            }

            // if (i < L1) branch BODY1
            emit.mark(CHECK1);
            emit.ldloc_s(i)
                .ldloc_s(L1)
                .blt(BODY1);

            // return instance;
            emit.ret_s(instance);
        }
    }
}
