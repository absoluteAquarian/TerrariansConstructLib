using TerrariansConstructLib.Items;

namespace TerrariansConstructLib {
	public class ItemPartActionsBuilder {
		internal ItemPart.PartItemFunc onInitialized;
		internal ItemPart.PartProjectileFunc setProjectileDefaults;
		internal ItemPart.PartPlayerFunc onUse;
		internal ItemPart.PartPlayerFunc onHold;
		internal ItemPart.PartPlayerFunc onGenericHotkeyUsage;
		internal ItemPart.PartProjectileSpawnFunc onProjectileSpawn;
		internal ItemPart.PartProjectileHitNPCFunc onProjectileHitNPC;
		internal ItemPart.PartProjectileHitPlayerFunc onProjectileHitPlayer;
		internal ItemPart.PartModifyWeaponDamageFunc modifyWeaponDamage;
		internal ItemPart.PartModifyWeaponKnockbackFunc modifyWeaponKnockback;
		internal ItemPart.PartModifyWeaponCritFunc modifyWeaponCrit;
		internal ItemPart.PartProjectileFunc projectileAI;
		internal ItemPart.PartToolPowerFunc modifyToolPower;
		internal ItemPart.PartTileDestructionFunc onTileDestroyed;
		internal ItemPart.PartItemHitNPCFunc onItemHitNPC;
		internal ItemPart.PartItemHitPlayerFunc onItemHitPlayer;
		internal ItemPart.PartItemUseSpeedMultiplier useSpeedMultiplier;
		internal ItemPart.PartPlayerFunc onUpdateInventory;
		internal ItemPart.PartCanLoseDurability canLoseDurability;

		private bool isReadonly;

		public ItemPartActionsBuilder MarkAsReadonly() {
			isReadonly = true;
			return this;
		}

		public ItemPartActionsBuilder Clone(bool isReadonly = false) {
			ItemPartActionsBuilder builder = (ItemPartActionsBuilder)MemberwiseClone();
			builder.isReadonly = isReadonly;
			return builder;
		}
		
