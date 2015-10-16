using System;
using UnityEngine;

namespace Vexe.Fast.Serialization
{
    /// <summary>
    /// Used by the default serialization logic.
    /// Mark non-public fields|properties with this attribute in order for them to be serialized
    /// NOTE: non-public members *only* work when compiling dynamic methods as opposed to a DLL
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SerializeAttribute : Attribute
    {
    }

    /// <summary>
    /// Used by the default serialization logic.
    /// Mark members with this attribute to make them non-serializable
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
    public class DontSerializeAttribute : Attribute
    {
    }

    /// <summary>
    /// Attributes that could be used by some serialization logic to help determine if a member or type is serializable or not
    /// </summary>
    public interface ISerializationAttributes
    {
        /// <summary>
        /// Marks a member (field|property) as serializable
        /// </summary>
        Type[] SerializeMember { get; }

        /// <summary>
        /// Marks a member (field|property) or type (class|struct) as not serializable
        /// </summary>
        Type[] NotSerialized { get; }

        /// <summary>
        /// Marks a type (class|struct) as serializable
        /// </summary>
        Type[] SerializeType { get; }
    }

    public class DefaultSerializationAttributes : ISerializationAttributes
    {
        public Type[] SerializeMember { get { return _serializeMember; } }
        public Type[] NotSerialized { get { return _notSerialized; } }
        public Type[] SerializeType { get { return _serializeType; } }

        static Type[] _serializeMember = new Type[]
        {
            typeof(SerializeField),
            typeof(SerializeAttribute),
        };

        static Type[] _notSerialized = new Type[]
        {
            typeof(DontSerializeAttribute),
        };

        static Type[] _serializeType = new Type[]
        {
            // Empty by default. We don't require any special attributes on types for them to be serializable
            // But should you wish to do that, add the attributes here. ex. typeof(Serializable)
        };
    }
}