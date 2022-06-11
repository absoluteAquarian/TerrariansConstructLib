using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API.Edits.MSIL {
	partial class Vanilla {
		internal static void Patch_PlayerDrawLayers_DrawPlayer_27_HeldItem(ILContext il) {
			ILCursor c = new(il);

			int patchNum = 1;

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: true);

			/*   IL_0086: ldsfld    class [ReLogic]ReLogic.Content.Asset`1<class [FNA]Microsoft.Xna.Framework.Graphics.Texture2D>[] Terraria.GameContent.TextureAssets::Item
			 *   IL_008B: ldloc.1
			 *   IL_008C: ldelem.ref
			 *   IL_008D: callvirt  instance !0 class [ReLogic]ReLogic.Content.Asset`1<class [FNA]Microsoft.Xna.Framework.Graphics.Texture2D>::get_Value()
			 *   IL_0092: stloc.3
			 *      <== NEED TO END UP HERE
			 */
			if(!c.TryGotoNext(MoveType.After, i => i.MatchStloc(3)))
				goto bad_il;

			patchNum++;

			c.Emit(OpCodes.Ldloc_0);
			c.Emit(OpCodes.Ldloc_3);

			c.EmitDelegate<Func<Item, Texture2D, Texture2D>>((heldItem, value) => {
				if (heldItem.ModItem is BaseTCItem tc)
					return CoreLibMod.ItemTextures.Get(tc.ItemDefinition, tc.parts, tc.modifiers);

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
