using TerrariansConstructLib.Items;

namespace TerrariansConstructLib {
	public class ItemPartActionsBuilder {
		internal ItemPart.PartItemFunc setItemDefaults;
		internal ItemPart.PartProjectileFunc setProjectileDefaults;
		internal ItemPart.PartPlayerFunc onUse;
		internal ItemPart.PartPlayerFunc onHold;
		internal ItemPart.PartPlayerFunc onGenericHotkeyUsage;
		internal ItemPart.PartProjectileSpawnFunc onProjectileSpawn;
		internal ItemPart.PartProjectileHitNPCFunc onHitNPC;
		internal ItemPart.PartProjectileHitPlayerFunc onHitPlayer;
		internal ItemPart.PartModifyWeaponDamageFunc modifyWeaponDamage;
		internal ItemPart.PartModifyWeaponKnockbackFunc modifyWeaponKnockback;
		internal ItemPart.PartModifyWeaponCritFunc modifyWeaponCrit;
		internal ItemPart.PartProjectileFunc projectileAI;

		private bool isReadonly;

		public ItemPartActionsBuilder(bool isReadonly = false) {
			this.isReadonly = isReadonly;
		}

		public ItemPartActionsBuilder AsReadonly() {
			if (isReadonly)
				return this;

			ItemPartActionsBuilder builder = (ItemPartActionsBuilder)MemberwiseClone();
			builder.isReadonly = true;
			return builder;
		}

		public ItemPartActionsBuilder WithItemDefaults(ItemPart.PartItemFunc setItemDefaults) {
			if (isReadonly)
				return new ItemPartActionsBuilder().WithItemDefaults(setItemDefaults);

			this.setItemDefaults = setItemDefaults;
			return this;
		}

		public ItemPartActionsBuilder WithProjectileDefaults(ItemPart.PartProjectileFunc setProjectileDefaults) {
			if (isReadonly)
				return new ItemPartActionsBuilder().WithProjectileDefaults(setProjectileDefaults);

			this.setProjectileDefaults = setProjectileDefaults;
			return this;
		}

		public ItemPartActionsBuilder WithOnUse(ItemPart.PartPlayerFunc onUse) {
			if (isReadonly)
				return new ItemPartActionsBuilder().WithOnUse(onUse);

			this.onUse = onUse;
			return this;
		}

		public ItemPartActionsBuilder WithOnHold(ItemPart.PartPlayerFunc onHold) {
			if (isReadonly)
				return new ItemPartActionsBuilder().WithOnHold(onHold);

			this.onHold = onHold;
			return this;
		}

		public ItemPartActionsBuilder WithOnGenericHotkeyUsage(ItemPart.PartPlayerFunc onGenericHotkeyUsage) {
			if (isReadonly)
				return new ItemPartActionsBuilder().WithOnGenericHotkeyUsage(onGenericHotkeyUsage);

			this.onGenericHotkeyUsage = onGenericHotkeyUsage;
			return this;
		}

		public ItemPartActionsBuilder WithOnProjectileSpawn(ItemPart.PartProjectileSpawnFunc onProjectileSpawn) {
			if (isReadonly)
				return new ItemPartActionsBuilder().WithOnProjectileSpawn(onProjectileSpawn);

			this.onProjectileSpawn = onProjectileSpawn;
			return this;
		}

		public ItemPartActionsBuilder WithOnHitNPC(ItemPart.PartProjectileHitNPCFunc onHitNPC) {
			if (isReadonly)
				return new ItemPartActionsBuilder().WithOnHitNPC(onHitNPC);

			this.onHitNPC = onHitNPC;
			return this;
		}

		public ItemPartActionsBuilder WithOnHitPlayer(ItemPart.PartProjectileHitPlayerFunc onHitPlayer) {
			if (isReadonly)
				return new ItemPartActionsBuilder().WithOnHitPlayer(onHitPlayer);

			this.onHitPlayer = onHitPlayer;
			return this;
		}

		public ItemPartActionsBuilder WithModifyWeaponDamage(ItemPart.PartModifyWeaponDamageFunc modifyWeaponDamage) {
			if (isReadonly)
				return new ItemPartActionsBuilder().WithModifyWeaponDamage(modifyWeaponDamage);

			this.modifyWeaponDamage = modifyWeaponDamage;
			return this;
		}

		public ItemPartActionsBuilder WithModifyWeaponKnockback(ItemPart.PartModifyWeaponKnockbackFunc modifyWeaponKnockback) {
			if (isReadonly)
				return new ItemPartActionsBuilder().WithModifyWeaponKnockback(modifyWeaponKnockback);

			this.modifyWeaponKnockback = modifyWeaponKnockback;
			return this;
		}

		public ItemPartActionsBuilder WithModifyWeaponCrit(ItemPart.PartModifyWeaponCritFunc modifyWeaponCrit) {
			if (isReadonly)
				return new ItemPartActionsBuilder().WithModifyWeaponCrit(modifyWeaponCrit);

			this.modifyWeaponCrit = modifyWeaponCrit;
			return this;
		}

		public ItemPartActionsBuilder WithAI(ItemPart.PartProjectileFunc projectileAI) {
			if (isReadonly)
				return new ItemPartActionsBuilder().WithAI(projectileAI);

			this.projectileAI = projectileAI;
			return this;
		}
	}
}
