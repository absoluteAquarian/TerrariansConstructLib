using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Reflection;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Projectiles;

namespace TerrariansConstructLib {
	public static class Utility {
		/// <summary>
		/// Spawns a shallow copy of an item in the world
		/// </summary>
		/// <param name="source">The entity source</param>
		/// <param name="item">The item instance</param>
		/// <param name="rectangle">The area where the item will spawn</param>
		public static void DropItem(IEntitySource source, Item item, Rectangle rectangle) {
			ReflectionHelperVoid<Item, IEntitySource, Item, Rectangle>.InvokeMethod("DropItem", null, source, item, rectangle);
		}

		public static void FindAndModify(List<TooltipLine> tooltips, string searchPhrase, string replacePhrase){
			int searchIndex = tooltips.FindIndex(t => t.Text.Contains(searchPhrase));
			if(searchIndex >= 0)
				tooltips[searchIndex].Text = tooltips[searchIndex].Text.Replace(searchPhrase, replacePhrase);
		}

		public static void FindAndRemoveLine(List<TooltipLine> tooltips, string fullLine){
			int searchIndex = tooltips.FindIndex(t => t.Text == fullLine);
			if(searchIndex >= 0)
				tooltips.RemoveAt(searchIndex);
		}

		public static void FindAndInsertLines(Mod mod, List<TooltipLine> tooltips, string searchLine, Func<int, string> lineNames, string replaceLines){
			int searchIndex = tooltips.FindIndex(t => t.Text == searchLine);
			if(searchIndex >= 0){
				tooltips.RemoveAt(searchIndex);

				int inserted = 0;
				foreach(var line in replaceLines.Split('\n')){
					tooltips.Insert(searchIndex++, new TooltipLine(mod, lineNames(inserted), line));
					inserted++;
				}
			}
		}

		public static Color GetRarityColor(Item hoverItem) {
			int rare = hoverItem.rare;
			float num4 = Main.mouseTextColor / 255f;
			int a = Main.mouseTextColor;
			//Main.instance._mouseTextCache.diff
			byte diff = (byte)typeof(Main).GetNestedType("MouseTextCache", BindingFlags.NonPublic | BindingFlags.Instance)!
				.GetField("diff", BindingFlags.Public | BindingFlags.Instance)!
				.GetValue(ReflectionHelper<Main>.InvokeGetterFunction("_mouseTextCache", Main.instance))!;

			Color black = new(num4, num4, num4, num4);

			if (rare == -11)
				black = new Color((byte)(255f * num4), (byte)(175f * num4), (byte)(0f * num4), a);

			if (rare == -1)
				black = new Color((byte)(130f * num4), (byte)(130f * num4), (byte)(130f * num4), a);

			if (rare == 1)
				black = new Color((byte)(150f * num4), (byte)(150f * num4), (byte)(255f * num4), a);

			if (rare == 2)
				black = new Color((byte)(150f * num4), (byte)(255f * num4), (byte)(150f * num4), a);

			if (rare == 3)
				black = new Color((byte)(255f * num4), (byte)(200f * num4), (byte)(150f * num4), a);

			if (rare == 4)
				black = new Color((byte)(255f * num4), (byte)(150f * num4), (byte)(150f * num4), a);

			if (rare == 5)
				black = new Color((byte)(255f * num4), (byte)(150f * num4), (byte)(255f * num4), a);

			if (rare == 6)
				black = new Color((byte)(210f * num4), (byte)(160f * num4), (byte)(255f * num4), a);

			if (rare == 7)
				black = new Color((byte)(150f * num4), (byte)(255f * num4), (byte)(10f * num4), a);

			if (rare == 8)
				black = new Color((byte)(255f * num4), (byte)(255f * num4), (byte)(10f * num4), a);

			if (rare == 9)
				black = new Color((byte)(5f * num4), (byte)(200f * num4), (byte)(255f * num4), a);

			if (rare == 10)
				black = new Color((byte)(255f * num4), (byte)(40f * num4), (byte)(100f * num4), a);

			if (rare == 11)
				black = new Color((byte)(180f * num4), (byte)(40f * num4), (byte)(255f * num4), a);

			if (rare > 11)
				black = (typeof(RarityLoader).GetCachedMethod("GetRarity")!.Invoke(null, new object[]{ rare }) as ModRarity)!.RarityColor * num4;

			if (diff == 1)
				black = new Color((byte)(Main.mcColor.R * num4), (byte)(Main.mcColor.G * num4), (byte)(Main.mcColor.B * num4), a);

			if (diff == 2)
				black = new Color((byte)(Main.hcColor.R * num4), (byte)(Main.hcColor.G * num4), (byte)(Main.hcColor.B * num4), a);

			if (hoverItem.expert || rare == -12)
				black = new Color((byte)(Main.DiscoR * num4), (byte)(Main.DiscoG * num4), (byte)(Main.DiscoB * num4), a);

			if (hoverItem.master || rare == -13)
				black = new Color((byte)(255f * num4), (byte)(Main.masterColor * 200f * num4), 0, a);

			return black;
		}

