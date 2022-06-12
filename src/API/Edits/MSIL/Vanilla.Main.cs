using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Reflection;
using TerrariansConstructLib.Modifiers;
using TerrariansConstructLib.Projectiles;

namespace TerrariansConstructLib.API.Edits.MSIL {
	partial class Vanilla {
		public delegate void DrawProj_DrawNormalProjs_ModifySpriteEffectsDelegate(Projectile projectile, ref SpriteEffects effects);
		
		internal static void Patch_Main_DrawProj_DrawNormalProjs(ILContext il) {
			MethodInfo ProjectileLoader_DrawOffset = typeof(ProjectileLoader).GetCachedMethod("DrawOffset")!;
			FieldInfo Projectile_spriteDirection = typeof(Projectile).GetCachedField("spriteDirection")!;
			FieldInfo Main_projFrames = typeof(Main).GetCachedField("projFrames")!;
			FieldInfo Projectile_type = typeof(Projectile).GetCachedField("type")!;
			FieldInfo TextureAssets_Projectile = typeof(TextureAssets).GetCachedField("Projectile")!;
			ConstructorInfo Color_ctor = typeof(Color).GetCachedConstructor(typeof(int), typeof(int), typeof(int))!;
			MethodInfo Projectile_GetAlpha = typeof(Projectile).GetCachedMethod("GetAlpha")!;
			FieldInfo Projectile_bobber = typeof(Projectile).GetCachedField("bobber")!;

			ILHelper.EnsureAreNotNull((ProjectileLoader_DrawOffset, typeof(ProjectileLoader).FullName + "::DrawOffset(Projectile, ref int, ref int, ref float)"),
				(Projectile_spriteDirection, typeof(Projectile).FullName + "::spriteDirection"),
				(Main_projFrames, typeof(Main).FullName + "::projFrames"),
				(Projectile_type, typeof(Projectile).FullName + "::type"),
				(TextureAssets_Projectile, typeof(TextureAssets).FullName + "::Projectile"),
				(Color_ctor, typeof(Color).FullName + "::.ctor(int, int, int)"),
				(Projectile_GetAlpha, typeof(Projectile).FullName + "::GetAlpha(Color)"),
				(Projectile_bobber, typeof(Projectile).FullName + "::bobber"));
			
			ILCursor c = new(il);

			int patchNum = 1;

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: true);

			//Edit: add a BaseTCProjectile "hook" for initializing the SpriteEffects
			int argProjectile = -1, spriteEffects = -1;
			if (!c.TryGotoNext(MoveType.After, i => i.MatchCall(ProjectileLoader_DrawOffset),
				i => i.MatchLdcI4(0),
				i => i.MatchStloc(out spriteEffects),
				i => i.MatchLdarg(out argProjectile),
				i => i.MatchLdfld(Projectile_spriteDirection),
				i => i.MatchLdcI4(-1),
				i => i.MatchBneUn(out _),
				i => i.MatchLdcI4(1),
				i => i.MatchStloc(spriteEffects),
				i => i.MatchLdarg(argProjectile)))
				goto bad_il;

			patchNum++;

			c.Emit(OpCodes.Ldarg, argProjectile);
			c.Emit(OpCodes.Ldloca, spriteEffects);
			c.EmitDelegate<DrawProj_DrawNormalProjs_ModifySpriteEffectsDelegate>((Projectile projectile, ref SpriteEffects effects) => {
				if (projectile.ModProjectile is BaseTCProjectile tc)
					tc.SetSpriteEffects(ref effects);
			});

			//Edits: redirect the projectile texture used for BaseTCProjectile objects
			//Edit #1:  `if (Main.projFrames[projectile.type] > 1)` block
			if (!c.TryGotoNext(MoveType.After, i => i.MatchRet(),
				i => i.MatchLdsfld(Main_projFrames),
				i => i.MatchLdarg(argProjectile),
				i => i.MatchLdfld(Projectile_type),
				i => i.MatchLdelemI4(),
				i => i.MatchLdcI4(1),
				i => i.MatchBle(out _)))
				goto bad_il;

			bool EmitTextureSwap() {
				if (!c.TryGotoNext(MoveType.After, i => i.MatchLdsfld(TextureAssets_Projectile),
					i => i.MatchLdarg(argProjectile),
					i => i.MatchLdfld(Projectile_type),
					i => i.MatchLdelemRef()))
					return false;
				
				c.Emit(OpCodes.Ldarg, argProjectile);
				
				c.EmitDelegate<Func<Asset<Texture2D>, Projectile, Asset<Texture2D>>>((orig, projectile) => {
					if (projectile.ModProjectile is BaseTCProjectile tc) {
						Texture2D texture = tc.GetTextureOverride(tc.parts) ?? CoreLibMod.ItemTextures.Get(-tc.ProjectileDefinition, tc.parts, Array.Empty<BaseTrait>());

						orig = Utility.CloneAndOverwriteValue(orig, texture);
					}

					return orig;
				});

				return true;
			}

			if (!EmitTextureSwap())
				goto bad_il;

			patchNum++;

			//Jump after the `if (projectile.type == 111)` block
			if (!c.TryGotoNext(MoveType.After, i => i.MatchCall(Projectile_GetAlpha)))
				goto bad_il;

			//Edit #2: `EntitySpriteDraw(TextureAssets.Projectile[projectile.type].get_Value(), ... num277, TextureAssets.Projectile[projectile.type].Width(), num276), ...` redirection
			if (c.TryGotoNext(MoveType.After, i => i.MatchCall(Color_ctor)))
				goto bad_il;

			if (!EmitTextureSwap())
				goto bad_il;

			if (!EmitTextureSwap())
				goto bad_il;

			patchNum++;

			//Edit #3: `if (projectile.bobber)` block
			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdfld(Projectile_bobber)))
				goto bad_il;

			if (!EmitTextureSwap())
				goto bad_il;

			patchNum++;

			//Edit #4: `else` block after `if (projectile.bobber)` block
			//`EntitySpriteDraw(TextureAssets.Projectile[projectile.type].get_Value(), ... new Rectangle(0, 0, TextureAssets.Projectile[projectile.type].Width(), TextureAssets.Projectile[projectile.type].Height()` redirection
			if (!EmitTextureSwap())
				goto bad_il;

			if (!EmitTextureSwap())
				goto bad_il;

			if (!EmitTextureSwap())
				goto bad_il;

			patchNum++;

			ILHelper.UpdateInstructionOffsets(c);

			ILHelper.CompleteLog(CoreLibMod.Instance, c, beforeEdit: false);

			return;
			bad_il:
			throw new Exception("Unable to fully patch " + il.Method.Name + "()\n" +
				"Reason: Could not find instruction sequence for patch #" + patchNum);
		}
	}
}
