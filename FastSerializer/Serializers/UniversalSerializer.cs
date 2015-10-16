using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using Vexe.Fast.Serializer.Internal;

namespace Vexe.Fast.Serializer.Serializers
{
    public class UniversalSerializer : IBaseSerializer
    {
        public override bool RequiresInheritance(Type type)
        {
            return !type.IsValueType && !type.IsSealed;
        }

        public override Type[] GetTypeDependency(Type type)
        {
            return ReflectionHelper.GetSerializableMembersTypes(ctx.Predicates, type);
        }

        public override bool CanHandle(Type type)
        {
            return true;
        }

        public virtual MemberInfo[] GetMembers(Type type)
        {
            return ReflectionHelper.GetSerializableMembers(ctx.Predicates, type);
        }

        public override void EmitWrite(Type type)
        {
            // arg0: stream, arg1: value, arg2: ctx

            if (!type.IsValueType)
                EmitHelper.EmitSerializeRef();

            var onSerializing = type.GetMethodMarkedWith(typeof(OnSerializingAttribute));
            if (onSerializing != null)
            {
                // value.OnSerializing(ctx)
                emit.ldarg_1()
                    .ldarg_2()
                    .callvirt(onSerializing);
            }

            var members = GetMembers(type);
            for (int i = 0; i < members.Length; i++)
            {
                var member = new MetaMember(members[i]);
                var memberType = member.Type;

                // Serialize(stream, <type|value>.member, ctx)
                {
                    if (member.IsStatic)
                    {
                        var SKIP = emit.deflabel();

                        Statics.HandleMark(2, SKIP);

                        // push stream
                        emit.ldarg_0();

                        // push member
                        member.emit_load();

                        // push ctx and call serialize
                        var serialize = ctx.GetWriteMethod(memberType);
                        emit.ldarg_2()
                            .call(serialize);

                        emit.mark(SKIP);
                    }
                    else
                    {
                        // push stream
                        emit.ldarg_0();

                        // push value if member is not static
                        if (type.IsValueType)
                            emit.ldarga_s(1);
                        else emit.ldarg_s(1);

                        // push member
                        member.emit_load();

                        // push ctx and call serialize
                        var serialize = ctx.GetWriteMethod(memberType);
                        emit.ldarg_2()
                            .call(serialize);
                    }
                }
            }

            var onSerialized = type.GetMethodMarkedWith(typeof(OnSerializedAttribute));
            if (onSerialized != null)
            {
                // value.OnSerialized(ctx)
                emit.ldarg_1()
                    .ldarg_2()
                    .callvirt(onSerialized);
            }

            emit.ret();
        }

        public virtual LocalBuilder EmitNewInstance(Type type)
        {
            var instance = emit.declocal(type);
            if (type.IsValueType)
            {
                // instance = new T();
                emit.ldloca_s(instance)
                    .initobj(type);
            }
            else
            {
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor != null)
                {
                    // instance = new T();
                    emit.newobj(ctor)
                        .stloc_s(instance);
                }
                else
                {
                    // instance = (T)GetUninitializedObject(typeof(T));
                    emit.ldtoken(type)                  // convert to runtime representation (handle) and push
                        .call(GetTypeFromHandle)        // pops the handle, pushes the type
                        .call(GetUninitializedObject)   // pops the type, pushes the object
                        .unbox_any(type)                // cast object to right type
                        .stloc_s(instance);             // pop to local
                }
            }
            return instance;
        }

