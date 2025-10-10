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
        public static ConfigEntry<Body> BodyArea;

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

        public static ConfigEntry<float> BreastScaleAccelerationFactor;
        public static ConfigEntry<float> BreastScaleDecelerationFactor;
        public static ConfigEntry<float> BreastScaleLerpSpeed;
        public static ConfigEntry<float> BreastScaleMaxDistortion;
        public static ConfigEntry<bool> BreastScalePreserveVolume;
        public static ConfigEntry<Vector3> BreastScaleUnevenDistribution;

        public static ConfigEntry<float> BreastTetheringMultiplier;
        public static ConfigEntry<float> BreastTetheringFrequency;
        public static ConfigEntry<float> BreastTetheringDamping;
        public static ConfigEntry<float> BreastTetheringMaxAngle;



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

        public static ConfigEntry<float> ButtScaleAccelerationFactor;
        public static ConfigEntry<float> ButtScaleDecelerationFactor;
        public static ConfigEntry<float> ButtScaleLerpSpeed;
        public static ConfigEntry<float> ButtScaleMaxDistortion;
        public static ConfigEntry<bool> ButtScalePreserveVolume;
        public static ConfigEntry<Vector3> ButtScaleUnevenDistribution;

        public static ConfigEntry<float> ButtTetheringMultiplier;
        public static ConfigEntry<float> ButtTetheringFrequency;
        public static ConfigEntry<float> ButtTetheringDamping;
        public static ConfigEntry<float> ButtTetheringMaxAngle;


        #endregion





        private void Awake()
        {
            Logger = base.Logger;

            Enable = Config.Bind("", "Enable", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100 }));



            BodyArea = Config.Bind("", "ShowArea", Body.None, new ConfigDescription("Reload the config window for new entries to show.", null, new ConfigurationManagerAttributes { Order = 80 }));





            CharacterApi.RegisterExtraBehaviour<AniMorphCharaController>(GUID);


            BodyArea.SettingChanged += (_, _) => BindConfig();

            BindConfig();

            AddSettingChangedParam();
        }

        private void BindConfig()
        {
            //if ((BodyArea.Value & Body.Breast) != 0)
            {

                BreastEffects = Config.Bind("Breast", "Effects", Effect.Linear | Effect.Angular | Effect.Tethering | Effect.Acceleration | Effect.Deceleration, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110 }));

                BreastLinearSpringStrength = Config.Bind("Breast", "Linear Strength", 15f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100 }));
                BreastLinearDamping = Config.Bind("Breast", "Linear Damping", 7f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 90 }));
                BreastLinearGravity = Config.Bind("Breast", "Linear Gravity", 0.075f, new ConfigDescription("", new AcceptableValueRange<float>(-1f, 1f), new ConfigurationManagerAttributes { Order = 80 }));
                BreastLinearMass = Config.Bind("Breast", "Linear Mass", 1f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 79 }));
                BreastLinearLimitPositive = Config.Bind("Breast", "Linear LimitPlus", Vector3.one, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 78 }));
                BreastLinearLimitNegative = Config.Bind("Breast", "Linear LimitMinus", Vector3.one, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 77 }));

                BreastAngularSpringStrength = Config.Bind("Breast", "AngularStrength", 30f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 70 }));
                BreastAngularDamping = Config.Bind("Breast", "AngularDamping", 5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 60 }));

                BreastScaleAccelerationFactor = Config.Bind("Breast", "ScaleAccelerationFactor", 40f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 50 }));
                BreastScaleDecelerationFactor = Config.Bind("Breast", "ScaleDecelerationFactor", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 40 }));
                BreastScaleLerpSpeed = Config.Bind("Breast", "ScaleLerpSpeed", 8f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 30 }));
                BreastScaleMaxDistortion = Config.Bind("Breast", "ScaleMaxDistortion", 0.4f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 20 }));
                BreastScaleUnevenDistribution = Config.Bind("Breast", "ScaleUnevenDistribution", new Vector3(0.67f, 0.5f, 0.33f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 19 }));
                BreastScalePreserveVolume = Config.Bind("Breast", "ScalePreserveVolume", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 18 }));

                BreastTetheringMultiplier = Config.Bind("Breast", "TetheringMultiplier", -500f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 10 }));
                BreastTetheringFrequency = Config.Bind("Breast", "TetheringFrequency", 3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 0 }));
                BreastTetheringDamping = Config.Bind("Breast", "TetheringDamping", 0.3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -10 }));
                BreastTetheringMaxAngle = Config.Bind("Breast", "TetheringMaxAngle", 30f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -20 }));
            }
            //if ((BodyArea.Value & Body.Butt) != 0)
            {
                ButtEffects = Config.Bind("Butt", "Effects", Effect.Linear | Effect.Acceleration | Effect.Deceleration, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110 }));

                ButtLinearSpringStrength = Config.Bind("Butt", "LinearStrength", 30f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100 }));
                ButtLinearDamping = Config.Bind("Butt", "LinearDamping", 20f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 90 }));
                ButtLinearGravity = Config.Bind("Butt", "LinearGravity", 0.1f, new ConfigDescription("", new AcceptableValueRange<float>(-1f, 1f), new ConfigurationManagerAttributes { Order = 80 }));
                ButtLinearMass = Config.Bind("Butt", "LinearMass", 1f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 79 }));
                ButtLinearLimitPositive = Config.Bind("Butt", "LinearLimitPositive", Vector3.one, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 78 }));
                ButtLinearLimitNegative = Config.Bind("Butt", "LinearLimitNegative", Vector3.one, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 77 }));

                ButtAngularSpringStrength = Config.Bind("Butt", "AngularStrength", 30f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 70 }));
                ButtAngularDamping = Config.Bind("Butt", "AngularDamping", 5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 60 }));

                ButtScaleAccelerationFactor = Config.Bind("Butt", "ScaleAccelerationFactor", 40f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 50 }));
                ButtScaleDecelerationFactor = Config.Bind("Butt", "ScaleDecelerationFactor", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 40 }));
                ButtScaleLerpSpeed = Config.Bind("Butt", "ScaleLerpSpeed", 8f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 30 }));
                ButtScaleMaxDistortion = Config.Bind("Butt", "ScaleMaxDistortion", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 20 }));
                ButtScaleUnevenDistribution = Config.Bind("Butt", "ScaleUnevenDistribution", new Vector3(0.4f, 0.5f, 0.6f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 19 }));
                ButtScalePreserveVolume = Config.Bind("Butt", "ScalePreserveVolume", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 18 }));

                ButtTetheringMultiplier = Config.Bind("Butt", "TetheringMultiplier", -500f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 10 }));
                ButtTetheringFrequency = Config.Bind("Butt", "TetheringFrequency", 3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 0 }));
                ButtTetheringDamping = Config.Bind("Butt", "TetheringDamping", 0.3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -10 }));
                ButtTetheringMaxAngle = Config.Bind("Butt", "TetheringMaxAngle", 30f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -20 }));
            }

        }


        private void AddSettingChangedParam()
        {
            var fields = typeof(AniMorph).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            var configTypeVector = typeof(ConfigEntry<Vector3>);
            var configTypeFloat = typeof(ConfigEntry<float>);
            var configTypeEffect = typeof(ConfigEntry<Effect>);
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
            None = 0,
            Breast = 1,
            Butt = 2,

        }
    }
}
