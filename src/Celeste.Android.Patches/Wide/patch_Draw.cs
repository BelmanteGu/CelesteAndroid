using CelesteAndroid;
using Microsoft.Xna.Framework;
using MonoMod;

namespace Monocle
{
	[MonoModPatch("Monocle.Draw")]
	public static class patch_Draw
	{
		public static extern void orig_Rect(float x, float y, float width, float height, Color color);

		/// <summary>
		/// Fundos de tela cheia da interface (escurecer a tela no menu de pausa, fades, flashes) são
		/// retângulos do tipo (-10, -10, 1940, 1100) no espaço 1920×1080. Em widescreen a interface
		/// fica no centro (ver <see cref="Wide"/>), então esses retângulos são esticados até as duas
		/// bordas. Retângulos do jogo (espaço 320×180 ou do mundo) nunca têm esse tamanho.
		/// </summary>
		public static void Rect(float x, float y, float width, float height, Color color)
		{
			if (Wide.Enabled && x <= 0f && y <= 0f && x + width >= Wide.BaseTargetWidth && y + height >= 1080f && width < 4000f)
			{
				x -= Wide.HudOffset;
				width += Wide.HudOffset * 2f;
			}
			orig_Rect(x, y, width, height, color);
		}
	}
}
