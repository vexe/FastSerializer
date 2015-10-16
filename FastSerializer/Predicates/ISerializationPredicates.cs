using System;
using System.Reflection;

namespace Vexe.Fast.Serialization
{
    public interface ISerializationPredicates
    {
        bool IsSerializableField(FieldInfo field);
        bool IsSerializableProperty(PropertyInfo property);
        bool IsSerializableType(Type type);
    }
}