		public static double Average<T>(this IEnumerable<T> collection, Func<T, double> func, double defaultValueIfEmpty = 0)
			=> !collection.Any() ? defaultValueIfEmpty : Enumerable.Average(collection, func);

		public static StatModifier Sum<T>(this IEnumerable<T> collection, Func<T, StatModifier> func, StatModifier? defaultValueIfEmpty = null) {
			StatModifier def = defaultValueIfEmpty ?? StatModifier.Default;

			if (!collection.Any())
				return def;

			StatModifier modifier = StatModifier.Default;

			foreach (var stat in collection.Select(func))
				modifier = modifier.CombineWith(stat);

			return modifier;
		}

		internal static MethodInfo LocalizationLoader_AutoloadTranslations, LocalizationLoader_SetLocalizedText;
		internal static FieldInfo LanguageManager__localizedTexts;

		/// <summary>
		/// Force's the localization for the given mod, <paramref name="mod"/>, to be loaded for use with <seealso cref="Language"/>
		/// </summary>
		/// <param name="mod">The mod instance</param>
		public static void ForceLoadModHJsonLocalization(Mod mod) {
			Dictionary<string, ModTranslation> modTranslationDictionary = new();

			LocalizationLoader_AutoloadTranslations.Invoke(null, new object[] { mod, modTranslationDictionary });

			Dictionary<string, LocalizedText> dict = (LanguageManager__localizedTexts.GetValue(LanguageManager.Instance) as Dictionary<string, LocalizedText>)!;

			var culture = Language.ActiveCulture;
			foreach (ModTranslation translation in modTranslationDictionary.Values) {
				//LocalizedText text = new LocalizedText(translation.Key, translation.GetTranslation(culture));
				LocalizedText text = (Activator.CreateInstance(typeof(LocalizedText), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, null, new object[] { translation.Key, translation.GetTranslation(culture) }, CultureInfo.InvariantCulture) as LocalizedText)!;

				LocalizationLoader_SetLocalizedText.Invoke(null, new object[] { dict, text });
			}
		}

		public static T[] Create1DArray<T>(T value, uint length) where T : struct {
			T[] arr = new T[length];
			for(uint i = 0; i < length; i++)
				arr[i] = value;
			return arr;
		}

		public static Point16 TileCoord(this Tile tile)
			=> new(tile.TileFrameX / 18, tile.TileFrameY / 18);

		public static bool TryGetTileEntity<T>(Point16 position, out T? tileEntity) where T : TileEntity {
			tileEntity = null;

			if(TileEntity.ByPosition.TryGetValue(position, out var entity))
				tileEntity = entity as T;  //'as' will make 'tileEntity' null if the TileEntity at the position isn't the same type

			return tileEntity != null;
		}

		public static Type? FindType(string fullName) {
			foreach (var alc in AssemblyLoadContext.All) {
				foreach (var asm in alc.Assemblies) {
					Type? type = asm.GetType(fullName);

					if (type is not null)
						return type;
				}
			}

			//Couldn't be found
			return null;
		}

