using System;
using System.Linq;
using System.Reflection;
using Vexe.Fast.Serializer;
using Vexe.Fast.Serializer.Internal;

namespace Vexe.Fast.Serialization
{
    public class DefaultSerializationPredicates : ISerializationPredicates
    {
        private ISerializationAttributes attributes;

        public DefaultSerializationPredicates(ISerializationAttributes attributes)
        {
            this.attributes = attributes;
        }

        public bool IsSerializableField(FieldInfo field)
        {
            // if we have any NotSerialized attributes and the field is marked with any of them
            // then the field is not serializable
            if (attributes.NotSerialized.Any(field.IsDefined))
                return false;

            // const? nah we're good thanks.
            if (field.IsLiteral)
                return false;

            // if it's public it passes through, otherwise it has to be marked with one of the SerializeMember attributes
            if (!(field.IsPublic || attributes.SerializeMember.Any(field.IsDefined)))
                return false;

            // finally, the field's type must be serializable
            bool serializable = IsSerializableType(field.FieldType);
            return serializable;
        }

        public bool IsSerializableProperty(PropertyInfo property)
        {
            // tagged with one of the NotSerialized attributes? no soup for you!
            if (attributes.NotSerialized.Any(property.IsDefined))
                return false;

            // disallow properties with side effects, could be dangerous
            // (you could remove this constraint, if you know what you're doing...)
            if (!property.IsAutoProperty())
                return false;

            // either the getter or setter has to be public
            // otherwise the property has to be marked with an attribute from the SerializeMember array
            if (!(property.GetGetMethod(true).IsPublic ||
                  property.GetSetMethod(true).IsPublic ||
                  attributes.SerializeMember.Any(property.IsDefined)))
                return false;

            // finally, a property is serializable if its type is
            bool serializable = IsSerializableType(property.PropertyType);
            return serializable;
        }

        public bool IsSerializableType(Type type)
        {
            if (FastSerializer.IsNotQualified(type))
                return false;

            // a type is serializable if there's no attributes requirements
            // otherwise it must have one of the SerializeType attributes
            var serializeType = attributes.SerializeType;
            return serializeType == null ||
                   serializeType.Length == 0 ||
                   serializeType.Any(type.IsDefined);
        }
    }
}
