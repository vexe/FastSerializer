using System;
using System.Linq;
using System.Reflection;
using Vexe.Fast.Serializer.Internal;

namespace Vexe.Fast.Serializer.Serializers
{
    public abstract class IStaticSerializer : IBaseSerializer
    {
        public override sealed void EmitWrite(Type type)
        {
            MethodInfo serialize;
            var methodName = GetSerializeMethodName();
            if (methodName != null)
                serialize = GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            else
                serialize = GetType().GetMethods(BindingFlags.Public | BindingFlags.Static)
                                         .Where(x => x.Name.StartsWith("Serialize"))
                                         .Where(x => x.GetParameters().Length == (RequiresContextParameter() ? 3 : 2))
                                         .FirstOrDefault();
            if (serialize == null)
                throw new Exception("Serialize method not found for type: " + type.Name);

            EmitHelper.EmitCall(serialize, RequiresContextParameter() ? 3 : 2);
        }

        public override sealed void EmitRead(Type type)
        {
            MethodInfo deserialize;
            string methodName = GetDeserializeMethodName();
            if (methodName != null)
                deserialize = GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            else 
                deserialize = GetType().GetMethods(BindingFlags.Public | BindingFlags.Static)
                                       .Where(x => x.Name.StartsWith("Deserialize"))
                                       .Where(x => x.GetParameters().Length == (RequiresContextParameter() ? 2 : 1))
                                       .FirstOrDefault();
            if (deserialize == null)
                throw new Exception("Serialize method not found for type: " + type.Name);

            EmitHelper.EmitCall(deserialize, RequiresContextParameter() ? 2 : 1);
        }

        public virtual bool RequiresContextParameter()
        {
            return false;
        }
         
        public virtual string GetSerializeMethodName()
        {
            return null;
        }

        public virtual string GetDeserializeMethodName()
        {
            return null;
        }
    }

    public abstract class IStaticSerializer<T> : IStaticSerializer
    {
        public override bool CanHandle(Type type)
        {
            return type == typeof(T);
        }
    }
}
