using System;
using System.Reflection;
using Vexe.Fast.Serializer.Internal;

namespace Vexe.Fast.Serializer.Serializers
{
    public abstract class IReflectiveSerializer : UniversalSerializer
    {
        //@Note: Uses default (public|instance) binding flags

        public static MemberInfo[] GetMembersFromNames(Type type, string[] names)
        {
            var members = new MemberInfo[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var all = type.GetMember(name);
                if (all.Length == 0)
                {
                    UnityEngine.Debug.LogError("No member " + name + " found in type " + type.Name);
                    continue;
                }
                members[i] = all[0];
            }
            return members;
        }

        public sealed override MemberInfo[] GetMembers(Type type)
        {
            var names = GetMemberNames(type);
            return GetMembersFromNames(type, names);
        }

        public override Type[] GetTypeDependency(Type type)
        {
            var members = GetMembers(type);
            var result = ReflectionHelper.GetMembersTypes(members);
            return result;
        }

        public abstract string[] GetMemberNames(Type type);
    }
}
