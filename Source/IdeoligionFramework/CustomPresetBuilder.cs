using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace IdeoligionFramework
{
    // Surfaces every mod-bundled .rid as a card in a "Custom" group at the top of the vanilla
    // "Choose your ideoligion" preset list. Each card is built directly from its .rid's own
    // <name>/<description>/<memes>, so a mod author only ships the .rid file: there is no preset
    // def to author, and no naming convention to keep in sync. Selecting a card and pressing Next
    // loads the full configured ideoligion from the file (see Patch_Page_ChooseIdeoPreset_DoPreset),
    // rather than generating a fresh one from memes the way a normal preset does.
    public static class CustomPresetBuilder
    {
        private const string CategoryDefName = "IdeoligionFramework_Custom";

        // Maps each card we create back to the .rid it was built from, so the Next handler knows
        // which file to load (and which cards are ours versus vanilla presets).
        private static readonly Dictionary<IdeoPresetDef, string> RidByPreset = new Dictionary<IdeoPresetDef, string>();

        public static bool TryGetRidPath(IdeoPresetDef preset, out string ridPath)
        {
            return RidByPreset.TryGetValue(preset, out ridPath);
        }

        public static void BuildFromBundledIdeoligions()
        {
            RidByPreset.Clear();

            // Collect every .rid a running mod bundles under a root "Ideos" folder (same contract as
            // the file-sync side of the framework).
            var ridFiles = new List<string>();
            foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
            {
                string dir = Path.Combine(mod.RootDir, "Ideos");
                if (Directory.Exists(dir))
                {
                    ridFiles.AddRange(Directory.GetFiles(dir, "*.rid"));
                }
            }
            if (ridFiles.Count == 0)
            {
                return;
            }

            // Only create the "Custom" group once we know there is at least one card to put in it.
            IdeoPresetCategoryDef category = GetOrCreateCategory();

            int built = 0;
            foreach (string path in ridFiles.OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase))
            {
                if (TryBuildPreset(path, category))
                {
                    built++;
                }
            }

            if (built > 0)
            {
                MoveCategoryToTop(category);
                Log.Message("[Ideoligion Framework] Added " + built + " bundled ideoligion(s) to the Custom preset group.");
            }
        }

        private static bool TryBuildPreset(string path, IdeoPresetCategoryDef category)
        {
            try
            {
                // .rid layout is <savedideo><meta/><ideo>...</ideo></savedideo>; read the ideo node's
                // own direct children (not the nested precept <name> tags deeper in the file).
                XElement ideo = XDocument.Load(path).Root?.Element("ideo");
                if (ideo == null)
                {
                    Log.Warning("[Ideoligion Framework] " + Path.GetFileName(path) + " has no <ideo> node; skipping.");
                    return false;
                }

                string name = ideo.Element("name")?.Value;
                if (string.IsNullOrEmpty(name))
                {
                    name = Path.GetFileNameWithoutExtension(path);
                }
                string description = ideo.Element("description")?.Value ?? string.Empty;

                // Meme icons shown on the card; resolve by defName against whatever the owning mod loads.
                var memes = new List<MemeDef>();
                XElement memesNode = ideo.Element("memes");
                if (memesNode != null)
                {
                    foreach (XElement li in memesNode.Elements("li"))
                    {
                        MemeDef meme = DefDatabase<MemeDef>.GetNamedSilentFail(li.Value);
                        if (meme != null)
                        {
                            memes.Add(meme);
                        }
                    }
                }

                var preset = new IdeoPresetDef
                {
                    defName = "IdeoligionFramework_Rid_" + Path.GetFileNameWithoutExtension(path).Replace(' ', '_'),
                    label = name,
                    description = description,
                    categoryDef = category,
                    memes = memes,
                };
                DefDatabase<IdeoPresetDef>.Add(preset);
                RidByPreset[preset] = path;
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("[Ideoligion Framework] Could not build a preset card from " + Path.GetFileName(path) + ": " + ex.Message);
                return false;
            }
        }

        private static IdeoPresetCategoryDef GetOrCreateCategory()
        {
            IdeoPresetCategoryDef existing = DefDatabase<IdeoPresetCategoryDef>.GetNamedSilentFail(CategoryDefName);
            if (existing != null)
            {
                return existing;
            }

            // label == groupLabel so the vanilla drawer treats "Modded" as the group header without
            // also drawing a redundant sub-label under it.
            var category = new IdeoPresetCategoryDef
            {
                defName = CategoryDefName,
                label = "Modded",
                groupLabel = "Modded",
                description = "Custom ideoligions from your mods.",
            };
            DefDatabase<IdeoPresetCategoryDef>.Add(category);
            return category;
        }

        // The preset screen draws category groups in DefDatabase order, so move ours to the front to
        // sit above Mild/Strong. AllDefsListForReading is the live backing list, so this reorders it
        // in place for every later enumeration.
        private static void MoveCategoryToTop(IdeoPresetCategoryDef category)
        {
            List<IdeoPresetCategoryDef> list = DefDatabase<IdeoPresetCategoryDef>.AllDefsListForReading;
            if (list.Remove(category))
            {
                list.Insert(0, category);
            }
        }
    }
}
