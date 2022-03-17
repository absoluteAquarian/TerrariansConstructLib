namespace TerrariansConstructLib.API.Edits {
	internal static class EditsLoader {
		public static void Load() {
			ILHelper.LogILEdits = true;

			On.Terraria.Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float += Detours.Vanilla.Hook_Projectile_NewProjectile;

			ILHelper.InitMonoModDumps();

			IL.Terraria.UI.ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += MSIL.Vanilla.Patch_ItemSlot_Draw;
			IL.Terraria.DataStructures.PlayerDrawLayers.DrawPlayer_27_HeldItem += MSIL.Vanilla.Patch_PlayerDrawLayers_DrawPlayer_27_HeldItem;

			ILHelper.DeInitMonoModDumps();

			ILHelper.LogILEdits = false;
		}
	}
}
