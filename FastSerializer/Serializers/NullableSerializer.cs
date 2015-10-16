using System;

namespace Vexe.Fast.Serializer.Serializers
{
    public class NullableSerializer : IBaseSerializer
    {
        public override bool CanHandle(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public override Type[] GetTypeDependency(Type type)
        {
            return new Type[] { Nullable.GetUnderlyingType(type) };
        }

        // Note:
        // 1- IL doesn't know anything about implicit/explicit operators
        //    so we can't make use of the T to Nullable<T> nor Nullable<T> to T operators
        //    that's why we have to use the Value property when serializing and the ctor when deserializing
        // 2- Nullable<T> is a struct
        //    so we use ldarga when calling the property getter when serializing (the property getter is an instance method, so the first argument is always the 'this', but since we're dealing with structs we have to pass 'this' by ref hence ldarga)
        //    then use stobj opcode when constructing an instance when deserializing

        public override void EmitWrite(Type type)
        {
            // arg0: stream, arg1: value, arg2: ctx

            var underlyingType = Nullable.GetUnderlyingType(type);
            var serialize = ctx.GetWriteMethod(underlyingType);
            var getValue = type.GetProperty("Value").GetGetMethod();

            // Serialize(stream, value.get_Value(), ctx);
            emit.ldarg_0()
                .ldarga(1)
                .call(getValue)
                .ldarg_2()
                .call(serialize)
                .ret();
        }

        public override void EmitRead(Type type)
        {
            // arg0: stream, arg1: ctx

            var underlyingType = Nullable.GetUnderlyingType(type);
            var deserialize = ctx.GetReadMethod(underlyingType);

            // T tmp = DeserializeT(stream, ctx);
            var tmp = emit.declocal(underlyingType);
            emit.ldarg_0()
                .ldarg_1()
                .call(deserialize)
                .stloc_s(tmp);

            // return new Nullable<T>(tmp);
            var ctor = type.GetConstructor(new Type[] { underlyingType });
            emit.ldloc_s(tmp)
                .newobj(ctor)
                .ret();
        }
    }
}
