using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.API;
using TerrariansConstructLib.API.Sources;
using TerrariansConstructLib.API.Stats;
using TerrariansConstructLib.DataStructures;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.Items {
	public class ItemPart : TagSerializable, INetHooks {
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Item"/>&#160;<paramref name="item"/>
		/// </summary>
		public delegate void PartItemFunc(int partID, Item item);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Player"/>&#160;<paramref name="player"/>,
		/// <see cref="Item"/>&#160;<paramref name="item"/>
		/// </summary>
		public delegate void PartPlayerFunc(int partID, Player player, Item item);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Projectile"/>&#160;<paramref name="projectile"/>
		/// </summary>
		public delegate void PartProjectileFunc(int partID, Projectile projectile);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Projectile"/>&#160;<paramref name="projectile"/>,
		/// <see cref="IEntitySource"/>&#160;<paramref name="source"/>,
		/// <see langword="float"/>&#160;<paramref name="X"/>,
		/// <see langword="float"/>&#160;<paramref name="Y"/>,
		/// <see langword="float"/>&#160;<paramref name="SpeedX"/>,
		/// <see langword="float"/>&#160;<paramref name="SpeedY"/>,
		/// <see langword="int"/>&#160;<paramref name="Type"/>,
		/// <see langword="int"/>&#160;<paramref name="Damage"/>,
		/// <see langword="float"/>&#160;<paramref name="KnockBack"/>,
		/// <see langword="int"/>&#160;<paramref name="Owner"/>,
		/// <see langword="float"/>&#160;<paramref name="ai0"/>,
		/// <see langword="float"/>&#160;<paramref name="ai1"/>
		/// </summary>
		public delegate void PartProjectileSpawnFunc(int partID, Projectile projectile, IEntitySource source, float X, float Y, float SpeedX, float SpeedY, int Type, int Damage, float KnockBack, int Owner, float ai0, float ai1);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Projectile"/>&#160;<paramref name="projectile"/>,
		/// <see cref="NPC"/>&#160;<paramref name="target"/>,
		/// <see langword="int"/>&#160;<paramref name="damage"/>,
		/// <see langword="float"/>&#160;<paramref name="knockback"/>,
		/// <see langword="bool"/>&#160;<paramref name="crit"/>
		/// </summary>
		public delegate void PartProjectileHitNPCFunc(int partID, Projectile projectile, NPC target, int damage, float knockback, bool crit);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Projectile"/>&#160;<paramref name="projectile"/>,
		/// <see cref="Player"/>&#160;<paramref name="target"/>,
		/// <see langword="int"/>&#160;<paramref name="damage"/>,
		/// <see langword="bool"/>&#160;<paramref name="crit"/>
		/// </summary>
		public delegate void PartProjectileHitPlayerFunc(int partID, Projectile projectile, Player target, int damage, bool crit);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Player"/>&#160;<paramref name="player"/>,
		/// <see cref="StatModifier"/>&#160;<paramref name="damage"/>,
		/// <see langword="ref float"/>&#160;<paramref name="flat"/>
		/// </summary>
		public delegate void PartModifyWeaponDamageFunc(int partID, Player player, ref StatModifier damage, ref float flat);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Player"/>&#160;<paramref name="player"/>,
		/// <see cref="StatModifier"/>&#160;<paramref name="knockback"/>,
		/// <see langword="ref float"/>&#160;<paramref name="flat"/>
		/// </summary>
		public delegate void PartModifyWeaponKnockbackFunc(int partID, Player player, ref StatModifier knockback, ref float flat);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Player"/>&#160;<paramref name="player"/>,
		/// <see langword="ref int"/>&#160;<paramref name="crit"/>
		/// </summary>
		public delegate void PartModifyWeaponCritFunc(int partID, Player player, ref int crit);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Player"/>&#160;<paramref name="player"/>,
		/// <see cref="Item"/>&#160;<paramref name="item"/>,
		/// <see cref="TileDestructionContext"/>&#160;<paramref name="context"/>,
		/// <see langword="ref int"/>&#160;<paramref name="power"/>
		/// </summary>
		public delegate void PartToolPowerFunc(int partID, Player player, Item item, TileDestructionContext context, ref int power);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Player"/>&#160;<paramref name="player"/>,
		/// <see cref="Item"/>&#160;<paramref name="item"/>,
		/// <see langword="int"/>&#160;<paramref name="x"/>
		/// <see langword="int"/>&#160;<paramref name="y"/>
		/// <see cref="TileDestructionContext"/>&#160;<paramref name="context"/>
		/// </summary>
		public delegate void PartTileDestructionFunc(int partID, Player player, Item item, int x, int y, TileDestructionContext context);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Item"/>&#160;<paramref name="item"/>,
		/// <see cref="Player"/>&#160;<paramref name="owner"/>,
		/// <see cref="NPC"/>&#160;<paramref name="target"/>,
		/// <see langword="int"/>&#160;<paramref name="damage"/>,
		/// <see langword="float"/>&#160;<paramref name="knockback"/>,
		/// <see langword="bool"/>&#160;<paramref name="crit"/>
		/// </summary>
		public delegate void PartItemHitNPCFunc(int partID, Item item, Player owner, NPC target, int damage, float knockback, bool crit);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Item"/>&#160;<paramref name="item"/>,
		/// <see cref="Player"/>&#160;<paramref name="owner"/>,
		/// <see cref="Player"/>&#160;<paramref name="target"/>,
		/// <see langword="int"/>&#160;<paramref name="damage"/>,
		/// <see langword="bool"/>&#160;<paramref name="crit"/>
		/// </summary>
		public delegate void PartItemHitPlayerFunc(int partID, Item item, Player owner, Player target, int damage, bool crit);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Item"/>&#160;<paramref name="item"/>,
		/// <see cref="Player"/>&#160;<paramref name="player"/>,
		/// <see langword="ref float"/>&#160;<paramref name="multiplier"/>
		/// </summary>
		public delegate void PartItemUseSpeedMultiplier(int partID, Item item, Player player, ref float multiplier);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Player"/>&#160;<paramref name="player"/>,
		/// <see cref="Item"/>&#160;<paramref name="item"/>,
		/// <see cref="IDurabilityModificationSource"/>&#160;<paramref name="source"/>
		/// </summary>
		public delegate bool PartCanLoseDurability(int partID, Player player, Item item, IDurabilityModificationSource source);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Player"/>&#160;<paramref name="player"/>,
		/// <see cref="NPC"/>&#160;<paramref name="target"/>,
		/// <see langword="ref int"/>&#160;<paramref name="damage"/>,
		/// <see langword="ref float"/>&#160;<paramref name="knockBack"/>,
		/// <see langword="ref bool"/>&#160;<paramref name="crit"/>
		/// </summary>
		public delegate void PartModifyHitNPCFunc(int partID, Player player, NPC target, ref int damage, ref float knockBack, ref bool crit);
		/// <summary>
		/// <see langword="int"/>&#160;<paramref name="partID"/>,
		/// <see cref="Player"/>&#160;<paramref name="player"/>,
		/// <see cref="Item"/>&#160;<paramref name="item"/>,
		/// <see cref="IDurabilityModificationSource"/>&#160;<paramref name="source"/>,
		/// <see langword="ref int"/>&#160;<paramref name="amount"/>
		/// </summary>
		public delegate bool PartPreModifyDurability(int partID, Player player, Item item, IDurabilityModificationSource source, ref int amount);

		internal static PartsDictionary<ItemPart> partData;

		public IPartStats? GetStat(StatType type)
			=> material.GetStat(type);

		public T? GetStat<T>(StatType type) where T : class, IPartStats
			=> material.GetStat<T>(type);

		/// <summary>
		/// The material used to create this item part
		/// </summary>
		public Material material;

		/// <summary>
		/// The part type associated with this item part
		/// </summary>
		public int partID;

		public virtual ItemPart Clone() => new(){
			material = material.Clone(),
			partID = partID
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

		public PartCanLoseDurability? CanLoseDurability => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).canLoseDurability;

		public PartModifyHitNPCFunc? ModifyHitNPC => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).modifyHitNPC;

		/// <remarks>If the <c>amount</c> parameter is &lt; 0, then the modification was a durability removal, otherwise it's a durability addition</remarks>
		public PartPreModifyDurability? PreModifyDurability => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).PreModifyDurability;

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
