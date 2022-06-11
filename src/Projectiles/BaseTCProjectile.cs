using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Reflection;
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
		internal int itemSource_registeredItemID = -1;

		protected ReadOnlySpan<ItemPart> GetParts() => parts;

		public T? GetModifier<T>() where T : BaseTrait
			=> modifiers.FirstOrDefault(t => t.GetType() == typeof(T)) as T;

		public int CountParts(Material material)
			=> parts.Count(p => p.material.Type == material.Type);

		protected ItemPart GetPart(int index) => parts[index];

		/// <summary>
		/// The name for the projectile, used in <see cref="SetStaticDefaults"/><br/>
		/// Defaults to: <c>"Projectile"</c>
		/// </summary>
		public virtual string ProjectileTypeName => "Projectile";

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

		public sealed override bool PreDraw(ref Color lightColor) {
			SafePreDraw(ref lightColor);

			// TODO: cached projectile textures
			Texture2D texture = CoreLibMod.ItemTextures.Get(itemSource_registeredItemID, parts, modifiers.ToArray());

			//Mimick the normal projectile drawing code
			int num136 = 0;
			int num137 = 0;
			float num138 = (texture.Width - Projectile.width) * 0.5f + Projectile.width * 0.5f;

			if (Projectile.bobber)
				num136 = 8;

			ProjectileLoader.DrawOffset(Projectile, ref num137, ref num136, ref num138);
			
			SpriteEffects spriteEffects = SpriteEffects.None;
			
			if (Projectile.spriteDirection == -1)
				spriteEffects = SpriteEffects.FlipHorizontally;

			if (Main.projFrames[Type] > 1) {
				int num381 = texture.Height / Main.projFrames[Projectile.type];
				int y27 = num381 * Projectile.frame;

				Color alpha13 = Projectile.GetAlpha(lightColor);

				Main.EntitySpriteDraw(texture,
					new Vector2(Projectile.position.X - Main.screenPosition.X + num138 + (float)num137, Projectile.position.Y - Main.screenPosition.Y + (float)(Projectile.height / 2) + Projectile.gfxOffY),
					new Rectangle(0, y27, texture.Width, num381 - 1),
					alpha13,
					Projectile.rotation,
					new Vector2(num138, Projectile.height / 2 + num136),
					Projectile.scale,
					spriteEffects,
					0);

				if (ModContent.RequestIfExists<Texture2D>(GlowTexture, out var glowTexture, AssetRequestMode.ImmediateLoad)) {
					Main.EntitySpriteDraw(glowTexture.Value,
						new Vector2(Projectile.position.X - Main.screenPosition.X + num138 + num137, Projectile.position.Y - Main.screenPosition.Y + Projectile.height / 2 + Projectile.gfxOffY),
						new Rectangle(0, y27, texture.Width, num381 - 1),
						new Color(250, 250, 250, Projectile.alpha),
						Projectile.rotation,
						new Vector2(num138, Projectile.height / 2 + num136),
						Projectile.scale,
						spriteEffects,
						0);
				}

				int num386 = (int)typeof(Main).GetCachedMethod("TryInteractingWithMoneyTrough")!.Invoke(null, new object[]{ Projectile })!;
				if (num386 == 0)
					return false;

				int num387 = (lightColor.R + lightColor.G + lightColor.B) / 3;
				if (num387 > 10) {
					int num388 = 94;
					if (Projectile.type == 960)
						num388 = 244;

					Color selectionGlowColor = Colors.GetSelectionGlowColor(num386 == 2, num387);
					Main.EntitySpriteDraw(TextureAssets.Extra[num388].Value,
						new Vector2(Projectile.position.X - Main.screenPosition.X + num138 + (float)num137, Projectile.position.Y - Main.screenPosition.Y + (float)(Projectile.height / 2) + Projectile.gfxOffY),
						new Rectangle(0, y27, texture.Width, num381 - 1),
						selectionGlowColor,
						Projectile.rotation,
						new Vector2(num138, Projectile.height / 2 + num136),
						1f,
						spriteEffects,
						0);
				}

				return false;
			}

			if (Projectile.bobber) {
				Vector2 mountedCenter = Main.player[Projectile.owner].MountedCenter;

				Color stringColor = new(200, 200, 200, 100);
				float polePosX = mountedCenter.X, polePosY = mountedCenter.Y;
				ProjectileLoader.ModifyFishingLine(Projectile, ref polePosX, ref polePosY, ref stringColor);

				if (Projectile.ai[1] > 0f && Projectile.ai[0] == 1f) {
					int num406 = (int)Projectile.ai[1];
					Vector2 center5 = Projectile.Center;
					float rotation30;
					Vector2 vector63 = center5;
					float num407 = polePosX - vector63.X;
					float num408 = polePosY - vector63.Y;
					if (Projectile.velocity.X > 0f) {
						spriteEffects = SpriteEffects.None;
						rotation30 = (float)Math.Atan2(num408, num407);
						rotation30 += 0.785f;
						if (Projectile.ai[1] == 2342f)
							rotation30 -= 0.785f;
					}
					else {
						spriteEffects = SpriteEffects.FlipHorizontally;
						rotation30 = (float)Math.Atan2(0f - num408, 0f - num407);
						rotation30 -= 0.785f;
						if (Projectile.ai[1] == 2342f)
							rotation30 += 0.785f;
					}

					Main.instance.LoadItem(num406);
					Main.EntitySpriteDraw(TextureAssets.Item[num406].Value,
						new Vector2(center5.X - Main.screenPosition.X, center5.Y - Main.screenPosition.Y),
						new Rectangle(0, 0, TextureAssets.Item[num406].Width(), TextureAssets.Item[num406].Height()),
						lightColor,
						rotation30,
						new Vector2(TextureAssets.Item[num406].Width() / 2, TextureAssets.Item[num406].Height() / 2),
						Projectile.scale,
						spriteEffects,
						0);
				} else if (Projectile.ai[0] <= 1f) {
					Main.EntitySpriteDraw(texture,
						new Vector2(Projectile.position.X - Main.screenPosition.X + num138 + (float)num137, Projectile.position.Y - Main.screenPosition.Y + (float)(Projectile.height / 2) + Projectile.gfxOffY),
						null,
						Projectile.GetAlpha(lightColor),
						Projectile.rotation,
						new Vector2(num138, Projectile.height / 2 + num136),
						Projectile.scale,
						spriteEffects,
						0);
				}
			} else {
				if (Projectile.ownerHitCheck && Main.player[Projectile.owner].gravDir == -1f) {
					if (Main.player[Projectile.owner].direction == 1)
						spriteEffects = SpriteEffects.FlipHorizontally;
					else if (Main.player[Projectile.owner].direction == -1)
						spriteEffects = SpriteEffects.None;
				}

				Main.EntitySpriteDraw(texture,
					new Vector2(Projectile.position.X - Main.screenPosition.X + num138 + num137, Projectile.position.Y - Main.screenPosition.Y + Projectile.height / 2 + Projectile.gfxOffY),
					null,
					Projectile.GetAlpha(lightColor),
					Projectile.rotation,
					new Vector2(num138, Projectile.height / 2 + num136),
					Projectile.scale,
					spriteEffects,
					0);

				if (Projectile.glowMask != -1)
					Main.EntitySpriteDraw(TextureAssets.GlowMask[Projectile.glowMask].Value,
						new Vector2(Projectile.position.X - Main.screenPosition.X + num138 + (float)num137, Projectile.position.Y - Main.screenPosition.Y + (float)(Projectile.height / 2) + Projectile.gfxOffY),
						texture.Frame(),
						new Color(250, 250, 250, Projectile.alpha),
						Projectile.rotation,
						new Vector2(num138, Projectile.height / 2 + num136),
						Projectile.scale,
						spriteEffects,
						0);

				if (ModContent.RequestIfExists<Texture2D>(GlowTexture, out var glowTexture, AssetRequestMode.ImmediateLoad)) {
					Main.EntitySpriteDraw(glowTexture.Value,
						new Vector2(Projectile.position.X - Main.screenPosition.X + num138 + num137, Projectile.position.Y - Main.screenPosition.Y + Projectile.height / 2 + Projectile.gfxOffY),
						texture.Frame(),
						new Color(250, 250, 250, Projectile.alpha),
						Projectile.rotation,
						new Vector2(num138, Projectile.height / 2 + num136),
						Projectile.scale,
						spriteEffects,
						0);
				}
			}

			return false;
		}

		/// <summary>
		/// This hook runs immediately before the projectile is drawn
		/// </summary>
		/// <param name="lightColor">The color of the light at the projectile's center</param>
		public virtual void SafePreDraw(ref Color lightColor) { }
	}
}
