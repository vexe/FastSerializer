using System;
using System.Collections.Generic;
using System.Reflection;
using Vexe.Fast.Serialization;

namespace Vexe.Fast.Serializer.Internal
{
    public class EmitContext
    {
        public readonly Dictionary<int, StubInfo> WriteStubs, ReadStubs;
        public readonly Func<Type, int> GetTypeId;
        public readonly ISerializationPredicates Predicates;
        public readonly List<Type> Types;
        public MethodInfo SerializationBase, DeserializationBase;

        readonly List<IBaseSerializer> _serializers;

        public EmitContext(ISerializationPredicates predicates, List<IBaseSerializer> serializers, Func<Type, int> getTypeId)
        {
            this.Predicates = predicates;
            this.GetTypeId = getTypeId;

            _serializers = serializers;

            WriteStubs = new Dictionary<int, StubInfo>();
            ReadStubs = new Dictionary<int, StubInfo>();
            Types = new List<Type>();
        }

        Func<Type, IBaseSerializer> _getSerializer;
        public IBaseSerializer GetSerializer(Type type)
        {
            if (_getSerializer == null)
            {
                _getSerializer = new Func<Type, IBaseSerializer>(x =>
                {
                    for (int i = 0; i < _serializers.Count; i++)
                    {
                        var serializer = _serializers[i];
                        if (serializer.CanHandle(x))
                            return serializer;
                    }
                    return null;
                }).Memoize();
            }
            return _getSerializer(type);
        }

        public MethodInfo GetWriteStub(Type forType)
        {
            var id = GetTypeId(forType);
            var stub = WriteStubs[id];
            return stub.method;
        }

        public MethodInfo GetReadStub(Type forType)
        {
            var id = GetTypeId(forType);
            var stub = ReadStubs[id];
            return stub.method;
        }

        public MethodInfo GetReadMethod(Type type)
        {
            return GetReadMethod(type, RequiresInheritance(type));
        }

        public MethodInfo GetReadMethod(Type type, bool inheritance)
        {
            if (inheritance)
                return DeserializationBase;
            return GetReadStub(type);
        }

        public MethodInfo GetWriteMethod(Type type)
        {
            if (RequiresInheritance(type))
                return SerializationBase;
            return GetWriteStub(type);
        }

        internal bool RequiresInheritance(Type type)
        {
            var serializer = GetSerializer(type);
            return serializer.RequiresInheritance(type);
        }
    }
}
