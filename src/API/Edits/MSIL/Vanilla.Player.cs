using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Diagnostics;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Achievements;
using TerrariansConstructLib.API.Reflection;
using TerrariansConstructLib.DataStructures;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API.Edits.MSIL {
	partial class Vanilla {
		internal static void Patch_Player_ItemCheck_UseMiningTools_ActuallyUseMiningTool(ILContext il) {
			MethodInfo WorldGen_CanKillTile = typeof(WorldGen).GetMethod("CanKillTile", BindingFlags.Public | BindingFlags.Static, new Type[]{ typeof(int), typeof(int) })!;
			MethodInfo AchievementsHelper_set_CurrentlyMining = typeof(AchievementsHelper).GetProperty("CurrentlyMining", BindingFlags.Public | BindingFlags.Static)!.GetSetMethod()!;

			ILHelper.EnsureAreNotNull((WorldGen_CanKillTile, typeof(WorldGen).FullName + "::CanKillTile(int, int)"),
				(AchievementsHelper_set_CurrentlyMining, typeof(AchievementsHelper).FullName + "::set_CurrentlyMining(bool)"));

			ILCursor c = new(il);

			int patchNum = 1;

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: true);

			ILLabel branchTarget = null!;
			bool FindSequence()
				=> c.TryGotoNext(MoveType.After, i => i.MatchLdarg(3),
					i => i.MatchLdarg(4),
					i => i.MatchCall(WorldGen_CanKillTile),
					i => i.MatchBrtrue(out branchTarget),
					i => i.MatchLdcI4(0),
					i => i.MatchStloc(1));

			if(!FindSequence())
				goto bad_il;

			patchNum++;

			c.Emit(OpCodes.Ldarg_0);

			branchTarget.Target = c.Prev;

			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldloc_1);
			c.Emit(OpCodes.Ldloc_2);
			c.EmitDelegate<Func<Player, Item, int, Tile, int>>((self, sItem, num2, tile) => {
				if (sItem.ModItem is BaseTCItem tc) {
					if (tc.CurrentDurability <= 0 && TCConfig.Instance.UseDurability)
						num2 = 0;
					else {
						var ctx = new TileDestructionContext(num2, tile.TileType, hammer: true);

						tc.modifiers.ModifyToolPower(self, tc, ctx, ref num2);
					}
				}

				return num2;
			});
			c.Emit(OpCodes.Stloc_1);

			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(0),
				i => i.MatchCall(AchievementsHelper_set_CurrentlyMining)))
				goto bad_il;

			patchNum++;

			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldarg_3);
			c.Emit(OpCodes.Ldarg, 4);
			c.Emit(OpCodes.Ldloc_1);
			c.Emit(OpCodes.Ldloc_2);
			c.EmitDelegate<Action<Player, Item, int, int, int, Tile>>((self, sItem, x, y, num2, tile) => {
				if (sItem.ModItem is BaseTCItem tc)
					tc.OnTileDestroyed(self, x, y, new TileDestructionContext(num2, tile.TileType, hammer: true));
			});

			if(!FindSequence())
				goto bad_il;

			patchNum++;

			c.Emit(OpCodes.Ldarg_0);

			branchTarget.Target = c.Prev;

			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldloc_1);
			c.Emit(OpCodes.Ldloc_2);
			c.EmitDelegate<Func<Player, Item, int, Tile, int>>((self, sItem, num2, tile) => {
				if (sItem.ModItem is BaseTCItem tc) {
					if (tc.CurrentDurability <= 0 && TCConfig.Instance.UseDurability)
						num2 = 0;
					else {
						var ctx = new TileDestructionContext(num2, tile.TileType, axe: true);

						tc.modifiers.ModifyToolPower(self, tc, ctx, ref num2);
					}
				}

				return num2;
			});
			c.Emit(OpCodes.Stloc_1);

			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(0),
				i => i.MatchCall(AchievementsHelper_set_CurrentlyMining)))
				goto bad_il;

			patchNum++;

			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldarg_3);
			c.Emit(OpCodes.Ldarg, 4);
			c.Emit(OpCodes.Ldloc_1);
			c.Emit(OpCodes.Ldloc_2);
			c.EmitDelegate<Action<Player, Item, int, int, int, Tile>>((self, sItem, x, y, num2, tile) => {
				if (sItem.ModItem is BaseTCItem tc)
					tc.OnTileDestroyed(self, x, y, new TileDestructionContext(num2, tile.TileType, axe: true));
			});

			ILHelper.UpdateInstructionOffsets(c);

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: false);

			return;
			bad_il:
			throw new Exception("Unable to fully patch " + il.Method.Name + "()\n" +
				"Reason: Could not find instruction sequence for patch #" + patchNum);
		}

		internal static void Patch_Player_ItemCheck_UseMiningTools_TryHittingWall(ILContext il) {
			MethodInfo WorldGen_KillWall = typeof(WorldGen).GetMethod("KillWall", BindingFlags.Public | BindingFlags.Static)!;

			ILHelper.EnsureAreNotNull((WorldGen_KillWall, typeof(WorldGen).FullName + "::KillWall(int, int, bool)"));

			ILCursor c = new(il);

			int patchNum = 1;

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: true);

			if(!c.TryGotoNext(MoveType.After, i => i.MatchStloc(2)))
				goto bad_il;

			patchNum++;

			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldloc_2);
			c.Emit(OpCodes.Ldloc_3);
			c.EmitDelegate<Func<Player, Item, int, Tile, int>>((self, sItem, num, tile) => {
				if (sItem.ModItem is BaseTCItem tc) {
					if (tc.CurrentDurability <= 0 && TCConfig.Instance.UseDurability)
						num = 0;
					else {
						var ctx = new TileDestructionContext(num, tile.TileType, hammerWall: true);

						tc.modifiers.ModifyToolPower(self, tc, ctx, ref num);
					}
				}

				return num;
			});
			c.Emit(OpCodes.Stloc_2);

			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(0),
				i => i.MatchCall(WorldGen_KillWall)))
				goto bad_il;

			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldarg_2);
			c.Emit(OpCodes.Ldarg_3);
			c.Emit(OpCodes.Ldloc_2);
			c.Emit(OpCodes.Ldloc_3);
			c.EmitDelegate<Action<Player, Item, int, int, int, Tile>>((self, sItem, wX, wY, num, tile) => {
				if (sItem.ModItem is BaseTCItem tc)
					tc.OnTileDestroyed(self, wX, wY, new TileDestructionContext(num, tile.TileType, hammerWall: true));
			});

			ILHelper.UpdateInstructionOffsets(c);

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: false);

			return;
			bad_il:
			throw new Exception("Unable to fully patch " + il.Method.Name + "()\n" +
				"Reason: Could not find instruction sequence for patch #" + patchNum);
		}

		internal static void Patch_Player_PickTile(ILContext il) {
			MethodInfo WorldGen_CanKillTile = typeof(WorldGen).GetMethod("CanKillTile", BindingFlags.Public | BindingFlags.Static, new Type[]{ typeof(int), typeof(int) })!;
			ConstructorInfo StackTrace_ctor = typeof(StackTrace).GetConstructor(BindingFlags.Public | BindingFlags.Instance, new Type[]{ typeof(int), typeof(bool) })!;
			MethodInfo AchievementsHelper_set_CurrentlyMining = typeof(AchievementsHelper).GetProperty("CurrentlyMining", BindingFlags.Public | BindingFlags.Static)!.GetSetMethod()!;

			ILHelper.EnsureAreNotNull((WorldGen_CanKillTile, typeof(WorldGen).FullName + "::CanKillTile(int, int)"),
				(StackTrace_ctor, typeof(StackTrace).FullName + "::.ctor"),
				(AchievementsHelper_set_CurrentlyMining, typeof(AchievementsHelper).FullName + "::set_CurrentlyMining(bool)"));

			ILCursor c = new(il);

			int patchNum = 1;

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: true);

			ILLabel label = null!;
			if(!c.TryGotoNext(MoveType.After, i => i.MatchLdarg(1),
				i => i.MatchLdarg(2),
				i => i.MatchCall(WorldGen_CanKillTile),
				i => i.MatchBrtrue(out label),
				i => i.MatchLdcI4(0),
				i => i.MatchStloc(2)))
				goto bad_il;

			patchNum++;

			c.Emit(OpCodes.Ldarg_0);

			label.Target = c.Prev;

			c.Emit(OpCodes.Ldloc_2);
			
			c.Emit(OpCodes.Ldc_I4_0);
			c.Emit(OpCodes.Ldc_I4_0);
			c.Emit(OpCodes.Newobj, StackTrace_ctor);
			c.Emit(OpCodes.Ldloc_1);
			c.EmitDelegate<Func<Player, int, StackTrace, Tile, int>>((self, num2, trace, tile) => {
				MethodInfo miningMethod = typeof(Player).GetCachedMethod("ItemCheck_UseMiningTools_ActuallyUseMiningTool")!;

				//Only do things if PickTile was called from ItemCheck_UseMiningTools_ActuallyUseMiningTool
				if (Utility.MethodExistsInStackTrace(trace, miningMethod)) {
					Item sItem = self.HeldItem;

					if (sItem.ModItem is BaseTCItem tc) {
						if (tc.CurrentDurability <= 0 && TCConfig.Instance.UseDurability)
							num2 = 0;
						else {
							var ctx = new TileDestructionContext(num2, tile.TileType, pickaxe: true);

							tc.modifiers.ModifyToolPower(self, tc, ctx, ref num2);
						}
					}
				}

				return num2;
			});
			c.Emit(OpCodes.Stloc_2);

			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(0),
				i => i.MatchCall(AchievementsHelper_set_CurrentlyMining)))
				goto bad_il;

			patchNum++;

			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldc_I4_0);
			c.Emit(OpCodes.Ldc_I4_0);
			c.Emit(OpCodes.Newobj, StackTrace_ctor);
			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldarg_2);
			c.Emit(OpCodes.Ldloc_2);
			c.Emit(OpCodes.Ldloc_1);
			c.EmitDelegate<Action<Player, StackTrace, int, int, int, Tile>>((self, trace, x, y, num2, tile) => {
				MethodInfo miningMethod = typeof(Player).GetCachedMethod("ItemCheck_UseMiningTools_ActuallyUseMiningTool")!;

				//Only do things if PickTile was called from ItemCheck_UseMiningTools_ActuallyUseMiningTool
				if (Utility.MethodExistsInStackTrace(trace, miningMethod)) {
					Item sItem = self.HeldItem;

					if (sItem.ModItem is BaseTCItem tc)
						tc.OnTileDestroyed(self, x, y, new TileDestructionContext(num2, tile.TileType, pickaxe: true));

				//	Main.NewText($"Destroyed tile (TC item? {sItem.ModItem is BaseTCItem})");
				}
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
