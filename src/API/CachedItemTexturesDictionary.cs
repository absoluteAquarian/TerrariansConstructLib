using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Exceptions;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Modifiers;

namespace TerrariansConstructLib.API {
	/// <summary>
	/// A collection of textures indexed by registered item type and item parts
	/// </summary>
	public class CachedItemTexturesDictionary {
		internal static bool SaveGeneratedTexturesToFiles = true;

		private readonly Dictionary<int, PartsDictionary<object>> dictionary = new();

		public Texture2D Get(int registeredItemID, ItemPartSlotCollection partsCollection, ModifierCollection modifiers) {
			//Get the parts
			ItemPart[] parts = partsCollection.ToArray();

			return Get(registeredItemID, parts, SelectModifiers(modifiers.ToArray()).ToArray());
		}

		public Texture2D Get(BaseTCItem tc)
			=> Get(tc.ItemDefinition, tc.parts, tc.modifiers);

		public Texture2D Get(int registeredItemID, ItemPart[] parts, BaseTrait[] modifiers) {
			var dict = TraverseTree(registeredItemID, parts, modifiers, out string identifier);

			if (!dict.TryGetValue(identifier, out object? texture))
				dict[identifier] = texture = BuildTextureThreadContext(registeredItemID, parts, modifiers);

			return (texture as Texture2D)!;
		}

		private Dictionary<string, object> TraverseTree(int registeredItemID, ItemPart[] parts, BaseTrait[] modifiers, out string identifier) {
			//Step down through the tree
			if (!dictionary.TryGetValue(registeredItemID, out var partsDictionary))
				partsDictionary = dictionary[registeredItemID] = new();

			Material material;
			int partID;
			for(int i = 0; i < parts.Length - 1; i++) {
				material = parts[i].material;
				partID = parts[i].partID;

				if(!partsDictionary!.TryGet(material, partID, out object? partsSubDictionary))
					partsDictionary.Set(material, partID, partsSubDictionary = new PartsDictionary<object>());

				partsDictionary = partsSubDictionary as PartsDictionary<object>;
			}

			material = parts[^1].material;
			partID = parts[^1].partID;

			if (!partsDictionary!.TryGet(material, partID, out object? dict))
				partsDictionary.Set(material, partID, dict = new Dictionary<string, object>());

			Dictionary<string, object> textureDict = (dict as Dictionary<string, object>)!;

			//This identifier represents an item with no modifiers attached to it
			StringBuilder sb = new("<>_Texture");

			//If "modifiers" has no BaseModifier entries, then the final step has been reached; no need to do any more iteration
			if (Array.Exists(modifiers, t => t is BaseModifier m && m.VisualTexture is not null)) {
				BaseModifier[] casted = SelectModifiers(modifiers).ToArray();

				for (int i = 0; i < casted.Length - 1; i++) {
					var modifier = casted[i];
					string id = modifier.GetType().FullName!;

					if (!textureDict.TryGetValue(id, out var subDict))
						subDict = textureDict[id] = new Dictionary<string, object>();

					textureDict = (subDict as Dictionary<string, object>)!;

					sb.Append("+" + modifier.GetType().FullName!);
				}
			}

			identifier = sb.ToString();

			return textureDict;
		}

		internal void Clear()
			=> ClearIterative(dictionary);

		private static void ClearIterative(IDictionary dictionary) {
			if (dictionary.Count == 0)
				return;
			
			Stack<object> stack = new();
			
			//Get the starting values
			foreach (var v in dictionary)
				stack.Push(v);

			while (stack.Count > 0) {
				object v = stack.Pop();

				if (v is IDictionary dict) {
					foreach (var obj in dict)
						stack.Push(obj);

					dict.Clear();
				} else {
					//Try to dispose the object, since it's likely to be a Texture2D
					(v as IDisposable)?.Dispose();
				}
			}
		}

		private static Dictionary<int, int> GenerateHashmap(ItemPart[] parts)
			=> parts.DistinctBy(p => p.partID).ToDictionary(part => part.partID, part => (from p in parts where p.partID == part.partID select p).Count());

		private static object BuildTextureThreadContext(int registeredItemID, ItemPart[] parts, BaseTrait[] modifiers) {
			object? texture = null;

			if (!AssetRepository.IsMainThread) {
				ManualResetEvent evt = new(false);
				
				Main.QueueMainThreadAction(() => {
					texture = BuildTexture(registeredItemID, parts, modifiers);
					evt.Set();
				});

				evt.WaitOne();
			} else
				texture = BuildTexture(registeredItemID, parts, modifiers);

			return texture!;
		}

		private static IEnumerable<BaseModifier> SelectModifiers(BaseTrait[] modifiers, bool? above = null)
			=> modifiers.Where(t => t is BaseModifier m && m.VisualTexture is not null && (above is null || m.VisualIsDisplayedAboveItem == above))
				.Select(t => (t as BaseModifier)!)
				.Distinct();

