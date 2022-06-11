using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Projectiles;

namespace TerrariansConstructLib.Global {
	internal class TCProjectileSpawning : GlobalProjectile {
		public override void OnSpawn(Projectile projectile, IEntitySource source) {
			if (projectile.ModProjectile is BaseTCProjectile tcProj) {
				//If the projectile wasn't spawned from an item, kill it immediately
				BaseTCItem? tcItem;

				if ((source is EntitySource_ItemUse sourceItem && (tcItem = sourceItem.Item.ModItem as BaseTCItem) is not null)
					|| (source is EntitySource_ItemUse_WithAmmo sourceItem_WithAmmo && (tcItem = sourceItem_WithAmmo.Item.ModItem as BaseTCItem) is not null)){
					tcProj.parts = new ItemPart[tcItem.parts.Length];
					tcProj.itemSource_registeredItemID = tcItem.ItemDefinition;

					//Perform a deep copy of each part
					for (int i = 0; i < tcItem.parts.Length; i++) {
						ItemPart part = tcItem.parts[i];

						tcProj.parts[i] = new(){
							material = part.material.Clone(),
							partID = part.partID
						};
					}

					tcProj.modifiers = tcItem.modifiers.Clone();
					
					tcProj.modifiers.OnProjectileSpawn(tcProj, source);
				} else {
					projectile.Kill();
				}
			}
		}
	}
}
