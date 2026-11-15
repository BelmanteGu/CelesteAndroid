namespace CelesteAndroid.Patcher
{
	/// <summary>
	/// Onde o <see cref="WidescreenPass"/> troca constantes. Tokens:
	/// <list type="bullet">
	/// <item>i320/f320: largura do jogo; i160/f160/f-160: metade dela (centro da câmera);
	/// f322: largura + 2 (retângulos que cobrem a tela do jogo com sobra).</item>
	/// <item>i1920/f1920/f960/i1922: alvo de alta resolução (largura, metade, +2).</item>
	/// <item>SM: Engine.ScreenMatrix → matriz sem o deslocamento da interface (desenho no espaço largo).</item>
	/// <item>EW: Engine.Width → 1920 (código que centraliza a interface pela largura da tela).</item>
	/// </list>
	/// Método "*" = todos os métodos do tipo. Um número só entra na lista depois de lido no contexto:
	/// 320 e 160 também são velocidades (Player, Puffer, BadelineBoost...), e essas ficam de fora.
	/// </summary>
	internal static class WidescreenRules
	{
		internal sealed record Rule(string Type, string Method, params string[] Tokens);

		public static readonly Rule[] All =
		{
			// --- Núcleo: tamanho do motor, buffers, câmera, composição da imagem ---
			new("Celeste.Celeste", ".ctor", "i1920"),
			new("Celeste.Celeste", "LoadContent", "i1922"),
			new("Celeste.HiresRenderer", "get_DrawToBuffer", "i1920"),
			new("Celeste.GameplayBuffers", "Create", "i320"),
			new("Celeste.GameplayRenderer", ".ctor", "i320"),
			new("Celeste.Level", "Render", "f320", "f160", "f322", "SM"),
			new("Celeste.Level", "*", "f320", "i320", "EW"),
			// Renomeado pelo patch_Player (o MonoMod roda antes deste passo).
			new("Celeste.Player", "orig_get_CameraTarget", "f160", "i320"),

			// --- Mapa do mundo / tela de título: desenhados no espaço largo ---
			new("Celeste.MountainModel", "ResetRenderTargets", "i1920"),
			new("Celeste.HiresSnow", "*", "SM", "i1920"),

			// --- Interface centralizada por Engine.Width ---
			new("Celeste.TextMenu", "*", "EW"),
			new("Celeste.MiniTextbox", "*", "EW"),
			new("Celeste.Postcard", "*", "EW"),
			new("Celeste.CS02_Journal", "*", "EW"),
			new("Celeste.CS03_Memo", "*", "EW"),

			// --- Fundos (backdrops) e efeitos que cobrem a tela do jogo ---
			new("Celeste.Parallax", "*", "f320", "f160"),
			new("Celeste.Snow", "*", "f320", "f160"),
			new("Celeste.RainFG", "*", "f160"),
			new("Celeste.StarsBG", "*", "f320"),
			new("Celeste.NorthernLights", "*", "f320", "i320"),
			new("Celeste.StarJumpController", "*", "f320", "i320"),
			new("Celeste.HeatWave", "*", "f320", "i320"),
			new("Celeste.FallEffects", "*", "f320", "i320"),
			new("Celeste.MirrorFG", "*", "f320", "i320"),
			new("Celeste.StardustFG", "*", "f320", "i320"),
			new("Celeste.CoreStarsFG", "*", "f320", "i320"),
			new("Celeste.ReflectionFG", "*", "f320", "i320"),
			new("Celeste.BlackholeBG", "*", "f320", "i320"),
			new("Celeste.GlassBlockBg", "*", "f320", "i320"),
			new("Celeste.DreamStars", "*", "f320"),
			new("Celeste.MirrorSurfaces", "*", "i320"),
			new("Celeste.DustEdges", "*", "f320"),
			new("Celeste.SeekerEffectsController", "*", "f320"),
			new("Celeste.Tentacles", "*", "f320", "f160"),
			new("Celeste.AscendManager", "*", "f320"),
			new("Celeste.LightningRenderer", "*", "f320"),
			new("Celeste.GondolaDarkness", "*", "f322"),
			new("Celeste.FormationBackdrop", "*", "f322"),

			// --- Entidades que só se desenham/atualizam dentro da câmera ---
			new("Celeste.CrystalStaticSpinner", "*", "f320"),
			new("Celeste.Lightning", "*", "f320"),
			new("Celeste.BirdNPC", "*", "f320"),
			new("Celeste.MoveBlock", "*", "f320"),
			new("Celeste.FakeWall", "*", "f320"),
			new("Celeste.ExitBlock", "*", "f320"),
			new("Celeste.LightingRenderer", "*", "f320"),

			// --- Câmera presa à sala / centralizada em algo ---
			new("Celeste.Lookout", "*", "i320", "f320", "f160", "f-160"),
			new("Celeste.ClutterDoor", "*", "f320", "i320"),
			new("Celeste.PlayerSeeker", "*", "f320", "i320", "f-160"),
			new("Celeste.CSGEN_StrawberrySeeds", "*", "i320", "f160"),
			new("Celeste.Cassette", "*", "f160", "i320"),
			new("Celeste.HeartGem", "*", "f320"),
			new("Celeste.FlingBirdIntro", "*", "f320"),
			new("Celeste.CameraTargetTrigger", "*", "f320"),
			new("Celeste.CameraAdvanceTargetTrigger", "*", "f320"),
			new("Celeste.NPC03_Theo_Vents", "*", "i320"),
			new("Celeste.ForegroundDebris", "*", "f320"),
			new("Celeste.Audio", "*", "f320"),
			new("Celeste.Decal", "*", "f160"),
			new("Celeste.SummitCloud", "*", "f160"),
			new("Celeste.BigWaterfall", "*", "f160"),
			new("Celeste.FinalBossBeam", "*", "f160"),
			new("Celeste.Key", "*", "f160"),

			// --- Modo espelho: x → largura − x ---
			new("Celeste.SpotlightWipe", "*", "f320"),
			new("Celeste.TalkComponent", "*", "f320"),
			new("Celeste.BirdTutorialGui", "*", "f320"),
		};
	}
}
