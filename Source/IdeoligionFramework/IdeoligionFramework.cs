using System;
using HarmonyLib;
using Verse;

namespace IdeoligionFramework
{
    // Lets mods ship fully-configured ideoligions (.rid files). RimWorld normally only offers ideoligions
    // it generates from memes, or ones loaded from the player's local save folder, so a complete .rid
    // (name, culture, memes, precepts, roles, apparel, style, relics, narrative all set) cannot travel with
    // a mod. This framework surfaces every .rid a mod bundles (in a folder named "Ideos" at the mod root)
    // as a card in a "Modded" group on the "Choose your ideoligion" screen; picking one loads the full
    // configured ideoligion directly from the mod file (see CustomPresetBuilder and
    // Patch_Page_ChooseIdeoPreset_DoPreset). Nothing is written to disk - the chosen ideoligion is baked
    // into the colony save like any other.
    [StaticConstructorOnStartup]
    public static class IdeoligionFrameworkMod
    {
        static IdeoligionFrameworkMod()
        {
            try
            {
                CustomPresetBuilder.BuildFromBundledIdeoligions();
            }
            catch (Exception ex)
            {
                Log.Warning("[Ideoligion Framework] Failed to build ideoligion preset cards: " + ex.Message);
            }

            try
            {
                new Harmony("wishRobber.ideoligionframework").PatchAll();
            }
            catch (Exception ex)
            {
                Log.Warning("[Ideoligion Framework] Failed to apply patches: " + ex.Message);
            }
        }
    }
}
