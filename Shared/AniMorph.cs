using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static AniMorph.BoneModifier;

namespace AniMorph
{
    [BepInPlugin(GUID, Name, Version)]
    [BepInProcess(KoikatuAPI.GameProcessName)]
    [BepInProcess(KoikatuAPI.StudioProcessName)]
#if !DEBUG
    [BepInDependency(KKABMX_Core.GUID, KKABMX_Core.Version)]
#endif

#if KK
    [BepInProcess(KoikatuAPI.GameProcessNameSteam)]
#endif
    internal class AniMorph : BaseUnityPlugin
    {
        public const string GUID = "AniMorph.ABMX";
        public const string Name = "Anisotropic Morph";
        public const string Version = "0.23";



        internal new static ManualLogSource Logger;

        public static ConfigEntry<Gender> Enable;
        public static ConfigEntry<bool> MaleEnableDB;

        #region Breast

        public static ConfigEntry<Effect> BreastEffects;
        public static ConfigEntry<bool> BreastAdjustForSize;

        public static ConfigEntry<float> BreastLinearGravity;
        public static ConfigEntry<float> BreastLinearSpringStrength;
        public static ConfigEntry<float> BreastLinearDamping;
        //public static ConfigEntry<float> BreastLinearMass;
        public static ConfigEntry<Vector3> BreastLinearLimitPositive;
        public static ConfigEntry<Vector3> BreastLinearLimitNegative;

        public static ConfigEntry<float> BreastAngularSpringStrength;
        public static ConfigEntry<float> BreastAngularDamping;
        public static ConfigEntry<float> BreastAngularMaxAngle;
        public static ConfigEntry<Axis> BreastAngularApplicationMaster;
        public static ConfigEntry<Axis> BreastAngularApplicationSlave;

        public static ConfigEntry<float> BreastScaleAccelerationFactor;
        public static ConfigEntry<float> BreastScaleDecelerationFactor;
        public static ConfigEntry<float> BreastScaleLerpSpeed;
        public static ConfigEntry<float> BreastScaleMaxDistortion;
        public static ConfigEntry<bool> BreastScalePreserveVolume;
        public static ConfigEntry<bool> BreastScaleDumbAcceleration;
        public static ConfigEntry<Vector3> BreastScaleUnevenDistribution;

        public static ConfigEntry<float> BreastTetheringMultiplier;
        public static ConfigEntry<float> BreastTetheringFrequency;
        public static ConfigEntry<float> BreastTetheringDamping;
        public static ConfigEntry<float> BreastTetheringMaxAngle;

        public static ConfigEntry<Vector3> BreastGravityUpUp;
        public static ConfigEntry<Vector3> BreastGravityUpMid;
        public static ConfigEntry<Vector3> BreastGravityUpDown;
        public static ConfigEntry<Vector3> BreastGravityFwdUp;
        public static ConfigEntry<Vector3> BreastGravityFwdMid;
        public static ConfigEntry<Vector3> BreastGravityFwdDown;
        public static ConfigEntry<Vector3> BreastGravityRightUp;
        public static ConfigEntry<Vector3> BreastGravityRightMid;
        public static ConfigEntry<Vector3> BreastGravityRightDown;

        #endregion


        #region Butt

        public static ConfigEntry<Effect> ButtEffects;
        public static ConfigEntry<bool> ButtAdjustForSize;

        public static ConfigEntry<float> ButtLinearGravity;
        public static ConfigEntry<float> ButtLinearSpringStrength;
        public static ConfigEntry<float> ButtLinearDamping;
        //public static ConfigEntry<float> ButtLinearMass;
        public static ConfigEntry<Vector3> ButtLinearLimitPositive;
        public static ConfigEntry<Vector3> ButtLinearLimitNegative;

        public static ConfigEntry<float> ButtAngularSpringStrength;
        public static ConfigEntry<float> ButtAngularDamping;
        public static ConfigEntry<float> ButtAngularMaxAngle;
        public static ConfigEntry<Axis> ButtAngularApplicationMaster;
        public static ConfigEntry<Axis> ButtAngularApplicationSlave;

