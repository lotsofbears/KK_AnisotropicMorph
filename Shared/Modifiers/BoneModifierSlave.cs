using KKABMX.GUI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace AniMorph
{
    internal class BoneModifierSlave : BoneModifier
    {



        internal BoneModifierSlave(
            Transform bone, 
            Transform centeredBone, 
            Mesh bakedMesh, 
            SkinnedMeshRenderer skinnedMesh, 
            KKABMX.Core.BoneModifierData boneModifierData, 
            bool animatedBone) : base(bone, centeredBone, bakedMesh, skinnedMesh, boneModifierData, animatedBone)
        {

        }


        /// <summary>
        /// Overload meant for master in tandem setup.
        /// </summary>
        /// <param name="effects">Uses bit shifting</param>
        internal void AddToModifiers(bool[] effects, Vector3 velocity, float deltaTime, float masterDotFwd, float masterDotRight)
        {
            if (effects[(int)RefEffect.Tethering])
                BoneModifierData.RotationModifier += Tethering.GetTetheringOffset(velocity, deltaTime);
                      
            if (effects[(int)RefEffect.GravityAngular])
                BoneModifierData.RotationModifier += GetGravityAngularOffset(masterDotFwd, masterDotRight);

            _prevVelocity = velocity;
//#if DEBUG
//            AniMorph.Logger.LogDebug($"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}: position{BoneModifierData.PositionModifier} rotation{BoneModifierData.RotationModifier} scale{BoneModifierData.ScaleModifier}");
//#endif
        }

        internal void UpdateModifiers(Vector3 positionModifier, Vector3 rotationModifier, Vector3 scaleModifier)
        {
            BoneModifierData.PositionModifier = positionModifier;
            // Remove not allowed axes
            BoneModifierData.RotationModifier = Vector3.Scale(rotationModifier, AngularApplication);
            BoneModifierData.ScaleModifier = scaleModifier;
        }


        internal override void OnConfigUpdate(AniMorph.Body part, ChaControl chara)
        {
            base.OnConfigUpdate(part, chara);

            UpdateAngularApplication(part switch
            {
                AniMorph.Body.Breast => AniMorph.BreastAngularApplicationSlave.Value,
                AniMorph.Body.Butt => AniMorph.ButtAngularApplicationSlave.Value,
                _ => 0
            });
        }
    }
}
