using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using TerrariansConstructLib.API.UI;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.API.Edits.Detours {
	partial class Vanilla {
		//Must use a delegate since generic types can't have "ref type"
		public delegate void Hook_ItemSlot_TextureModificationFunc(int context, ref Texture2D value);
		public delegate void Hook_ItemSlot_DrawExtra(Texture2D value6, Vector2 position, Rectangle rectangle, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth, int slot, int context);

		internal static void Hook_ItemSlot_Draw(ILContext il) {
			MethodInfo Utils_Size_Texture2D = typeof(Utils).GetMethod("Size", BindingFlags.Public | BindingFlags.Static, new Type[]{ typeof(Texture2D) });
			MethodInfo Vector2_op_Multiply = typeof(Vector2).GetMethod("op_Multiply", BindingFlags.Public | BindingFlags.Static, new Type[]{ typeof(Vector2), typeof(float) });
			MethodInfo Vector2_op_Division = typeof(Vector2).GetMethod("op_Division", BindingFlags.Public | BindingFlags.Static, new Type[]{ typeof(Vector2), typeof(float) });
			MethodInfo Vector2_op_Addition = typeof(Vector2).GetMethod("op_Addition", BindingFlags.Public | BindingFlags.Static, new Type[]{ typeof(Vector2), typeof(float) });
			MethodInfo Color_get_White = typeof(Color).GetProperty("White", BindingFlags.Public | BindingFlags.Static).GetGetMethod();
			MethodInfo Color_op_Multiply = typeof(Color).GetMethod("op_Multiply", BindingFlags.Public | BindingFlags.Static, new Type[]{ typeof(Color), typeof(float) });
			MethodInfo Utils_Size_Rectangle = typeof(Utils).GetMethod("Size", BindingFlags.Public | BindingFlags.Static, new Type[]{ typeof(Rectangle) });
			FieldInfo Item_type = typeof(Item).GetField("type", BindingFlags.Public | BindingFlags.Instance);
			FieldInfo Item_stack= typeof(Item).GetField("stack", BindingFlags.Public | BindingFlags.Instance);

			ILCursor c = new(il);

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

			c.Index++;

			//  ldarg.2
			c.Emit(OpCodes.Ldarg_2);
			//  ldloca     'value'
			c.Emit(OpCodes.Ldloca, 7);
			c.EmitDelegate<Hook_ItemSlot_TextureModificationFunc>((int context, ref Texture2D value) => {
				if (context >= TCUIItemSlot.SlotContexts.ForgeUI && context < TCUIItemSlot.SlotContexts.ForgeUI + PartRegistry.Count)
					value = TextureAssets.InventoryBack5.Value;
			});

			/*   IL_08B4: ldloc.s   'value'
			 *      <== NEED TO END UP HERE
			 *   IL_08B6: call      valuetype [FNA]Microsoft.Xna.Framework.Vector2 Terraria.Utils::Size(class [FNA]Microsoft.Xna.Framework.Graphics.Texture2D)
			 *   IL_08BB: ldloc.2
			 *   IL_08BC: call      valuetype [FNA]Microsoft.Xna.Framework.Vector2 [FNA]Microsoft.Xna.Framework.Vector2::op_Multiply(valuetype [FNA]Microsoft.Xna.Framework.Vector2, float32)
			 *   IL_08C1: stloc.s   'vector'
			 */
			if(!c.TryGotoNext(MoveType.Before, i => i.MatchLdloc(7),
				i => i.MatchCall(Utils_Size_Texture2D),
				i => i.MatchLdloc(7),
				i => i.MatchCall(Vector2_op_Multiply),
				i => i.MatchStloc(12)))
				goto bad_il;

			c.Index++;

			ILLabel postContextCheck = c.DefineLabel();

			/*   if (item.type <= 0 || item.type <= 0 || context < TCUIItemSlot.SlotContexts.ForgeUI || context >= TCUIItemSlot.SlotContexts.ForgeUI + MaterialPartID.TotalCount)
			 *       goto postContextCheck;
			 */

			//  ldloc.1
			c.Emit(OpCodes.Ldloc_1);
			//  ldfld     int32 Terraria.Item::'type'
			c.Emit(OpCodes.Ldfld, Item_type);
			//  ldc.i4.0
			c.Emit(OpCodes.Ldc_I4_0);
			//  ble.s     postContentCheck
			c.Emit(OpCodes.Ble_S, postContextCheck);
			//  ldloc.1
			c.Emit(OpCodes.Ldloc_1);
			//  ldfld     int32 Terraria.Item::stack
			c.Emit(OpCodes.Ldfld, Item_stack);
			//  ldc.i4.0
			c.Emit(OpCodes.Ldc_I4_0);
			//  ble.s    postContentCheck
			c.Emit(OpCodes.Ble_S, postContextCheck);
			//  ldarg.2
			c.Emit(OpCodes.Ldarg_2);
			//  ldc.14    TCUIItemSlot.SlotContexts.ForgeUI
			c.Emit(OpCodes.Ldc_I4, TCUIItemSlot.SlotContexts.ForgeUI);
			//  blt.s     postContextCheck
			c.Emit(OpCodes.Blt_S, postContextCheck);
			//  ldarg.2
			c.Emit(OpCodes.Ldarg_2);
			//  ldc.i4    TCUIItemSlot.SlotContexts.ForgeUI
			c.Emit(OpCodes.Ldc_I4, TCUIItemSlot.SlotContexts.ForgeUI);
			c.EmitDelegate<Func<int>>(() => PartRegistry.Count);
			//  add
			c.Emit(OpCodes.Add);
			//  bge.s     postContentCheck
			c.Emit(OpCodes.Bge_S, postContextCheck);

			/*   Pushes the following values to the stack for use with the delegate:
			 *   
			 *   Texture2D value6
			 *   position + value.Size() / 2f * inventoryScale
			 *   rectangle
			 *   Color.White * 0.35f
			 *   0f
			 *   rectangle.Size() / 2f
			 *   inventoryScale
			 *   SpriteEffects.None
			 *   0f
			 *   slot
			 *   context
			 */

			//  ldloc     value6
			c.Emit(OpCodes.Ldloc, 25);
			//  ldarg     position
			c.Emit(OpCodes.Ldarg, 4);
			//  ldloc     'value'
			c.Emit(OpCodes.Ldloc, 7);
			//  call      valuetype [FNA]Microsoft.Xna.Framework.Vector2 Terraria.Utils::Size(class [FNA]Microsoft.Xna.Framework.Graphics.Texture2D)
			c.Emit(OpCodes.Call, Utils_Size_Texture2D);
			//  ldc.r4    2
			c.Emit(OpCodes.Ldc_R4, 2f);
			//  call      valuetype [FNA]Microsoft.Xna.Framework.Vector2 [FNA]Microsoft.Xna.Framework.Vector2::op_Division(valuetype [FNA]Microsoft.Xna.Framework.Vector2, float32)
			c.Emit(OpCodes.Call, Vector2_op_Division);
			//  ldloc.2
			c.Emit(OpCodes.Ldloc_2);
			//  call      valuetype [FNA]Microsoft.Xna.Framework.Vector2 [FNA]Microsoft.Xna.Framework.Vector2::op_Multiply(valuetype [FNA]Microsoft.Xna.Framework.Vector2, float32)
			c.Emit(OpCodes.Call, Vector2_op_Multiply);
			//  call      valuetype [FNA]Microsoft.Xna.Framework.Vector2 [FNA]Microsoft.Xna.Framework.Vector2::op_Addition(valuetype [FNA]Microsoft.Xna.Framework.Vector2, valuetype [FNA]Microsoft.Xna.Framework.Vector2)
			c.Emit(OpCodes.Call, Vector2_op_Addition);
			//  ldloc.s   rectangle
			c.Emit(OpCodes.Ldloc, 27);
			//  call      valuetype [FNA]Microsoft.Xna.Framework.Color [FNA]Microsoft.Xna.Framework.Color::get_White()
			c.Emit(OpCodes.Call, Color_get_White);
			//  ldc.r4    0.35
			c.Emit(OpCodes.Ldc_R4, 0.35f);
			//  call      valuetype [FNA]Microsoft.Xna.Framework.Color [FNA]Microsoft.Xna.Framework.Color::op_Multiply(valuetype [FNA]Microsoft.Xna.Framework.Color, float32)
			c.Emit(OpCodes.Call, Color_op_Multiply);
			//  ldc.r4    0.0
			c.Emit(OpCodes.Ldc_R4, 0f);
			//  ldloc.s   rectangle
			c.Emit(OpCodes.Ldloc, 27);
			//  call      valuetype [FNA]Microsoft.Xna.Framework.Vector2 Terraria.Utils::Size(valuetype [FNA]Microsoft.Xna.Framework.Rectangle)
			c.Emit(OpCodes.Call, Utils_Size_Rectangle);
			//  ldc.r4    2
			c.Emit(OpCodes.Ldc_R4, 2f);
			//  call      valuetype [FNA]Microsoft.Xna.Framework.Vector2 [FNA]Microsoft.Xna.Framework.Vector2::op_Division(valuetype [FNA]Microsoft.Xna.Framework.Vector2, float32)
			c.Emit(OpCodes.Call, Vector2_op_Division);
			//  ldloc.2
			c.Emit(OpCodes.Ldloc_2);
			//  ldc.i4.0
			c.Emit(OpCodes.Ldc_I4_0);
			//  ldc.r4    0.0
			c.Emit(OpCodes.Ldc_R4, 0f);
			//  ldarg.3
			c.Emit(OpCodes.Ldarg_3);
			//  ldarg.2
			c.Emit(OpCodes.Ldarg_2);

			/*   Invokes the delegate
			 */

			c.EmitDelegate<Hook_ItemSlot_DrawExtra>((value6, position, rectangle, color, rotation, origin, scale, effects, layerDepth, slot, context) => {
				Texture2D texture = null;

				int partID = context - TCUIItemSlot.SlotContexts.ForgeUI;

				if (ModContent.RequestIfExists<Texture2D>(ItemPartItem.GetUnkownTexturePath(partID), out var asset, AssetRequestMode.ImmediateLoad)) {
					texture = asset.Value;
					rectangle = new(0, 0, texture.Width, texture.Height);
					origin = rectangle.Size() / 2f;
				} else
					texture = value6;

				Main.spriteBatch.Draw(texture, position, rectangle, color, rotation, origin, scale, effects, layerDepth);
			});

			ILHelper.UpdateInstructionOffsets(c);

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: false);

			return;
			bad_il:
			CoreLibMod.Instance.Logger.Error("Unable to fully patch " + il.Method.FullName);
		}
	}
}