        public static ConfigEntry<float> ButtScaleAccelerationFactor;
        public static ConfigEntry<float> ButtScaleDecelerationFactor;
        public static ConfigEntry<float> ButtScaleLerpSpeed;
        public static ConfigEntry<float> ButtScaleMaxDistortion;
        public static ConfigEntry<bool> ButtScalePreserveVolume;
        public static ConfigEntry<bool> ButtScaleDumbAcceleration;
        public static ConfigEntry<Vector3> ButtScaleUnevenDistribution;
#if DEBUG
        public static ConfigEntry<float> ButtTetheringMultiplier;
        public static ConfigEntry<float> ButtTetheringFrequency;
        public static ConfigEntry<float> ButtTetheringDamping;
        public static ConfigEntry<float> ButtTetheringMaxAngle;

        public static ConfigEntry<Vector3> ButtGravityUpUp;
        public static ConfigEntry<Vector3> ButtGravityUpMid;
        public static ConfigEntry<Vector3> ButtGravityUpDown;
        public static ConfigEntry<Vector3> ButtGravityFwdUp;
        public static ConfigEntry<Vector3> ButtGravityFwdMid;
        public static ConfigEntry<Vector3> ButtGravityFwdDown;
        public static ConfigEntry<Vector3> ButtGravityRightUp;
        public static ConfigEntry<Vector3> ButtGravityRightMid;
        public static ConfigEntry<Vector3> ButtGravityRightDown;
#endif


        #endregion

#if DEBUG
        #region Spine2

        public static ConfigEntry<Effect> ThighEffects;

        public static ConfigEntry<float> ThighLinearGravity;
        public static ConfigEntry<float> ThighLinearSpringStrength;
        public static ConfigEntry<float> ThighLinearDamping;
        //public static ConfigEntry<float> ThighLinearMass;
        public static ConfigEntry<Vector3> ThighLinearLimitPositive;
        public static ConfigEntry<Vector3> ThighLinearLimitNegative;

        public static ConfigEntry<float> ThighAngularSpringStrength;
        public static ConfigEntry<float> ThighAngularDamping;
        public static ConfigEntry<float> ThighAngularMaxAngle;
        public static ConfigEntry<Axis> ThighAngularApplicationMaster;
        public static ConfigEntry<Axis> ThighAngularApplicationSlave;

        public static ConfigEntry<float> ThighScaleAccelerationFactor;
        public static ConfigEntry<float> ThighScaleDecelerationFactor;
        public static ConfigEntry<float> ThighScaleLerpSpeed;
        public static ConfigEntry<float> ThighScaleMaxDistortion;
        public static ConfigEntry<bool> ThighScalePreserveVolume;
        public static ConfigEntry<bool> ThighScaleDumbAcceleration;
        public static ConfigEntry<Vector3> ThighScaleUnevenDistribution;

        public static ConfigEntry<float> ThighTetheringMultiplier;
        public static ConfigEntry<float> ThighTetheringFrequency;
        public static ConfigEntry<float> ThighTetheringDamping;
        public static ConfigEntry<float> ThighTetheringMaxAngle;

        public static ConfigEntry<Vector3> ThighGravityUpUp;
        public static ConfigEntry<Vector3> ThighGravityUpMid;
        public static ConfigEntry<Vector3> ThighGravityUpDown;
        public static ConfigEntry<Vector3> ThighGravityFwdUp;
        public static ConfigEntry<Vector3> ThighGravityFwdMid;
        public static ConfigEntry<Vector3> ThighGravityFwdDown;
        public static ConfigEntry<Vector3> ThighGravityRightUp;
        public static ConfigEntry<Vector3> ThighGravityRightMid;
        public static ConfigEntry<Vector3> ThighGravityRightDown;


        #endregion
#endif

        private static readonly Dictionary<Body, Effect> _allowedEffectsDic = new()
        {
            { Body.Breast, Effect.Linear | Effect.Angular | Effect.Tethering | Effect.Acceleration | Effect.Deceleration | Effect.GravityLinear | Effect.GravityAngular | Effect.GravityScale },
            { Body.Butt, Effect.Linear | Effect.Angular | Effect.Acceleration | Effect.Deceleration },
        };


