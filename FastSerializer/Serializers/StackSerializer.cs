using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Vexe.Fast.Serializer.Internal;

namespace Vexe.Fast.Serializer.Serializers
{
    public class StackSerializer : IBaseSerializer
    {
        public override bool CanHandle(Type type)
        {
            return type.IsSubclassOfRawGeneric(typeof(Stack<>));
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

        private Type GetElementType(Type stackType)
        {
            return stackType.GetGenericArgsInRawParentClass(typeof(Stack<>))[0];
        }

        public override void EmitWrite(Type type)
        {
            // arg0: stream, arg1: value, arg2: ctx

            //var array = value.ToArray();
            //Serialize(stream, array, ctx);

            EmitHelper.EmitSerializeRef();

            var elementType = GetElementType(type);
            var toArray = type.GetMethod("ToArray");

            // var array = value.ToArray();
            var arrayType = elementType.MakeArrayType();
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

            //T[] array = DeserializeArrayOfT(stream, ctx);
            //int length = array.Length;
            //var instance = new Stack<T>(array.Length);
            //for(int i = length-1; i >= 0; i--)
            //  instance.Push(array[i]);
            //return instance;

            var id = EmitHelper.EmitDeserializeRef(type);

            // T[] array = Deserialize(stream, ctx);
            var elementType = GetElementType(type);
            var arrayType = elementType.MakeArrayType();
            var array = emit.declocal(arrayType);
            var deserialize = ctx.GetReadStub(arrayType);
            emit.ldarg_0()
                .ldarg_1()
                .call(deserialize)
                .stloc_s(array);

            // int length = array.Length;
            var length = emit.declocal<int>();
            var getLength = arrayType.GetMethod("get_Length");
            emit.ldloc_s(array)
                .call(getLength)
                .stloc_s(length);

            // var instance = new Stack<T>(length);
            var instance = EmitHelper.EmitNewCollection(type, length);

            EmitHelper.EmitMarkRef(instance, id);

            var BODY = emit.deflabel();
            var CHECK = emit.deflabel();
            var i = emit.declocal<int>();
            var push = type.GetMethod("Push");

            // i = length-1;
            emit.ldloc_s(length)
                .ldc_i4_1()
                .sub()
                .stloc_s(i);

            emit.br_s(CHECK);

            emit.mark(BODY);

            // instance.Push(array[i]);
            emit.ldloc_s(instance)
                .ldloc_s(array)
                .ldloc_s(i)
                .ldelem(elementType)
                .callvirt(push);

            // i--
            emit.decrement_s(i);

            emit.mark(CHECK);

            // if (i >= 0) branch BODY
            emit.ldloc_s(i)
                .ldc_i4_0()
                .bge_s(BODY);

            // return instance;
            emit.ret_s(instance);
        }
    }
}
