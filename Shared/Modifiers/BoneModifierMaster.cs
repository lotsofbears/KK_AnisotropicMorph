using ADV.Commands.Base;
using KKABMX.Core;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KineticShift
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
            var positionModifier = GetLinearVelocity(unscaledDeltaTime, out var velocity, out var velocityMagnitude);
            //var velocity = _prevVelocity;
            // positionModifier = Vector3.zero;
            var rotationModifier = Vector3.zero; // GetAngularVelocity(unscaledDeltaTime);
            var scaleModifier = GetScaleSquash(velocity, velocityMagnitude, deltaTime);
            BoneModifierData.RotationModifier.z = rotationModifier.z;
            rotationModifier.z = 0f;
            foreach (var slave in _slaves)
            {
                slave.UpdateModifiers(positionModifier, rotationModifier, scaleModifier, velocity, Effect.Tethering, deltaTime, unscaledDeltaTime);
            }
            BoneModifierData.RotationModifier = rotationModifier;
            StoreVariables(velocity);
        }


    }
}