        private void Awake()
        {
            Logger = base.Logger;

            Enable = Config.Bind("", "Enable", Gender.Male | Gender.Female, new ConfigDescription("Choose none to disable", null, new ConfigurationManagerAttributes { Order = 100 }));


            CharacterApi.RegisterExtraBehaviour<AniMorphCharaController>(GUID);

            BindConfig();

            AddSettingChangedParam();


            MaleEnableDB = Config.Bind("", "MaleEnableDB", true, new ConfigDescription("Force enable Dynamic Bones on males in Main Game as they are usually turned off", null, new ConfigurationManagerAttributes { Order = 99 }));
            MaleEnableDB.SettingChanged += (_, _) => HooksMaleEnableDB.ApplyHooks();
            // Avoid hooking this one up for UpdateConfig().

            Hooks.ApplyHooks();
            HooksMaleEnableDB.ApplyHooks();
        }

        private void BindConfig()
        {
            #region Breast

            BreastEffects = Config.Bind("Breast", "Effects", 
                Effect.Linear | Effect.Angular | Effect.Tethering | Effect.Acceleration | Effect.Deceleration | Effect.GravityLinear | Effect.GravityAngular | Effect.GravityScale, 
                new ConfigDescription("Select effects to apply to the breast (bone)\nOnly appropriate effects can be selected\n" +
                "Linear – position of the bone is adjusted as if the bone was attached by an elastic band\n" +
                "Angular – rotation of the bone is adjusted as if the bone was a rod with an elastic attachment point, uses rotation of the bone\n" +
                "Tethering – rotation of the bone is adjusted as if the bone was a rod with an elastic attachment point, uses 'Linear' effect, synergies with 'Angular' effect\n" +
                "Acceleration – scale of the bone is extended along the axis of velocity and shrunk along perpendicular ones\n" +
                "Deceleration – scale of the bone is shrunk along the axis of deceleration and extended along perpendicular ones\n" +
                "GravityLinear – position of the bone is adjusted based on the rotation of the bone in the world space as imitation of the gravity\n" +
                "GravityAngular – rotation of the bone is adjusted based on the rotation of the bone in the world space as imitation of the gravity\n" +
                "GravityScale – scale of the bone is adjusted based on the rotation of the bone in the world space as imitation of the gravity", 
                null, new ConfigurationManagerAttributes { Order = 110 }));

            BreastAdjustForSize = Config.Bind("Breast", "AdjustForSize", true,
                new ConfigDescription("Adjust effects for the breast size\nUpdates after the scene change", null, new ConfigurationManagerAttributes { Order = 109 }));

            BreastLinearSpringStrength = Config.Bind("Breast", "LinearStrength", 15f, 
                new ConfigDescription("Strength of positional lag\nBigger value – more effort put out", null, new ConfigurationManagerAttributes { Order = 100 }));

            BreastLinearDamping = Config.Bind("Breast", "LinearDamping", 7f, 
                new ConfigDescription("Strength of negation of positional lag\nShould be smaller then LinearStrength for any effect", null, new ConfigurationManagerAttributes { Order = 90 }));

            BreastLinearGravity = Config.Bind("Breast", "LinearGravity", 0f, 
                new ConfigDescription("Strength of gravity for positional lag\nMost of the time looks better at 0 (disabled)", new AcceptableValueRange<float>(-1f, 1f), new ConfigurationManagerAttributes { Order = 80 }));

            //// TODO
            //BreastLinearMass = Config.Bind("Breast", "LinearMass", 1f, new ConfigDescription("Not implemented", null, new ConfigurationManagerAttributes { Order = 79 }));

            BreastLinearLimitPositive = Config.Bind("Breast", "LinearLimitPositive", Vector3.one, 
                new ConfigDescription("Axial limitation of movements in positive local space\n0..1..1+ to nullify/set default/amplify axis in positive local space", null, new ConfigurationManagerAttributes { Order = 78 }));

            BreastLinearLimitNegative = Config.Bind("Breast", "LinearLimitNegative", Vector3.one, 
                new ConfigDescription("Axial limitation of movements in negative local space\n0..1..1+ to nullify/set default/amplify axis in negative local space", null, new ConfigurationManagerAttributes { Order = 77 }));

            BreastAngularSpringStrength = Config.Bind("Breast", "AngularStrength", 20f, 
                new ConfigDescription("Strength of rotational lag\nBigger value – less lag", null, new ConfigurationManagerAttributes { Order = 70 }));

            BreastAngularDamping = Config.Bind("Breast", "AngularDamping", 5f, 
                new ConfigDescription("Strength of negation of rotational lag\nShould be smaller then AngularStrength for coherent effect", null, new ConfigurationManagerAttributes { Order = 60 }));

            // Will probably result in lots of NaNs if zero.
            BreastAngularMaxAngle = Config.Bind("Breast", "AngularMaxAngle", 45f, 
                new ConfigDescription("Rotational lag won't exceed this value in degrees", new AcceptableValueRange<float>(1f, 90f), new ConfigurationManagerAttributes { Order = 59 }));

            BreastAngularApplicationMaster = Config.Bind("Breast", "AngularApplyToRoot", Axis.Z,
                new ConfigDescription("Which axes or rotational lag should be applied to the root bone of the breast", null, new ConfigurationManagerAttributes { Order = 58 }));

            BreastAngularApplicationSlave = Config.Bind("Breast", "AngularApplyToBone", Axis.X | Axis.Y,
                new ConfigDescription("Which axes or rotational lag should be applied to the breast bones", null, new ConfigurationManagerAttributes { Order = 57 }));

            BreastScaleAccelerationFactor = Config.Bind("Breast", "ScaleAccelerationFactor", 0.35f, 
                new ConfigDescription("Strength of deformation during acceleration", null, new ConfigurationManagerAttributes { Order = 50 }));

            BreastScaleDecelerationFactor = Config.Bind("Breast", "ScaleDecelerationFactor", 0.5f, 
                new ConfigDescription("Strength of deformation during deceleration", null, new ConfigurationManagerAttributes { Order = 40 }));

            BreastScaleLerpSpeed = Config.Bind("Breast", "ScaleLerpSpeed", 8f, 
                new ConfigDescription("Speed of scale change\nBigger value – more rapid, less smooth change", null, new ConfigurationManagerAttributes { Order = 30 }));

            BreastScaleMaxDistortion = Config.Bind("Breast", "ScaleMaxDistortion", 0.4f, 
                new ConfigDescription("Scale deformation won't exceed scale of 1 +- this value", null, new ConfigurationManagerAttributes { Order = 20 }));

            BreastScaleUnevenDistribution = Config.Bind("Breast", "ScaleUnevenDistribution", new Vector3(0.6f, 0.5f, 0.4f), 
                new ConfigDescription("Preferential treatment of scale axes\nDefault value 0.5, can be used with no consideration towards balance", null, new ConfigurationManagerAttributes { Order = 19 }));

            BreastScalePreserveVolume = Config.Bind("Breast", "ScalePreserveVolume", true, 
                new ConfigDescription("Keep volume consistent", null, new ConfigurationManagerAttributes { Order = 18 }));

            BreastScaleDumbAcceleration = Config.Bind("Breast", "ScaleDumbAcceleration", true, 
                new ConfigDescription("Dumb looks better anyway", null, new ConfigurationManagerAttributes { Order = 17 }));

            BreastTetheringMultiplier = Config.Bind("Breast", "TetheringMultiplier", -500f, 
                new ConfigDescription("Strength of tethering", null, new ConfigurationManagerAttributes { Order = 10 }));

            BreastTetheringFrequency = Config.Bind("Breast", "TetheringFrequency", 3f, 
                new ConfigDescription("Ceiling for the amount oscillation per second", null, new ConfigurationManagerAttributes { Order = 0 }));

            BreastTetheringDamping = Config.Bind("Breast", "TetheringDamping", 0.3f, 
                new ConfigDescription("Strength of negation of tethering", null, new ConfigurationManagerAttributes { Order = -10 }));

            BreastTetheringMaxAngle = Config.Bind("Breast", "TetheringMaxAngle", 30f, 
                new ConfigDescription("Tethering won't exceed this value in degrees ", null, new ConfigurationManagerAttributes { Order = -20 }));

            // Not a slightest clue how to explain to users what the hell dots(cosines) do together with transform's directions.
            BreastGravityUpUp = Config.Bind("Breast", "GravityUpUp", Vector3.zero, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -100, IsAdvanced = true }));
            BreastGravityUpMid = Config.Bind("Breast", "GravityUpMid", new Vector3(0f, 0.02f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -101, IsAdvanced = true }));
            BreastGravityUpDown = Config.Bind("Breast", "GravityUpDown", new Vector3(0f, 0.05f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -102, IsAdvanced = true }));
            BreastGravityFwdUp = Config.Bind("Breast", "GravityFwdUp", new Vector3(0.075f, 0.075f, -0.15f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -103, IsAdvanced = true }));
            BreastGravityFwdMid = Config.Bind("Breast", "GravityFwdMid", Vector3.zero, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -104 , IsAdvanced = true }));
            BreastGravityFwdDown = Config.Bind("Breast", "GravityFwdDown", new Vector3(-0.05f, -0.05f, 0.2f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -105 , IsAdvanced = true }));
            BreastGravityRightUp = Config.Bind("Breast", "GravityRightUp", new Vector3(-0.025f, -0.02f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -106 , IsAdvanced = true }));
            BreastGravityRightMid = Config.Bind("Breast", "GravityRightMid", Vector3.zero, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -107 , IsAdvanced = true }));
            BreastGravityRightDown = Config.Bind("Breast", "GravityRightDown", new Vector3(0.025f, -0.02f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -108 , IsAdvanced = true }));

            #endregion


            #region Butt

            ButtEffects = Config.Bind("Butt", "Effects", Effect.Linear | Effect.Acceleration | Effect.Deceleration, 
                new ConfigDescription("Select effects to apply to the butt (bone)\nOnly appropriate effects can be selected\n" +
                "Linear – position of the bone is adjusted as if the bone was attached by an elastic band\n" +
                "Angular – rotation of the bone is adjusted as if the bone was a rod with an elastic attachment point, uses rotation of the bone\n" +
                "Tethering – rotation of the bone is adjusted as if the bone was a rod with an elastic attachment point, uses 'Linear' effect, synergies with 'Angular' effect\n" +
                "Acceleration – scale of the bone is extended along the axis of velocity and shrunk along perpendicular ones\n" +
                "Deceleration – scale of the bone is shrunk along the axis of deceleration and extended along perpendicular ones\n" +
                "GravityLinear – position of the bone is adjusted based on the rotation of the bone in the world space as imitation of the gravity\n" +
                "GravityAngular – rotation of the bone is adjusted based on the rotation of the bone in the world space as imitation of the gravity\n" +
                "GravityScale – scale of the bone is adjusted based on the rotation of the bone in the world space as imitation of the gravity", 
                null, new ConfigurationManagerAttributes { Order = 110 }));

            ButtAdjustForSize = Config.Bind("Butt", "AdjustForSize", true,
                new ConfigDescription("Adjust effects for the butt size", null, new ConfigurationManagerAttributes { Order = 109 }));

            ButtLinearSpringStrength = Config.Bind("Butt", "LinearStrength", 21f,
                new ConfigDescription("Strength of positional lag\nBigger value – more effort put out", null, new ConfigurationManagerAttributes { Order = 100 }));

            ButtLinearDamping = Config.Bind("Butt", "LinearDamping", 14f,
                new ConfigDescription("Strength of negation of positional lag\nShould be smaller then LinearStrength for any effect", null, new ConfigurationManagerAttributes { Order = 90 }));

            ButtLinearGravity = Config.Bind("Butt", "LinearGravity", 0f,
                new ConfigDescription("Strength of gravity for positional lag\nMost of the time looks better at 0 (disabled)", new AcceptableValueRange<float>(-1f, 1f), new ConfigurationManagerAttributes { Order = 80 }));

            //// TODO
            //ButtLinearMass = Config.Bind("Butt", "LinearMass", 1f, new ConfigDescription("Not implemented", null, new ConfigurationManagerAttributes { Order = 79 }));

            ButtLinearLimitPositive = Config.Bind("Butt", "LinearLimitPositive", new Vector3(1f, 1.33f, 1f),
                new ConfigDescription("Axial limitation of movements in positive local space\n0..1..1+ to nullify/set default/amplify axis in positive local space", null, new ConfigurationManagerAttributes { Order = 78 }));

            ButtLinearLimitNegative = Config.Bind("Butt", "LinearLimitNegative", new Vector3(1f, 0.67f, 1f),
                new ConfigDescription("Axial limitation of movements in negative local space\n0..1..1+ to nullify/set default/amplify axis in negative local space", null, new ConfigurationManagerAttributes { Order = 77 }));

            ButtAngularSpringStrength = Config.Bind("Butt", "AngularStrength", 10f,
                new ConfigDescription("Strength of rotational lag\nBigger value – less lag", null, new ConfigurationManagerAttributes { Order = 70 }));

            ButtAngularDamping = Config.Bind("Butt", "AngularDamping", 2f,
                new ConfigDescription("Strength of negation of rotational lag\nShould be smaller then AngularStrength for coherent effect", null, new ConfigurationManagerAttributes { Order = 60 }));

            // Will probably result in lots of NaNs if zero.
            ButtAngularMaxAngle = Config.Bind("Butt", "AngularMaxAngle", 45f,
                new ConfigDescription("Rotational lag won't exceed this value in degrees", new AcceptableValueRange<float>(1f, 90f), new ConfigurationManagerAttributes { Order = 59 }));

            // Might be THE controversial setting so far.
            ButtAngularApplicationMaster = Config.Bind("Butt", "AngularApplyToRootWIP", (Axis)0,
                new ConfigDescription("Which axes or rotational lag should be applied to the root of the butt bones", null, new ConfigurationManagerAttributes { Order = 58, IsAdvanced = true }));

            ButtAngularApplicationSlave = Config.Bind("Butt", "AngularApplyToBone", Axis.X | Axis.Y | Axis.Z,
                new ConfigDescription("Which axes or rotational lag should be applied to the butt bones", null, new ConfigurationManagerAttributes { Order = 57 }));

            ButtScaleAccelerationFactor = Config.Bind("Butt", "ScaleAccelerationFactor", 0.15f,
                new ConfigDescription("Strength of deformation during acceleration", null, new ConfigurationManagerAttributes { Order = 50 }));

            ButtScaleDecelerationFactor = Config.Bind("Butt", "ScaleDecelerationFactor", 0.3f,
                new ConfigDescription("Strength of deformation during deceleration", null, new ConfigurationManagerAttributes { Order = 40 }));

            ButtScaleLerpSpeed = Config.Bind("Butt", "ScaleLerpSpeed", 8f,
                new ConfigDescription("Speed of scale change\nBigger value – more rapid, less smooth change", null, new ConfigurationManagerAttributes { Order = 30 }));

            ButtScaleMaxDistortion = Config.Bind("Butt", "ScaleMaxDistortion", 0.4f,
                new ConfigDescription("Scale deformation won't exceed scale of 1 +- this value", null, new ConfigurationManagerAttributes { Order = 20 }));

            ButtScaleUnevenDistribution = Config.Bind("Butt", "ScaleUnevenDistribution", new Vector3(0.5f, 0.5f, 0.5f),
                new ConfigDescription("Preferential treatment of scale axes\nDefault value 0.5, can be used with no consideration towards balance", null, new ConfigurationManagerAttributes { Order = 19 }));

            ButtScalePreserveVolume = Config.Bind("Butt", "ScalePreserveVolume", true,
                new ConfigDescription("Keep volume consistent", null, new ConfigurationManagerAttributes { Order = 18 }));

            ButtScaleDumbAcceleration = Config.Bind("Butt", "ScaleDumbAcceleration", true,
                new ConfigDescription("Dumb looks better anyway", null, new ConfigurationManagerAttributes { Order = 17 }));
