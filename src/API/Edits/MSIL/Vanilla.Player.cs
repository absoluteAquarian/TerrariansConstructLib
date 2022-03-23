﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Achievements;
using TerrariansConstructLib.API.Reflection;
using TerrariansConstructLib.DataStructures;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API.Edits.MSIL {
	partial class Vanilla {
		internal static void Patch_Player_PickAmmo(ILContext il) {
			FieldInfo Player_inventory = typeof(Player).GetField("inventory", BindingFlags.Public | BindingFlags.Instance)!;
			FieldInfo Item_ammo = typeof(Item).GetField("ammo", BindingFlags.Public | BindingFlags.Instance)!;
			FieldInfo Item_useAmmo = typeof(Item).GetField("useAmmo", BindingFlags.Public | BindingFlags.Instance)!;
			FieldInfo Item_stack = typeof(Item).GetField("stack", BindingFlags.Public | BindingFlags.Instance)!;

			ILHelper.EnsureAreNotNull((Player_inventory, typeof(Player).FullName + "::inventory"),
				(Item_ammo, typeof(Item).FullName + "::ammo"),
				(Item_useAmmo, typeof(Item).FullName + "::useAmmo"),
				(Item_stack, typeof(Item).FullName + "::stack"));

			ILCursor c = new(il);

			int patchNum = 1;

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: true);

			ILLabel? labelIteratorTarget = null;
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

		internal static void Patch_Player_ItemCheck_UseMiningTools_ActuallyUseMiningTool(ILContext il) {
			MethodInfo WorldGen_CanKillTile = typeof(WorldGen).GetMethod("CanKillTile", BindingFlags.Public | BindingFlags.Static)!;
			MethodInfo AchievementsHelper_set_CurrentlyMining = typeof(AchievementsHelper).GetProperty("CurrentlyMining", BindingFlags.Public | BindingFlags.Static)!.GetGetMethod()!;

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

			bool EmitLoadAndDelegates(bool axe = false, bool hammer = false) {
				c.Emit(OpCodes.Ldarg_0);

				branchTarget.Target = c.Prev;

				c.Emit(OpCodes.Ldarg_1);
				c.Emit(OpCodes.Ldloc_1);
				c.EmitDelegate<Func<Player, Item, int, int>>((self, sItem, num2) => {
					if (sItem.ModItem is BaseTCItem tc) {
						if (tc.CurrentDurability <= 0 && TCConfig.Instance.UseDurability)
							num2 = 0;
						else {
							for (int i = 0; i < tc.parts.Length; i++)
								tc.parts[i].ModifyToolPower?.Invoke(tc.parts[i].partID, self, sItem, new TileDestructionContext(num2, axe: axe, hammer: hammer), ref num2);
						}
					}

					return num2;
				});
				c.Emit(OpCodes.Stloc_1);

				if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(0),
					i => i.MatchCall(AchievementsHelper_set_CurrentlyMining)))
					return false;

				patchNum++;

				c.Emit(OpCodes.Ldarg_0);
				c.Emit(OpCodes.Ldarg_1);
				c.Emit(OpCodes.Ldarg_3);
				c.Emit(OpCodes.Ldarg, 4);
				c.Emit(OpCodes.Ldloc_1);

				c.EmitDelegate<Action<Player, Item, int, int, int>>((self, sItem, x, y, num2) => {
					if (sItem.ModItem is BaseTCItem tc)
						tc.OnTileDestroyed(self, x, y, new TileDestructionContext(num2, axe: axe, hammer: hammer));
				});

				return true;
			}

			if(!FindSequence())
				goto bad_il;

			patchNum++;

			if(!EmitLoadAndDelegates(hammer: true))
				goto bad_il;

			if(!FindSequence())
				goto bad_il;

			patchNum++;

			if(!EmitLoadAndDelegates(axe: true))
				goto bad_il;

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
			c.EmitDelegate<Func<Player, Item, int, int>>((self, sItem, num) => {
				if (sItem.ModItem is BaseTCItem tc) {
					if (tc.CurrentDurability <= 0 && TCConfig.Instance.UseDurability)
						num = 0;
					else {
						for (int i = 0; i < tc.parts.Length; i++)
							tc.parts[i].ModifyToolPower?.Invoke(tc.parts[i].partID, self, sItem, new TileDestructionContext(num, hammerWall: true), ref num);
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

			c.EmitDelegate<Action<Player, Item, int, int, int>>((self, sItem, wX, wY, num) => {
				if (sItem.ModItem is BaseTCItem tc)
					tc.OnTileDestroyed(self, wX, wY, new TileDestructionContext(num, hammerWall: true));
			});

			ILHelper.UpdateInstructionOffsets(c);

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: false);

			return;
			bad_il:
			throw new Exception("Unable to fully patch " + il.Method.Name + "()\n" +
				"Reason: Could not find instruction sequence for patch #" + patchNum);
		}

		internal static void Patch_Player_PickTile(ILContext il) {
			MethodInfo WorldGen_CanKillTile = typeof(WorldGen).GetMethod("CanKillTile", BindingFlags.Public | BindingFlags.Static)!;
			ConstructorInfo StackTrace_ctor = typeof(StackTrace).GetConstructor(BindingFlags.Public | BindingFlags.Instance, new Type[]{ typeof(int), typeof(bool) })!;
			MethodInfo AchievementsHelper_set_CurrentlyMining = typeof(AchievementsHelper).GetProperty("CurrentlyMining", BindingFlags.Public | BindingFlags.Static)!.GetGetMethod()!;

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

			c.Emit(OpCodes.Ldc_I4_1);
			c.Emit(OpCodes.Ldc_I4_0);
			c.Emit(OpCodes.Newobj, StackTrace_ctor);
			c.EmitDelegate<Func<Player, int, StackTrace, int>>((self, num2, trace) => {
				MethodInfo miningMethod = typeof(Player).GetCachedMethod("ItemCheck_UseMiningTools_ActuallyUseMiningTool")!;

				//Only do things if PickTile was called from ItemCheck_UseMiningTools_ActuallyUseMiningTool
				if (trace.GetFrame(0)!.GetMethod()!.MethodHandle == miningMethod.MethodHandle) {
					Item sItem = self.HeldItem;

					if (sItem.ModItem is BaseTCItem tc) {
						if (tc.CurrentDurability <= 0 && TCConfig.Instance.UseDurability)
							num2 = 0;
						else {
							for (int i = 0; i < tc.parts.Length; i++)
								tc.parts[i].ModifyToolPower?.Invoke(tc.parts[i].partID, self, sItem, new TileDestructionContext(num2, pickaxe: true), ref num2);
						}
					}
				}

				return num2;
			});

			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(0),
				i => i.MatchCall(AchievementsHelper_set_CurrentlyMining)))
				goto bad_il;

			patchNum++;

			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldc_I4_1);
			c.Emit(OpCodes.Ldc_I4_0);
			c.Emit(OpCodes.Newobj, StackTrace_ctor);
			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldarg_2);
			c.Emit(OpCodes.Ldloc_2);

			c.EmitDelegate<Action<Player, StackTrace, int, int, int>>((self, trace, x, y, num2) => {
				MethodInfo miningMethod = typeof(Player).GetCachedMethod("ItemCheck_UseMiningTools_ActuallyUseMiningTool")!;

				//Only do things if PickTile was called from ItemCheck_UseMiningTools_ActuallyUseMiningTool
				if (trace.GetFrame(0)!.GetMethod()!.MethodHandle == miningMethod.MethodHandle) {
					Item sItem = self.HeldItem;

					if (sItem.ModItem is BaseTCItem tc)
						tc.OnTileDestroyed(self, x, y, new TileDestructionContext(num2, pickaxe: true));
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
