﻿using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API.Edits.MSIL {
	internal static partial class TML {
		internal static void Patch_ItemDefinitionOptionElement_DrawSelf(ILContext il) {
			FieldInfo ItemDefinitionOptionElement_item = typeof(Mod).Assembly.GetType("Terraria.ModLoader.Config.UI.ItemDefinitionOptionElement")!.GetField("item", BindingFlags.Public | BindingFlags.Instance)!;

			ILHelper.EnsureAreNotNull((ItemDefinitionOptionElement_item, "Terraria.ModLoader.Config.UI.ItemDefinitionOptionElement::item"));

			ILCursor c = new(il);

			int patchNum = 1;

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: true);

			/*   IL_0096: ldsfld    class [ReLogic]ReLogic.Content.Asset`1<class [FNA]Microsoft.Xna.Framework.Graphics.Texture2D>[] Terraria.GameContent.TextureAssets::Item
			 *   IL_009B: ldloc.2
			 *   IL_009C: ldelem.ref
			 *   IL_009D: callvirt  instance !0 class [ReLogic]ReLogic.Content.Asset`1<class [FNA]Microsoft.Xna.Framework.Graphics.Texture2D>::get_Value()
			 *   IL_00A2: stloc.3
			 *      <== NEED TO END UP HERE
			 */
			if(!c.TryGotoNext(MoveType.After, i => i.MatchStloc(3)))
				goto bad_il;

			patchNum++;

			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldfld, ItemDefinitionOptionElement_item);
			c.Emit(OpCodes.Ldloc_3);

			c.EmitDelegate<Func<Item, Texture2D, Texture2D>>((item, value) => {
				if (item.ModItem is BaseTCItem tc)
					return CoreLibMod.itemTextures.Get(tc.registeredItemID, tc.parts);

				return value;
			});

			c.Emit(OpCodes.Stloc_3);

			ILHelper.UpdateInstructionOffsets(c);

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: false);

			return;
			bad_il:
			throw new Exception("Unable to fully patch " + il.Method.Name + "()\n" +
				"Reason: Could not find instruction sequence for patch #" + patchNum);
		}
	}
}