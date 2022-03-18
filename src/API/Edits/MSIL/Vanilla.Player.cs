using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API.Edits.MSIL {
	partial class Vanilla {
		internal static void Patch_Player_PickAmmo(ILContext il) {
			FieldInfo Player_inventory = typeof(Player).GetField("inventory", BindingFlags.Public | BindingFlags.Instance);
			FieldInfo Item_ammo = typeof(Item).GetField("ammo", BindingFlags.Public | BindingFlags.Instance);
			FieldInfo Item_useAmmo = typeof(Item).GetField("useAmmo", BindingFlags.Public | BindingFlags.Instance);
			FieldInfo Item_stack = typeof(Item).GetField("stack", BindingFlags.Public | BindingFlags.Instance);

			ILHelper.EnsureAreNotNull((Player_inventory, typeof(Player).FullName + "::inventory"),
				(Item_ammo, typeof(Item).FullName + "::ammo"),
				(Item_useAmmo, typeof(Item).FullName + "::useAmmo"),
				(Item_stack, typeof(Item).FullName + "::stack"));

			ILCursor c = new(il);

			int patchNum = 1;

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: true);

			ILLabel labelIteratorTarget = null;
			bool FindSequence(int localNum)
				=> c.TryGotoNext(MoveType.After, i => i.MatchLdarg(0),
					i => i.MatchLdfld(Player_inventory),
					i => i.MatchLdloc(localNum),
					i => i.MatchLdelemRef(),
					i => i.MatchLdfld(Item_ammo),
					i => i.MatchLdarg(1),
					i => i.MatchLdfld(Item_useAmmo),
					i => i.MatchBneUn(out labelIteratorTarget),
					i => i.MatchLdarg(0),
					i => i.MatchLdfld(Player_inventory),
					i => i.MatchLdloc(localNum),
					i => i.MatchLdelemRef(),
					i => i.MatchLdfld(Item_stack),
					i => i.MatchLdcI4(0),
					i => i.MatchBle(out _));

			void EmitLoadAndDelegate(int localNum) {
				c.Emit(OpCodes.Ldarg_0);
				c.Emit(OpCodes.Ldfld, Player_inventory);
				c.Emit(OpCodes.Ldloc, localNum);
				c.Emit(OpCodes.Ldelem_Ref);

				c.EmitDelegate<Func<Item, bool>>(item => item.ModItem is not BaseTCItem tc || tc.ammoReserve > 0);

				c.Emit(OpCodes.Brfalse, labelIteratorTarget);
			}

			if(!FindSequence(5))
				goto bad_il;

			patchNum++;

			EmitLoadAndDelegate(5);

			if(!FindSequence(6))
				goto bad_il;

			patchNum++;

			EmitLoadAndDelegate(6);

			if(!FindSequence(7))
				goto bad_il;

			patchNum++;

			EmitLoadAndDelegate(7);

			ILHelper.UpdateInstructionOffsets(c);

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: false);

			return;
			bad_il:
			throw new Exception("Unable to fully patch " + il.Method.Name + "()\n" +
				"Reason: Could not find instruction sequence for patch #" + patchNum);
		}
	}
}
