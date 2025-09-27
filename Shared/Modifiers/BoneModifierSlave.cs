using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KineticShift
{
    internal class BoneModifierSlave : BoneModifier
    {
        internal BoneModifierSlave(Transform bone, Transform centeredBone, Mesh bakedMesh, SkinnedMeshRenderer skinnedMesh, KKABMX.Core.BoneModifierData boneModifierData) : base(bone, centeredBone, bakedMesh, skinnedMesh, boneModifierData)
        {
        }


        /// <summary>
        /// Overload meant for master in tandem setup.
        /// </summary>
        /// <param name="effects">Uses bit shifting</param>
        internal void UpdateModifiers(Vector3 velocity, Effect effects, float deltaTime, float unscaledDeltaTime)
        {
            if ((effects & Effect.VelocityAxial) != 0)
                BoneModifierData.PositionModifier += GetLinearVelocity(unscaledDeltaTime, out velocity, out var velocityMagnitude);

            if ((effects & Effect.VelocityAngular) != 0)
                BoneModifierData.RotationModifier += GetAngularVelocity(unscaledDeltaTime);

            if ((effects & Effect.Tethering) != 0 && Tethering != null)
                BoneModifierData.RotationModifier += Tethering.ApplyAdvancedTethering(velocity, deltaTime);

            if ((effects & Effect.AccelerationScale) != 0)
                BoneModifierData.ScaleModifier += GetScaleSquash(velocity, deltaTime, unscaledDeltaTime);

            if ((effects & Effect.DecelerationSquash) != 0)
            {

            }
            _prevVelocity = velocity;
        }

        /// <summary>
        /// Overload meant for master in tandem setup.
        /// </summary>
        /// <param name="effects">Uses bit shifting</param>
        internal void UpdateModifiers(Vector3? positionModifier, Vector3? rotationModifier, Vector3? scaleModifier, Vector3 velocity, Effect effects, float deltaTime, float unscaledDeltaTime)
        {
            if (positionModifier != null)
                BoneModifierData.PositionModifier = (Vector3)positionModifier;

            if (rotationModifier != null)
                BoneModifierData.RotationModifier = (Vector3)rotationModifier;

            if (scaleModifier != null)
                BoneModifierData.ScaleModifier = (Vector3)scaleModifier;

            UpdateModifiers(velocity, effects, deltaTime, unscaledDeltaTime);
        }
    }
}
