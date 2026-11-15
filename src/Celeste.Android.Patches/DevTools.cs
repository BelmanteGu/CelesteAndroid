using System;
using System.IO;
using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace CelesteAndroid
{
	/// <summary>
	/// Ferramentas de desenvolvimento, ligadas só por variável de ambiente (no Android nenhuma
	/// existe, então tudo aqui é inerte). Servem para testar patches de vídeo no host desktop
	/// sem precisar jogar até a tela que interessa:
	/// <list type="bullet">
	/// <item><c>CELESTE_DEV_LEVEL=1</c> (ou <c>1:b</c>): ao chegar no mapa do mundo, entra direto no
	/// capítulo (lado A/B/C) com um save de depuração.</item>
	/// <item><c>CELESTE_DEV_SHOTS=pasta</c> e <c>CELESTE_DEV_SHOT_FRAMES=300,600</c>: salva o que
	/// está na tela nesses quadros como PNG.</item>
	/// <item><c>CELESTE_DEV_EXIT_FRAME=700</c>: fecha o jogo nesse quadro.</item>
	/// <item><c>CELESTE_DEV_PAUSE=2000</c>: abre o menu de pausa nesse quadro (dentro de uma fase).</item>
	/// <item><c>CELESTE_DEV_WINDOW=1560x720</c>: tamanho da janela (proporção de celular, por exemplo).</item>
	/// </list>
	/// </summary>
	internal static class DevTools
	{
		private static readonly string? Level = NullIfEmpty(Environment.GetEnvironmentVariable("CELESTE_DEV_LEVEL"));
		private static readonly string? ShotDir = Environment.GetEnvironmentVariable("CELESTE_DEV_SHOTS");
		private static readonly int[] ShotFrames = ParseFrames(Environment.GetEnvironmentVariable("CELESTE_DEV_SHOT_FRAMES"));
		private static readonly int ExitFrame = int.TryParse(Environment.GetEnvironmentVariable("CELESTE_DEV_EXIT_FRAME"), out int f) ? f : -1;

		private static readonly string? Window = Environment.GetEnvironmentVariable("CELESTE_DEV_WINDOW");
		private static readonly int PauseFrame = int.TryParse(Environment.GetEnvironmentVariable("CELESTE_DEV_PAUSE"), out int p) ? p : -1;

		private static int frame;
		private static bool levelStarted;

		public static void AfterRender(GraphicsDevice device)
		{
			if (Level == null && ShotDir == null && ExitFrame < 0 && Window == null && PauseFrame < 0)
			{
				return;
			}
			frame++;

			if (frame == 2 && Window != null)
			{
				string[] size = Window.Split('x');
				Engine.SetWindowed(int.Parse(size[0]), int.Parse(size[1]));
			}

			if (Level != null && !levelStarted && Engine.Scene is Overworld)
			{
				levelStarted = true;
				StartLevel(Level);
			}

			if (ShotDir != null && Array.IndexOf(ShotFrames, frame) >= 0)
			{
				SaveShot(device, Path.Combine(ShotDir, $"frame{frame:D5}.png"));
			}

			if (frame == PauseFrame && Engine.Scene is Level level && !level.Paused)
			{
				level.Pause();
			}

			if (frame == ExitFrame)
			{
				Engine.Instance.Exit();
			}
		}

		private static void StartLevel(string spec)
		{
			string[] parts = spec.Split(':');
			int chapter = int.Parse(parts[0]);
			AreaMode mode = parts.Length > 1 ? parts[1].ToLowerInvariant() switch
			{
				"b" => AreaMode.BSide,
				"c" => AreaMode.CSide,
				_ => AreaMode.Normal,
			} : AreaMode.Normal;
			string? checkpoint = parts.Length > 2 ? parts[2] : null;

			SaveData.InitializeDebugMode(loadExisting: false);
			Engine.Scene = new LevelLoader(new Session(new AreaKey(chapter, mode), checkpoint));
		}

		private static void SaveShot(GraphicsDevice device, string path)
		{
			PresentationParameters pp = device.PresentationParameters;
			int w = pp.BackBufferWidth;
			int h = pp.BackBufferHeight;
			Color[] pixels = new Color[w * h];
			device.GetBackBufferData(pixels);
			using Texture2D texture = new(device, w, h);
			texture.SetData(pixels);
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			using FileStream stream = File.Create(path);
			texture.SaveAsPng(stream, w, h);
			Console.WriteLine("DevTools: " + path);
		}

		private static string? NullIfEmpty(string? text) => string.IsNullOrEmpty(text) ? null : text;

		private static int[] ParseFrames(string? text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return Array.Empty<int>();
			}
			string[] parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			int[] frames = new int[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				frames[i] = int.Parse(parts[i]);
			}
			return frames;
		}
	}
}
