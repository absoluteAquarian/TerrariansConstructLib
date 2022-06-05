﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.API;
using TerrariansConstructLib.API.Sources;
using TerrariansConstructLib.DataStructures;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Projectiles;

namespace TerrariansConstructLib.Modifiers {
	public sealed class ModifierCollection : IEnumerable<BaseTrait>, INetHooks {
		private class Member {
			public BaseTrait? singleton;

			public List<BaseTrait>? modifiers;

			public List<TagCompound> unloadedData;

			public IEnumerable<BaseTrait> GetModifiers() => singleton is not null ? new BaseTrait[] { singleton } : modifiers is not null ? modifiers : Array.Empty<BaseTrait>();

			public Member Clone()
				=> new() {
					singleton = singleton?.Clone(),
					modifiers = modifiers?.Select(a => a.Clone()).ToList(),
					unloadedData = unloadedData
				};
		}

		private Dictionary<string, Member> members = new();

		internal static Dictionary<string, BaseTrait> registeredModifiers;
		internal static List<string> idToIdentifier;

		private ModifierCollection() { }

		public ModifierCollection(BaseTCItem tc) {
			var materials = tc.parts.DistinctBy(p => p.material.Type).Select(p => p.material);

			foreach (var material in materials) {
				string identifier = material.GetIdentifier();

				BaseTrait? copy = registeredModifiers[identifier]?.Clone();

				if (!members.TryGetValue(identifier, out var member))
					member = members[identifier] = new();

				if (copy is null)
					continue;

				//Tier assignment should only happen if the instance is a BaseTrait and not a BaseModifier, since BaseModifier overwrites it
				if (copy.IsSingleton) {
					if (member.singleton is null)
						member.singleton = copy;

					if (member.singleton is not BaseModifier && member.singleton.IsEquivalentForTier(copy.GetType(), out uint worth))
						member.singleton.Tier += (int)worth;
				} else if (member.modifiers is null)
					member.modifiers = new() { copy };
				else
					member.modifiers.Add(copy);

				if (!copy.IsSingleton) {
					foreach (var modifier in member.modifiers!)
						if (modifier is not BaseModifier && modifier.IsEquivalentForTier(copy.GetType(), out uint worth))
							modifier.Tier += (int)worth;
				}
			}
		}

		public ModifierCollection Clone()
			=> new() { members = members.Select(k => k).ToDictionary(k => k.Key, k => k.Value.Clone()) };

		/// <summary>
		/// Adds a modifier to the collection
		/// </summary>
		/// <param name="id">The ID for the modifier</param>
		/// <returns>The modifier instance</returns>
		/// <exception cref="Exception"/>
		public BaseModifier AddModifier(int id)
			=> AddModifier(CoreLibMod.GetModifierIdentifier(id));
		
		/// <summary>
		/// Adds a modifier to the collection
		/// </summary>
		/// <param name="identifier">The identifier for the modifier</param>
		/// <returns>The modifier instance</returns>
		/// <exception cref="Exception"/>
		public BaseModifier AddModifier(string identifier) {
			var instance = registeredModifiers[identifier].Clone();

			if (instance is not BaseModifier)
				throw new Exception("Cannot add traits to a modifier collection once it's been initialized");

			if(!members.TryGetValue(identifier, out var member))
				member = members[identifier] = new();

			BaseModifier modifier = ((member.singleton ??= instance) as BaseModifier)!;

			//Increase the tier in the singleton
			modifier.Tier++;

			return modifier;
		}

		internal void Update(Player player) => PerformActions(a => a.Update(player));

		internal void UpdateInventory(Player player, BaseTCItem item) => PerformActions(a => a.OnUpdateInventory(player, item));

		internal void HoldItem(Player player, BaseTCItem item) => PerformActions(a => a.OnHoldItem(player, item));

		internal void OnHotkeyPressed(Player player) => PerformActions(a => a.OnAbilityHotkeyPressed(player));

		internal void UseSpeedMultiplier(Player player, BaseTCItem item, ref float multiplier) {
			float mult = multiplier;

			PerformActions(a => a.UseSpeedMultiplier(player, item, ref mult));

			multiplier = mult;
		}

		internal void ModifyToolPower(Player player, BaseTCItem item, TileDestructionContext context, ref int power) {
			int pwr = power;

			PerformActions(a => a.ModifyToolPower(player, item, context, ref pwr));

			power = pwr;
		}
		
