using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TerrariansConstructLib.API.Edits {
	internal static class DirectDetourManager {
		private static readonly List<Hook> detours = new();
		private static readonly List<(MethodInfo, Delegate)> delegates = new();

		private static readonly Dictionary<string, MethodInfo> cachedMethods = new();

		public static void ModCtorLoad() {
			try {
				ILHelper.LogILEdits = true;
				
				MonoModHooks.RequestNativeAccess();

				// Usage: freeing the custom writer so that the log file can be viewed
				DetourHook(typeof(ModContent).GetCachedMethod("Load"), typeof(Detours.TML).GetCachedMethod(nameof(Detours.TML.Hook_ModContent_Load)));

				// Usage: autoloading mold and item definition items after mod loading, but before array resizing
				IntermediateLanguageHook(typeof(ModContent).GetCachedMethod("Load"), typeof(MSIL.TML).GetCachedMethod(nameof(MSIL.TML.Patch_ModContent_Load)));

				ILHelper.LogILEdits = false;
			} catch (Exception ex) {
				throw new Exception("An error occurred while doing patching in TerrariansConstructLib." +
				                    "\nReport this error to the mod devs and disable the mod in the meantime." +
				                    "\n\n" +
				                    ex);
			}
		}

		public static void Load() {
			try {
				ILHelper.LogILEdits = true;
				
				CoreLibMod.SetLoadingSubProgressText(Language.GetTextValue("Mods.TerrariansConstructLib.Loading.DirectDetourManager.Detours"));

				// Usage: displaying in the loading UI when an item's defaults are being processed
				DetourHook(typeof(ModItem).GetCachedMethod("SetupContent"), typeof(Detours.TML).GetCachedMethod(nameof(Detours.TML.Hook_ModItem_SetupContent)));
				DetourHook(typeof(Mod).GetCachedMethod("SetupContent"), typeof(Detours.TML).GetCachedMethod(nameof(Detours.TML.Hook_Mod_SetupContent)));

				CoreLibMod.SetLoadingSubProgressText(Language.GetTextValue("Mods.TerrariansConstructLib.Loading.DirectDetourManager.ILEdits"));

				// Usage: proper drawing of constructed items in tModLoader's config UI
				IntermediateLanguageHook(typeof(Mod).Assembly.GetType("Terraria.ModLoader.Config.UI.ItemDefinitionOptionElement")!.GetCachedMethod("DrawSelf"),
					typeof(MSIL.TML).GetCachedMethod(nameof(MSIL.TML.Patch_ItemDefinitionOptionElement_DrawSelf)));

				ILHelper.LogILEdits = false;
			} catch (Exception ex) {
				throw new Exception("An error occurred while doing patching in TerrariansConstructLib." +
				                    "\nReport this error to the mod devs and disable the mod in the meantime." +
				                    "\n\n" +
				                    ex);
			}
		}

		private static MethodInfo GetCachedMethod(this Type type, string method) {
			string key = $"{type.FullName}::{method}";
			if (cachedMethods.TryGetValue(key, out MethodInfo? value))
				return value;

			return cachedMethods[key] = type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)!;
		}

		public static void Unload() {
			try {
				foreach (Hook hook in detours)
					if (hook.IsValid && hook.IsApplied)
						hook.Undo();

				foreach ((MethodInfo method, Delegate hook) in delegates)
					HookEndpointManager.Unmodify(method, hook);
			} catch (Exception ex) {
				//If an exception was thrown (e.g. due to hooks not being added fully), just ignore it and put the error in the log
				CoreLibMod.Instance.Logger.Error("DirectDetourManager was not able to unload properly", ex);
			}
		}

		private static void IntermediateLanguageHook(MethodInfo orig, MethodInfo modify) {
			Delegate hook = Delegate.CreateDelegate(typeof(ILContext.Manipulator), modify);
			delegates.Add((orig, hook));
			HookEndpointManager.Modify(orig, hook);
		}

		private static void DetourHook(MethodInfo orig, MethodInfo modify) {
			Hook hook = new(orig, modify);
			detours.Add(hook);
			hook.Apply();
		}
	}
}
