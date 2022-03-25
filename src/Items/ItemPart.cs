using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.API;
using TerrariansConstructLib.DataStructures;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.Items {
	public class ItemPart : TagSerializable, INetHooks {
		/// <summary>
		/// <see langword="int"/> <paramref name="partID"/>,
		/// <see cref="Item"/> <paramref name="item"/>
		/// </summary>
		public delegate void PartItemFunc(int partID, Item item);
		/// <summary>
		/// <see langword="int"/> <paramref name="partID"/>,
		/// <see cref="Player"/> <paramref name="player"/>,
		/// <see cref="Item"/> <paramref name="item"/>
		/// </summary>
		public delegate void PartPlayerFunc(int partID, Player player, Item item);
		/// <summary>
		/// <see langword="int"/> <paramref name="partID"/>,
		/// <see cref="Projectile"/> <paramref name="projectile"/>
		/// </summary>
		public delegate void PartProjectileFunc(int partID, Projectile projectile);
		/// <summary>
		/// <see langword="int"/> <paramref name="partID"/>,
		/// <see cref="Projectile"/> <paramref name="projectile"/>,
		/// <see cref="IEntitySource"/> <paramref name="source"/>,
		/// <see langword="float"/> <paramref name="X"/>,
		/// <see langword="float"/> <paramref name="Y"/>,
		/// <see langword="float"/> <paramref name="SpeedX"/>,
		/// <see langword="float"/> <paramref name="SpeedY"/>,
		/// <see langword="int"/> <paramref name="Type"/>,
		/// <see langword="int"/> <paramref name="Damage"/>,
		/// <see langword="float"/> <paramref name="KnockBack"/>,
		/// <see langword="int"/> <paramref name="Owner"/>,
		/// <see langword="float"/> <paramref name="ai0"/>,
		/// <see langword="float"/> <paramref name="ai1"/>
		/// </summary>
		public delegate void PartProjectileSpawnFunc(int partID, Projectile projectile, IEntitySource source, float X, float Y, float SpeedX, float SpeedY, int Type, int Damage, float KnockBack, int Owner, float ai0, float ai1);
		/// <summary>
		/// <see langword="int"/> <paramref name="partID"/>,
		/// <see cref="Projectile"/> <paramref name="projectile"/>,
		/// <see cref="NPC"/> <paramref name="target"/>,
		/// <see langword="int"/> <paramref name="damage"/>,
		/// <see langword="float"/> <paramref name="knockback"/>,
		/// <see langword="bool"/> <paramref name="crit"/>
		/// </summary>
		public delegate void PartProjectileHitNPCFunc(int partID, Projectile projectile, NPC target, int damage, float knockback, bool crit);
		/// <summary>
		/// <see langword="int"/> <paramref name="partID"/>,
		/// <see cref="Projectile"/> <paramref name="projectile"/>,
		/// <see cref="Player"/> <paramref name="target"/>,
		/// <see langword="int"/> <paramref name="damage"/>,
		/// <see langword="bool"/> <paramref name="crit"/>
		/// </summary>
		public delegate void PartProjectileHitPlayerFunc(int partID, Projectile projectile, Player target, int damage, bool crit);
		/// <summary>
		/// <see langword="int"/> <paramref name="partID"/>,
		/// <see cref="Player"/> <paramref name="player"/>,
		/// <see cref="StatModifier"/> <paramref name="damage"/>,
		/// <see langword="ref float"/> <paramref name="flat"/>
		/// </summary>
		public delegate void PartModifyWeaponDamageFunc(int partID, Player player, ref StatModifier damage, ref float flat);
		/// <summary>
		/// <see langword="int"/> <paramref name="partID"/>,
		/// <see cref="Player"/> <paramref name="player"/>,
		/// <see cref="StatModifier"/> <paramref name="knockback"/>,
		/// <see langword="ref float"/> <paramref name="flat"/>
		/// </summary>
		public delegate void PartModifyWeaponKnockbackFunc(int partID, Player player, ref StatModifier knockback, ref float flat);
		/// <summary>
		/// <see langword="int"/> <paramref name="partID"/>,
		/// <see cref="Player"/> <paramref name="player"/>,
		/// <see langword="ref int"/> <paramref name="crit"/>
		/// </summary>
		public delegate void PartModifyWeaponCritFunc(int partID, Player player, ref int crit);
		/// <summary>
		/// <see langword="int"/> <paramref name="partID"/>,
		/// <see cref="Player"/> <paramref name="player"/>,
		/// <see cref="Item"/> <paramref name="item"/>,
		/// <see cref="TileDestructionContext"/> <paramref name="context"/>,
		/// <see langword="ref int"/> <paramref name="power"/>
		/// </summary>
		public delegate void PartToolPowerFunc(int partID, Player player, Item item, TileDestructionContext context, ref int power);
		/// <summary>
		/// <see langword="int"/> <paramref name="partID"/>,
		/// <see cref="Player"/> <paramref name="player"/>,
		/// <see cref="Item"/> <paramref name="item"/>,
		/// <see langword="int"/> <paramref name="x"/>
		/// <see langword="int"/> <paramref name="y"/>
		/// <see cref="TileDestructionContext"/> <paramref name="context"/>
		/// </summary>
		public delegate void PartTileDestructionFunc(int partID, Player player, Item item, int x, int y, TileDestructionContext context);
		/// <summary>
		/// <see langword="int"/> <paramref name="partID"/>,
		/// <see cref="Item"/> <paramref name="item"/>,
		/// <see cref="Player"/> <paramref name="owner"/>,
		/// <see cref="NPC"/> <paramref name="target"/>,
		/// <see langword="int"/> <paramref name="damage"/>,
		/// <see langword="float"/> <paramref name="knockback"/>,
		/// <see langword="bool"/> <paramref name="crit"/>
		/// </summary>
		public delegate void PartItemHitNPCFunc(int partID, Item item, Player owner, NPC target, int damage, float knockback, bool crit);
		/// <summary>
		/// <see langword="int"/> <paramref name="partID"/>,
		/// <see cref="Item"/> <paramref name="item"/>,
		/// <see cref="Player"/> <paramref name="owner"/>,
		/// <see cref="Player"/> <paramref name="target"/>,
		/// <see langword="int"/> <paramref name="damage"/>,
		/// <see langword="bool"/> <paramref name="crit"/>
		/// </summary>
		public delegate void PartItemHitPlayerFunc(int partID, Item item, Player owner, Player target, int damage, bool crit);
		/// <summary>
		/// <see langword="int"/> <paramref name="partID"/>,
		/// <see cref="Item"/> <paramref name="item"/>,
		/// <see cref="Player"/> <paramref name="player"/>,
		/// <see langword="ref float"/> <paramref name="multiplier"/>
		/// </summary>
		public delegate void PartItemUseSpeedMultiplier(int partID, Item item, Player player, ref float multiplier);

		internal static PartsDictionary<ItemPart> partData;

		public void SetGlobalTooltip(string tooltip)
			=> SetGlobalTooltip(material, partID, tooltip);

		/// <summary>
		/// Sets the global tooltip for item parts using the material, <paramref name="material"/>, and the part ID, <paramref name="partID"/>, to <paramref name="tooltip"/>
		/// </summary>
		/// <param name="material">The material</param>
		/// <param name="partID">The part ID</param>
		/// <param name="tooltip">The new global tooltip</param>
		public static void SetGlobalTooltip(Material material, int partID, string tooltip) {
			//Unloaded/Unknown material should not be tampered with
			if (material is not UnloadedMaterial or UnknownMaterial)
				partData.Get(material, partID).tooltip = tooltip;
		}

		public ModifierText? GetModifierText()
			=> GetGlobalModifierText(material, partID);

		/// <summary>
		/// Gets the global modifier text for item parts using the material, <paramref name="material"/>, and the part ID, <paramref name="partID"/>
		/// </summary>
		/// <param name="material">The material</param>
		/// <param name="partID">The part ID</param>
		public static ModifierText? GetGlobalModifierText(Material material, int partID)
			=> partData.Get(material, partID).modifierText;

		/// <summary>
		/// The material used to create this item part
		/// </summary>
		public Material material;

		/// <summary>
		/// The part type associated with this item part
		/// </summary>
		public int partID;

		internal string? tooltip;

		internal ModifierText? modifierText;

		public virtual ItemPart Clone() => new(){
			material = material.Clone(),
			partID = partID,
			tooltip = tooltip,
			modifierText = modifierText
		};

		public PartItemFunc? OnInitialized => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onInitialized;

		public PartProjectileFunc? SetProjectileDefaults => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).setProjectileDefaults;

		public PartPlayerFunc? OnUse => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onUse;

		public PartPlayerFunc? OnHold => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onHold;

		public PartPlayerFunc? OnGenericHotkeyUsage => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onGenericHotkeyUsage;

		public PartProjectileSpawnFunc? OnProjectileSpawn => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onProjectileSpawn;

		public PartProjectileHitNPCFunc? OnProjectileHitNPC => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onProjectileHitNPC;

		public PartProjectileHitPlayerFunc? OnProjectileHitPlayer => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onProjectileHitPlayer;

		public PartModifyWeaponDamageFunc? ModifyWeaponDamage => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).modifyWeaponDamage;

		public PartModifyWeaponKnockbackFunc? ModifyWeaponKnockback => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).modifyWeaponKnockback;

		public PartModifyWeaponCritFunc? ModifyWeaponCrit => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).modifyWeaponCrit;

		public PartProjectileFunc? ProjectileAI => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).projectileAI;

		public PartToolPowerFunc? ModifyToolPower => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).modifyToolPower;

		public PartTileDestructionFunc? OnTileDestroyed => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onTileDestroyed;

		public PartItemHitNPCFunc? OnItemHitNPC => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onItemHitNPC;

		public PartItemHitPlayerFunc? OnItemHitPlayer => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onItemHitPlayer;

		public PartItemUseSpeedMultiplier? UseSpeedMultiplier => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).useSpeedMultiplier;

		public PartPlayerFunc? OnUpdateInventory => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onUpdateInventory;

		public TagCompound SerializeData() {
			TagCompound tag = new();
			
			tag["material"] = material;

			if (this is UnloadedItemPart u) {
				tag["part"] = new TagCompound() {
					["mod"] = u.mod,
					["name"] = u.internalName
				};

				return tag;
			}

			var data = PartRegistry.registeredIDs[partID];

			tag["part"] = new TagCompound() {
				["mod"] = data.mod.Name,
				["name"] = data.internalName
			};

			return tag;
		}

		public static Func<TagCompound, ItemPart> DESERIALIZER = tag => {
			Material material = tag.Get<Material>("material");

			if (material is null)
				material = tag.Get<UnloadedMaterial>("material");

			TagCompound part = tag.GetCompound("part");

			string modName = part.GetString("mod");
			string internalName = part.GetString("name");

			if (!ModLoader.TryGetMod(modName, out var mod) || !PartRegistry.TryFindData(mod, internalName, out int id)) {
				// Unloaded part.  Save the mod and name, but nothing else
				return new UnloadedItemPart() {
					mod = modName,
					internalName = internalName,
					partID = -1,
					material = material
				};
			}

			return (ItemPart)partData!.Get(material, id).MemberwiseClone();
		};

		public void NetSend(BinaryWriter writer) {
			writer.Write(material.Type);
			material.NetSend(writer);
			writer.Write(partID);
		}

		public void NetReceive(BinaryReader reader) {
			material = Material.FromItem(reader.ReadInt32());
			material.NetReceive(reader);
			partID = reader.ReadInt32();

			tooltip = partData.Get(material, partID).tooltip;
			modifierText = partData.Get(material, partID).modifierText;
		}

		public override bool Equals(object? obj)
			=> obj is ItemPart part && material.Type == part.material.Type && partID == part.partID;

		public override int GetHashCode()
			=> HashCode.Combine(material.Type, partID);

		public static bool operator ==(ItemPart left, ItemPart right)
			=> left.material.Type == right.material.Type && left.partID == right.partID;

		public static bool operator !=(ItemPart left, ItemPart right)
			=> !(left == right);
	}
}