		internal void OnTileDestroyed(Player player, BaseTCItem item, int x, int y, TileDestructionContext context) => PerformActions(a => a.OnTileDestroyed(player, item, x, y, context));

		internal bool CanLoseDurability(Player player, BaseTCItem item, IDurabilityModificationSource source) {
			bool lose = true;

			PerformActions(a => lose &= a.CanLoseDurability(player, item, source));

			return lose;
		}

		internal void ModifyHitNPC(Player player, NPC target, BaseTCItem item, ref int damage, ref float knockBack, ref bool crit) {
			int d = damage;
			float k = knockBack;
			bool c = crit;

			PerformActions(a => a.ModifyHitNPC(player, target, item, ref d, ref k, ref c));

			damage = d;
			knockBack = k;
			crit = c;
		}

		internal void ModifyHitNPCWithProjectile(BaseTCProjectile projectile, NPC target, ref int damage, ref float knockBack, ref bool crit, ref int hitDirection) {
			int d = damage;
			float k = knockBack;
			bool c = crit;
			int h = hitDirection;

			PerformActions(a => a.ModifyHitNPCWithProjectile(projectile, target, ref d, ref k, ref c, ref h));

			damage = d;
			knockBack = k;
			crit = c;
			hitDirection = h;
		}

		internal void ModifyHitPlayer(Player player, Player target, BaseTCItem item, ref int damage, ref bool crit) {
			int d = damage;
			bool c = crit;

			PerformActions(a => a.ModifyHitPlayer(player, target, item, ref d, ref c));

			damage = d;
			crit = c;
		}

		internal void ModifyHitPlayerWithProjectile(BaseTCProjectile projectile, Player target, ref int damage, ref bool crit) {
			int d = damage;
			bool c = crit;

			PerformActions(a => a.ModifyHitPlayerWithProjectile(projectile, target, ref d, ref c));

			damage = d;
			crit = c;
		}

		internal void OnHitNPC(Player player, NPC target, BaseTCItem item, int damage, float knockBack, bool crit) => PerformActions(a => a.OnHitNPC(player, target, item, damage, knockBack, crit));

		internal void OnHitPlayer(Player owner, Player target, BaseTCItem item, int damage, bool crit) => PerformActions(a => a.OnHitPlayer(owner, target, item, damage, crit));

		internal void OnHitPlayerWithProjectile(BaseTCProjectile projectile, Player target, int damage, bool crit) => PerformActions(a => a.OnHitPlayerWithProjectile(projectile, target, damage, crit));

		internal void OnHitNPCWithProjectile(BaseTCProjectile projectile, NPC target, int damage, float knockBack, bool crit) => PerformActions(a => a.OnHitNPCWithProjectile(projectile, target, damage, knockBack, crit));

		internal void OnProjectileSpawn(BaseTCProjectile projectile, IEntitySource source)
			=> PerformActions(a => a.OnProjectileSpawn(projectile, source));

		internal void ModifyWeaponDamage(Player player, BaseTCItem item, ref StatModifier damage) {
			StatModifier d = damage;

			PerformActions(a => a.ModifyWeaponDamage(player, item, ref d));

			damage = d;
		}

		internal void ModifyWeaponKnockback(Player player, BaseTCItem item, ref StatModifier knockback) {
			StatModifier k = knockback;

			PerformActions(a => a.ModifyWeaponKnockback(player, item, ref k));

			knockback = k;
		}
		
		internal void ModifyWeaponCrit(Player player, BaseTCItem item, ref float crit) {
			float c = crit;

			PerformActions(a => a.ModifyWeaponCrit(player, item, ref c));

			crit = c;
		}

		internal void PreModifyDurability(Player player, BaseTCItem item, IDurabilityModificationSource source, ref int amount) {
			int amt = amount;

			PerformActions(a => a.PreModifyDurability(player, item, source, ref amt));

			amount = amt;
		}

		internal void UseItem(Player player, BaseTCItem item) => PerformActions(a => a.UseItem(player, item));

		internal bool CanConsumeAmmo(BaseTCItem weapon, BaseTCItem ammo, Player player) {
			bool consume = true;

			PerformActions(a => consume &= a.CanConsumeAmmo(weapon, ammo, player));

			return consume;
		}

		private void PerformActions(Action<BaseTrait> func) {
			foreach (var member in members.Values) {
				//No need to check for IsSingleton here, since that's handled in the ctor
				if (member.unloadedData is not null)
					continue;

				foreach (var ability in member.GetModifiers())
					func(ability);
			}
		}

