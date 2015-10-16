//#define LOGGING

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Vexe.Fast.Serialization;
using Vexe.Fast.Serializer.Internal;
using Vexe.Fast.Serializer.Serializers;

namespace Vexe.Fast.Serializer
{
    public delegate void SerializationDelegate(Stream stream, object value, SerializationContext ctx);
    public delegate object DeserializationDelegate(Stream stream, SerializationContext ctx);

    public class FastSerializer
    {
        private SerializationContext _serializationCtx;
        private SerializationDelegate _serialize;
        private DeserializationDelegate _deserialize;
        private ISerializationPredicates _predicates;
        private Dictionary<Type, int> _typeLookup;
        private EmitContext _emitCtx;
        private int _nextId;
        private Action<Type> _onTypeGenerated;

        public List<SerializationDelegate> _serializationDelegates;
        public List<DeserializationDelegate> _deserializationDelegates;

        static readonly DefaultSerializationPredicates DefaultPredicates = new DefaultSerializationPredicates(new DefaultSerializationAttributes());

        static readonly Type[] IgnoredTypes = new Type[]
        {
            typeof(Delegate),
            typeof(Exception),
            typeof(Attribute),
        };

        static readonly Type[] IncludedBaseTypes = new Type[]
        {
            typeof(Type),
            typeof(FieldInfo),
            typeof(PropertyInfo),
            typeof(MethodInfo),
        };

        private FastSerializer()
        {
            _serializationDelegates = new List<SerializationDelegate>();
            _deserializationDelegates = new List<DeserializationDelegate>();
        }

        public static FastSerializer CompileDynamic(List<Type> userTypes, IBaseSerializer[] userSerializers, ISerializationPredicates userPredicates, Action<Type> onTypeGenerated)
        {
            var serializer = new FastSerializer();
            serializer._onTypeGenerated = onTypeGenerated;
            serializer.Initialize(userTypes, userSerializers, userPredicates);
            var ctx = serializer._emitCtx;
            EmitHelper.EmitDynamic(ctx, out serializer._serialize, out serializer._deserialize);

            // generate write null and serialize delegators
            {
                //public static void Write_Null(Stream, value, ctx)
                //{
                //}

                // foreach type generate a delegator to the actual write method
                //public static void Delegate_Write_X(Stream, object value, ctx)
                //{
                //    Write_X(stream, (X)value, ctx);
                //}

                var emit = EmitHelper.emit;

                var writeNull = new DynamicMethod("Write_Null", typeof(void), EmitHelper.SerializationParameters);
                {
                    emit.il = writeNull.GetILGenerator();
                    emit.ret();
                }

                var writeNullDelegate = writeNull.CreateDelegate<SerializationDelegate>();
                serializer._serializationDelegates.Add(writeNullDelegate);

                for ( int i = 0; i < ctx.Types.Count; i++ )
                {
                    var type = ctx.Types[i];
                    var writer = ctx.GetWriteStub(type);
                    var delegator = EmitHelper.EmitDynamicSerializeDelegator(type, writer);
                    serializer._serializationDelegates.Add(delegator);
                }
            }

            // generate read null and deserialize delegators
            {
                //public static object Read_Null(Stream, ctx)
                //{
                //  return null;
                //}

                // foreach type generate a delegator to the actual read method
                //public static object Delegate_Read_X(Stream, ctx)
                //{
                //    return <box if needed>Read_X(stream, ctx);
                //}

                var emit = EmitHelper.emit;

                var readNull = new DynamicMethod("Read_Null", typeof(object), EmitHelper.DeserializationParameters);
                {
                    emit.il = readNull.GetILGenerator();
                    emit.retnull();
                }

                var readNullDelegate = readNull.CreateDelegate<DeserializationDelegate>();
                serializer._deserializationDelegates.Add(readNullDelegate);

                for ( int i = 0; i < ctx.Types.Count; i++ )
                {
                    var type = ctx.Types[i];
                    var reader = ctx.GetReadStub(type);
                    var delegator = EmitHelper.EmitDynamicDeserializeDelegator(type, reader);
                    serializer._deserializationDelegates.Add(delegator);
                }
            }

            return serializer;
        }

