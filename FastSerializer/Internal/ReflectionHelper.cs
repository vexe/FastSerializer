using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Vexe.Fast.Serialization;

namespace Vexe.Fast.Serializer.Internal
{
    public static class ReflectionHelper
    {
        public static Type[] GetGenericArgsInRawParentInterface(this Type type, Type rawParent)
        {
            if (!rawParent.IsGenericTypeDefinition)
                return Type.EmptyTypes;

            var interfaces = type.GetInterfaces();
            var parentInterface = interfaces.FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == rawParent);
            if (parentInterface == null)
                return Type.EmptyTypes;

            return parentInterface.GetGenericArguments();
        }

        public static Type[] GetGenericArgsInRawParentClass(this Type type, Type rawParent)
        {
            if (!rawParent.IsGenericTypeDefinition)
                return Type.EmptyTypes;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == rawParent)
                return type.GetGenericArguments();

            Type baseType = type.BaseType;

            while (baseType != typeof(object) && baseType.GetGenericTypeDefinition() != rawParent)
                baseType = baseType.BaseType;

            return baseType == typeof(object) ? Type.EmptyTypes : baseType.GetGenericArguments();
        }

        public static bool HasConstructor<T>(this Type type)
        {
            return type.GetConstructor(new Type[] { typeof(T) }) != null;
        }

        public static ConstructorInfo GetEmptyConstructor(this Type type)
        {
            return GetEmptyConstructor(type, "No public empty ctor in type: " + type);
        }

        public static ConstructorInfo GetEmptyConstructor(this Type type, string msg)
        {
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
                throw new Exception(msg);
            return ctor;
        }

        public static bool IsSerializableMember(ISerializationPredicates p, MemberInfo member)
        {
            if (member.MemberType == MemberTypes.Method)
                return false;

            var field = member as FieldInfo;
            if (field != null)
                return p.IsSerializableField(field);

            var property = member as PropertyInfo;
            if (property != null)
                return p.IsSerializableProperty(property);

            return false;
        }

        public static MemberInfo[] GetSerializableMembers(ISerializationPredicates p, Type forType)
        {
            var members = forType.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var result = members.Where(x => IsSerializableMember(p, x)).ToArray();
            return result;
        }

        public static Type[] GetSerializableMembersTypes(ISerializationPredicates p, Type forType)
        {
            var members = GetSerializableMembers(p, forType);
            var result = GetMembersTypes(members);
            return result;
        }

        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type baseType)
        {
            while (toCheck != typeof(object) && toCheck != null)
            {
                Type current = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (current == baseType)
                    return true;
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        public static bool IsImplementerOfRawGeneric(this Type type, Type baseType)
        {
            return type.GetInterfaces().Any(interfaceType =>
            {
                var current = interfaceType.IsGenericType ? interfaceType.GetGenericTypeDefinition() : interfaceType;
                return current == baseType;
            });
        }

        public static Type[] GetMembersTypes(MemberInfo[] members)
        {
            return members.Select<MemberInfo, Type>(GetMemberType).ToArray();
        }

        public static MethodInfo GetMethodMarkedWith(this Type type, Type attribute)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var result = methods.FirstOrDefault(x => x.IsDefined(attribute));
            return result;
        }

        public static Type GetMemberType(this MemberInfo member)
        {
            if (member == null)
                throw new ArgumentNullException("member");

            var field = member as FieldInfo;
            if (field != null)
                return field.FieldType;

            var property = member as PropertyInfo;
            if (property != null)
                return property.PropertyType;

            throw new NotSupportedException("Unsupported member: " + member.Name);
        }

        public static bool IsCompilerGenerated(this Type type)
        {
            return type.IsDefined(typeof(CompilerGeneratedAttribute), true);
        }

        public static bool IsStatic(this Type type)
        {
            return type.IsSealed && type.IsAbstract;
        }

        public static bool IsA<T>(this Type type)
        {
            return typeof(T).IsAssignableFrom(type);
        }

        public static bool IsA(this Type type, Type other)
        {
            return other.IsAssignableFrom(type);
        }

        public static bool IsDefined(this MemberInfo member, Type type)
        {
            return member.IsDefined(type, false);
        }

        public static bool IsAutoProperty(this PropertyInfo property)
        {
            // first make sure the property has both a getter and setter
            if (!(property.CanWrite && property.CanWrite))
                return false;

            // then, check to make sure there's a complier generated backing field for the property
            // the backing field would be something like: "<PropName>k__BackingField;
            // and would have a [CompilerGenerated] attribute on it. but checking for the name is enough
            // it would also be private on an instance basis
            var flag = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            string compilerGeneratedName = "<" + property.Name + ">";
            return property.DeclaringType.GetFields(flag).Any(f => f.Name.Contains(compilerGeneratedName));
        }

        public static Func<TIn, TOut> Memoize<TIn, TOut>(this Func<TIn, TOut> fn)
        {
            var dic = new Dictionary<TIn, TOut>();
            return _in =>
            {
                TOut _out;
                if (!dic.TryGetValue(_in, out _out))
                {
                    _out = fn(_in);
                    dic.Add(_in, _out);
                }
                return _out;
            };
        }
    }
}