        public override void EmitRead(Type type)
        {
            // arg0: stream, arg1: ctx

            LocalBuilder instance;
            if (type.IsValueType)
                instance = EmitNewInstance(type);
            else
            {
                var id = EmitHelper.EmitDeserializeRef(type);
                instance = EmitNewInstance(type);
                EmitHelper.EmitMarkRef(instance, id);
            }

            //var instance = <get instance>;
            //instance.OnDeserializing();
            //<deserialize members if any>
            //instance.OnDeserialized();
            //return instance;

            //var instance = <get instance>;

            var onDeserializing = type.GetMethodMarkedWith(typeof(OnDeserializingAttribute));
            if (onDeserializing != null)
            {
                // instance.OnDeserializing(ctx)
                emit.ldloc_s(instance)
                    .ldarg_1()
                    .callvirt(onDeserializing);
            }

            Action<MetaMember, MethodInfo, bool> emitDeserializeMember = (member, deserialize, inheritance) =>
            {
                // push stream and ctx and call Deserialize
                emit.ldarg_0()
                    .ldarg_1()
                    .call(deserialize);

                // cast if we went through base (which returns 'object')
                if (inheritance)
                    emit.cast(member.Type);

                // store deserialize result in member
                member.emit_store();
            };

            var members = GetMembers(type);
            for (int i = 0; i < members.Length; i++)
            {
                var member = new MetaMember(members[i]);

                var inheritance = ctx.RequiresInheritance(member.Type);
                var deserialize = ctx.GetReadMethod(member.Type, inheritance);

                //<type|instance>.field|property = <cast if necessary>DeserializeT(stream, ctx);
                {
                    if (member.IsStatic)
                    {
                        var SKIP = emit.deflabel();

                        Statics.HandleMark(1, SKIP);

                        emitDeserializeMember(member, deserialize, inheritance);

                        emit.mark(SKIP);
                    }
                    else
                    {
                        // push value if not static
                        if (type.IsValueType)
                            emit.ldloca_s(instance);
                        else emit.ldloc_s(instance);

                        emitDeserializeMember(member, deserialize, inheritance);
                    }
                }
            }

            var onDeserialized = type.GetMethodMarkedWith(typeof(OnDeserializedAttribute));
            if (onDeserialized != null)
            {
                // instance.OnDeserialized(ctx)
                emit.ldloc_s(instance)
                    .ldarg_1()
                    .callvirt(onDeserialized);
            }

            // return instance;
            emit.ret_s(instance);
        }

        static MethodInfo GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);
        static MethodInfo GetUninitializedObject = typeof(FormatterServices).GetMethod("GetUninitializedObject", BindingFlags.Public | BindingFlags.Static);

        public struct MetaMember
        {
            public readonly Type Type;
            public readonly bool IsStatic;
            public readonly MemberInfo Info;

            public MetaMember(MemberInfo info)
            {
                this.Info = info;

                var field = info as FieldInfo;
                if (field != null)
                {
                    Type = field.FieldType;
                    IsStatic = field.IsStatic;
                }
                else
                {
                    var property = info as PropertyInfo;
                    if (property == null)
                        throw new NotSupportedException(info.Name);

                    Type = property.PropertyType;
                    IsStatic = property.GetGetMethod(true).IsStatic;
                }
            }

            public void emit_store()
            {
                var field = Info as FieldInfo;
                if (field != null)
                {
                    emit.setfld(field);
                }
                else
                {
                    var property = Info as PropertyInfo;
                    var setter = property.GetSetMethod(true);
                    if (setter == null)
                        throw new InvalidOperationException("Property should have a setter! " + property.Name + " in " + property.DeclaringType.Name);
                    emit.call(setter);
                }
            }

            public void emit_load()
            {
                var field = Info as FieldInfo;
                if (field != null)
                {
                    emit.lodfld(field);
                }
                else
                {
                    var property = Info as PropertyInfo;
                    var getter = property.GetGetMethod(true);
                    emit.call(getter);
                }
            }
        }

        private static class Statics
        {
            static readonly MethodInfo IsMarked = typeof(CacheContext).GetMethod("IsStaticMarked");
            static readonly MethodInfo Mark = typeof(CacheContext).GetMethod("MarkStatic");
            static readonly FieldInfo Cache = typeof(SerializationContext).GetField("Cache");
            static int Counter;

            internal static void HandleMark(int argNum, Label SKIP)
            {
                var id = Counter++;

                // ctx arg numeber
                if (argNum == 1)
                    emit.ldarg_1();
                else emit.ldarg_2();

                emit.ldfld(Statics.Cache)
                    .ldc_i4(id)
                    .call(Statics.IsMarked)
                    .brtrue(SKIP);

                if (argNum == 1)
                    emit.ldarg_1();
                else emit.ldarg_2();

                emit.ldfld(Statics.Cache)
                    .ldc_i4(id)
                    .call(Statics.Mark);
            }
        }

    }
}