        public static void CompileDynamicDbg(List<Type> types, string asmname, string typename)
        {
            var serializer = new FastSerializer();
            serializer.Initialize(types, null, null);
            EmitHelper.EmitDbgAsm(serializer, types, asmname, null, typename, serializer._emitCtx);
        }

        public static void CompileAssembly(List<Type> userTypes, IBaseSerializer[] userSerializers, ISerializationPredicates userPredicates, string assemblyName, string assemblyDir, string typeName)
        {
            var serializer = new FastSerializer();
            serializer.Initialize(userTypes, userSerializers, userPredicates);
            EmitHelper.EmitAssembly(assemblyName, assemblyDir, typeName, serializer._emitCtx);
        }

        void Initialize(List<Type> userTypes, IBaseSerializer[] userSerializers, ISerializationPredicates userPredicates)
        {
            _predicates = userPredicates ?? DefaultPredicates;
            _serializationCtx = new SerializationContext(this);
            _typeLookup = new Dictionary<Type, int>();

            var serializers = new List<IBaseSerializer>()
            {
                new Basic(),
                new Array1DSerializer(),
                new ListSerializer(),
                new EnumSerializer(),
                new DictionarySerializer(),
                new UnityMiscSerializer(),
                new TypeSerializer(),
                new GuidSerializer(),
                new StackSerializer(),
                new QueueSerializer(),
                new HashSetSerializer(),
                new NullableSerializer(),
                new Array2DSerializer(),
                new FieldInfoSerializer(),
                new PropertyInfoSerializer(),
                new MethodInfoSerializer(),
                new UniversalSerializer()
            };

            if (userSerializers != null)
                serializers.InsertRange(0, userSerializers);

            _emitCtx = new EmitContext(_predicates, serializers, GetTypeId);
            for (int i = 0; i < serializers.Count; i++)
                serializers[i].ctx = _emitCtx;

            if (userTypes != null)
            {
                _nextId = 0;
                for (int i = 0; i < userTypes.Count; i++)
                    TryResolveType(userTypes[i], ref _nextId, null);
            }
        }

        private bool TryResolveType(Type type, ref int nextId, List<Type> resolvedTypes)
        {
            if (IsNotQualified(type))
            {
                Log("Type is not qualified: " + type.Name);
                return false;
            }

            if (!_predicates.IsSerializableType(type))
            {
                Log("Type is not serializable: " + type.Name);
                return false;
            }

            if (_typeLookup.ContainsKey(type))
                return true;

            _emitCtx.Types.Add(type);

            if (resolvedTypes != null)
                resolvedTypes.Add(type);

            _typeLookup[type] = ++nextId;

            var serializer = _emitCtx.GetSerializer(type);
            var deps = serializer.GetTypeDependency(type);
            Log("Dependencies for {0} are {1}",
                type.Name, string.Join(", ", deps.Select(x => x.Name).ToArray()));
            for(int i = 0; i < deps.Length; i++)
            {
                if (!TryResolveType(deps[i], ref nextId, resolvedTypes))
                    return false;
            }

            return true;
        }

        internal static void Log(string msg, params object[] args)
        {
#if LOGGING
#if UNITY_EDITOR
            UnityEngine.Debug.Log(string.Format(msg, args));
#else
            Console.WriteLine(msg, args);
            //UnityEngine.Debug.Log(string.Format(msg, args));
#endif
#endif
        }

        public static bool IsNotQualified(Type type)
        {
            if (IncludedBaseTypes.Any(type.IsA))
                return false;

            return type == null ||
                   type == typeof(object) ||
                   type == typeof(void) ||
            	   type.Name.StartsWith("<>") ||
            	   type.IsGenericTypeDefinition ||
            	   type.IsCompilerGenerated() ||
            	   type.IsStatic() ||
                   type.IsAbstract ||
                   IgnoredTypes.Any(type.IsA);
        }

