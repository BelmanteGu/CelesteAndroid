using CelesteAndroid;
using Microsoft.Xna.Framework;
using MonoMod;

namespace Celeste
{
	[MonoModPatch("Celeste.Player")]
	public class patch_Player : Player
	{
		[MonoModIgnore]
		private Level level = null!;

		[MonoModIgnore]
		public patch_Player(Vector2 position, PlayerSpriteMode spriteMode) : base(position, spriteMode)
		{
		}

		public extern Vector2 orig_get_CameraTarget();

		/// <summary>
		/// Em widescreen, uma sala mais estreita que a tela fica centralizada (o original encosta a
		/// câmera na borda esquerda e mostra o vazio só de um lado).
		/// </summary>
		public new Vector2 CameraTarget
		{
			get
			{
				Vector2 target = orig_get_CameraTarget();
				if (Wide.Enabled && EnforceLevelBounds && level != null && level.Bounds.Width < Wide.GameWidth)
				{
					target.X = level.Bounds.Left - (Wide.GameWidth - level.Bounds.Width) / 2f;
				}
				return target;
			}
		}
	}
}