#if DEBUG
            ButtTetheringMultiplier = Config.Bind("Butt", "TetheringMultiplier", -500f,
                new ConfigDescription("Strength of tethering", null, new ConfigurationManagerAttributes { Order = 10 }));

            ButtTetheringFrequency = Config.Bind("Butt", "TetheringFrequency", 3f,
                new ConfigDescription("Ceiling for the amount oscillation per second", null, new ConfigurationManagerAttributes { Order = 0 }));

            ButtTetheringDamping = Config.Bind("Butt", "TetheringDamping", 0.3f,
                new ConfigDescription("Strength of negation of tethering", null, new ConfigurationManagerAttributes { Order = -10 }));

            ButtTetheringMaxAngle = Config.Bind("Butt", "TetheringMaxAngle", 30f,
                new ConfigDescription("Tethering won't exceed this value in degrees ", null, new ConfigurationManagerAttributes { Order = -20 }));


            ButtGravityUpUp = Config.Bind("Butt", "GravityUpUp", Vector3.zero, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -100, IsAdvanced = true }));
            ButtGravityUpMid = Config.Bind("Butt", "GravityUpMid", new Vector3(0f, 0.02f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -100, IsAdvanced = true }));
            ButtGravityUpDown = Config.Bind("Butt", "GravityUpDown", new Vector3(0f, 0.05f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -101, IsAdvanced = true }));
            ButtGravityFwdUp = Config.Bind("Butt", "GravityFwdUp", new Vector3(-0.1f, 0f, 0.15f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -102, IsAdvanced = true }));
            ButtGravityFwdMid = Config.Bind("Butt", "GravityFwdMid", Vector3.zero, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -103, IsAdvanced = true }));
            ButtGravityFwdDown = Config.Bind("Butt", "GravityFwdDown", new Vector3(-0.05f, -0.05f, 0.2f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -104, IsAdvanced = true }));
            ButtGravityRightUp = Config.Bind("Butt", "GravityRightUp", new Vector3(-0.025f, -0.02f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -105, IsAdvanced = true }));
            ButtGravityRightMid = Config.Bind("Butt", "GravityRightMid", Vector3.zero, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -106, IsAdvanced = true }));
            ButtGravityRightDown = Config.Bind("Butt", "GravityRightDown", new Vector3(0.025f, -0.02f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -107, IsAdvanced = true }));
