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
    internal class MasterBoneModifier : BoneModifier
    {
        private readonly BoneModifier[] _slaves;


        internal MasterBoneModifier(
            Transform master, 
            BoneModifier[] slaves, 
            BoneModifierData boneModifierData) : base(master, null, null, null, boneModifierData)
        {
            _slaves = slaves;
        }


        internal override void UpdateModifiers(float deltaTime, float unscaledDeltaTime)
        {
            var positionModifier = ApplyAxialVelocity(unscaledDeltaTime, out var velocity);
            var rotationModifier = ApplyAngularVelocity(unscaledDeltaTime);
            var scaleModifier = ApplyScaleModification(velocity, deltaTime, unscaledDeltaTime);

            foreach (var slave in _slaves)
            {
                slave.UpdateModifiers(positionModifier, rotationModifier, scaleModifier, velocity, Effect.Tethering, deltaTime, unscaledDeltaTime);
            }
            //BoneModifierData.RotationModifier = new Vector3(0f, 0f, rotationModifier.z);
            StoreVariables(velocity);
        }


    }
}
