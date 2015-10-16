using System;
using Vexe.Fast.Serializer.Internal;

namespace Vexe.Fast.Serializer.Serializers
{
    public class EnumSerializer : IBaseSerializer
    {
        public override bool CanHandle(Type type)
        {
            return type.IsEnum;
        }

        public override Type[] GetTypeDependency(Type type)
        {
            return new Type[] { Enum.GetUnderlyingType(type) };
        }

        public override void EmitWrite(Type type)
        {
            var underlyingType = Enum.GetUnderlyingType(type);
            var writer = Basic.GetWriter(underlyingType);
            EmitHelper.EmitCall(writer, 2);
        }

        public override void EmitRead(Type type)
        {
            var underlyingType = Enum.GetUnderlyingType(type);
            var reader = Basic.GetReader(underlyingType);
            EmitHelper.EmitCall(reader, 1);
        }
    }
}
