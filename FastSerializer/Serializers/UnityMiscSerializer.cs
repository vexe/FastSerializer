using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vexe.Fast.Serializer.Serializers
{
    public class UnityMiscSerializer : IReflectiveSerializer
    {
        //@Note: Depending on the serialization logic we go for, the following types might not be detected
        // (because we might be serializing auto-properties and not properties with side effects)
        // but we definitely need to include those properties in order for them to serialize correctly
        // However; Color, GradientColorKey, GradientAlphaKey and Vector2/3/4 have public fields
        // and should be well handled by our GenericSerializer, but just in case, we'll include them as well
        public readonly Dictionary<Type, string[]> SupportedTypes = new Dictionary<Type, string[]>()
        {
            { typeof(Vector2),                  new string[] { "x", "y" }},
            { typeof(Vector3),                  new string[] { "x", "y", "z" }},
            { typeof(Vector4),                  new string[] { "x", "y", "z", "w" }},
            { typeof(Quaternion),               new string[] { "x", "y", "z", "w" }},
            { typeof(Color),                    new string[] { "r", "g", "b", "a" }},
            { typeof(Bounds),                   new string[] { "center", "size" }},
            { typeof(Keyframe),                 new string[] { "time", "value", "tangentMode", "inTangent", "outTangent" }},
            { typeof(AnimationCurve),           new string[] { "keys", "preWrapMode", "postWrapMode" }},
            { typeof(LayerMask),                new string[] { "value" }},
            { typeof(Gradient),                 new string[] { "alphaKeys", "colorKeys" }},
            { typeof(Rect),                     new string[] { "xMin", "yMin", "xMax", "yMax" }},
            { typeof(JointMotor2D),             new string[] { "motorSpeed", "maxMotorTorque" }},
            { typeof(JointMotor),               new string[] { "force", "forceSpin", "targetVelocity" }},
            { typeof(JointTranslationLimits2D), new string[] { "min", "max" }},
            { typeof(JointAngleLimits2D),       new string[] { "min", "max" }},
            { typeof(JointSuspension2D),        new string[] { "angle", "dampingRatio", "frequency" }},
        };

        public override string[] GetMemberNames(Type type)
        {
            return SupportedTypes[type];
        }

        public override bool CanHandle(Type type)
        {
            return SupportedTypes.ContainsKey(type);
        }
    }
}
