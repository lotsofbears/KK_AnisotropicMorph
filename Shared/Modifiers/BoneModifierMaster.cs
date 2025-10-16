using ActionGame.Point;
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
            BoneModifierData boneModifierData
            ) : base(master, null, null, null, boneModifierData)
        {
            _slaves = slaves;
        }


        internal override void UpdateModifiers(float deltaTime, float fps)
        {
            // TODO Decentralize some calculations?
            var effects = Effects;

            // Apply linear offset, its calculations are necessary to other methods even if the offset itself isn't.
            var positionModifier = GetLinearOffset(deltaTime, out var velocity, out var velocityMagnitude);

            //// Remove linear offset if setting
            if (!effects[(int)RefEffect.Linear])
            {
                positionModifier = Vector3.zero;
            }
            // Apply angular offset
            var rotationModifier = effects[(int)RefEffect.Angular] ? GetAngularOffset(deltaTime) : Vector3.zero;

            var scaleModifier = Vector3.Scale(
                BoneModifierData.ScaleModifier,
                GetScaleOffset(
                    velocity,
                    velocityMagnitude,
                    deltaTime,
                    fps,
                    effects[(int)RefEffect.Acceleration],
                    effects[(int)RefEffect.Deceleration]
                    )
                );

            var dotUp = Vector3.Dot(Bone.up, Vector3.up);
            var dotR = Vector3.Dot(Bone.right, Vector3.up);
            var dotFwd = Vector3.Dot(Bone.forward, Vector3.up);

            // Apply gravity linear offset
            if (effects[(int)RefEffect.GravityLinear])
                positionModifier += GetGravityPositionOffset(dotUp, dotR);

            // Apply gravity scale offset
            if (effects[(int)RefEffect.GravityScale])
                scaleModifier = Vector3.Scale(scaleModifier, GetGravityScaleOffset(dotFwd));

            // Discard not allowed axes
            BoneModifierData.RotationModifier = Vector3.Scale(rotationModifier, AngularApplication);

            foreach (var slave in _slaves)
            {
                slave.UpdateModifiers(positionModifier, rotationModifier, scaleModifier);
                slave.AddToModifiers(effects, velocity, deltaTime, dotFwd, dotR);
            }

            // Store current variables as "previous" for the next frame.
            //AniMorph.Logger.LogDebug($"MasterUpdate:posMod({BoneModifierData.PositionModifier.x:F3},{BoneModifierData.PositionModifier.y:F3},{BoneModifierData.PositionModifier.z:F3}) rotMod{BoneModifierData.RotationModifier} scaleMod{BoneModifierData.ScaleModifier}");
            StoreVariables(velocity);
        }


    }
}
