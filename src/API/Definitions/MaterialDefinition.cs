using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Stats;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Modifiers;

namespace TerrariansConstructLib.API.Definitions {
	public abstract class MaterialDefinition : ModType {
		public int Type { get; private set; }

		/// <summary>
		/// Return the material for the definition here.  A return value of <see langword="null"/> indicates that the material is unloaded
		/// </summary>
		/// <returns></returns>
		public abstract Material? Material { get; }

		/// <summary>
		/// How many items of this material definition's material item must be used to count as one "unit" for creating parts.
		/// Defaults to 1
		/// </summary>
		public virtual int MaterialWorth => 1;

		/// <summary>
		/// Return the ability trait for the definition here.  A return value of <see langword="null"/> indicates that the ability trait is unloaded
		/// </summary>
		public abstract BaseTrait? Trait { get; }
		
		/// <summary>
		/// A collection of the valid part definitions that this material can be used with.
		/// Defaults to no part definitions
		/// </summary>
		public virtual IEnumerable<PartDefinition> ValidParts => Array.Empty<PartDefinition>();

		internal Material MaterialOrUnloaded => Material ?? new UnloadedMaterial();

		internal BaseTrait TraitOrUnloaded => Trait ?? GetTrait<UnloadedTrait>()!;

		/// <summary>
		/// Gets a clone of a registered BaseTrait's instance, or <see langword="null"/> if it does not exist
		/// </summary>
		/// <typeparam name="T">The trait type</typeparam>
		protected static T? GetTrait<T>() where T : BaseTrait => ModContent.GetInstance<T>()?.Clone() as T;

		protected sealed override void Register() {
			ModTypeLookup<MaterialDefinition>.Register(this);
			Type = MaterialDefinitionLoader.Add(this);
		}

		public sealed override void SetupContent() => SetStaticDefaults();

		/// <summary>
		/// Return the part stats for the material here.
		/// </summary>
		public virtual IEnumerable<IPartStats> GetMaterialStats() => Array.Empty<ExtraPartStats>();
	}
}
