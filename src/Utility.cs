using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Reflection;

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
			int searchIndex = tooltips.FindIndex(t => t.text.Contains(searchPhrase));
			if(searchIndex >= 0)
				tooltips[searchIndex].text = tooltips[searchIndex].text.Replace(searchPhrase, replacePhrase);
		}

		public static void FindAndRemoveLine(List<TooltipLine> tooltips, string fullLine){
			int searchIndex = tooltips.FindIndex(t => t.text == fullLine);
			if(searchIndex >= 0)
				tooltips.RemoveAt(searchIndex);
		}

		public static void FindAndInsertLines(Mod mod, List<TooltipLine> tooltips, string searchLine, Func<int, string> lineNames, string replaceLines){
			int searchIndex = tooltips.FindIndex(t => t.text == searchLine);
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
			StatModifier def = defaultValueIfEmpty ?? StatModifier.One;

			if (!collection.Any())
				return def;

			StatModifier modifier = StatModifier.One;

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
	}
}
