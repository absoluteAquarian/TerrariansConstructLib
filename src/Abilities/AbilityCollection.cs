using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.DataStructures;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Materials;

namespace TerrariansConstructLib.Abilities {
	internal class AbilityCollection : IEnumerable<BaseAbility> {
		private class Member {
			public BaseAbility? singleton;

			public List<BaseAbility>? abilities;

			public List<TagCompound> unloadedData;
		}

		private readonly Dictionary<Material, Member> members = new();

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

		internal void Update(Player player) => PerformActions(player, (a, p) => a.Update(p));

		internal void UpdateInventory(Player player, BaseTCItem item) => PerformActions(player, (a, p) => a.OnUpdateInventory(p, item));

		internal void HoldItem(Player player, BaseTCItem item) => PerformActions(player, (a, p) => a.OnHoldItem(p, item));

		internal void OnHotkeyPressed(Player player) => PerformActions(player, (a, p) => a.OnAbilityHotkeyPressed(p));

		internal void UseSpeedMultiplier(Player player, BaseTCItem item, ref float multiplier) {
			float mult = multiplier;

			PerformActions(player, (a, p) => a.UseSpeedMultiplier(p, item, ref mult));

			multiplier = mult;
		}

		internal void OnTileDestroyed(Player player, BaseTCItem item, int x, int y, TileDestructionContext context) => PerformActions(player, (a, p) => a.OnTileDestroyed(p, item, x, y, context));

		private void PerformActions(Player player, Action<BaseAbility, Player> func) {
			foreach (var member in members.Values) {
				//No need to check for IsSingleton here, since that's handled in the ctor
				if (member.unloadedData is not null)
					continue;

				if (member.singleton is not null)
					func(member.singleton, player);
				else if (member.abilities is not null) {
					foreach (var ability in member.abilities)
						func(ability, player);
				}
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

				List<BaseAbility>? values = member.singleton is not null ? new() { member.singleton } : member.abilities;

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
				.SelectMany(m => m.singleton is not null ? new BaseAbility[] { m.singleton } : m.abilities is not null ? m.abilities.ToArray() : Array.Empty<BaseAbility>())
				.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		internal static Dictionary<Material, BaseAbility?> registeredAbilities;
	}
}
