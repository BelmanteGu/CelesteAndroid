using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace CelesteAndroid
{
	/// <summary>
	/// Modo widescreen: o jogo passa a ter a proporção da tela em vez de 16:9.
	/// <para>
	/// O Celeste tem a resolução embutida como constante (<c>Celeste.GameWidth = 320</c>,
	/// <c>TargetWidth = 1920</c>), e o compilador copia o número para cada lugar que o usa. O
	/// <c>WidescreenPass</c> do patcher troca esses números, nos métodos listados nele, por chamadas
	/// aos métodos daqui. Com a proporção 16:9 (ou o modo desligado) todos devolvem o valor
	/// original, então o jogo fica idêntico ao do PC.
	/// </para>
	/// <para>
	/// Dois espaços de coordenadas:
	/// <list type="bullet">
	/// <item>Jogo (pixel art): <see cref="GameWidth"/> × 180, ampliado 6× para <see cref="TargetWidth"/> × 1080.
	/// A câmera mostra mais cenário dos lados.</item>
	/// <item>Interface (HUD, menus, textos): continua 1920 × 1080, centralizada na tela por
	/// <see cref="HudOffset"/> (ver patch_Engine.UpdateView). Assim nenhum menu fica preso à esquerda;
	/// só os fundos de tela cheia são esticados até as bordas (ver patch_Draw).</item>
	/// </list>
	/// </para>
	/// </summary>
	public static class Wide
	{
		public const int BaseWidth = 320;
		public const int BaseHeight = 180;
		public const int BaseTargetWidth = 1920;

		/// <summary>Limite de largura: 32:9 já é 640. Acima disso a câmera mostraria salas vizinhas demais.</summary>
		private const int MaxWidth = 640;

		public static readonly bool Enabled;

		/// <summary>Largura do jogo em pixels de jogo (320 no original).</summary>
		public static readonly int GameWidth;

		/// <summary>Largura do alvo de alta resolução (1920 no original).</summary>
		public static readonly int TargetWidth;

		/// <summary>Quanto a interface de 1920 de largura é deslocada para ficar no centro (em pixels de 1080p).</summary>
		public static readonly float HudOffset;

		static Wide()
		{
			float aspect = HostConfig.AspectRatio;
			int width = BaseWidth;
			if (aspect > 16f / 9f + 0.01f)
			{
				// Largura par: metade da tela cai num pixel inteiro (a câmera centraliza pela metade).
				width = (int)Math.Round(BaseHeight * aspect / 2f) * 2;
				width = Math.Clamp(width, BaseWidth, MaxWidth);
			}
			GameWidth = width;
			Enabled = width != BaseWidth;
			TargetWidth = width * 6;
			HudOffset = (TargetWidth - BaseTargetWidth) / 2f;
		}

		// Substitutos das constantes (chamados pelo IL reescrito; ver WidescreenPass). Cada um
		// devolve o mesmo tipo da constante que substitui, para a pilha do IL não mudar.

		public static int IW() => GameWidth;
		public static float FW() => GameWidth;
		public static int IHalfW() => GameWidth / 2;
		public static float FHalfW() => GameWidth / 2f;
		public static float FNegHalfW() => -GameWidth / 2f;
		public static float FW2() => GameWidth + 2;
		public static float FW10() => GameWidth + 10;
		public static float FW20() => GameWidth + 20;
		public static float FW32() => GameWidth + 32;
		public static int IW32() => GameWidth + 32;
		public static int ITW() => TargetWidth;
		public static float FTW() => TargetWidth;
		public static float FHalfTW() => TargetWidth / 2f;
		public static int ITW2() => TargetWidth + 2;

		/// <summary>Largura da interface (1920): substitui Engine.Width em código que desenha a interface.</summary>
		public static int HudWidth() => BaseTargetWidth;

		/// <summary>
		/// Matriz da tela sem o deslocamento da interface: para o que é desenhado no espaço largo
		/// (a imagem do jogo, ampliada 6×). Substitui Engine.ScreenMatrix nesses lugares.
		/// </summary>
		public static Matrix WorldMatrix() => Engine.ScreenMatrix * Matrix.CreateTranslation(-HudOffset * Scale, 0f, 0f);

		/// <summary>Escala tela/1080p (a mesma de Engine.ScreenMatrix).</summary>
		public static float Scale => Engine.ViewWidth / (float)Engine.Width;
	}
}