		private static Texture2D BuildTexture(int registeredItemID, ItemPart[] parts, BaseTrait[] modifiers) {
			string visualsFolder = ItemDefinitionLoader.Get(registeredItemID)!.RelativeVisualsFolder;

			var hashmap = GenerateHashmap(parts);

			Texture2D partTexture = GetVisualTexture(visualsFolder, parts[^1], hashmap);
			Texture2D texture = new(Main.graphics.GraphicsDevice, partTexture.Width, partTexture.Height);

			void ApplyModifierTextures(bool above) {
				//Apply modifier textures
				foreach (var modifier in SelectModifiers(modifiers, above)) {
					Texture2D modifierTexture = GetVisualTexture(visualsFolder, modifier);

					ApplyPixels(registeredItemID, texture, modifierTexture);
				}
			}

			if (modifiers.Length > 0)
				ApplyModifierTextures(above: false);

			ApplyPixels(registeredItemID, texture, partTexture);

			//Parts at the front of the array should be on top of parts at the bottom
			for (int i = parts.Length - 2; i >= 0; i--) {
				partTexture = GetVisualTexture(visualsFolder, parts[i], hashmap);

				ApplyPixels(registeredItemID, texture, partTexture);
			}

			if (modifiers.Length > 0)
				ApplyModifierTextures(above: true);

			if (SaveGeneratedTexturesToFiles) {
				string textureImage = ItemDefinitionLoader.Get(registeredItemID)!.Name + "_"
					+ string.Join("_", parts.Select(p => "M" + (p.material is UnloadedMaterial
						? "U"
						: p.material is UnknownMaterial
							? "K"
							: p.material is ColorMaterial cm
								? "C" + Enum.GetName(cm.ColorType)![0]
								: p.material.Type.ToString())
						+ "+P" + p.partID));

				string path = Program.SavePath;
				path = Path.Combine(path, "aA Mods", ItemDefinitionLoader.Get(registeredItemID)!.Mod.Name, "Generated Textures");

				Directory.CreateDirectory(path);

				path = Path.Combine(path, textureImage + ".png");

				texture.SaveAsPng(File.Open(path, FileMode.Create), texture.Width, texture.Height);
			}

			return texture;
		}

		private static Texture2D GetVisualTexture(string visualsFolder, ItemPart part, Dictionary<int, int> hashmap) {
			hashmap[part.partID]--;
			int numCount = hashmap[part.partID];

			string path = visualsFolder + "/" + PartDefinitionLoader.Get(part.partID)!.Name + "/" + (numCount > 0 ? numCount + "_" : "") + part.material.GetName();

			return GetVisualTexture(path);
		}

		private static Texture2D GetVisualTexture(string visualsFolder, BaseModifier modifier) {
			string path = visualsFolder + "/Modifiers/" + modifier.VisualTexture;

			return GetVisualTexture(path);
		}

		private static Texture2D GetVisualTexture(string path) {
			if (CoreLibMod.Instance.RequestAssetIfExists<Texture2D>(path, out var asset))
				return asset.Value;

			//Try to find a mod which has the texture, error otherwise
			foreach (Mod mod in CoreLibMod.Dependents)
				if (mod.RequestAssetIfExists(path, out asset))
					return asset.Value;

			throw new MissingResourceException($"Could not find asset \"{path}\" in any mods which have Terrarians' Construct Library as a dependency.");
		}

		internal static bool TextureExists(string relativePath, [NotNullWhen(true)] out string? fullPath) {
			if (CoreLibMod.Instance.RequestAssetIfExists<Texture2D>(relativePath, out _)) {
				fullPath = CoreLibMod.Instance.Name + "/" + relativePath;
				return true;
			}

			//Try to find a mod which has the texture
			foreach (Mod mod in CoreLibMod.Dependents) {
				if (mod.RequestAssetIfExists<Texture2D>(relativePath, out _)) {
					fullPath = mod.Name + "/" + relativePath;
					return true;
				}
			}

			fullPath = null;
			return false;
		}

		private static void ApplyPixels(int registeredItemID, Texture2D builtTexture, Texture2D incoming) {
			//Unfortunately, GetData/SetData have to be used... I cannot control when a texture is requested and Terraria doesn't use RenderTargetUsage.PreserveContents
			// -- absoluteAquarian
			if (builtTexture.Width != incoming.Width || builtTexture.Height != incoming.Height)
				throw new Exception($"The part textures for registered item \"{ItemDefinitionLoader.Get(registeredItemID)!.FullName}\" did not have the same dimensions");

			Color[] incomingColor = new Color[incoming.Width * incoming.Height];
			Color[] builtColor = new Color[builtTexture.Width * builtTexture.Height];
			
			incoming.GetData(incomingColor);
			builtTexture.GetData(builtColor);

			for (int r = 0; r < incoming.Height; r++) {
				for (int c = 0; c < incoming.Width; c++) {
					int index = c + r * incoming.Height;

					ref Color i = ref incomingColor[index];
					ref Color b = ref builtColor[index];

					//Alpha blending:  pixel = src_pixel + (1 - src_pixel.a) * dest_pixel
					Vector4 src = i.ToVector4();
					b = new Color(src + (1 - src.W) * b.ToVector4());
				}
			}

			builtTexture.SetData(builtColor);
		}
	}
}