        /// <summary>
        /// Creates the [de]serialization delegates from the specified assembly and type name
        /// (call this when you precompile a static serialization assembly and you'd like to use it instead of emitting code dynamically)
        /// Throws a IOException if the path doesn't exist
        /// Throws a NullReferenceException if the type wasn't found in the assembly
        /// </summary>
        public static FastSerializer BindAssembly(string assemblyPath, string serializerTypeName)
        {
            if (!File.Exists(assemblyPath))
                throw new IOException("Assembly path doesn't exist: " + assemblyPath);

            var asm = Assembly.LoadFrom(assemblyPath);
            var serializerType = asm.GetType(serializerTypeName);

            if (serializerType == null)
                throw new NullReferenceException("Serializer type was not found: " + serializerTypeName);

            return BindSerializer(serializerType);
        }

        public static FastSerializer BindSerializer(Type serializerType)
        {
            var serializer = new FastSerializer();

            if (serializerType == null)
                throw new ArgumentNullException("serializerType");

            var serializeMethod = serializerType.GetMethod("Serialize", BindingFlags.Static | BindingFlags.Public);
            if (serializeMethod == null)
                throw new NullReferenceException("Serializer type: " + serializerType + " must have a public static method called `Serialize` that could be pointed to via a SerializationDelegate delegate");

            var deserializeMethod = serializerType.GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public);
            if (deserializeMethod == null)
                throw new NullReferenceException("Serializer type: " + serializerType + " must have a public static method called `Deserialize` that could be pointed to via a DeserializationDelegate delegate");

            serializer._serialize = (SerializationDelegate)Delegate.CreateDelegate(typeof(SerializationDelegate), null, serializeMethod);
            serializer._deserialize = (DeserializationDelegate)Delegate.CreateDelegate(typeof(DeserializationDelegate), null, deserializeMethod);

            return serializer;
        }

        public void Serialize(Stream stream, object value)
        {
            Serialize(stream, value, null);
        }

        public void Serialize(Stream stream, object value, object context)
        {
            if (_serializationCtx == null)
                _serializationCtx = new SerializationContext(this);

            _serializationCtx.Context = context;
            _serialize(stream, value, _serializationCtx);
            _serializationCtx.Cache.Clear();
        }

        public object Deserialize(Stream stream)
        {
            return Deserialize(stream, null);
        }

        public object Deserialize(Stream stream, object context)
        {
            if (_serializationCtx == null)
                _serializationCtx = new SerializationContext(this);

            _serializationCtx.Context = context;
            var result = _deserialize(stream, _serializationCtx);
            _serializationCtx.Cache.Clear();
            return result;
        }

        public T Deserialize<T>(Stream stream, object context)
        {
            return (T)Deserialize(stream, context);
        }

        public T Deserialize<T>(Stream stream)
        {
            return (T)Deserialize(stream, null);
        }

        int GetTypeId(Type type)
        {
            int id;

            if (_typeLookup.TryGetValue(type, out id))
                return id;

            // if the type isn't available in the lookup table,
            // return the id for the first type that is assignable from the input type
            // this addresses situations where the runtime type is unknown/inaccessable (ex RtFieldInfo)
            var keys = _typeLookup.Keys;
            var iter = keys.GetEnumerator();
            while (iter.MoveNext())
            {
                var current = iter.Current;
                if (current.IsAssignableFrom(type))
                    return _typeLookup[current];
            }

            // Todo: better error message
            if (!TryGenerate(type, out id))
                throw new Exception("Couldn't dynamically generate code for type: " + type.Name + " Either it or one of its dependent types are not valid");

            return id;
        }

