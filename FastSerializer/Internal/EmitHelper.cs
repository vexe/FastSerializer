using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Vexe.Fast.Serializer.Serializers;

namespace Vexe.Fast.Serializer.Internal
{
    public struct StubInfo
    {
        public readonly MethodInfo method;
        public readonly ILGenerator il;

        public StubInfo(MethodInfo method, ILGenerator il)
        {
            this.method = method;
            this.il = il;
        }
    }

    public static class EmitHelper
    {
        internal static Type[] SerializationParameters = new Type[] { typeof(Stream), typeof(object), typeof(SerializationContext) };
        internal static Type[] DeserializationParameters = new Type[] { typeof(Stream), typeof(SerializationContext) };

        internal static ILEmitter emit = new ILEmitter();

        internal static readonly MethodInfo WriteInt = Basic.GetWriter<int>();

        internal static readonly FieldInfo Cache = typeof(SerializationContext).GetField("Cache");

        private static class Refs
        {
            internal static readonly MethodInfo GetId    = typeof(CacheContext).GetMethod("GetRefId");
            internal static readonly MethodInfo IsMarked = typeof(CacheContext).GetMethod("IsRefMarked");
            internal static readonly MethodInfo Mark     = typeof(CacheContext).GetMethod("MarkRef");
            internal static readonly MethodInfo GetRef   = typeof(CacheContext).GetMethod("GetRef");
        }

        private static class Strings
        {
            internal static readonly MethodInfo GetId    = typeof(CacheContext).GetMethod("GetStrId");
            internal static readonly MethodInfo IsMarked = typeof(CacheContext).GetMethod("IsStrMarked");
            internal static readonly MethodInfo Mark     = typeof(CacheContext).GetMethod("MarkStr");
            internal static readonly MethodInfo GetStr   = typeof(CacheContext).GetMethod("GetStr");
        }

        internal static void EmitLoadArrayElement(LocalBuilder array, LocalBuilder i, Type elementType)
        {
            emit.ldloc_s(array)
                .ldloc_s(i)
                .ldelem(elementType);
        }

        internal static LocalBuilder EmitNewCollection(Type type, LocalBuilder capacity)
        {
            bool tmp;
            return EmitNewCollection(type, capacity, out tmp);
        }

        internal static LocalBuilder EmitNewCollection(Type type, LocalBuilder capacity, out bool hasCapacityrCtor)
        {
            ConstructorInfo ctor = null;
            if (capacity != null)
                ctor = type.GetConstructor(new Type[] { typeof(int) });

            hasCapacityrCtor = ctor != null;

            if (hasCapacityrCtor)
                emit.ldloc_s(capacity);
            else ctor = type.GetEmptyConstructor("Collection type " + type + " must either have a public empty ctor or one that takes an int capacity parameter");

            var instance = emit.declocal(type);
            emit.newobj(ctor)
                .stloc_s(instance);

            return instance;
        }

        internal static void EmitArrayForLoop(Type type, LocalBuilder array, Action<LocalBuilder> body)
        {
            var BODY = emit.deflabel();
            var CHECK = emit.deflabel();

            var i = emit.declocal(typeof(int));

            // i = 0
            emit.ldc_i4_0()
                .stloc_s(i);

            emit.br_s(CHECK);

            emit.mark(BODY);

            body(i);

            // i++
            emit.increment_s(i);

            emit.mark(CHECK);

            // if (i < array.Length) branch BODY
            emit.ldloc_s(i);
            if (array == null)
                emit.ldarg_1();
            else emit.ldloc_s(array);
            emit.ldlen()
                .blt(BODY);
        }

        internal static void EmitLoadCache_1()
        {
            emit.ldarg_1()
                .ldfld(Cache);
        }

        internal static void EmitLoadCache_2()
        {
            emit.ldarg_2()
                .ldfld(Cache);
        }

        internal static void EmitCachedSerialize(MethodInfo getId, MethodInfo isMarked, MethodInfo mark)
        {
            // arg0: stream, arg1: value, arg2: ctx

            var id = emit.declocal<int>();

            // var id = ctx.Cache.GetXId(value)
            EmitLoadCache_2();
            emit.ldarg_1()
                .call(getId)
                .stloc_s(id);

            // Write(stream, id)
            emit.ldarg_0()
                .ldloc_s(id)
                .call(WriteInt);

            var NOT_REF = emit.deflabel();

            // if (!ctx.Cache.IsXMarked(id)) branch NOT_REF
            EmitLoadCache_2();
            emit.ldloc_s(id)
                .call(isMarked)
                .brfalse(NOT_REF);

            // WriteByte(stream, true)
            // return
            var writeBool = Basic.GetWriter<bool>();
            emit.ldarg_0()
                .ldc_i4_1()
                .call(writeBool)
                .ret();

            emit.mark(NOT_REF);

            // Write(stream, false)
            emit.ldarg_0()
                .ldc_i4_0()
                .call(writeBool);

            // ctx.Cache.MarkX(value, id)
            EmitLoadCache_2();
            emit.ldarg_1()
                .ldloc_s(id)
                .call(mark);
        }

