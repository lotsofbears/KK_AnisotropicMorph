﻿using ActionGame.Place;
using HarmonyLib;
using KKAPI.Chara;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace AniMorph
{
    public class Hooks
    {
        private static Harmony _patch;

        //private static Func<bool> InTransition;


        //public static bool CrossFaderInTransition
        //{
        //    get
        //    {
        //        if (InTransition == null) return false;

        //        return InTransition.Invoke();
        //    }
        //}

        public static void ApplyHooks()
        {
            _patch ??= Harmony.CreateAndPatchAll(typeof(Hooks));

            
            //SetupDelegates();
        }
        //private static void SetupDelegates()
        //{
        //    var type = AccessTools.TypeByName("KK_VR.Features.CrossFader");

        //    if (type == null) return;

        //    var methodInfo = AccessTools.PropertyGetter(type, "InTransition");

        //    if (methodInfo != null)
        //    {
        //        InTransition = AccessTools.MethodDelegate<Func<bool>>(methodInfo);
        //    }
        //}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.ChangeAnimator))]
        public static void HSceneProc_ChangeAnimator_Postfix(HSceneProc.AnimationListInfo _nextAinmInfo, HSceneProc __instance)
        {
            var type = typeof(AniMorphCharaController);
            foreach (var charaController in CharacterApi.GetBehaviours())
            {
                if (charaController != null && charaController.GetType() == type)
                {
                    ((AniMorphCharaController)charaController).BoneEffector?.OnChangeAnimator();
                }
            }
#if KK
            if (AniMorph.MaleEnableDB.Value)
            {
                if (__instance.male != null)
                    EnableCharaDB(__instance.male);

                // Account for versions without 2nd male.
                var traverse = Traverse.Create(__instance);
                var male1 = traverse.Field("male1").GetValue<ChaControl>();

                if (male1 != null)
                    EnableCharaDB(male1);
            }

            static void EnableCharaDB(ChaControl chara)
            {
                foreach (ChaInfo.DynamicBoneKind dbKind in Enum.GetValues(typeof(ChaInfo.DynamicBoneKind)))
                {
                    chara.playDynamicBoneBust(dbKind, true);
                }
            }
#endif
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(HActionBase), nameof(HActionBase.SetPlay))]
        public static void HActionBase_SetPlay_Postfix(string _nextAnimation)
        {
            var type = typeof(AniMorphCharaController);
            foreach (var charaController in CharacterApi.GetBehaviours())
            {
                if (charaController != null && charaController.GetType() == type)
                {
                    ((AniMorphCharaController)charaController).BoneEffector?.OnSetPlay(_nextAnimation);
                }
            }
        }

    }

    
    /// <summary>
    /// Harmony hook to enable dynamic bones on males in KKS
    /// </summary>
    public class HooksMaleEnableDB
    {
        private static Harmony _patchMaleEnableDB;
        public static void ApplyHooks()
        {
#if DEBUG
            AniMorph.Logger.LogDebug($"ApplyMaleEnableDB[{AniMorph.MaleEnableDB.Value}]");
#endif
            // KK doesn't proactively setup male DBs, so we do it on animator change.
#if KKS
            if (AniMorph.MaleEnableDB.Value)
            {
                _patchMaleEnableDB ??= Harmony.CreateAndPatchAll(typeof(HooksMaleEnableDB));
            }
            else
            {
                if (_patchMaleEnableDB != null)
                {
                    _patchMaleEnableDB.UnpatchSelf();
                    _patchMaleEnableDB = null;
                }
            }
#endif
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.playDynamicBoneBust), [typeof(ChaInfo.DynamicBoneKind), typeof(bool)])]
        public static void playDynamicBoneBustPrefix2(ChaInfo.DynamicBoneKind _eArea, ref bool _bPlay, ChaControl __instance)
        {
#if DEBUG
            AniMorph.Logger.LogDebug($"{MethodBase.GetCurrentMethod().Name}:eArea[{_eArea}] bPlay[{_bPlay}] male[{__instance.sex == 0}]");
#endif
            // Enabled instead of disabling DBs on males
            if (!_bPlay && __instance.sex == 0)
            {
                _bPlay = true;

            }
        }
    }
}