		private static readonly Regex rtMethodRegex_DMD = new(@"DMD<([^:]*)::([^>]*)>.*", RegexOptions.Compiled);
		private static readonly Regex rtMethodRegex_Trampoline = new(@"Trampoline<([^:]*)::([^>]*)>.*", RegexOptions.Compiled);
		private static readonly Regex rtMethodRegex_Native = new(@"Trampoline:Native<([^:]*)::([^>]*)>.*", RegexOptions.Compiled);
		private static readonly Regex rtMethodRegex_Chain = new(@"Chain<([^:]*)::([^>]*)>.*", RegexOptions.Compiled);

		public static bool MethodExistsInStackTrace(StackTrace trace, MethodBase target) {
			Type? type = typeof(System.Reflection.Emit.DynamicMethod).GetNestedType("RTDynamicMethod", BindingFlags.NonPublic | BindingFlags.Instance);

			if (type is null)
				throw new Exception("Could not find type: System.Reflection.Emit.DynamicMethod.RTDynamicMethod");

			/*
			Main.NewText("[c/cccccc:Attempting to find method in stacktrace:]");
			Main.NewText($"[c/cccccc:{target.DeclaringType!.FullName}::{target.Name}]");
			*/

			int numFrame = -1;
			foreach (var frame in trace.GetFrames()) {
				numFrame++;
				MethodBase? m = frame.GetMethod();

				if (m is null) {
				//	Main.NewText($"Frame {numFrame} did not refer to a valid method.");
					continue;
				}
				
				string name = m.Name;

				string? declType;
				/*
				declType = m.DeclaringType is not null ? m.DeclaringType.Name + "::" : "";

				Main.NewText($"Evaluating Frame {numFrame}: {declType}{m.Name}");
				*/

				if (m is System.Reflection.Emit.DynamicMethod || type.IsAssignableFrom(m.GetType())) {
					var regex = rtMethodRegex_DMD.IsMatch(name)
						? rtMethodRegex_DMD
						: rtMethodRegex_Trampoline.IsMatch(name)
							? rtMethodRegex_Trampoline
							: rtMethodRegex_Native.IsMatch(name)
								? rtMethodRegex_Native
								: rtMethodRegex_Chain.IsMatch(name)
									? rtMethodRegex_Chain
									: null;

					if (regex is not null) {
						//Extract the type and method name, then compare
						var matches = regex.Matches(name);

						if (matches.Count != 1) {
						//	Main.NewText($"Regex match count ({matches.Count}) was not the expected value of 1");
							continue;
						}

						var groups = matches[0].Groups;

						if (groups.Count != 3) {
							/*
							Main.NewText($"Regex group count ({groups.Count}) was not the expected value of 2");
							Main.NewText("Groups:");
							int groupNum = 0;
							foreach (var group in groups.Values) {
								Main.NewText($"  {groupNum}: \"{group.Value}\"");
								groupNum++;
							}
							*/
							continue;
						}

						declType = groups[1].Value;
						string declMethod = groups[2].Value;

						Type? methodType = FindType(declType);
						MethodBase? decl = methodType?.GetCachedMethod(declMethod);

						if (decl?.MethodHandle == target.MethodHandle) {
						//	Main.NewText($"Frame {numFrame} [c/00ff00:was] a dynamic method and was a valid match");
							return true;
						} /* else if(methodType is null)
							Main.NewText($"Could not find definition for type: {declType}");
						else if (decl is null)
							Main.NewText($"Could not find definition for method: {declType}::{declMethod}");
						*/
					} /* else {
						Main.NewText($"Could not match Frame {numFrame} to a regex:");
						Main.NewText($"  {name}");
					}
					*/
				} else if (m.MethodHandle == target.MethodHandle) {
				//	Main.NewText($"Frame {numFrame} [c/ff0000:was not] a dynamic method and was a valid match");
					return true;
				}
			}

		//	Main.NewText("Could not find a match in the stacktrace");

			return false;
		}

		public static BaseTCItem? AsTCItem(this Item item)
			=> item.ModItem as BaseTCItem;

		public static BaseTCProjectile? AsTCProjectile(this Projectile projectile)
			=> projectile.ModProjectile as BaseTCProjectile;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double InverseLerp(double min, double max, double value)
			=> (value - min) / (max - min);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double InverseLerp(float min, float max, float value)
			=> (value - min) / (max - min);
	}
}
