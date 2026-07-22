using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoligionFramework
{
    // Page_ChooseIdeoPreset.DoPreset runs when the player has selected a preset card and pressed Next;
    // vanilla generates a fresh ideo from the card's memes. For our Custom-group cards (each backed by a
    // bundled .rid) we instead load the complete, pre-configured ideoligion from its file and advance the
    // page, so the player gets the exact ideoligion the mod author saved, not a memes approximation.
    [HarmonyPatch(typeof(Page_ChooseIdeoPreset), "DoPreset")]
    public static class Patch_Page_ChooseIdeoPreset_DoPreset
    {
        private static readonly AccessTools.FieldRef<Page_ChooseIdeoPreset, IdeoPresetDef> SelectedIdeoRef =
            AccessTools.FieldRefAccess<Page_ChooseIdeoPreset, IdeoPresetDef>("selectedIdeo");

        // Non-virtual invoker for Page.DoNext (the base "advance to next page" step). We can't just call
        // it: the instance overrides DoNext, so a normal/reflected call would re-enter the override and
        // recurse. A tiny DynamicMethod emits a non-virtual `call` to the base method.
        private static readonly Action<Page> BaseDoNext = BuildBaseDoNext();

        public static bool Prefix(Page_ChooseIdeoPreset __instance)
        {
            IdeoPresetDef selected = SelectedIdeoRef(__instance);
            if (selected == null || !CustomPresetBuilder.TryGetRidPath(selected, out string ridPath))
            {
                return true; // vanilla preset: generate from memes as normal
            }

            if (!GameDataSaveLoader.TryLoadIdeo(ridPath, out Ideo ideo) || ideo == null)
            {
                Log.Error("[Ideoligion Framework] Failed to load bundled ideoligion '" + selected.label +
                    "' from " + ridPath + "; falling back to the vanilla memes preset.");
                return true;
            }

            // The loaded ideo already carries its own styles/precepts/roles from the file, so we do not
            // apply or randomize styles the way vanilla DoPreset would.
            AssignIdeoToPlayer(ideo);
            Find.IdeoManager.RemoveUnusedStartingIdeos();
            Find.Scenario.PostIdeoChosen();
            BaseDoNext(__instance);
            return false;
        }

        // Mirrors Page_ChooseIdeoPreset.AssignIdeoToPlayer (private).
        private static void AssignIdeoToPlayer(Ideo ideo)
        {
            Faction.OfPlayer.ideos.SetPrimary(ideo);
            foreach (Ideo other in Find.IdeoManager.IdeosListForReading)
            {
                other.initialPlayerIdeo = false;
            }
            ideo.initialPlayerIdeo = true;
            Find.IdeoManager.Add(ideo);
        }

        private static Action<Page> BuildBaseDoNext()
        {
            MethodInfo baseMethod = AccessTools.Method(typeof(Page), "DoNext");
            var dm = new DynamicMethod(
                "IdeoligionFramework_BasePageDoNext",
                returnType: null,
                parameterTypes: new[] { typeof(Page) },
                owner: typeof(Patch_Page_ChooseIdeoPreset_DoPreset),
                skipVisibility: true);
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, baseMethod);
            il.Emit(OpCodes.Ret);
            return (Action<Page>)dm.CreateDelegate(typeof(Action<Page>));
        }
    }
}
