using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.API.Sources;
using TerrariansConstructLib.DataStructures;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Projectiles;

namespace TerrariansConstructLib.Abilities {
	internal class AbilityCollection : IEnumerable<BaseAbility> {
		private class Member {
			public BaseAbility? singleton;

			public List<BaseAbility>? abilities;

			public List<TagCompound> unloadedData;

			public IEnumerable<BaseAbility> GetAbilities() => singleton is not null ? new BaseAbility[] { singleton } : abilities is not null ? abilities : Array.Empty<BaseAbility>();

			public Member Clone()
				=> new() {
					singleton = singleton?.Clone(),
					abilities = abilities?.Select(a => a.Clone()).ToList(),
					unloadedData = unloadedData
				};
		}

		private Dictionary<Material, Member> members = new();

		private AbilityCollection() { }

		public AbilityCollection(BaseTCItem tc) {
			var materials = tc.parts.DistinctBy(p => p.material.Type).Select(p => p.material);

			foreach (var material in materials) {
				BaseAbility? copy = registeredAbilities[material]?.Clone();

				if (!members.TryGetValue(material, out var member))
					member = members[material] = new();

				if (copy is null)
					continue;

				if (copy.IsSingleton) {
					if (member.singleton is null)
						member.singleton = copy;
				} else if (member.abilities is null)
					member.abilities = new() { copy };
				else
					member.abilities.Add(copy);
			}
		}

		public AbilityCollection Clone()
			=> new() { members = members.Select(k => k).ToDictionary(k => k.Key.Clone(), k => k.Value.Clone()) };

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

		internal void OnHitNPC(Player player, NPC target, BaseTCItem item, int damage, float knockBack, bool crit) => PerformActions(a => a.OnHitNPC(player, target, item, damage, knockBack, crit));

		internal void OnHitPlayer(Player owner, Player target, BaseTCItem item, int damage, bool crit) => PerformActions(a => a.OnHitPlayer(owner, target, item, damage, crit));

		internal void OnHitPlayerWithProjectile(BaseTCProjectile projectile, Player target, int damage, bool crit) => PerformActions(a => a.OnHitPlayerWithProjectile(projectile, target, damage, crit));

		internal void OnHitNPCWithProjectile(BaseTCProjectile projectile, NPC target, int damage, float knockBack, bool crit) => PerformActions(a => a.OnHitNPCWithProjectile(projectile, target, damage, knockBack, crit));

		internal void OnProjectileSpawn(BaseTCProjectile projectile, IEntitySource source, float X, float Y, float SpeedX, float SpeedY, int Type, int Damage, float KnockBack, int Owner, float ai0, float ai1)
			=> PerformActions(a => a.OnProjectileSpawn(projectile, source, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1));

		internal void ModifyWeaponDamage(Player player, BaseTCItem item, ref StatModifier damage, ref float flat) {
			StatModifier d = damage;
			float f = flat;

			PerformActions(a => a.ModifyWeaponDamage(player, item, ref d, ref f));

			damage = d;
			flat = f;
		}

		internal void ModifyWeaponKnockback(Player player, BaseTCItem item, ref StatModifier knockback, ref float flat) {
			StatModifier k = knockback;
			float f = flat;

			PerformActions(a => a.ModifyWeaponKnockback(player, item, ref k, ref f));

			knockback = k;
			flat = f;
		}
		
		internal void ModifyWeaponCrit(Player player, BaseTCItem item, ref int crit) {
			int c = crit;

			PerformActions(a => a.ModifyWeaponCrit(player, item, ref c));

			crit = c;
		}

		internal void PreModifyDurability(Player player, BaseTCItem item, IDurabilityModificationSource source, ref int amount) {
			int amt = amount;

			PerformActions(a => a.PreModifyDurability(player, item, source, ref amt));

			amount = amt;
		}

		internal void UseItem(Player player, BaseTCItem item) => PerformActions(a => a.UseItem(player, item));

		private void PerformActions(Action<BaseAbility> func) {
			foreach (var member in members.Values) {
				//No need to check for IsSingleton here, since that's handled in the ctor
				if (member.unloadedData is not null)
					continue;

				foreach (var ability in member.GetAbilities())
					func(ability);
			}
		}

		internal void SaveData(TagCompound tag) {
			List<TagCompound> list = new();
			foreach (var (material, member) in members) {
				if (member.unloadedData is not null) {
					list.AddRange(member.unloadedData);
					continue;
				}

				TagCompound data = new() {
					["material"] = material
				};

				List<BaseAbility>? values = new(member.GetAbilities());
				if (values.Count == 0)
					values = null;

				if (values is not null)  {
					data["singleton"] = values[0].IsSingleton;

					data["values"] = values.Select(
						a => {
							TagCompound tag = new();
							a.SaveData(tag);
							return tag;
						}).ToList();
				}

				list.Add(data);
			}

			tag["data"] = list;
		}

		internal void LoadData(TagCompound tag) {
			if (tag.GetList<TagCompound>("data") is var list) {
				Member unloaded = new();

				foreach (var data in list) {
					Material? material = data.Get<Material>("material");

					if (material is null || !registeredAbilities.ContainsKey(material)) {
						if (unloaded.unloadedData is null)
							unloaded.unloadedData = new();

						unloaded.unloadedData.Add(data);
						continue;
					}

					bool singleton = data.GetBool("singleton");

					if (data.GetList<TagCompound>("values") is var values) {
						if (singleton) {
							if (values.Count > 1)
								throw new IOException("Singleton entry expects only 1 value, multiple values detected");

							members[material].singleton!.LoadData(values[0]);
						} else {
							int index = 0;

							var memberList = members[material].abilities;

							if (memberList is not null) {
								if (values.Count != memberList.Count)
									throw new IOException($"Ability list count mistmatch (existing: {memberList.Count}, data: {values.Count})");

								foreach (var value in values) {
									memberList[index].LoadData(value);
									index++;
								}
							}
						}
					}
				}

				if (unloaded.unloadedData is not null)
					members.Add(CoreLibMod.RegisteredMaterials.Unloaded, unloaded);
			}
		}

		public IEnumerator<BaseAbility> GetEnumerator()
			=> members.Values
				.Where(m => m.unloadedData is null)
				.SelectMany(m => m.GetAbilities())
				.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		internal static Dictionary<Material, BaseAbility?> registeredAbilities;
	}
}