        internal static void EmitSerializeRef()
        {
            EmitCachedSerialize(Refs.GetId, Refs.IsMarked, Refs.Mark);
        }

        internal static void EmitSerializeStr()
        {
            EmitCachedSerialize(Strings.GetId, Strings.IsMarked, Strings.Mark);
        }

        internal static LocalBuilder EmitCachedDeserialized(MethodInfo getCached, Type type)
        {
            //id = read_int();
            //is ref = read_byte() == 1;
            //if is ref
               //return marked[id];

            // var id = ReadInt(stream);
            var id = EmitReadInt();

            // var byte = ReadByte(stream):
            var readBool = Basic.GetReader<bool>();
            var isRef = emit.declocal<bool>();
            emit.ldarg_0()
                .call(readBool)
                .stloc_s(isRef);

            var NOT_REF = emit.deflabel();

            // if (!isRef) branch NOT_REF
            emit.ldloc_s(isRef)
                .brfalse(NOT_REF);

            // return ctx.Cache.GetX(id);
            EmitLoadCache_1();
            emit.ldloc_s(id)
                .call(getCached);

            if (type != null)
                emit.cast(type);

            emit.ret();

            emit.mark(NOT_REF);

            return id;
        }

        internal static LocalBuilder EmitDeserializeRef(Type type)
        {
            return EmitCachedDeserialized(Refs.GetRef, type);
        }

        internal static LocalBuilder EmitDeserializeStr()
        {
            return EmitCachedDeserialized(Strings.GetStr, null);
        }

        internal static void EmitMarkCached(MethodInfo mark, LocalBuilder value, LocalBuilder id)
        {
            // ctx.Cache.MarkX(instance, id);
            EmitLoadCache_1();
            emit.ldloc_s(value)
                .ldloc_s(id)
                .call(mark);
        }

        internal static void EmitMarkRef(LocalBuilder instance, LocalBuilder id)
        {
            EmitMarkCached(Refs.Mark, instance, id);
        }

        internal static void EmitMarkStr(LocalBuilder instance, LocalBuilder id)
        {
            EmitMarkCached(Strings.Mark, instance, id);
        }

        internal static void EmitWriteCount(LocalBuilder count)
        {
            //WriteInt(stream, count);
            emit.ldarg_0()
                .ldloc_s(count)
                .call(WriteInt);
        }

        internal static void EmitWriteCount(MethodInfo getCount)
        {
            //WriteInt(stream, value.get_Count());
            emit.ldarg_0()
                .ldarg_1()
                .call(getCount)
                .call(WriteInt);
        }

        internal static void HandleNullWrite()
        {
            var Not_Null = emit.deflabel();

            // if (value != null) branch Not_Null;
            emit.ldarg_1()
                .brtrue_s(Not_Null);

            //if (value == null)
            //{
            //  WriteInt(stream, -1);
            //  return;
            //}
            emit.ldarg_0()
                .ldc_i4(-1)
                .call(WriteInt)
                .ret();

            emit.mark(Not_Null);
        }

        internal static void EmitReturnNullIfNull(LocalBuilder value)
        {
            // if (value == null)
            //    return null;
            var Not_Null = emit.deflabel();
            emit.ldloc_s(value)
                .brtrue(Not_Null)
                .retnull()
                .mark(Not_Null);
        }

        internal static void HandleNullRead(LocalBuilder count)
        {
            // if (count == -1)
            //   return null;
            // else branch Not_Null;
            var Not_Null = emit.deflabel();
            emit.ldloc_s(count)
                .ldc_i4(-1)
                .bneq_s(Not_Null)
                .retnull()
                .mark(Not_Null);
        }

        internal static LocalBuilder EmitReadInt()
        {
            var readInt = Basic.GetReader<int>();

            // int value = ReadInt32(stream)
            var value = emit.declocal<int>();
            emit.ldarg_0()
            	.call(readInt)
                .stloc_s(value);

            return value;
        }

        internal static void EmitCall(MethodInfo method, int nArgs)
        {
            switch(nArgs)
            {
                case 0: break;
                case 1: emit.ldarg_0(); break;
                case 2: emit.ldarg_0().ldarg_1(); break; ;
                case 3: emit.ldarg_0().ldarg_1().ldarg_2(); break;
            }
            emit.call(method).ret();
        }

        internal static void EmitAssembly(string assemblyName, string assemblyDir, string typeName, EmitContext ctx)
        {
            var asmName = new AssemblyName(assemblyName);
            var asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave, assemblyDir);
            var modBuilder = asmBuilder.DefineDynamicModule(assemblyName, assemblyName + ".dll");
            var typeBuilder = modBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.Class);