		/// <inheritdoc cref="ItemPart.PartItemFunc"/>
		public ItemPartActionsBuilder WithItemDefaults(ItemPart.PartItemFunc onInitialized) {
			if (isReadonly)
				return Clone().WithItemDefaults(onInitialized);

			this.onInitialized = onInitialized;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartProjectileFunc"/>
		public ItemPartActionsBuilder WithProjectileDefaults(ItemPart.PartProjectileFunc setProjectileDefaults) {
			if (isReadonly)
				return Clone().WithProjectileDefaults(setProjectileDefaults);

			this.setProjectileDefaults = setProjectileDefaults;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartPlayerFunc"/>
		public ItemPartActionsBuilder WithOnUse(ItemPart.PartPlayerFunc onUse) {
			if (isReadonly)
				return Clone().WithOnUse(onUse);

			this.onUse = onUse;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartPlayerFunc"/>
		public ItemPartActionsBuilder WithOnHold(ItemPart.PartPlayerFunc onHold) {
			if (isReadonly)
				return Clone().WithOnHold(onHold);

			this.onHold = onHold;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartPlayerFunc"/>
		public ItemPartActionsBuilder WithOnGenericHotkeyUsage(ItemPart.PartPlayerFunc onGenericHotkeyUsage) {
			if (isReadonly)
				return Clone().WithOnGenericHotkeyUsage(onGenericHotkeyUsage);

			this.onGenericHotkeyUsage = onGenericHotkeyUsage;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartProjectileSpawnFunc"/>
		public ItemPartActionsBuilder WithOnProjectileSpawn(ItemPart.PartProjectileSpawnFunc onProjectileSpawn) {
			if (isReadonly)
				return Clone().WithOnProjectileSpawn(onProjectileSpawn);

			this.onProjectileSpawn = onProjectileSpawn;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartProjectileHitNPCFunc"/>
		public ItemPartActionsBuilder WithOnProjectileHitNPC(ItemPart.PartProjectileHitNPCFunc onProjectileHitNPC) {
			if (isReadonly)
				return Clone().WithOnProjectileHitNPC(onProjectileHitNPC);

			this.onProjectileHitNPC = onProjectileHitNPC;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartProjectileHitPlayerFunc"/>
		public ItemPartActionsBuilder WithOnProjectileHitPlayer(ItemPart.PartProjectileHitPlayerFunc onProjectileHitPlayer) {
			if (isReadonly)
				return Clone().WithOnProjectileHitPlayer(onProjectileHitPlayer);

			this.onProjectileHitPlayer = onProjectileHitPlayer;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartModifyWeaponDamageFunc"/>
		public ItemPartActionsBuilder WithModifyWeaponDamage(ItemPart.PartModifyWeaponDamageFunc modifyWeaponDamage) {
			if (isReadonly)
				return Clone().WithModifyWeaponDamage(modifyWeaponDamage);

			this.modifyWeaponDamage = modifyWeaponDamage;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartModifyWeaponKnockbackFunc"/>
		public ItemPartActionsBuilder WithModifyWeaponKnockback(ItemPart.PartModifyWeaponKnockbackFunc modifyWeaponKnockback) {
			if (isReadonly)
				return Clone().WithModifyWeaponKnockback(modifyWeaponKnockback);

			this.modifyWeaponKnockback = modifyWeaponKnockback;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartModifyWeaponCritFunc"/>
		public ItemPartActionsBuilder WithModifyWeaponCrit(ItemPart.PartModifyWeaponCritFunc modifyWeaponCrit) {
			if (isReadonly)
				return Clone().WithModifyWeaponCrit(modifyWeaponCrit);

			this.modifyWeaponCrit = modifyWeaponCrit;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartProjectileFunc"/>
		public ItemPartActionsBuilder WithAI(ItemPart.PartProjectileFunc projectileAI) {
			if (isReadonly)
				return Clone().WithAI(projectileAI);

			this.projectileAI = projectileAI;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartToolPowerFunc"/>
		public ItemPartActionsBuilder WithModifyToolPower(ItemPart.PartToolPowerFunc modifyToolPower) {
			if (isReadonly)
				return Clone().WithModifyToolPower(modifyToolPower);

			this.modifyToolPower = modifyToolPower;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartTileDestructionFunc"/>
		public ItemPartActionsBuilder WithOnTileDestroyed(ItemPart.PartTileDestructionFunc onTileDestroyed) {
			if (isReadonly)
				return Clone().WithOnTileDestroyed(onTileDestroyed);

			this.onTileDestroyed = onTileDestroyed;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartItemHitNPCFunc"/>
		public ItemPartActionsBuilder WithOnItemHitNPC(ItemPart.PartItemHitNPCFunc onItemHitNPC) {
			if (isReadonly)
				return Clone().WithOnItemHitNPC(onItemHitNPC);

			this.onItemHitNPC = onItemHitNPC;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartItemHitPlayerFunc"/>
		public ItemPartActionsBuilder WithOnItemHitPlayer(ItemPart.PartItemHitPlayerFunc onItemHitPlayer) {
			if (isReadonly)
				return Clone().WithOnItemHitPlayer(onItemHitPlayer);

			this.onItemHitPlayer = onItemHitPlayer;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartItemUseSpeedMultiplier"/>
		public ItemPartActionsBuilder WithUseSpeedMultiplier(ItemPart.PartItemUseSpeedMultiplier useSpeedMultiplier) {
			if (isReadonly)
				return Clone().WithUseSpeedMultiplier(useSpeedMultiplier);

			this.useSpeedMultiplier = useSpeedMultiplier;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartPlayerFunc"/>
		public ItemPartActionsBuilder WithOnUpdateInventory(ItemPart.PartPlayerFunc onUpdateInventory) {
			if (isReadonly)
				return Clone().WithOnUpdateInventory(onUpdateInventory);

			this.onUpdateInventory = onUpdateInventory;
			return this;
		}

		/// <inheritdoc cref="ItemPart.PartCanLoseDurability"/>
		public ItemPartActionsBuilder WithCanLoseDurability(ItemPart.PartCanLoseDurability canLoseDurability) {
			if (isReadonly)
				return Clone().WithCanLoseDurability(canLoseDurability);

			this.canLoseDurability = canLoseDurability;
			return this;
		}
	}
}
