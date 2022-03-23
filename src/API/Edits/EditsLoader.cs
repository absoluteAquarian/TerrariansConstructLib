namespace TerrariansConstructLib.API.Edits {
	internal static class EditsLoader {
		public static void Load() {
			ILHelper.LogILEdits = true;

			CoreLibMod.SetLoadingSubProgressText("Adding method detours");

			On.Terraria.Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float += Detours.Vanilla.Hook_Projectile_NewProjectile;
			On.Terraria.Player.HasAmmo += Detours.Vanilla.Hook_Player_HasAmmo;

			ILHelper.InitMonoModDumps();

			CoreLibMod.SetLoadingSubProgressText("Adding IL edits");

			IL.Terraria.UI.ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += MSIL.Vanilla.Patch_ItemSlot_Draw;
			IL.Terraria.DataStructures.PlayerDrawLayers.DrawPlayer_27_HeldItem += MSIL.Vanilla.Patch_PlayerDrawLayers_DrawPlayer_27_HeldItem;
			IL.Terraria.Player.PickAmmo += MSIL.Vanilla.Patch_Player_PickAmmo;
			IL.Terraria.Player.ItemCheck_UseMiningTools_ActuallyUseMiningTool += MSIL.Vanilla.Patch_Player_ItemCheck_UseMiningTools_ActuallyUseMiningTool;
			IL.Terraria.Player.ItemCheck_UseMiningTools_TryHittingWall += MSIL.Vanilla.Patch_Player_ItemCheck_UseMiningTools_TryHittingWall;

			ILHelper.DeInitMonoModDumps();

			ILHelper.LogILEdits = false;
		}
	}
}
