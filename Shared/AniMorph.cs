using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
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

#if KK
    [BepInProcess(KoikatuAPI.GameProcessNameSteam)]
#endif
    internal class AniMorph : BaseUnityPlugin
    {
        public const string GUID = "AniMorph.ABMX";
        public const string Name = "Anisotropic Morph";
        public const string Version = "0.1";



        internal new static ManualLogSource Logger;

        public static ConfigEntry<bool> Enable;

        #region Breast

        public static ConfigEntry<Effect> BreastEffects;

        public static ConfigEntry<float> BreastLinearGravity;
        public static ConfigEntry<float> BreastLinearSpringStrength;
        public static ConfigEntry<float> BreastLinearDamping;
        public static ConfigEntry<float> BreastLinearMass;
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

        public static ConfigEntry<float> ButtLinearGravity;
        public static ConfigEntry<float> ButtLinearSpringStrength;
        public static ConfigEntry<float> ButtLinearDamping;
        public static ConfigEntry<float> ButtLinearMass;
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


        #endregion





        private void Awake()
        {
            Logger = base.Logger;

            Enable = Config.Bind("", "Enable", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100 }));

            CharacterApi.RegisterExtraBehaviour<AniMorphCharaController>(GUID);

            BindConfig();

            AddSettingChangedParam();

            // Avoid hooking this one up for UpdateConfig().
        }

        private void BindConfig()
        {
            #region Breast

            BreastEffects = Config.Bind("Breast", "Effects", Effect.Linear | Effect.Angular | Effect.Tethering | Effect.Acceleration | Effect.Deceleration, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110 }));

            BreastLinearSpringStrength = Config.Bind("Breast", "LinearStrength", 15f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100 }));
            BreastLinearDamping = Config.Bind("Breast", "LinearDamping", 7f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 90 }));
            BreastLinearGravity = Config.Bind("Breast", "LinearGravity", 0.075f, new ConfigDescription("", new AcceptableValueRange<float>(-1f, 1f), new ConfigurationManagerAttributes { Order = 80 }));
            BreastLinearMass = Config.Bind("Breast", "LinearMass", 1f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 79 }));
            BreastLinearLimitPositive = Config.Bind("Breast", "LinearLimitPlus", Vector3.one, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 78 }));
            BreastLinearLimitNegative = Config.Bind("Breast", "LinearLimitMinus", Vector3.one, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 77 }));

            BreastAngularSpringStrength = Config.Bind("Breast", "AngularStrength", 30f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 70 }));
            BreastAngularDamping = Config.Bind("Breast", "AngularDamping", 5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 60 }));
            // Will probably result in lots of NaNs if zero.
            BreastAngularMaxAngle = Config.Bind("Breast", "AngularMaxAngle", 45f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 180f), new ConfigurationManagerAttributes { Order = 59 }));
            BreastAngularApplicationMaster = Config.Bind("Breast", "AngularApplicationMaster", Axis.Z, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 58 }));
            BreastAngularApplicationSlave = Config.Bind("Breast", "AngularApplicationSlave", Axis.X | Axis.Y, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 57 }));

            BreastScaleAccelerationFactor = Config.Bind("Breast", "ScaleAccelerationFactor", 40f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 50 }));
            BreastScaleDecelerationFactor = Config.Bind("Breast", "ScaleDecelerationFactor", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 40 }));
            BreastScaleLerpSpeed = Config.Bind("Breast", "Scale LerpSpeed", 8f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 30 }));
            BreastScaleMaxDistortion = Config.Bind("Breast", "ScaleMaxDistortion", 0.4f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 20 }));
            BreastScaleUnevenDistribution = Config.Bind("Breast", "ScaleUnevenDistribution", new Vector3(0.67f, 0.5f, 0.33f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 19 }));
            BreastScalePreserveVolume = Config.Bind("Breast", "ScalePreserveVolume", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 18 }));
            BreastScaleDumbAcceleration = Config.Bind("Breast", "ScaleDumbAcceleration", true, new ConfigDescription("Dumb looks better anyway", null, new ConfigurationManagerAttributes { Order = 17 }));

            BreastTetheringMultiplier = Config.Bind("Breast", "TetheringMultiplier", -500f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 10 }));
            BreastTetheringFrequency = Config.Bind("Breast", "TetheringFrequency", 3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 0 }));
            BreastTetheringDamping = Config.Bind("Breast", "TetheringDamping", 0.3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -10 }));
            BreastTetheringMaxAngle = Config.Bind("Breast", "TetheringMaxAngle", 30f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -20 }));

            // Not a slightest clue how to explain to users what the hell dots(cosines) do together with transform's directions.
            BreastGravityUpUp = Config.Bind("Breast", "GravityUpUp", Vector3.zero, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -100, IsAdvanced = true }));
            BreastGravityUpMid = Config.Bind("Breast", "GravityUpMid", new Vector3(0f, 0.02f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -100, IsAdvanced = true }));
            BreastGravityUpDown = Config.Bind("Breast", "GravityUpDown", new Vector3(0f, 0.05f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -101, IsAdvanced = true }));
            BreastGravityFwdUp = Config.Bind("Breast", "GravityFwdUp", new Vector3(0.075f, 0.075f, -0.15f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -102, IsAdvanced = true }));
            BreastGravityFwdMid = Config.Bind("Breast", "GravityFwdMid", Vector3.zero, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -103 , IsAdvanced = true }));
            BreastGravityFwdDown = Config.Bind("Breast", "GravityFwdDown", new Vector3(-0.05f, -0.05f, 0.2f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -104 , IsAdvanced = true }));
            BreastGravityRightUp = Config.Bind("Breast", "GravityRightUp", new Vector3(-0.025f, -0.02f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -105 , IsAdvanced = true }));
            BreastGravityRightMid = Config.Bind("Breast", "GravityRightMid", Vector3.zero, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -106 , IsAdvanced = true }));
            BreastGravityRightDown = Config.Bind("Breast", "GravityRightDown", new Vector3(0.025f, -0.02f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -107 , IsAdvanced = true }));

            #endregion


            #region Butt

            ButtEffects = Config.Bind("Butt", "Effects", Effect.Linear | Effect.Acceleration | Effect.Deceleration, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110 }));

            ButtLinearSpringStrength = Config.Bind("Butt", "LinearStrength", 30f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100 }));
            ButtLinearDamping = Config.Bind("Butt", "LinearDamping", 20f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 90 }));
            ButtLinearGravity = Config.Bind("Butt", "LinearGravity", 0.1f, new ConfigDescription("", new AcceptableValueRange<float>(-1f, 1f), new ConfigurationManagerAttributes { Order = 80 }));
            ButtLinearMass = Config.Bind("Butt", "LinearMass", 1f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 79 }));
            ButtLinearLimitPositive = Config.Bind("Butt", "LinearLimitPositive", Vector3.one, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 78 }));
            ButtLinearLimitNegative = Config.Bind("Butt", "LinearLimitNegative", Vector3.one, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 77 }));

            ButtAngularSpringStrength = Config.Bind("Butt", "AngularStrength", 30f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 70 }));
            ButtAngularDamping = Config.Bind("Butt", "AngularDamping", 5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 60 }));
            ButtAngularMaxAngle = Config.Bind("Butt", "AngularMaxAngle", 45f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 180f), new ConfigurationManagerAttributes { Order = 59 }));
            ButtAngularApplicationMaster = Config.Bind("Butt", "AngularApplicationMaster", Axis.X | Axis.Y | Axis.Z, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 58 }));
            ButtAngularApplicationSlave = Config.Bind("Butt", "AngularApplicationSlave", Axis.Y, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 57 }));

            ButtScaleAccelerationFactor = Config.Bind("Butt", "ScaleAccelerationFactor", 40f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 50 }));
            ButtScaleDecelerationFactor = Config.Bind("Butt", "ScaleDecelerationFactor", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 40 }));
            ButtScaleLerpSpeed = Config.Bind("Butt", "ScaleLerpSpeed", 8f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 30 }));
            ButtScaleMaxDistortion = Config.Bind("Butt", "ScaleMaxDistortion", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 20 }));
            ButtScaleUnevenDistribution = Config.Bind("Butt", "ScaleUnevenDistribution", new Vector3(0.4f, 0.5f, 0.6f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 19 }));
            ButtScalePreserveVolume = Config.Bind("Butt", "ScalePreserveVolume", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 18 }));
            ButtScaleDumbAcceleration = Config.Bind("Butt", "Scale DumbAcceleration", true, new ConfigDescription("Dumb looks better anyway", null, new ConfigurationManagerAttributes { Order = 17 }));

            ButtTetheringMultiplier = Config.Bind("Butt", "TetheringMultiplier", -500f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 10 }));
            ButtTetheringFrequency = Config.Bind("Butt", "TetheringFrequency", 3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 0 }));
            ButtTetheringDamping = Config.Bind("Butt", "TetheringDamping", 0.3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -10 }));
            ButtTetheringMaxAngle = Config.Bind("Butt", "TetheringMaxAngle", 30f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -20 }));


            ButtGravityUpUp = Config.Bind("Butt", "GravityUpUp", Vector3.zero, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -100, IsAdvanced = true }));
            ButtGravityUpMid = Config.Bind("Butt", "GravityUpMid", new Vector3(0f, 0.02f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -100, IsAdvanced = true }));
            ButtGravityUpDown = Config.Bind("Butt", "GravityUpDown", new Vector3(0f, 0.05f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -101, IsAdvanced = true }));
            ButtGravityFwdUp = Config.Bind("Butt", "GravityFwdUp", new Vector3(-0.1f, 0f, 0.15f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -102, IsAdvanced = true }));
            ButtGravityFwdMid = Config.Bind("Butt", "GravityFwdMid", Vector3.zero, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -103, IsAdvanced = true }));
            ButtGravityFwdDown = Config.Bind("Butt", "GravityFwdDown", new Vector3(-0.05f, -0.05f, 0.2f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -104, IsAdvanced = true }));
            ButtGravityRightUp = Config.Bind("Butt", "GravityRightUp", new Vector3(-0.025f, -0.02f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -105, IsAdvanced = true }));
            ButtGravityRightMid = Config.Bind("Butt", "GravityRightMid", Vector3.zero, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -106, IsAdvanced = true }));
            ButtGravityRightDown = Config.Bind("Butt", "GravityRightDown", new Vector3(0.025f, -0.02f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -107, IsAdvanced = true }));

            #endregion


        }


        private void AddSettingChangedParam()
        {
            var fields = typeof(AniMorph).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            var configTypeEffect = typeof(ConfigEntry<Effect>);
            var configTypeAxis = typeof(ConfigEntry<Axis>);
            var configTypeVector = typeof(ConfigEntry<Vector3>);
            var configTypeFloat = typeof(ConfigEntry<float>);
            var configTypeBool = typeof(ConfigEntry<bool>);

            foreach (var f in fields)
            {
                if (f.FieldType == configTypeVector)
                {
                    var configEntry = (ConfigEntry<Vector3>)f.GetValue(null);
                    configEntry.SettingChanged += (_, _) => UpdateConfig();
                }
                else if (f.FieldType == configTypeFloat)
                {
                    var configEntry = (ConfigEntry<float>)f.GetValue(null);
                    configEntry.SettingChanged += (_, _) => UpdateConfig();
                }
                else if (f.FieldType == configTypeEffect)
                {
                    var configEntry = (ConfigEntry<Effect>)f.GetValue(null);
                    configEntry.SettingChanged += (_, _) => UpdateConfig();
                }
                else if (f.FieldType == configTypeBool)
                {
                    var configEntry = (ConfigEntry<bool>)f.GetValue(null);
                    configEntry.SettingChanged += (_, _) => UpdateConfig();
                }
                else if (f.FieldType == configTypeAxis)
                {
                    var configEntry = (ConfigEntry<Axis>)f.GetValue(null);
                    configEntry.SettingChanged += (_, _) => UpdateConfig();
                }
            }
        }


        private void UpdateConfig()
        {
            var type = typeof(AniMorphCharaController);
            foreach (var charaController in CharacterApi.GetBehaviours())
            {
                if (charaController != null && charaController.GetType() == type)
                {
                    ((AniMorphCharaController)charaController).BoneEffector.OnConfigUpdate();
                }
            }
        }
        [Flags]
        public enum Body
        {
            Breast = 1,
            Butt = 2,

        }
        [Flags]
        public enum Axis
        {
            X = 1,
            Y = 2,
            Z = 4,
        }
    }
}
