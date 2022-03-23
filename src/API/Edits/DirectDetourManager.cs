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
				MonoModHooks.RequestNativeAccess();

				// TODO: make detours that need to happen before Mod.Load()
			} catch (Exception ex) {
				throw new Exception("An error occurred while doing patching in TerrariansConstructLib." +
				                    "\nReport this error to the mod devs and disable the mod in the meantime." +
				                    "\n\n" +
				                    ex);
			}
		}

		public static void Load() {
			try {
				CoreLibMod.SetLoadingSubProgressText(Language.GetTextValue("Mods.TerrariansConstructLib.Loading.DirectDetourManager.Detours"));

				CoreLibMod.SetLoadingSubProgressText(Language.GetTextValue("Mods.TerrariansConstructLib.Loading.DirectDetourManager.ILEdits"));

				// Usage: proper drawing of constructed items in tModLoader's config UI
				IntermediateLanguageHook(typeof(Mod).Assembly.GetType("Terraria.ModLoader.Config.UI.ItemDefinitionOptionElement")!.GetCachedMethod("DrawSelf"),
					typeof(MSIL.TML).GetCachedMethod(nameof(MSIL.TML.Patch_ItemDefinitionOptionElement_DrawSelf)));
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
			foreach (Hook hook in detours)
				if (hook.IsValid && hook.IsApplied)
					hook.Undo();

			foreach ((MethodInfo method, Delegate hook) in delegates)
				HookEndpointManager.Unmodify(method, hook);
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
