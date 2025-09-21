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
    internal class TandemBoneModifier : BoneModifier
    {
        private readonly BoneModifier[] _slaves;


        internal TandemBoneModifier(
            Transform master, 
            BoneModifier[] slaves, 
            BoneModifierData boneModifierData) : base(master, null, null, null, boneModifierData)
        {
            _slaves = slaves;
        }


        internal override void UpdateModifiers(float deltaTime)
        {
            var positionModifier = ApplyAxialVelocity(deltaTime, out var velocity);
            positionModifier = Vector3.zero;
            var rotationModifier = ApplyAngularVelocity(deltaTime);

            foreach (var slave in _slaves)
            {
                slave.UpdateModifiers(positionModifier, null, null, velocity, Effect.None, deltaTime);
            }
            BoneModifierData.RotationModifier = rotationModifier; // new Vector3(0f, 0f, rotationModifier.z);
        }


    }
}