            // serialization code
            {
                var serializationBase = typeBuilder.DefineMethod("Serialize",
                    MethodAttributes.Public | MethodAttributes.Static,
                    CallingConventions.Standard, typeof(void),
                    new Type[] { typeof(Stream), typeof(object), typeof(SerializationContext) });

                serializationBase.DefineParameter(1, ParameterAttributes.None, "stream");
                serializationBase.DefineParameter(2, ParameterAttributes.None, "value");
                serializationBase.DefineParameter(3, ParameterAttributes.None, "ctx");

                ctx.SerializationBase = serializationBase;

                EmitSerializationCode(ctx, (n, t) => CreateAssemblySerializeStub(n, t, typeBuilder));

                // generate a dictionary <type, int> field
                var lookup = typeBuilder.DefineField("TypeLookup", typeof(Dictionary<Type, int>),
                    FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

                // generate ctor to init lookup dictionary
                var asmCtor = typeBuilder.DefineConstructor(
                    MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
                    CallingConventions.Standard, Type.EmptyTypes);

                emit.il = asmCtor.GetILGenerator();
                {
                    // lookup = new Dictionary<Type, int>(<type num>);
                    var dictType = typeof(Dictionary<Type, int>);
                    var dictCtor = dictType.GetConstructor(new Type[] { typeof(int) });
                    emit.ldc_i4_s(ctx.Types.Count)
                        .newobj(dictCtor)
                        .stsfld(lookup);

                    // lookup[type] = id;
                    var getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);
                    var add = dictType.GetMethod("Add");
                    var types = ctx.Types;
                    for (int i = 0; i < types.Count; i++)
                    {
                        var type = types[i];
                        var id = ctx.GetTypeId(type);
                        emit.ldsfld(lookup)
                            .ldtoken(type)
                            .call(getTypeFromHandle)
                            .ldc_i4(id)
                            .callvirt(add);
                    }
                    emit.ret();
                }

                //public int GetObjId(object obj)
                //{
                //    if (obj == null)
                //        return 0;
                //    var type = obj.GetType();
                //    int id;
                //    if (TypeLookup.TryGetValue(type, out id))
                //        return id;
                //    var iter = TypeLookup.Keys.GetEnumerator();
                //    while(iter.MoveNext())
                //    {
                //        var current = iter.Current;
                //        if (current.IsAssignableFrom(type))
                //            return TypeLookup[current];
                //    }
                //    throw new Exception("No id for type " + type.Name);
                //}

                // generate int GetObjId(object) method
                var getObjId = typeBuilder.DefineMethod("GetObjId",
                    MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig,
                    CallingConventions.Standard, typeof(int), new Type[] { typeof(object) });

                emit.il = getObjId.GetILGenerator();
                {
                    // arg0: obj

                    // if (obj != null) branch Not_Null;
                    var Not_Null = emit.deflabel();
                    emit.ldarg_0()
                        .brtrue(Not_Null);

                    // return 0;
                    emit.ret(0);

                    emit.mark(Not_Null);

                    // var type = obj.GetType();
                    var type = emit.declocal(typeof(Type));
                    var getType = typeof(object).GetMethod("GetType");
                    emit.ldarg_0()
                        .call(getType)
                        .stloc_s(type);

                    // int id; if (TypeLookup.TryGetValue(type, out id)) return id;
                    var dictionaryType = typeof(Dictionary<Type, int>);
                    var id = emit.declocal<int>();
                    var WhileLoop = emit.deflabel();
                    var tryGetValue = dictionaryType.GetMethod("TryGetValue");
                    emit.ldsfld(lookup)
                        .ldloc_s(type)
                        .ldloca_s(id)
                        .call(tryGetValue)
                        .brfalse(WhileLoop);

                    // return id;
                    emit.ret(id);

                    emit.mark(WhileLoop);

                    // var iter = TypeLookup.Keys.GetEnumerator();
                    var enumeratorType = typeof(Dictionary<Type, int>.KeyCollection.Enumerator);
                    var keysType = typeof(Dictionary<Type, int>.KeyCollection);
                    var getKeys = dictionaryType.GetMethod("get_Keys");
                    var getEnumerator = keysType.GetMethod("GetEnumerator");
                    var iter = emit.declocal(enumeratorType);
                    emit.ldsfld(lookup)
                        .callvirt(getKeys)
                        .callvirt(getEnumerator)
                        .stloc_s(iter);

                    // while(iter.MoveNext())
                    // {
                    //    ...
                    // }

                    var BODY = emit.deflabel();
                    var CHECK = emit.deflabel();

                    emit.br_s(CHECK);

                    emit.mark(BODY);

                    // KeyValuePair<TKey, TValue> current = iter.Current;
                    var getCurrent = enumeratorType.GetMethod("get_Current");
                    var current = emit.declocal(typeof(Type));
                    emit.ldloca_s(iter)
                        .call(getCurrent)
                        .stloc_s(current);

                    // if (!current.IsAssignableFrom(type)) branch CHECK
                    var isAssignableFrom = typeof(Type).GetMethod("IsAssignableFrom");
                    var getItem = dictionaryType.GetMethod("get_Item");
                    emit.ldloc_s(current)
                        .ldloc_s(type)
                        .callvirt(isAssignableFrom)
                        .brfalse(CHECK);

                    // return TypeLookup[current];
                    emit.ldsfld(lookup)
                        .ldloc_s(current)
                        .callvirt(getItem)
                        .ret();

                    emit.mark(CHECK);

                    // if (iter.MoveNext()) branch BODY
                    var moveNext = enumeratorType.GetMethod("MoveNext");
                    emit.ldloca_s(iter)
                        .call(moveNext)
                        .brtrue_s(BODY);

                    // throw new Exception("No id for type: " + type.Name);
                    var concat = typeof(string).GetMethod("Concat", new Type[] { typeof(object), typeof(object) });
                    var ctor = typeof(Exception).GetConstructor(new Type[] { typeof(string) });
                    var getName = typeof(Type).GetMethod("get_Name");
                    emit.ldstr("No id for type: ")
                        .ldloc_s(type)
                        .callvirt(getName)
                        .call(concat)
                        .throw_new(ctor);
                }

                emit.il = serializationBase.GetILGenerator();
                EmitAssemblySerializationBase(ctx, getObjId);
            }

