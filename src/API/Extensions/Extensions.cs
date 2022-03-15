using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariansConstructLib.API {
	public static class Extensions {
		public static void DefaultToMeleeWeapon(this Item item, int singleUseTime, int useStyle, bool useTurn = true, int projOnSwing = ProjectileID.None, float projSpeed = 0f) {
			item.DamageType = DamageClass.Melee;
			item.useTime = item.useAnimation = singleUseTime;
			item.useStyle = useStyle;
			item.useTurn = useTurn;

			if (projOnSwing > ProjectileID.None) {
				item.shoot = projOnSwing;
				item.shootSpeed = projSpeed;
			}
		}

		/// <summary>
		/// Returns a string representing how you'd type a Type in C#, e.g. <c>Asset&lt;Texture2D&gt;</c>
		/// </summary>
		/// <param name="type">The type instance</param>
		public static string? GetSimplifiedGenericTypeName(this Type type) {
			//Handle all invalid cases here:
			if (type.FullName is null)
				return type.Name;

			if (!type.IsGenericType)
				return type.FullName;

			string parent = type.GetGenericTypeDefinition().FullName!;

			//Include all but the "`X" part
			parent = parent[..parent.IndexOf('`')];

			//Construct the child types
			return $"{parent}<{string.Join(", ", type.GetGenericArguments().Select(GetSimplifiedGenericTypeName))}>";
		}
	}
}
