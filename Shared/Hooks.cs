using ActionGame.Place;
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
        public static void HSceneProc_ChangeAnimator_Postfix(HSceneProc.AnimationListInfo _nextAinmInfo)
        {
            var type = typeof(AniMorphCharaController);
            foreach (var charaController in CharacterApi.GetBehaviours())
            {
                if (charaController != null && charaController.GetType() == type)
                {
                    ((AniMorphCharaController)charaController).BoneEffector?.OnChangeAnimator();
                }
            }
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

    public class HooksMaleEnableDB
    {
        private static Harmony _patchMaleEnableDB;
        public static void ApplyHooks()
        {
#if DEBUG
            AniMorph.Logger.LogDebug($"ApplyMaleEnableDB[{AniMorph.MaleEnableDB.Value}]");
#endif
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
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.playDynamicBoneBust), [typeof(int), typeof(bool)])]
        public static void playDynamicBoneBustPostfix1(int _nArea, ref bool _bPlay, ChaControl __instance)
        {
            // Enabled instead of disabling DBs on males
            if (!_bPlay && __instance.sex == 0)
            {
#if DEBUG
                AniMorph.Logger.LogDebug($"{MethodBase.GetCurrentMethod().Name}:[{_bPlay} => [{!_bPlay}]");
#endif
                _bPlay = true;

            }
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.playDynamicBoneBust), [typeof(ChaInfo.DynamicBoneKind), typeof(bool)])]
        public static void playDynamicBoneBustPostfix2(ChaInfo.DynamicBoneKind _eArea, ref bool _bPlay, ChaControl __instance)
        {
            // Enabled instead of disabling DBs on males
            if (!_bPlay && __instance.sex == 0)
            {
#if DEBUG
                AniMorph.Logger.LogDebug($"{MethodBase.GetCurrentMethod().Name}:[{_bPlay} => [{!_bPlay}]");
#endif
                _bPlay = true;

            }
        }
    }
}
