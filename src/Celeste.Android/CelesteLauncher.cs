using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Android.Content;
using Build = Android.OS.Build;
using BuildVersionCodes = Android.OS.BuildVersionCodes;
using Android.Runtime;
using Android.Util;
using Android.Views;

namespace CelesteAndroid
{
	/// <summary>
	/// Carrega o Celeste.dll patcheado e chama o Main original, com o mesmo contrato do host desktop.
	/// </summary>
	public static class CelesteLauncher
	{
		public static void Run(Context context)
		{
			string gameDir = GameInstaller.GameDir(context);
			Log.Info(GameActivity.LogTag, $"Iniciando Celeste de {gameDir}");

			// Ver CelesteAndroid.HostConfig (compilado de src/Shared/HostConfig.cs).
			bool touch = LauncherPrefs.TouchControls(context);
			float aspect = LauncherPrefs.Widescreen(context) ? ScreenAspect(context) : 0f;
			HostConfig.Publish(
				"Android",
				GameInstaller.UserDir(context),
				GameInstaller.BackgroundPng(context),
				touch,
				LauncherPrefs.TouchLayout(context),
				aspect
			);
			Log.Info(GameActivity.LogTag, $"Controles de toque: {(touch ? "ligados" : "desligados")}, widescreen: {(aspect > 0f ? aspect.ToString("0.000") : "desligado")}");

			// O FNA resolve o Content relativo ao diretório de trabalho no Android.
			AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY", gameDir + Path.DirectorySeparatorChar);
			Environment.CurrentDirectory = gameDir;

			Assembly celeste = AssemblyLoadContext.Default.LoadFromAssemblyPath(GameInstaller.PatchedDll(context));

			// Engine.AssemblyDirectory vem de Assembly.Location, que não aponta para o jogo.
			celeste.GetType("Monocle.Engine", throwOnError: true)!
				.GetField("AssemblyDirectory", BindingFlags.NonPublic | BindingFlags.Static)!
				.SetValue(null, gameDir);

			MethodInfo main = celeste.GetType("Celeste.Celeste", throwOnError: true)!
				.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static)!;
			main.Invoke(null, new object[] { Array.Empty<string>() });
		}

		/// <summary>
		/// Proporção da tela em paisagem (lado maior / menor), para o widescreen. O jogo roda em tela
		/// cheia imersiva, então vale a área inteira da janela (inclusive o recorte da câmera).
		/// </summary>
		private static float ScreenAspect(Context context)
		{
			try
			{
				int w, h;
				IWindowManager? wm = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
				if (Build.VERSION.SdkInt >= BuildVersionCodes.R && wm?.CurrentWindowMetrics is WindowMetrics metrics)
				{
					w = metrics.Bounds.Width();
					h = metrics.Bounds.Height();
				}
				else
				{
					DisplayMetrics real = new();
#pragma warning disable CA1422 // Android 8-10: não há WindowMetrics.
					wm?.DefaultDisplay?.GetRealMetrics(real);
#pragma warning restore CA1422
					w = real.WidthPixels;
					h = real.HeightPixels;
				}
				float longSide = Math.Max(w, h);
				float shortSide = Math.Min(w, h);
				return shortSide > 0 ? longSide / shortSide : 0f;
			}
			catch (Exception e)
			{
				Log.Warn(GameActivity.LogTag, "widescreen: sem medida da tela: " + e.Message);
				return 0f;
			}
		}
	}
}
