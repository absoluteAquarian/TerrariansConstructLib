using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Exceptions;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.API {
	/// <summary>
	/// A collection of textures indexed by registered item type and item parts
	/// </summary>
	internal class CachedItemTexturesDictionary {
		internal static bool SaveGeneratedTexturesToFiles = true;

		private readonly Dictionary<int, PartsDictionary<object>> dictionary = new();

		public Texture2D Get(int registeredItemID, ItemPartSlotCollection partsCollection) {
			//Get the parts
			ItemPart[] parts = partsCollection.ToArray();
			
			//Step down through the tree
			if (!dictionary.TryGetValue(registeredItemID, out var partsDictionary))
				partsDictionary = dictionary[registeredItemID] = new();

			Material material;
			int partID;
			for(int i = 0; i < parts.Length - 1; i++) {
				material = parts[i].material;
				partID = parts[i].partID;

				if(!partsDictionary.TryGet(material, partID, out object partsSubDictionary))
					partsDictionary.Set(material, partID, partsSubDictionary = new PartsDictionary<object>());

				partsDictionary = partsSubDictionary as PartsDictionary<object>;
			}

			//"partsDictionary" is now set to the final step in the tree.  Initialize it if necessary
			material = parts[^1].material;
			partID = parts[^1].partID;
			if (!partsDictionary.TryGet(material, partID, out object texture)) {
				if (!AssetRepository.IsMainThread) {
					ManualResetEvent evt = new(false);
				
					Main.QueueMainThreadAction(() => {
						texture = BuildTexture(registeredItemID, parts);
						evt.Set();
					});

					evt.WaitOne();
				} else
					texture = BuildTexture(registeredItemID, parts);

				partsDictionary.Set(material, partID, texture);
			}

			return texture as Texture2D;
		}

		public void Clear()
			=> Clear(dictionary);

		private static void Clear<T>(Dictionary<int, T> dictionary) {
			int[] ids = new int[dictionary.Keys.Count];
			dictionary.Keys.CopyTo(ids, 0);

			for (int i = 0; i < ids.Length; i++){
				int id = ids[i];

				//Step down through the tree
				if (dictionary.TryGetValue(id, out var partsDictionary) && partsDictionary is PartsDictionary<object> dict)
					Clear(dict);
				else {
					//This dictionary is the one with the textures
					foreach (var v in dictionary.Values)
						(v as IDisposable)?.Dispose();

					dictionary.Clear();
					return;
				}
			}
		}

		private static Texture2D BuildTexture(int registeredItemID, ItemPart[] parts) {
			string visualsFolder = ItemRegistry.registeredIDs[registeredItemID].partVisualsFolder;

			Texture2D partTexture = GetVisualTexture(visualsFolder, parts[^1]);
			Texture2D texture = new(Main.graphics.GraphicsDevice, partTexture.Width, partTexture.Height);

			ApplyPixels(registeredItemID, texture, partTexture);

			//Parts at the front of the array should be on top of parts at the bottom
			for (int i = parts.Length - 2; i >= 0; i--) {
				partTexture = GetVisualTexture(visualsFolder, parts[i]);

				ApplyPixels(registeredItemID, texture, partTexture);
			}

			if (SaveGeneratedTexturesToFiles) {
				string textureImage = ItemRegistry.registeredIDs[registeredItemID].internalName + "_"
					+ string.Join("_", parts.Select(p => "M" + (p.material is UnloadedMaterial ? "U" : p.material is UnknownMaterial ? "K" : p.material.type.ToString())
						+ "+P" + p.partID));

				string path = Program.SavePath;
				path = Path.Combine(path, "aA Mods", ItemRegistry.registeredIDs[registeredItemID].mod.Name, "Generated Textures");

				Directory.CreateDirectory(path);

				path = Path.Combine(path, textureImage + ".png");

				texture.SaveAsPng(File.Open(path, FileMode.Create), texture.Width, texture.Height);
			}

			return texture;
		}

		private static Texture2D GetVisualTexture(string visualsFolder, ItemPart part) {
			string path = visualsFolder + "/" + PartRegistry.registeredIDs[part.partID].internalName + "/" + part.material.GetName();

			if (CoreLibMod.Instance.RequestAssetIfExists<Texture2D>(path, out var asset))
				return asset.Value;

			//Try to find a mod which has the texture, error otherwise
			foreach (Mod mod in CoreLibMod.Dependents)
				if (mod.RequestAssetIfExists(path, out asset))
					return asset.Value;

			throw new MissingResourceException($"Could not find asset \"{path}\" in any mods which have Terrarians' Construct Library as a dependency.");
		}

		private static void ApplyPixels(int registeredItemID, Texture2D builtTexture, Texture2D incoming) {
			//Unfortunately, GetData/SetData have to be used... I cannot control when a texture is requested and Terraria doesn't use RenderTargetUsage.PreserveContents
			// -- absoluteAquarian
			if (builtTexture.Width != incoming.Width || builtTexture.Height != incoming.Height)
				throw new Exception($"The part textures for registered item \"{ItemRegistry.registeredIDs[registeredItemID].mod.Name}:{CoreLibMod.GetItemInternalName(registeredItemID)}\" did not have the same dimensions");

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
