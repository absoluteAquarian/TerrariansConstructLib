using Terraria.Localization;

namespace TerrariansConstructLib.API.Edits {
	internal static class EditsLoader {
		public static void Load() {
			ILHelper.LogILEdits = true;

			CoreLibMod.SetLoadingSubProgressText(Language.GetTextValue("Mods.TerrariansConstructLib.Loading.EditsLoader.Detours"));

			On.Terraria.Player.HasAmmo_Item_bool += Detours.Vanilla.Hook_Player_HasAmmo;
			On.Terraria.Player.IsAmmoFreeThisShot += Detours.Vanilla.Hook_Player_IsAmmoFreeThisShot;
			On.Terraria.UI.ItemSlot.LeftClick_ItemArray_int_int += Detours.Vanilla.Hook_ItemSlot_LeftClick_ItemArray_int_int;
			On.Terraria.UI.ItemSlot.RightClick_ItemArray_int_int += Detours.Vanilla.Hook_ItemSlot_RightClick_ItemArray_int_int;

			ILHelper.InitMonoModDumps();

			CoreLibMod.SetLoadingSubProgressText(Language.GetTextValue("Mods.TerrariansConstructLib.Loading.EditsLoader.ILEdits"));

			IL.Terraria.UI.ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += MSIL.Vanilla.Patch_ItemSlot_Draw;
			IL.Terraria.DataStructures.PlayerDrawLayers.DrawPlayer_27_HeldItem += MSIL.Vanilla.Patch_PlayerDrawLayers_DrawPlayer_27_HeldItem;
			IL.Terraria.Player.ItemCheck_UseMiningTools_ActuallyUseMiningTool += MSIL.Vanilla.Patch_Player_ItemCheck_UseMiningTools_ActuallyUseMiningTool;
			IL.Terraria.Player.ItemCheck_UseMiningTools_TryHittingWall += MSIL.Vanilla.Patch_Player_ItemCheck_UseMiningTools_TryHittingWall;
			IL.Terraria.Player.PickTile += MSIL.Vanilla.Patch_Player_PickTile;

			ILHelper.DeInitMonoModDumps();

			ILHelper.LogILEdits = false;
		}
	}
}