#endif
#endregion

#if DEBUG
            #region Spine2

            ThighEffects = Config.Bind("Thigh", "Effects", Effect.Linear | Effect.Acceleration | Effect.Deceleration, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110 }));

            ThighLinearSpringStrength = Config.Bind("Thigh", "LinearStrength", 30f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100 }));
            ThighLinearDamping = Config.Bind("Thigh", "LinearDamping", 20f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 90 }));
            ThighLinearGravity = Config.Bind("Thigh", "LinearGravity", 0.1f, new ConfigDescription("", new AcceptableValueRange<float>(-1f, 1f), new ConfigurationManagerAttributes { Order = 80 }));
            //ThighLinearMass = Config.Bind("Thigh", "LinearMass", 1f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 79 }));
            ThighLinearLimitPositive = Config.Bind("Thigh", "LinearLimitPositive", Vector3.one, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 78 }));
            ThighLinearLimitNegative = Config.Bind("Thigh", "LinearLimitNegative", Vector3.one, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 77 }));

            ThighAngularSpringStrength = Config.Bind("Thigh", "AngularStrength", 30f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 70 }));
            ThighAngularDamping = Config.Bind("Thigh", "AngularDamping", 5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 60 }));
            ThighAngularMaxAngle = Config.Bind("Thigh", "AngularMaxAngle", 45f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 180f), new ConfigurationManagerAttributes { Order = 59 }));
            ThighAngularApplicationMaster = Config.Bind("Thigh", "AngularApplicationMaster", Axis.X | Axis.Y | Axis.Z, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 58 }));
            ThighAngularApplicationSlave = Config.Bind("Thigh", "AngularApplicationSlave", Axis.Y, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 57 }));

            ThighScaleAccelerationFactor = Config.Bind("Thigh", "ScaleAccelerationFactor", 40f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 50 }));
            ThighScaleDecelerationFactor = Config.Bind("Thigh", "ScaleDecelerationFactor", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 40 }));
            ThighScaleLerpSpeed = Config.Bind("Thigh", "ScaleLerpSpeed", 8f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 30 }));
            ThighScaleMaxDistortion = Config.Bind("Thigh", "ScaleMaxDistortion", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 20 }));
            ThighScaleUnevenDistribution = Config.Bind("Thigh", "ScaleUnevenDistribution", new Vector3(0.4f, 0.5f, 0.6f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 19 }));
            ThighScalePreserveVolume = Config.Bind("Thigh", "ScalePreserveVolume", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 18 }));
            ThighScaleDumbAcceleration = Config.Bind("Thigh", "Scale DumbAcceleration", true, new ConfigDescription("Dumb looks better anyway", null, new ConfigurationManagerAttributes { Order = 17 }));

            ThighTetheringMultiplier = Config.Bind("Thigh", "TetheringMultiplier", -500f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 10 }));
            ThighTetheringFrequency = Config.Bind("Thigh", "TetheringFrequency", 3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 0 }));
            ThighTetheringDamping = Config.Bind("Thigh", "TetheringDamping", 0.3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -10 }));
            ThighTetheringMaxAngle = Config.Bind("Thigh", "TetheringMaxAngle", 30f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -20 }));


            ThighGravityUpUp = Config.Bind("Thigh", "GravityUpUp", Vector3.zero, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -100, IsAdvanced = true }));
            ThighGravityUpMid = Config.Bind("Thigh", "GravityUpMid", new Vector3(0f, 0.02f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -100, IsAdvanced = true }));
            ThighGravityUpDown = Config.Bind("Thigh", "GravityUpDown", new Vector3(0f, 0.05f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -101, IsAdvanced = true }));
            ThighGravityFwdUp = Config.Bind("Thigh", "GravityFwdUp", new Vector3(-0.1f, 0f, 0.15f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -102, IsAdvanced = true }));
            ThighGravityFwdMid = Config.Bind("Thigh", "GravityFwdMid", Vector3.zero, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -103, IsAdvanced = true }));
            ThighGravityFwdDown = Config.Bind("Thigh", "GravityFwdDown", new Vector3(-0.05f, -0.05f, 0.2f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -104, IsAdvanced = true }));
            ThighGravityRightUp = Config.Bind("Thigh", "GravityRightUp", new Vector3(-0.025f, -0.02f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -105, IsAdvanced = true }));
            ThighGravityRightMid = Config.Bind("Thigh", "GravityRightMid", Vector3.zero, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -106, IsAdvanced = true }));
            ThighGravityRightDown = Config.Bind("Thigh", "GravityRightDown", new Vector3(0.025f, -0.02f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -107, IsAdvanced = true }));

            #endregion
