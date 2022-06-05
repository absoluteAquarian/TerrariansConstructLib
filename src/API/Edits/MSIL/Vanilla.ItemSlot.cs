using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariansConstructLib.API.UI;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.API.Edits.MSIL {
	internal static partial class Vanilla {
		internal static void Patch_ItemSlot_Draw(ILContext il) {
			MethodInfo Utils_Size_Texture2D = typeof(Utils).GetMethod("Size", BindingFlags.Public | BindingFlags.Static, new Type[]{ typeof(Texture2D) })!;
			MethodInfo Vector2_op_Multiply = typeof(Vector2).GetMethod("op_Multiply", BindingFlags.Public | BindingFlags.Static, new Type[]{ typeof(Vector2), typeof(float) })!;
			MethodInfo Vector2_op_Division = typeof(Vector2).GetMethod("op_Division", BindingFlags.Public | BindingFlags.Static, new Type[]{ typeof(Vector2), typeof(float) })!;
			MethodInfo Vector2_op_Addition = typeof(Vector2).GetMethod("op_Addition", BindingFlags.Public | BindingFlags.Static, new Type[]{ typeof(Vector2), typeof(Vector2) })!;
			MethodInfo Color_get_White = typeof(Color).GetProperty("White", BindingFlags.Public | BindingFlags.Static)!.GetGetMethod()!;
			MethodInfo Color_op_Multiply = typeof(Color).GetMethod("op_Multiply", BindingFlags.Public | BindingFlags.Static, new Type[]{ typeof(Color), typeof(float) })!;
			MethodInfo Utils_Size_Rectangle = typeof(Utils).GetMethod("Size", BindingFlags.Public | BindingFlags.Static, new Type[]{ typeof(Rectangle) })!;
			FieldInfo Item_useAmmo = typeof(Item).GetField("useAmmo", BindingFlags.Public | BindingFlags.Instance)!;
			FieldInfo Item_stack = typeof(Item).GetField("stack", BindingFlags.Public | BindingFlags.Instance)!;

			ILHelper.EnsureAreNotNull(
				(Utils_Size_Texture2D, typeof(Utils).FullName + "::Size(Texture2D)"),
				(Vector2_op_Multiply, typeof(Vector2).FullName + "::op_Multiply(Vector2, float)"),
				(Vector2_op_Division, typeof(Vector2).FullName + "::op_Division(Vector2, float)"),
				(Vector2_op_Addition, typeof(Vector2).FullName + "::op_Addition(Vector2, Vector2)"),
				(Color_get_White, typeof(Color).FullName + "::get_White()"),
				(Color_op_Multiply, typeof(Color).FullName + "::op_Multiply(Color, float)"),
				(Utils_Size_Rectangle, typeof(Utils).FullName + "::Size(Rectangle)"),
				(Item_useAmmo, typeof(Item).FullName + "::useAmmo"),
				(Item_stack, typeof(Item).FullName + "::stack"));

			ILCursor c = new(il);
			int patchNum = 1;

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: true);

			/*   IL_04AB: ldarg.2
			 *      <== NEED TO END UP HERE
			 *   IL_04AC: brfalse.s IL_04B5
			 *   IL_04AE: ldarg.2
			 *   IL_04AF: ldc.i4.2
			 *   IL_04B0: bne.un    IL_0558
			 */
			if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdarg(2),
				i => i.MatchBrfalse(out _),
				i => i.MatchLdarg(2),
				i => i.MatchLdcI4(2),
				i => i.MatchBneUn(out _)))
				goto bad_il;

			patchNum++;

			c.Index++;

			//  ldarg.2
			c.Emit(OpCodes.Ldarg_2);
			//  ldloc     'value'
			c.Emit(OpCodes.Ldloc, 7);
			c.EmitDelegate<Func<int, Texture2D, Texture2D>>((context, value) => {
				if (TCUIItemSlot.SlotContexts.IsValidContext(context))
					value = TextureAssets.InventoryBack5.Value;

				return value;
			});
			//  stloc     'value'
			c.Emit(OpCodes.Stloc, 7);

			/*      <== NEED TO END UP HERE
			 *   IL_08B4: ldloc.s   'value'
			 *   IL_08B6: call      valuetype [FNA]Microsoft.Xna.Framework.Vector2 Terraria.Utils::Size(class [FNA]Microsoft.Xna.Framework.Graphics.Texture2D)
			 *   IL_08BB: ldloc.2
			 *   IL_08BC: call      valuetype [FNA]Microsoft.Xna.Framework.Vector2 [FNA]Microsoft.Xna.Framework.Vector2::op_Multiply(valuetype [FNA]Microsoft.Xna.Framework.Vector2, float32)
			 *   IL_08C1: stloc.s   'vector'
			 */
			if(!c.TryGotoNext(MoveType.Before, i => i.MatchLdloc(7),
				i => i.MatchCall(Utils_Size_Texture2D),
				i => i.MatchLdloc(2),
				i => i.MatchCall(Vector2_op_Multiply),
				i => i.MatchStloc(12)))
				goto bad_il;

			patchNum++;

			ILLabel postContextCheck = c.DefineLabel();

			List<ILLabel> labels = c.IncomingLabels.ToList();

			//  ldloc.1
			c.Emit(OpCodes.Ldloc_1);

			Instruction newTarget = c.Prev;

			//  ldarg.2
			c.Emit(OpCodes.Ldarg_2);

			c.EmitDelegate<Func<Item, int, bool>>((item, context) => item.type <= ItemID.None || item.stack <= 0 || TCUIItemSlot.SlotContexts.IsValidContext(context));

			c.Emit(OpCodes.Brtrue_S, postContextCheck);

			/*   Pushes the following values to the stack for use with the delegate:
			 *   
			 *   TextureAssets.Extra[54].Value
			 *   position + TextureAssets.Extra[54].Value.Size() / 2f * inventoryScale
			 *   TextureAssets.Extra[54].Value.Frame(3, 6, num7 % 3, num7 / 3, 0, 0)
			 *   Color.White * 0.35f
			 *   0f
			 *   TextureAssets.Extra[54].Value.Frame(3, 6, num7 % 3, num7 / 3, 0, 0).Size() / 2f
			 *   inventoryScale
			 *   SpriteEffects.None
			 *   0f
			 *   slot
			 *   context
			 */

			// texture
			c.EmitDelegate(() => TextureAssets.Extra[54].Value);
			// position
			c.Emit(OpCodes.Ldarg, 4);
			c.Emit(OpCodes.Ldloc_2);
			c.EmitDelegate<Func<Vector2, float, Vector2>>((position, inventoryScale) => position + TextureAssets.Extra[54].Value.Size() / 2f * inventoryScale);
			// source
			c.EmitDelegate(() => TextureAssets.Extra[54].Value);
			c.Emit(OpCodes.Ldloc, 11);
			c.EmitDelegate<Func<Texture2D, int, Rectangle>>((texture, num7) => texture.Frame(3, 6, num7 % 3, num7 / 3, 0, 0));
			// color
			c.Emit(OpCodes.Call, Color_get_White);
			c.Emit(OpCodes.Ldc_R4, 0.35f);
			c.Emit(OpCodes.Call, Color_op_Multiply);
			// rotation
			c.Emit(OpCodes.Ldc_R4, 0f);
			// origin
			c.EmitDelegate(() => TextureAssets.Extra[54].Value);
			c.Emit(OpCodes.Ldloc, 11);
			c.EmitDelegate<Func<Texture2D, int, Vector2>>((texture, num7) => texture.Frame(3, 6, num7 % 3, num7 / 3, 0, 0).Size() / 2f);
			// scale
			c.Emit(OpCodes.Ldloc_2);
			// spriteEffects
			c.Emit(OpCodes.Ldc_I4_0);
			// depth
			c.Emit(OpCodes.Ldc_R4, 0f);
			// slot
			c.Emit(OpCodes.Ldarg_3);
			// context
			c.Emit(OpCodes.Ldarg_2);

			/*   Invokes the delegate
			 */

			c.EmitDelegate<Action<Texture2D, Vector2, Rectangle, Color, float, Vector2, float, SpriteEffects, float, int, int>>((value6, position, rectangle, color, rotation, origin, scale, effects, layerDepth, slot, context) => {
				Texture2D? texture = null;

				int partID = context - TCUIItemSlot.SlotContexts.ForgeUIItemPartSlot;

				bool hasAsset;
				Asset<Texture2D> asset;
				if (partID >= 0 && partID < PartRegistry.Count)
					hasAsset = ModContent.RequestIfExists(ItemPartItem.GetUnkownTexturePath(partID), out asset, AssetRequestMode.ImmediateLoad);
				else {
					//Attempt to load a material item slot
					int moldID = context - TCUIItemSlot.SlotContexts.ForgeMaterialSlot;

					if (moldID == -2) {
						//Wood icon
						hasAsset = true;
						asset = TextureAssets.Item[ItemID.Wood];
					} else if (moldID == -1) {
						//Shard icon
						hasAsset = ModContent.RequestIfExists(ItemPartItem.GetUnkownTexturePath(CoreLibMod.RegisteredParts.Shard), out asset, AssetRequestMode.ImmediateLoad);
					} else if (moldID >= 0 && moldID < PartMold.moldsByPartID.Count) {
						//Mold icon
						var mold = PartMold.moldsByPartID[moldID].complexPlatinum;

						hasAsset = true;
						asset = TextureAssets.Item[mold.Type];
					} else {
						hasAsset = false;
						asset = null!;
					}
				}

				if (hasAsset) {
					texture = asset.Value;
					rectangle = new(0, 0, texture.Width, texture.Height);
					origin = rectangle.Size() / 2f;
				} else
					texture = value6;

				Main.spriteBatch.Draw(texture, position, rectangle, color, rotation, origin, scale, effects, layerDepth);
			});

			c.MarkLabel(postContextCheck);

			foreach (var label in labels)
				if (!object.ReferenceEquals(label, postContextCheck))  //Update the labels that weren't created by this IL edit
					label.Target = newTarget;

			/*   // _ = item.useAmmo;
			 *   IL_0bb9: ldloc.1
			 *   IL_0bba: ldfld int32 Terraria.Item::useAmmo
			 *   IL_0bbf: pop
			 *   // num11 = 0;
			 *   IL_0bc0: ldc.i4.0
			 *   IL_0bc1: stloc.s 35
			 *   ...
			 *   IL_0bdf: brfalse.s IL_0bef
			 *   // num11 += inv[j].stack;
			 *   IL_0be1: ldloc.s 35
			 *   IL_0be3: ldarg.1
			 *   IL_0be4: ldloc.s 38
			 *   IL_0be6: ldelem.ref
			 *   IL_0be7: ldfld int32 Terraria.Item::stack
			 *      <== NEED TO END UP HERE
			 *   IL_0bec: add
			 *   IL_0bed: stloc.s 35
			 */
			int num11 = -1, j = -1;
			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdloc(1),
				i => i.MatchLdfld(Item_useAmmo),
				i => i.MatchPop(),
				i => i.MatchLdcI4(0),
				i => i.MatchStloc(out num11)))
				goto bad_il;
			if (!c.TryGotoNext(MoveType.After, i => i.MatchBrfalse(out _),
				i => i.MatchLdloc(num11),
				i => i.MatchLdarg(1),
				i => i.MatchLdloc(out j),
				i => i.MatchLdelemRef(),
				i => i.MatchLdfld(Item_stack)))
				goto bad_il;

			patchNum++;

			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldloc, j);
			c.Emit(OpCodes.Ldelem_Ref);
			c.EmitDelegate<Func<int, Item, int>>((stack, item) => {
				if (item.ModItem is BaseTCItem tc)
					stack = tc.ammoReserve;

				return stack;
			});

			ILHelper.UpdateInstructionOffsets(c);

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: false);

			return;
			bad_il:
			throw new Exception("Unable to fully patch " + il.Method.Name + "()\n" +
				"Reason: Could not find instruction sequence for patch #" + patchNum);
		}
	}
}
