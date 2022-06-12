using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariansConstructLib.API;
using TerrariansConstructLib.API.Definitions;
using TerrariansConstructLib.API.Reflection;
using TerrariansConstructLib.API.Stats;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Modifiers;

namespace TerrariansConstructLib.Projectiles {
	/// <summary>
	/// The base projectile class for any projectiles fired from Terrarians' Construct weapons
	/// </summary>
	public abstract class BaseTCProjectile : ModProjectile {
		internal ItemPart[] parts = Array.Empty<ItemPart>();
		internal ModifierCollection modifiers = null!;

		protected ReadOnlySpan<ItemPart> GetParts() => parts;

		public T? GetModifier<T>() where T : BaseTrait
			=> modifiers.FirstOrDefault(t => t.GetType() == typeof(T)) as T;

		public int CountParts(Material material)
			=> parts.Count(p => p.material.Type == material.Type);

		protected IEnumerable<ItemPart> FilterParts(StatType type)
			=> parts?.Where(p => PartDefinitionLoader.Get(p.partID)?.StatType == type) ?? Array.Empty<ItemPart>();

		protected IEnumerable<ItemPart> FilterHeadParts() => FilterParts(StatType.Head);

		protected IEnumerable<ItemPart> FilterHandleParts() => FilterParts(StatType.Handle);

		protected IEnumerable<ItemPart> FilterExtraParts() => FilterParts(StatType.Extra);

		protected IEnumerable<S> GetPartStats<S>(StatType type) where S : class, IPartStats
			=> parts.Where(p => PartDefinitionLoader.Get(p.partID)?.StatType == type).Select(p => p.GetStat<S>(type)!).Where(s => s is not null);

		protected IEnumerable<HeadPartStats> GetHeadParts() => GetPartStats<HeadPartStats>(StatType.Head);

		protected IEnumerable<HandlePartStats> GetHandleParts() => GetPartStats<HandlePartStats>(StatType.Handle);

		protected IEnumerable<ExtraPartStats> GetExtraParts() => GetPartStats<ExtraPartStats>(StatType.Extra);

		protected IEnumerable<HeadPartStats> SelectToolPickaxeStats()
			=> parts.Where(p => (PartDefinitionLoader.Get(p.partID)?.ToolType & ToolType.Pickaxe) != 0).Select(p => p.GetStat<HeadPartStats>(StatType.Head)!).Where(s => s is not null);

		protected IEnumerable<HeadPartStats> SelectToolAxeStats()
			=> parts.Where(p => (PartDefinitionLoader.Get(p.partID)?.ToolType & ToolType.Axe) != 0).Select(p => p.GetStat<HeadPartStats>(StatType.Head)!).Where(s => s is not null);

		protected IEnumerable<HeadPartStats> SelectToolHammerStats()
			=> parts.Where(p => (PartDefinitionLoader.Get(p.partID)?.ToolType & ToolType.Hammer) != 0).Select(p => p.GetStat<HeadPartStats>(StatType.Head)!).Where(s => s is not null);

		public ItemPart this[int index] {
			get => parts[index];
			set => parts[index] = value;
		}

		/// <summary>
		/// The ID of the <see cref="TCProjectileDefinition"/> that this projectile retrieves its data from
		/// </summary>
		public abstract int ProjectileDefinition { get; }

		/// <summary>
		/// The name for the projectile, used in <see cref="SetStaticDefaults"/><br/>
		/// Defaults to: <c>ProjectileDefinitionLoader.Get(ProjectileDefinition)!.Name</c>
		/// </summary>
		public virtual string ProjectileTypeName => ProjectileDefinitionLoader.Get(ProjectileDefinition)!.Name;

		// TODO: projectile drawing
		public sealed override string Texture => "TerrariansConstructLib/Assets/DummyProjectile";

		public sealed override void SetStaticDefaults() {
			SafeSetStaticDefaults();

			DisplayName.SetDefault("Constructed " + ProjectileTypeName);
		}

		/// <inheritdoc cref="SetStaticDefaults"/>
		public virtual void SafeSetStaticDefaults() { }

		public sealed override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection) {
			modifiers.ModifyHitNPCWithProjectile(this, target, ref damage, ref knockback, ref crit, ref hitDirection);

			SafeModifyHitNPC(target, ref damage, ref knockback, ref crit, ref hitDirection);
		}

		/// <inheritdoc cref="ModifyHitNPC(NPC, ref int, ref float, ref bool, ref int)"/>
		public virtual void SafeModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection) { }

		public sealed override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) {
			modifiers.ModifyHitPlayerWithProjectile(this, target, ref damage, ref crit);
			
			SafeModifyHitPlayer(target, ref damage, ref crit);
		}

		public sealed override void ModifyHitPvp(Player target, ref int damage, ref bool crit) => ModifyHitPlayer(target, ref damage, ref crit);

		public virtual void SafeModifyHitPlayer(Player target, ref int damage, ref bool crit) { }

		public sealed override void OnHitNPC(NPC target, int damage, float knockback, bool crit) {
			modifiers.OnHitNPCWithProjectile(this, target, damage, knockback, crit);

			SafeOnHitNPC(target, damage, knockback, crit);
		}

		/// <inheritdoc cref="OnHitNPC(NPC, int, float, bool)"/>
		public virtual void SafeOnHitNPC(NPC target, int damage, float knockback, bool crit) { }

		public sealed override void OnHitPlayer(Player target, int damage, bool crit) {
			modifiers.OnHitPlayerWithProjectile(this, target, damage, crit);

			SafeOnHitPlayer(target, damage, crit);
		}

		public override void OnHitPvp(Player target, int damage, bool crit) => OnHitPlayer(target, damage, crit);

		/// <inheritdoc cref="OnHitPlayer(Player, int, bool)"/>
		public virtual void SafeOnHitPlayer(Player target, int damage, bool crit) { }

		public sealed override void AI() {
			SafeAI();
		}

		/// <inheritdoc cref="AI"/>
		public virtual void SafeAI() { }

		/// <summary>
		/// This hook lets you set the <see cref="SpriteEffects"/> used when drawing this <see cref="BaseTCProjectile"/> projectile.
		/// This hook runs after <see cref="Projectile.spriteDirection"/> is checked
		/// </summary>
		/// <param name="effects">The flipping effects</param>
		public virtual void SetSpriteEffects(ref SpriteEffects effects) { }

		/// <summary>
		/// This hook lets you override what texture is used for this <see cref="BaseTCProjectile"/> projectile.
		/// Defaults to the texture created from the <see cref="ItemPart"/> parts stored in the projectile.
		/// </summary>
		/// <param name="parts">The parts stored in this <see cref="BaseTCProjectile"/></param>
		/// <returns><see langword="null"/> to use the default texture, a <see cref="Texture2D"/> object otherwise</returns>
		public virtual Texture2D? GetTextureOverride(ReadOnlySpan<ItemPart> parts) => null;
	}
}
