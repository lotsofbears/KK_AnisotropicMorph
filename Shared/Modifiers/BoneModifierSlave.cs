using KKABMX.GUI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AniMorph
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
        internal void AddToModifiers(bool[] effects, Vector3 velocity, float velocityMagnitude, float deltaTime, float unscaledDeltaTime, float masterDotFwd, float masterDotRight)
        {
            //if (effects[(int)RefEffect.Linear])
            //    BoneModifierData.PositionModifier += GetLinearOffset(unscaledDeltaTime, out velocity, out var velocityMagnitude);

            //if (effects[(int)RefEffect.Angular])
            //    BoneModifierData.RotationModifier += GetAngularOffset(unscaledDeltaTime);

            if (effects[(int)RefEffect.Tethering])
                BoneModifierData.RotationModifier += Tethering.GetTetheringOffset(velocity, deltaTime);

            var acceleration = effects[(int)RefEffect.Acceleration];
            var deceleration = effects[(int)RefEffect.Deceleration];

            if (acceleration || deceleration)
                BoneModifierData.ScaleModifier = Vector3.Scale(BoneModifierData.ScaleModifier, GetScaleDistortion(velocity, velocityMagnitude, unscaledDeltaTime, acceleration, deceleration));

            if (effects[(int)RefEffect.GravityAngular])
                BoneModifierData.RotationModifier += GetGravityAngularOffset(masterDotFwd, masterDotRight);

            _prevVelocity = velocity;
        }

        internal void UpdateModifiers(Vector3 positionModifier, Vector3 rotationModifier, Vector3 scaleModifier)
        {
            BoneModifierData.PositionModifier = positionModifier;
            BoneModifierData.RotationModifier = rotationModifier;
            BoneModifierData.ScaleModifier = scaleModifier;
        }

        private float _sidewaysAngleLimit = 20f;
        private Quaternion _upRotation = Quaternion.Euler(90f, 0f, 0f);
        private Quaternion _downRotation = Quaternion.Euler(-90f, 0f, 0f);

        
        internal Vector3 GetGravityAngularOffset(float masterDotFwd, float masterDotRight)
        {
            var dotFwd = masterDotFwd; // Vector3.Dot(Bone.forward, Vector3.up);
            var dotRight = masterDotRight; // Vector3.Dot(Bone.right, Vector3.up);

            var absDotFwd = Math.Abs(dotFwd);
            var absDotRight = Math.Abs(dotRight);

            var angleLimit = _sidewaysAngleLimit;

            if (_isLeftPosition) angleLimit = -angleLimit;

            var result = new Vector3(0f, (angleLimit * dotFwd) + (angleLimit * dotRight), 0f);

            //var boneUp = Bone.up;

            ////var boneRotation = Bone.rotation;

            ////var deltaEuler = (boneRotation * Quaternion.Inverse(_upRotation)).eulerAngles;

            ////var deltaAngleY = Mathf.DeltaAngle(0f, deltaEuler.y);

            ////var deviationY = Mathf.Min(_angleLimitRad, Mathf.Abs(deltaAngleY));

            ////if (deltaAngleY < 0f) deviationY = -deviationY;

            //var lookRot = Quaternion.LookRotation(-Vector3.up, boneUp);
            ////var result = new Vector3(0f, deviationY * masterFwdDot, 0f);
            AniMorph.Logger.LogDebug($"dotFwd[{dotFwd:F2}] dotRight[{dotRight:F2}] result{result}");
            return result;
        }
    }
}
