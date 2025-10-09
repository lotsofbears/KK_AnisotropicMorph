using ADV.Commands.Base;
using KKABMX.Core;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AniMorph
{
    /// <summary>
    /// Adjust slaves based on the master calculations, apply partial adjustments to the master.
    /// </summary>
    internal class BoneModifierMaster : BoneModifier
    {
        private readonly BoneModifierSlave[] _slaves;


        internal BoneModifierMaster(
            Transform master,
            BoneModifierSlave[] slaves, 
            BoneModifierData boneModifierData) : base(master, null, null, null, boneModifierData)
        {
            _slaves = slaves;
        }


        internal override void UpdateModifiers(float deltaTime, float unscaledDeltaTime)
        {
            var effects = Effects;

            // Apply linear offset, its calculations are necessary to other methods even if the offset itself isn't.
            var positionModifier = GetLinearOffset(unscaledDeltaTime, out var velocity, out var velocityMagnitude);

            //// Remove linear offset if setting
            if (!effects[(int)RefEffect.Linear])
            {
                positionModifier = Vector3.zero;
            }
            // Apply angular offset
            var rotationModifier = effects[(int)RefEffect.Angular] ? GetAngularOffset(unscaledDeltaTime) : Vector3.zero;

            //// Apply acceleration scale distortion
            //var scaleModifier = effects[(int)RefEffect.Acceleration] ? GetScaleDistortion(velocity, velocityMagnitude, deltaTime) : Vector3.one;
            var scaleModifier = Vector3.one;

            var dotUp = 0f;
            var dotR = 0f;
            var dotFwd = 0f;

            // Apply gravity linear offset if setting
            if (effects[(int)RefEffect.GravityLinear])
                positionModifier += GetGravityPositionOffset(out dotUp, out dotR);

            // Apply deceleration scale distortion if setting
            if (effects[(int)RefEffect.GravityScale])
                scaleModifier += GetGravityScaleOffset(out dotFwd);


            
            // Apply only Z axis rotation to master, keep X and Y for slaves.
            BoneModifierData.RotationModifier.z = rotationModifier.z;
            rotationModifier.z = 0f;

            foreach (var slave in _slaves)
            {
                slave.UpdateModifiers(positionModifier, rotationModifier, scaleModifier);
                slave.AddToModifiers(effects, velocity, velocityMagnitude, deltaTime, unscaledDeltaTime, dotFwd, dotR);
            }

            // Store current variables as "previous" for the next frame.
            StoreVariables(velocity);
        }


    }
}