		//Used to keep track of when SaveData changes to force no data to load
		private const int SAVE_VERSION = 2;

		internal void SaveData(TagCompound tag) {
			List<TagCompound> list = new();
			foreach (var (identifier, member) in members) {
				static TagCompound GetTag(BaseTrait trait) {
					TagCompound tag = new();
					trait.SaveData(tag);
					return tag;
				}

				if (member.unloadedData is not null) {
					list.AddRange(member.unloadedData);
					continue;
				}

				TagCompound data = new() {
					["id"] = identifier
				};

				if (member.singleton is not null) {
					data["singleton"] = true;

					data["instance"] = GetTag(member.singleton);
				} else {
					List<BaseTrait> values = new(member.GetModifiers());
					
					if (values.Count > 0)  {
						data["singleton"] = false;
						
						data["values"] = values.Select(GetTag).ToList();
					}
				}					

				list.Add(data);
			}

			tag["data"] = list;
			tag[nameof(SAVE_VERSION)] = SAVE_VERSION;
		}

		internal void LoadData(TagCompound tag) {
			//Load SAVE_VERSION first, so we can skip the rest if it's not the same
			if (!tag.ContainsKey(nameof(SAVE_VERSION)) || tag.GetInt(nameof(SAVE_VERSION)) != SAVE_VERSION) {
				CoreLibMod.Instance.Logger.Warn("Save data version mismatch in " + nameof(ModifierCollection) + ", skipping");
				return;
			}

			if (tag.GetList<TagCompound>("data") is var list) {
				Member unloaded = new();

				foreach (var data in list) {
					string? identifier = data.GetString("id");

					if (identifier is null)
						throw new IOException("Identifier expected in tag, none found");

					if (!registeredModifiers.ContainsKey(identifier)) {
						if (unloaded.unloadedData is null)
							unloaded.unloadedData = new();

						var split = identifier.Split(':');
						var instance = new UnloadedTrait() {
							mod = split[0],
							name = split[1]
						};
						
						if (unloaded.modifiers is null)
							unloaded.modifiers = new() { instance };
						else
							unloaded.modifiers.Add(instance);

						unloaded.unloadedData.Add(data);
						continue;
					}

					bool singleton = data.GetBool("singleton");

					if (singleton)
						(members[identifier].singleton ??= CoreLibMod.GetModifier(identifier)).LoadData(data.GetCompound("instance"));
					else if (data.GetList<TagCompound>("values") is var values) {
						int index = 0;

						var memberList = members[identifier].modifiers;

						if (memberList is not null) {
							if (values.Count != memberList.Count)
								throw new IOException($"Modifier list count mistmatch (existing: {memberList.Count}, data: {values.Count})");

							foreach (var value in values) {
								memberList[index].LoadData(value);
								index++;
							}
						}
					} else
						throw new IOException("Could not read NBT structure");
				}

				if (unloaded.unloadedData is not null)
					members.Add(CoreLibMod.RegisteredMaterials.Unloaded.GetIdentifier(), unloaded);
			}
		}

		public IEnumerator<BaseTrait> GetEnumerator()
			=> members.Values
				.SelectMany(m => m.GetModifiers())
				.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		public void NetSend(BinaryWriter writer) {
			writer.Write(members.Count);
			foreach(var (identifier, member) in members) {
				writer.Write(identifier);

				var modifiers = member.GetModifiers().ToList();
				writer.Write(modifiers.Count);
				foreach(var modifier in modifiers)
					modifier.NetSend(writer);
			}
		}

		public void NetReceive(BinaryReader reader) {
			int count = reader.ReadInt32();
			for(int i = 0; i < count; i++) {
				string identifier = reader.ReadString();
				int abilityCount = reader.ReadInt32();

				var member = new Member();
				members.Add(identifier, member);

				for(int j = 0; j < abilityCount; j++) {
					var modifier = registeredModifiers[identifier].Clone();

					if (modifier.IsSingleton && abilityCount > 1)
						throw new IOException("Singleton entry expects only 1 value, multiple values detected");

					modifier.NetReceive(reader);

					if (modifier.IsSingleton)
						member.singleton = modifier;
					else if (member.modifiers is null)
						member.modifiers = new() { modifier };
					else
						member.modifiers.Add(modifier);
				}
			}
		}
	}
}