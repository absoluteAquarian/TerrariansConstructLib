using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.Players {
	internal class DurabilityDisplay : PlayerDrawLayer {
		public override bool IsHeadLayer => false;

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
			=> drawInfo.drawPlayer.HeldItem?.ModItem is BaseTCItem;

		public override Position GetDefaultPosition()
			=> new AfterParent(PlayerDrawLayers.HeldItem);

		protected override void Draw(ref PlayerDrawSet drawInfo) {
			//Durability bar only appears when:
			//  1) the held item is a Terrarians' Construct item
			//  2) the item is in use or it's at the end of the use, the player is still holding down the use button and the item is auto-reusable
			if (drawInfo.drawPlayer.HeldItem?.ModItem is not BaseTCItem tc)
				return;

			bool itemIsInUse = !drawInfo.drawPlayer.ItemTimeIsZero || (drawInfo.drawPlayer.controlUseItem && drawInfo.drawPlayer.CanAutoReuseItem(drawInfo.drawPlayer.HeldItem));
			if (!itemIsInUse)
				return;

			int max = tc.GetMaxDurability();
			if (TCConfig.Instance.UseDurability && tc.CurrentDurability < max) {
				Texture2D durabilityBar = CoreLibMod.Instance.Assets.Request<Texture2D>("Assets/DurabilityBar").Value;

				int frameY = (int)(15 * (1 - (float)tc.CurrentDurability / max));
				Rectangle barFrame = durabilityBar.Frame(1, 16, 0, frameY);
				barFrame.Y += 22;
				barFrame.Height -= 22;
				Vector2 size = barFrame.Size();

				Vector2 position = drawInfo.Center + new Vector2(0, drawInfo.drawPlayer.height + 16 * 2.5f) - Main.screenPosition;
				position = new((int)position.X, (int)position.Y);

				drawInfo.DrawDataCache.Add(new DrawData(durabilityBar, position, barFrame, Color.White, 0f, size / 2f, 2f, SpriteEffects.None, 0));
			}
		}
	}
}