            // deserialization code
            {
                var deserializationBase = typeBuilder.DefineMethod("Deserialize",
                    MethodAttributes.Public | MethodAttributes.Static,
                    CallingConventions.Standard, typeof(object),
                    new Type[] { typeof(Stream), typeof(SerializationContext) });

                deserializationBase.DefineParameter(1, ParameterAttributes.None, "stream");
                deserializationBase.DefineParameter(2, ParameterAttributes.None, "ctx");

                ctx.DeserializationBase = deserializationBase;

                EmitDeserializationCode(ctx, (n, t) => CreateAssemblyDeserializeStub(n, t, typeBuilder));

                emit.il = deserializationBase.GetILGenerator();
                EmitDeserializationBase(ctx);
            }

            typeBuilder.CreateType();
            asmBuilder.Save(assemblyName + ".dll");
        }

        internal static void EmitDbgAsm(FastSerializer serializer, List<Type> types, string assemblyName, string assemblyDir, string typeName, EmitContext ctx)
        {
            var asmName = new AssemblyName(assemblyName);
            var asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave, assemblyDir);
            var modBuilder = asmBuilder.DefineDynamicModule(assemblyName, assemblyName + ".dll");
            var typeBuilder = modBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.Class);

            // serialization
            { 
                var serializationBase = typeBuilder.DefineMethod("Serialize",
                    MethodAttributes.Public | MethodAttributes.Static,
                    CallingConventions.Standard, typeof(void),
                    new Type[] { typeof(Stream), typeof(object), typeof(SerializationContext) });

                serializationBase.DefineParameter(1, ParameterAttributes.None, "stream");
                serializationBase.DefineParameter(2, ParameterAttributes.None, "value");
                serializationBase.DefineParameter(3, ParameterAttributes.None, "ctx");

                ctx.SerializationBase = serializationBase;

                EmitSerializationCode(ctx, (n, t) => CreateAssemblySerializeStub(n, t, typeBuilder));

                emit.il = serializationBase.GetILGenerator();
                EmitDynamicSerializationBase(ctx);

                var writeNull = typeBuilder.DefineMethod("Delegate_Write_Null",
                    MethodAttributes.Public | MethodAttributes.Static,
                    CallingConventions.Standard, typeof(void), SerializationParameters);
                {
                    emit.il = writeNull.GetILGenerator();
                    emit.ret();
                }

                for ( int i = 0; i < ctx.Types.Count; i++ )
                {
                    var type = ctx.Types[i];
                    var writer = ctx.GetWriteStub(type);
                    EmitHelper.EmitAssemblySerializeDelegator(type, writer, typeBuilder);
                }
            }

            // deserialization code
            {
                var deserializationBase = typeBuilder.DefineMethod("Deserialize",
                    MethodAttributes.Public | MethodAttributes.Static,
                    CallingConventions.Standard, typeof(object), DeserializationParameters);

                deserializationBase.DefineParameter(1, ParameterAttributes.None, "stream");
                deserializationBase.DefineParameter(2, ParameterAttributes.None, "ctx");

                ctx.DeserializationBase = deserializationBase;

                EmitDeserializationCode(ctx, (n, t) => CreateAssemblyDeserializeStub(n, t, typeBuilder));

                emit.il = deserializationBase.GetILGenerator();
                EmitDeserializationBase(ctx);

                var readNull = typeBuilder.DefineMethod("Delegate_Read_Null",
                    MethodAttributes.Public | MethodAttributes.Static,
                    CallingConventions.Standard, typeof(object), DeserializationParameters);
                {
                    emit.il = readNull.GetILGenerator();
                    emit.retnull();
                }

                for ( int i = 0; i < ctx.Types.Count; i++ )
                {
                    var type = ctx.Types[i];
                    var reader = ctx.GetReadStub(type);
                    EmitHelper.EmitAssemblyDeserializeDelegator(type, reader, typeBuilder);
                }
            }

            typeBuilder.CreateType();
            asmBuilder.Save(assemblyName + ".dll");
        }

        internal static void EmitDynamic(EmitContext ctx, out SerializationDelegate serialize, out DeserializationDelegate deserialize)
        {
            // serialization code
            {
                var serializationBase = new DynamicMethod("Serialize", typeof(void), SerializationParameters, true);

                serializationBase.DefineParameter(1, ParameterAttributes.None, "stream");
                serializationBase.DefineParameter(2, ParameterAttributes.None, "value");
                serializationBase.DefineParameter(3, ParameterAttributes.None, "ctx");

                ctx.SerializationBase = serializationBase;

                EmitSerializationCode(ctx, EmitDynamicSerializeStub);

                emit.il = serializationBase.GetILGenerator();
                EmitDynamicSerializationBase(ctx);

                serialize = (SerializationDelegate)serializationBase.CreateDelegate(typeof(SerializationDelegate));
            }

            // deserialization code
            {
                var deserializationBase = new DynamicMethod("Deserialize", typeof(object),
                    new Type[] { typeof(Stream), typeof(SerializationContext) }, true);

                deserializationBase.DefineParameter(1, ParameterAttributes.None, "stream");
                deserializationBase.DefineParameter(2, ParameterAttributes.None, "ctx");

                ctx.DeserializationBase = deserializationBase;

                EmitDeserializationCode(ctx, EmitDynamicDeserializeStub);

                emit.il = deserializationBase.GetILGenerator();
                EmitDeserializationBase(ctx);

                deserialize = (DeserializationDelegate)deserializationBase.CreateDelegate(typeof(DeserializationDelegate));
            }
        }

        internal static void EmitDeserializationCode(EmitContext ctx, Func<string, Type, StubInfo> emitStub)
        {
            ctx.ReadStubs.Clear();
            var types = ctx.Types;
            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                var id = ctx.GetTypeId(type);
                var stubName = GetDeserializeStubName(types, type);
                var stub = emitStub(stubName, type);
                ctx.ReadStubs[id] = stub;
            }

            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                var id = ctx.GetTypeId(type);
                var stub = ctx.ReadStubs[id];
                emit.il = stub.il;
                var serializer = ctx.GetSerializer(type);
                serializer.EmitRead(type);
            }
        }

        internal static void EmitSerializationCode(EmitContext ctx, Func<string, Type, StubInfo> emitStub)
        {
            var types = ctx.Types;
            ctx.WriteStubs.Clear();
            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                var id = ctx.GetTypeId(type);
                var stubName = GetSerializeStubName(types, type);
                var stub = emitStub(stubName, type);
                ctx.WriteStubs[id] = stub;
            }

            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                var id = ctx.GetTypeId(type);
                var stub = ctx.WriteStubs[id];
                emit.il = stub.il;
                var serializer = ctx.GetSerializer(type);
                serializer.EmitWrite(type);
            }
        }

        internal static void EmitDeserializationCode(EmitContext ctx, List<Type> types, Func<string, Type, StubInfo> emitStub)
        {
            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                var id = ctx.GetTypeId(type);
                var stubName = GetDeserializeStubName(types, type);
                var stub = emitStub(stubName, type);
                ctx.ReadStubs[id] = stub;
            }

            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                var id = ctx.GetTypeId(type);
                var stub = ctx.ReadStubs[id];
                emit.il = stub.il;
                var serializer = ctx.GetSerializer(type);
                serializer.EmitRead(type);
            }
        }

        internal static void EmitSerializationCode(EmitContext ctx, List<Type> types, Func<string, Type, StubInfo> emitStub)
        {
            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                var id = ctx.GetTypeId(type);
                var stubName = GetSerializeStubName(types, type);
                var stub = emitStub(stubName, type);
                ctx.WriteStubs[id] = stub;
            }

            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                var id = ctx.GetTypeId(type);
                var stub = ctx.WriteStubs[id];
                emit.il = stub.il;
                var serializer = ctx.GetSerializer(type);
                serializer.EmitWrite(type);
            }
        }

        static string GetSerializeStubName(List<Type> list, Type type)
        {
            if (HasDuplicates(list, type))
                return SerializeMethodName + "_" + GetTypeName(type.DeclaringType) + "_" + GetTypeName(type);
            return SerializeMethodName + "_" + GetTypeName(type);
        }

        static string GetDeserializeStubName(List<Type> list, Type type)
        {
            if (HasDuplicates(list, type))
                return DeserializeMethodName + "_" + GetTypeName(type.DeclaringType) + "_" + GetTypeName(type);
            return DeserializeMethodName + "_" + GetTypeName(type);
        }

        static StubInfo CreateAssemblyDeserializeStub(string stubName, Type type, TypeBuilder builder)
        {
            var method = builder.DefineMethod(stubName,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                type, DeserializationParameters);

            method.DefineParameter(1, ParameterAttributes.None, "stream");
            method.DefineParameter(2, ParameterAttributes.None, "ctx");

            return new StubInfo(method, method.GetILGenerator());
        }

        static StubInfo CreateAssemblySerializeStub(string stubName, Type type, TypeBuilder builder)
        {
            var parameters = new Type[] { typeof(Stream), type, typeof(SerializationContext) };

            var method = builder.DefineMethod(stubName,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                typeof(void), parameters);

            method.DefineParameter(1, ParameterAttributes.None, "stream");
            method.DefineParameter(2, ParameterAttributes.None, "value");
            method.DefineParameter(3, ParameterAttributes.None, "ctx");

            return new StubInfo(method, method.GetILGenerator());
        }

        internal static StubInfo EmitDynamicSerializeStub(string stubName, Type type)
        {
            var parameters = new Type[] { typeof(Stream), type, typeof(SerializationContext) };
            var method = new DynamicMethod(stubName, typeof(void), parameters, true);

            method.DefineParameter(1, ParameterAttributes.None, "stream");
            method.DefineParameter(2, ParameterAttributes.None, "value");
            method.DefineParameter(3, ParameterAttributes.None, "ctx");

            return new StubInfo(method, method.GetILGenerator());
        }

        internal static StubInfo EmitDynamicDeserializeStub(string stubName, Type type)
        {
            var parameters = new Type[] { typeof(Stream), typeof(SerializationContext) };
            var method = new DynamicMethod(stubName, type, parameters, true);

            method.DefineParameter(1, ParameterAttributes.None, "stream");
            method.DefineParameter(2, ParameterAttributes.None, "ctx");

            return new StubInfo(method, method.GetILGenerator());
        }

        static void EmitDeserializationBase(EmitContext ctx)
        {
            // arg0: stream, arg1: ctx

            // int id = ReadInt(stream);
            // if (id < 0 || id >= list.Count)
            //    throw;
            // return list[id].Invoke(stream, ctx);
            var id = emit.declocal<int>();
            var intReader = Basic.GetReader<int>();
            emit.ldarg_0()
                .call(intReader)
                .stloc_s(id);

            var serializerField = typeof(SerializationContext).GetField("Serializer");
            var delegates = typeof(FastSerializer).GetField("_deserializationDelegates", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var listType = delegates.FieldType;
            var getCount = listType.GetMethod("get_Count");
            var getItem = listType.GetMethod("get_Item");
            var invoke = typeof(DeserializationDelegate).GetMethod("Invoke");
            var THROW = emit.deflabel();

            emit.ldloc_s(id)
                .ldc_i4_0()
                .blt(THROW);

            emit.ldloc_s(id)
                .ldarg_1()
                .ldfld(serializerField)
                .ldfld(delegates)
                .call(getCount)
                .bge(THROW);

            // ctx.serializer.delegates[id].invoke(0, 1);
            emit.ldarg_1()
                .ldfld(serializerField)
                .ldfld(delegates)
                .ldloc_s(id)
                .call(getItem)
                .ldarg_0()
                .ldarg_1()
                .call(invoke)
                .ret();

            emit.mark(THROW);
            EmitThrowIdOutOfBounds(id);

            //var nTypes = ctx.Types.Count;
            //var cases = new Label[nTypes + 1];  // an extra case for null
            //for (int i = 0; i < cases.Length; i++)
                //cases[i] = emit.deflabel();

            //// push local and pop to switch
            //emit.ldloc(id)
                //.Switch(cases);

            //// default case
            //EmitThrowUnknownId(id);

            //// return null;
            //emit.mark(cases[0]);
            //emit.ldnull()
                //.ret();

            //for (int i = 0; i < nTypes; i++)
            //{
                //var type = ctx.Types[i];
                //var deserialize = ctx.GetReadStub(type);

                //emit.mark(cases[i + 1]);

                //// return DeserializeMethod(stream, ctx);
                //emit.ldarg_0()
                    //.ldarg_1()
                    //.call(deserialize)
                    //.ifvaluetype_box(type)
                    //.ret();
            //}
        }

        static void EmitAssemblySerializationBase(EmitContext ctx, MethodInfo getObjIdMethod)
        {
            // arg0: stream, arg1: value, arg2: ctx

            // int id = GetObjId(value);
            var id = emit.declocal<int>();
            emit.ldarg_1()               // push value
                .call(getObjIdMethod)	 // serializer.GetObjId(value)
                .stloc(id);				 // store in local

            var types = ctx.Types;

            // write id
            var writeInt = Basic.GetWriter<int>();
            emit.ldarg_0()          	// push stream
                .ldloc(id)              // push id
                .call(writeInt);        // pop stream and id, pass to write

            var nTypes = types.Count;
            var cases = new Label[nTypes + 1];		// +1 for null case
            for (int i = 0; i < cases.Length; i++)	// define labels
                cases[i] = emit.deflabel();

            // push local id and pop to switch
            emit.ldloc(id)
                .Switch(cases);

            // default case
            EmitThrowUnknownId(id);

            // null case
            emit.mark(cases[0])
                .ret();

            // setup cases and mark labels
            for (int i = 0; i < nTypes; i++)
            {
                var type = types[i];
                var writer = ctx.GetWriteStub(type);

                emit.mark(cases[i + 1]);
                emit.ldarg_0()		   // push stream
                    .ldarg_1()		   // push object
                    .unbox_any(type)   // cast to type
                    .ldarg_2()         // push ctx
                    .call(writer)	   // call writer
                    .ret();			   // return
            }
        }

        //static void EmitDynamicSerializationBase(EmitContext ctx)
        //{
        //    // arg0: stream, arg1: value, arg2: ctx

        //    var types = ctx.Types;

        //    var getObjIdMethod = typeof(FastSerializer).GetMethod("GetObjId",
        //        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        //    var serializerField = typeof(SerializationContext).GetField("Serializer");

        //    // local id
        //    var id = emit.declocal(typeof(int));

        //    // load and store id
        //    emit.ldarg_2()               // push ctx
        //        .ldfld(serializerField)  // push serializer
        //        .ldarg_1()               // push value
        //        .call(getObjIdMethod)	 // serializer.GetObjId(value)
        //        .stloc(id);				 // store in local

        //    // write id
        //    var writeInt = Basic.GetWriter<int>();
        //    emit.ldarg_0()          	// push stream
        //        .ldloc(id)              // push id
        //        .call(writeInt);       // pop stream and id, pass to write

        //    var nTypes = types.Count;
        //    var cases = new Label[nTypes + 1];		// +1 for null case
        //    for (int i = 0; i < cases.Length; i++)	// define labels
        //        cases[i] = emit.deflabel();

        //    // push local id and pop to switch
        //    emit.ldloc(id)
        //        .Switch(cases);

        //    // default case
        //    EmitThrowUnknownId(id);

        //    // null case
        //    emit.mark(cases[0])
        //        .ret();

        //    // setup cases and mark labels
        //    for (int i = 0; i < nTypes; i++)
        //    {
        //        var type = types[i];
        //        var writer = ctx.GetWriteStub(type);

        //        emit.mark(cases[i + 1]);
        //        emit.ldarg_0()		   // push stream
        //            .ldarg_1()		   // push object
        //            .unbox_any(type)   // cast to type
        //            .ldarg_2()         // push ctx
        //            .call(writer)	   // call writer
        //            .ret();			   // return
        //    }
        //}

        static void EmitDynamicSerializationBase(EmitContext ctx)
        {
            // arg0: stream, arg1: value, arg2: ctx

            // int id = get id;
            // write id
            // var list = serializer.delegates;
            // if (id < 0 || id >= list.Count)
            //    throw
            // serializationdel method = list[id];
            // method.Invoke(stream, value, ctx);

            var getObjIdMethod = typeof(FastSerializer).GetMethod("GetObjId",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            var serializerField = typeof(SerializationContext).GetField("Serializer");

            // local id
            var id = emit.declocal(typeof(int));

            // load and store id
            emit.ldarg_2()               // push ctx
                .ldfld(serializerField)  // push serializer
                .ldarg_1()               // push value
                .call(getObjIdMethod)	 // serializer.GetObjId(value)
                .stloc(id);				 // store in local

            // write id
            var writeInt = Basic.GetWriter<int>();
            emit.ldarg_0()              // push stream
                .ldloc(id)              // push id
                .call(writeInt);        // pop stream and id, pass to write

            var delegates = typeof(FastSerializer).GetField("_serializationDelegates", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var listType = delegates.FieldType;
            var getCount = listType.GetMethod("get_Count");
            var getItem = listType.GetMethod("get_Item");
            var invoke = typeof(SerializationDelegate).GetMethod("Invoke");
            var THROW = emit.deflabel();

            emit.ldloc_s(id)
                .ldc_i4_0()
                .blt(THROW);

            emit.ldloc_s(id)
                .ldarg_2()
                .ldfld(serializerField)
                .ldfld(delegates)
                .call(getCount)
                .bge(THROW);

            emit.ldarg_2()
                .ldfld(serializerField)
                .ldfld(delegates)
                .ldloc_s(id)
                .call(getItem)
                .ldarg_0()
                .ldarg_1()
                .ldarg_2()
                .call(invoke)
                .ret();

            emit.mark(THROW);
            EmitThrowIdOutOfBounds(id);
        }

        internal static DeserializationDelegate EmitDynamicDeserializeDelegator(Type type, MethodInfo reader)
        {
            // arg0: stream, arg1: object value, arg2: ctx

            var delegator = new DynamicMethod("Delegate_" + reader.Name, typeof(object), EmitHelper.DeserializationParameters, true);
            {
                emit.il = delegator.GetILGenerator();
                emit.ldarg_0()
                    .ldarg_1()
                    .call(reader)
                    .ifvaluetype_box(type)
                    .ret();
            }

            return delegator.CreateDelegate<DeserializationDelegate>();
        }

        internal static void EmitAssemblySerializeDelegator(Type type, MethodInfo writer, TypeBuilder builder)
        {
            // arg0: stream, arg1: object value, arg2: ctx

            var delegator = builder.DefineMethod("Delegate_" + writer.Name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard, typeof(void), SerializationParameters);
            {
                emit.il = delegator.GetILGenerator();
                emit.ldarg_0()
                    .ldarg_1()
                    .unbox_any(type)
                    .ldarg_2()
                    .call(writer)
                    .ret();
            }
        }

        internal static void EmitAssemblyDeserializeDelegator(Type type, MethodInfo reader, TypeBuilder builder)
        {
            // arg0: stream, arg1: object value, arg2: ctx

            var delegator = builder.DefineMethod("Delegate_" + reader.Name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard, typeof(object), DeserializationParameters);
            {
                emit.il = delegator.GetILGenerator();
                emit.ldarg_0()
                    .ldarg_1()
                    .call(reader)
                    .ifvaluetype_box(type)
                    .ret();
            }
        }

        internal static SerializationDelegate EmitDynamicSerializeDelegator(Type type, MethodInfo writer)
        {
            // arg0: stream, arg1: object value, arg2: ctx

            var delegator = new DynamicMethod("Delegate_" + writer.Name, typeof(void), EmitHelper.SerializationParameters, true);
            {
                emit.il = delegator.GetILGenerator();
                emit.ldarg_0()
                    .ldarg_1()
                    .unbox_any(type)
                    .ldarg_2()
                    .call(writer)
                    .ret();
            }

            return delegator.CreateDelegate<SerializationDelegate>();
        }

        internal static T CreateDelegate<T>(this DynamicMethod method) where T : class
        {
            return method.CreateDelegate(typeof(T)) as T;
        }

        static void EmitThrowIdOutOfBounds(LocalBuilder id)
        {
            var concat = typeof(string).GetMethod("Concat", new Type[] { typeof(object), typeof(object) });
            var ctor = typeof(Exception).GetConstructor(new Type[] { typeof(string) });

            emit.ldstr("Id out of bounds: ")
                .ldloc_s(id)
                .box<int>()
                .call(concat)
                .throw_new(ctor);
        }

        static void EmitThrowUnknownId(LocalBuilder id)
        {
            var concat = typeof(string).GetMethod("Concat", new Type[] { typeof(object), typeof(object) });
            var ctor = typeof(Exception).GetConstructor(new Type[] { typeof(string) });

            emit.ldstr("Unknown id: ")
                .ldloc_s(id)
                .box<int>()
                .call(concat)
                .throw_new(ctor);
        }

        static bool HasDuplicates(List<Type> list, Type type)
        {
            int n = 0;
            for(int i = 0; i < list.Count; i++)
                if (GetTypeName(list[i]) == GetTypeName(type))
                    n++;
            return n > 1;
        }

        static string GetTypeName(Type type)
        {
            if (type == null)
                return string.Empty;

            FastSerializer.Log("Getting type name for type: {0}", type.FullName);

            if (_getTypeName == null)
                _getTypeName = new Func<Type, string>(x =>
                {
                    if (x.IsArray)
                        return GetTypeName(x.GetElementType()) + "Array" + x.GetArrayRank();

                    if (x.IsGenericParameter || !x.IsGenericType)
                        return x.Name;

                    var builder = new StringBuilder();
                    var name = x.Name;
                    var index = name.IndexOf("`");
                    builder.Append(name.Substring(0, index));
                    builder.Append("Of");
                    var args = x.GetGenericArguments();
                    for (int i = 0; i < args.Length; i++)
                    {
                        var arg = args[i];
                        builder.Append(GetTypeName(arg));
                    }
                    return builder.ToString();
                }).Memoize();

            return _getTypeName(type);
        }

        static Func<Type, string> _getTypeName;
        const string SerializeMethodName = "Write";
        const string DeserializeMethodName = "Read";
    }
}