        bool TryGenerate(Type type, out int id)
        {
            // 0- try to resolve type
            var resolvedTypes = new List<Type>();
            int nextId = _nextId;
            if (!TryResolveType(type, ref nextId, resolvedTypes))
            {
                id = -1;
                return false;
            }

            // 1- generate the actual code for the type
            EmitHelper.EmitSerializationCode(_emitCtx, resolvedTypes, EmitHelper.EmitDynamicSerializeStub);
            EmitHelper.EmitDeserializationCode(_emitCtx, resolvedTypes, EmitHelper.EmitDynamicDeserializeStub);

            // 2- generate delegators and add to list
            for (int i = 0; i < resolvedTypes.Count; i++)
            {
                var resolved = resolvedTypes[i];
                var writer = _emitCtx.GetWriteStub(resolved);
                var serializeDelegator = EmitHelper.EmitDynamicSerializeDelegator(resolved, writer);
                _serializationDelegates.Add(serializeDelegator);

                var reader = _emitCtx.GetReadStub(resolved);
                var deserializeDelegator = EmitHelper.EmitDynamicDeserializeDelegator(resolved, reader);
                _deserializationDelegates.Add(deserializeDelegator);
            }

            if (_onTypeGenerated != null)
            {
                for ( int i = 0; i < resolvedTypes.Count; i++ )
                    _onTypeGenerated(resolvedTypes[i]);
            }

            // 3- set ids
            _nextId = nextId;
            id = GetTypeId(type);

            return true;
        }

        public int GetObjId(object obj)
        {
            if (obj == null)
                return 0;

            var objType = obj.GetType();
            return GetTypeId(objType);
        }

        public string SerializeToString<T>(T value)
        {
           using (var ms = new MemoryStream())
           {
               Serialize(ms, value, null);
               var bytes = ms.ToArray();
               var result = Convert.ToBase64String(bytes);
               return result;
           }
        }

        public T DeserializeFromString<T>(string serializedState)
        {
            var bytes = Convert.FromBase64String(serializedState);
            using (var ms = new MemoryStream(bytes))
                return Deserialize<T>(ms);
        }
    }

    /// <summary>
    /// Idea of marking refernces is inspired by FullSerializer. Thanks Jacob!
    /// </summary>
    public class CacheContext
    {
        private int _nextRefId, _nextStringId;

        readonly Dictionary<int, bool> _markedStatics = new Dictionary<int, bool>();
        readonly Dictionary<int, string> _markedStrings = new Dictionary<int, string>();
        readonly Dictionary<int, object> _markedRefs = new Dictionary<int, object>();
        readonly Dictionary<string, int> _stringIds = new Dictionary<string, int>();
        readonly Dictionary<object, int> _refIds = new Dictionary<object, int>(RefComparer.Instance);

        public string GetStr(int id)
        {
            return _markedStrings[id];
        }

        public bool IsStrMarked(int strId)
        {
            return _markedStrings.ContainsKey(strId);
        }

        public void MarkStr(string obj, int id)
        {
            _markedStrings[id] = obj;
        }

        public int GetStrId(string str)
        {
            int id;
            if (!_stringIds.TryGetValue(str, out id))
            {
                id = _nextStringId++;
                _stringIds[str] = id;
            }
            return id;
        }

        public object GetRef(int id)
        {
            return _markedRefs[id];
        }

        public bool IsRefMarked(int objId)
        {
            return _markedRefs.ContainsKey(objId);
        }

        public void MarkRef(object obj, int id)
        {
            _markedRefs[id] = obj;
        }

        public int GetRefId(object obj)
        {
            int id;
            if (!_refIds.TryGetValue(obj, out id))
            {
                id = _nextRefId++;
                _refIds[obj] = id;
            }
            return id;
        }

        public bool IsStaticMarked(int id)
        {
            return _markedStatics.ContainsKey(id);
        }

        public void MarkStatic(int id)
        {
            _markedStatics[id] = true;
        }

        internal void Clear()
        {
            _markedStatics.Clear();
            _markedRefs.Clear();
            _markedStrings.Clear();
            _stringIds.Clear();
            _refIds.Clear();
            _nextRefId = 0;
            _nextStringId = 0;
        }

		private class RefComparer : IEqualityComparer<object>
		{
			public static readonly RefComparer Instance = new RefComparer();

			private RefComparer()
			{
			}

			public bool Equals(object x, object y)
			{
				return x == y;
			}

			public int GetHashCode(object obj)
			{
				return RuntimeHelpers.GetHashCode(obj);
			}
		}
    }

    public class SerializationContext
    {
        public readonly FastSerializer Serializer;

        public readonly CacheContext Cache = new CacheContext();

        public object Context;

        public SerializationContext(FastSerializer serializer)
        {
            this.Serializer = serializer;
        }
    }
}
