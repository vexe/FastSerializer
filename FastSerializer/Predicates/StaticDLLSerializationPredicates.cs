using System.Reflection;

namespace Vexe.Fast.Serialization
{
    /// <summary>
    /// The serialization logic of use when precompiling a serialization assembly
    /// Fields and properties in objects *must* be public otherwise the generated code won't be valid
    /// </summary>
    public class StaticDLLSerializationPredicates : DefaultSerializationPredicates
    {
        public StaticDLLSerializationPredicates(ISerializationAttributes attributes) : base(attributes)
        {
        }

        public bool IsSerializableField(FieldInfo field)
        {
            if (!field.IsPublic)
                return false;

            return base.IsSerializableField(field);
        }

        public bool IsSerializableProperty(PropertyInfo property)
        {
            return property.GetGetMethod(true).IsPublic &&
                   property.GetSetMethod(true).IsPublic &&
                   base.IsSerializableProperty(property);
        }
    }
}