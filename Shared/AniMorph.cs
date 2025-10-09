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
        public static ConfigEntry<Effect> Effects;

        private void Awake()
        {
            Logger = base.Logger;

            Enable = Config.Bind("", "Enable", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100 }));

            Effects = Config.Bind("", "Effects", Effect.Linear | Effect.Angular | Effect.Tethering, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 90 }));

            CharacterApi.RegisterExtraBehaviour<AniMorphCharaController>(GUID);

            Effects.SettingChanged += (_, _) => UpdateConfig();

            //AddSettingChangedParam();
        }


        private void AddSettingChangedParam()
        {
            var field = typeof(AniMorph).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            var configTypeVector = typeof(ConfigEntry<Vector3>);
            var configTypeFloat = typeof(ConfigEntry<float>);

            foreach (var f in field)
            {
                if (f.FieldType == configTypeVector)
                {
                    var configEntry = (ConfigEntry<Vector3>)f.GetValue(null);
                    configEntry.SettingChanged += (s, e) => UpdateConfig();
                }
                else if (f.FieldType == configTypeFloat)
                {
                    var configEntry = (ConfigEntry<float>)f.GetValue(null);
                    configEntry.SettingChanged += (s, e) => UpdateConfig();
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
    }
}