#endif

        }


        private void AddSettingChangedParam()
        {
            var fields = typeof(AniMorph).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            var configTypeEffect = typeof(ConfigEntry<Effect>);
            var configTypeAxis = typeof(ConfigEntry<Axis>);
            var configTypeVector = typeof(ConfigEntry<Vector3>);
            var configTypeFloat = typeof(ConfigEntry<float>);
            var configTypeBool = typeof(ConfigEntry<bool>);
            var configTypeGender = typeof(ConfigEntry<Gender>);

            foreach (var f in fields)
            {
                if (f.FieldType == configTypeVector)
                {
                    var configEntry = (ConfigEntry<Vector3>)f.GetValue(null);

                    if (configEntry == null) continue;
                    configEntry.SettingChanged += (_, _) => UpdateConfig();
                }
                else if (f.FieldType == configTypeFloat)
                {
                    var configEntry = (ConfigEntry<float>)f.GetValue(null);

                    if (configEntry == null) continue;
                    configEntry.SettingChanged += (_, _) => UpdateConfig();
                }
                else if (f.FieldType == configTypeEffect)
                {
                    var configEntry = (ConfigEntry<Effect>)f.GetValue(null);

                    if (configEntry == null) continue;
                    configEntry.SettingChanged += (_, _) => UpdateConfig();
                }
                else if (f.FieldType == configTypeBool)
                {
                    var configEntry = (ConfigEntry<bool>)f.GetValue(null);

                    if (configEntry == null) continue;
                    configEntry.SettingChanged += (_, _) => UpdateConfig();
                }
                else if (f.FieldType == configTypeAxis)
                {
                    var configEntry = (ConfigEntry<Axis>)f.GetValue(null);

                    if (configEntry == null) continue;
                    configEntry.SettingChanged += (_, _) => UpdateConfig();
                }
                else if (f.FieldType == configTypeGender)
                {
                    var configEntry = (ConfigEntry<Gender>)f.GetValue(null);

                    if (configEntry == null) continue;
                    configEntry.SettingChanged += (_, _) => UpdateConfig();
                }
            }
        }


        private void UpdateConfig()
        {
#if !DEBUG
            AdjustAllowedEffects();
#endif

            var type = typeof(AniMorphCharaController);
            foreach (var charaController in CharacterApi.GetBehaviours())
            {
                if (charaController != null && charaController.GetType() == type)
                {
                    var aniMorphCharaController = (AniMorphCharaController)charaController;
                    aniMorphCharaController.OnConfigUpdate();
                }
            }
        }

        private void AdjustAllowedEffects()
        {
            foreach (Effect effect in Enum.GetValues(typeof(Effect)))
            {
                if ((_allowedEffectsDic[Body.Breast] & effect) == 0)
                {
                    BreastEffects.Value &= ~effect;
                }
                if ((_allowedEffectsDic[Body.Butt] & effect) == 0)
                {
                    ButtEffects.Value &= ~effect;
                }
            }
        }

        [Flags]
        public enum Body
        {
            Breast,
            Butt,
            Thigh,


        }
        [Flags]
        public enum Axis
        {
            X = 1,
            Y = 2,
            Z = 4,
        }
        [Flags]
        public enum Gender
        {
            Male = 1,
            Female = 2,
        }
        
    }
}
