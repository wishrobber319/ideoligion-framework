# Ideology Framework

Ship a **complete, ready-made ideoligion inside your mod** for RimWorld 1.6. Players pick it straight from the "Choose your ideoligion" screen, with every meme, precept, role, apparel rule, style, relic and narrative exactly as you authored it.

**Requires** the [Ideology DLC](https://store.steampowered.com/app/1392840/RimWorld__Ideology/) and [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077).

## Why

RimWorld only ever offers ideologies it **generates from memes**, or ones you **save and load by hand** from your local `Ideos` folder. So a fully-authored ideoligion cannot travel with a mod: there is no vanilla way to hand someone a finished belief system. This framework adds one.

## What it does

Every ideoligion a mod bundles shows up as a card in a new **"Modded"** group at the top of the ideology preset list, right where players choose their starting beliefs. Selecting a card loads the complete ideoligion from the mod's own file, not a rough version regenerated from memes. Nothing is copied to the player's save folder; the chosen ideoligion is baked into the colony save like any other.

## For mod authors

Bundling an ideoligion takes four steps and no code:

1. **Build it in-game.** Start a colony, open the ideology editor (or the dev tools), and design your ideoligion, name, memes, precepts, roles, apparel, styles, relics, narrative.
2. **Save it.** Use the editor's *Save* button. RimWorld writes a `.rid` file into your local `Ideos` folder (`%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Ideos` on Windows).
3. **Drop the `.rid` into your mod.** Create a folder named `Ideos` at the root of your mod (next to `About/`, `Defs/`, etc.) and put the file in it:

   ```
   YourMod/
     About/About.xml
     Ideos/Your Ideology.rid
   ```

4. **Depend on this framework** in your `About/About.xml`:

   ```xml
   <modDependencies>
     <li>
       <packageId>wishRobber.ideoligionframework</packageId>
       <displayName>Ideology Framework</displayName>
       <steamWorkshopUrl>steam://url/CommunityFilePage/3769638747</steamWorkshopUrl>
     </li>
   </modDependencies>
   <loadAfter>
     <li>wishRobber.ideoligionframework</li>
   </loadAfter>
   ```

That is all. The card is built from the file itself, its name, description and memes are read straight out of the `.rid`, so there is no preset def to write and no naming convention to keep in sync.

### Tips

- **Give it a distinctive file name.** The file name is only used internally, but a unique one avoids clashing with another mod's bundled ideoligion.
- **Custom memes and precepts re-link by defName.** A `.rid` references its memes and precepts by name, so if your ideology uses custom memes your mod defines, they resolve automatically as long as your mod provides those defs.
- **Trim the file (optional).** A freshly-saved `.rid` opens with a large `<meta>` block that lists your entire active modlist. It is not needed to load the ideoligion, you can delete the whole `<savedideo>` ... `<meta>` ... `</meta>` block, leaving just `<savedideo><ideo> ... </ideo></savedideo>`. The file gets much smaller and stops carrying your exact setup.

## Example

**[Ideology: Mercenary Creed](https://github.com/wishrobber319/ideology-mercenary-creed)** is a complete, working mod built on this framework. It is the smallest useful reference:

- [`Ideos/Mercenary Creed.rid`](https://github.com/wishrobber319/ideology-mercenary-creed/blob/main/Ideos/Mercenary%20Creed.rid): the bundled ideoligion (with the `<meta>` block stripped, as above).
- [`About/About.xml`](https://github.com/wishrobber319/ideology-mercenary-creed/blob/main/About/About.xml): the framework dependency, exactly as shown above.

Everything else in that repo (a custom meme, a thought, a bit of C# for its mood mechanic) is Mercenary Creed's own content, not framework boilerplate. The framework only needs the `Ideos/` folder and the dependency.

## How it works

At startup the framework scans every running mod for `Ideos/*.rid` files and reads each one's `name`, `description` and `memes` to build an `IdeoPresetDef` card at runtime, filed under a "Modded" category it sorts to the top of the preset list. When the player selects a card and clicks **Next**, a Harmony patch on `Page_ChooseIdeoPreset` loads the full ideoligion from the mod's file (via `GameDataSaveLoader.TryLoadIdeo`) and assigns it, instead of generating one from memes. Vanilla presets are untouched, and colonies with no bundled ideoligion behave exactly as before.

---

A RimWorld 1.6 mod by wishRobber.
