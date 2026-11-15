using CelesteAndroid;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod;

namespace Celeste
{
	/// <summary>
	/// Interface em widescreen (ver <see cref="Wide"/>). Quando a tela tem menos de 1080 linhas, o
	/// jogo desenha a interface num buffer e depois amplia o buffer; o buffer passa a ter a largura
	/// toda (ver WidescreenPass: Celeste.LoadContent) e a interface vai para o centro dele.
	/// </summary>
	[MonoModPatch("Celeste.HiresRenderer")]
	public class patch_HiresRenderer : HiresRenderer
	{
		[MonoModReplace]
		public new static void BeginRender(BlendState blend = null!, SamplerState sampler = null!)
		{
			blend ??= BlendState.AlphaBlend;
			sampler ??= SamplerState.LinearClamp;
			Matrix matrix = DrawToBuffer ? Matrix.CreateTranslation(Wide.HudOffset, 0f, 0f) : Engine.ScreenMatrix;
			Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, blend, sampler, DepthStencilState.Default, RasterizerState.CullNone, null, matrix);
		}

		[MonoModReplace]
		public override void Render(Scene scene)
		{
			if (DrawToBuffer)
			{
				// O buffer já tem a interface deslocada: amplia sem deslocar de novo.
				Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Wide.WorldMatrix());
				Draw.SpriteBatch.Draw((RenderTarget2D)Buffer, new Vector2(-1f, -1f), Color.White);
				Draw.SpriteBatch.End();
			}
			else
			{
				RenderContent(scene);
			}
		}
	}
}
