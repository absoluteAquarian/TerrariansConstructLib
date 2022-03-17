﻿using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;

namespace TerrariansConstructLib.API.Edits {
	internal static class DirectDetourManager {
		private static readonly List<Hook> detours = new();
		private static readonly List<(MethodInfo, Delegate)> delegates = new();

		private static readonly Dictionary<string, MethodInfo> cachedMethods = new();

		public static void ModCtorLoad() {
			try {
				MonoModHooks.RequestNativeAccess();

				//Usage: makes Item Part Mold tiers and Item Part Molds load after all content has loaded
				IntermediateLanguageHook(typeof(ModContent).GetCachedMethod("Load"), typeof(MSIL.TML).GetCachedMethod(nameof(MSIL.TML.Path_ModContent_Load)));
			} catch (Exception ex) {
				throw new Exception("An error occurred while doing patching in TerrariansConstructLib." +
				                    "\nReport this error to the mod devs and disable the mod in the meantime." +
				                    "\n\n" +
				                    ex);
			}
		}

		public static void Load() {
			try {
				// TODO: make direct detours
			} catch (Exception ex) {
				throw new Exception("An error occurred while doing patching in TerrariansConstructLib." +
				                    "\nReport this error to the mod devs and disable the mod in the meantime." +
				                    "\n\n" +
				                    ex);
			}
		}

		private static MethodInfo GetCachedMethod(this Type type, string method) {
			string key = $"{type.FullName}::{method}";
			if (cachedMethods.TryGetValue(key, out MethodInfo value))
				return value;

			return cachedMethods[key] = type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
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
