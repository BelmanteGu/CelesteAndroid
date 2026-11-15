using System;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Views;
using Uri = Android.Net.Uri;

namespace CelesteAndroid
{
	/// <summary>
	/// Launcher. Fluxo:
	/// <list type="number">
	/// <item>Sem o jogo: tela "Select game files" (pasta da versão FNA da Steam ou .zip).</item>
	/// <item>Com o jogo importado: logo + Play, e embaixo os saves (importar do PC, exportar) e
	/// as configurações.</item>
	/// <item>Opcional ("Open game directly"): o app abre o jogo direto, sem a tela do Play. O atalho
	/// "Settings" do ícone (toque longo) continua levando ao launcher.</item>
	/// </list>
	/// O visual está em LauncherUi.cs e as configurações em LauncherSettings.cs.
	/// </summary>
	[Activity(
		Name = "org.celesteandroid.celeste.LauncherActivity",
		Label = "Celeste",
		MainLauncher = true,
		Exported = true,
		Theme = "@style/Theme.Celeste",
		ScreenOrientation = ScreenOrientation.SensorLandscape,
		LaunchMode = LaunchMode.SingleTop,
		ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
			| ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation)]
	public partial class LauncherActivity : Activity
	{
		/// <summary>Ação do atalho "Settings": abre o launcher em vez do jogo.</summary>
		public const string ActionSettings = "org.celesteandroid.celeste.SETTINGS";

		private const int RequestFolder = 1;
		private const int RequestZip = 2;
		private const int RequestSaves = 3;
		private const int RequestExportSaves = 4;

		// Uma tentativa de refazer o patch por processo: se falhar, o erro fica na tela e o Play
		// continua livre, em vez de refazer tudo a cada volta ao launcher.
		private static bool repatchTried;

		private bool busy;
		private bool settingsRequested;
		private bool launching;

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			PublishShortcuts();
			settingsRequested = Intent?.Action == ActionSettings;

			// Opção "Open game directly", jogo instalado e com o patch desta versão: pula a tela do Play.
			if (!settingsRequested && LauncherPrefs.DirectStart(this) && CanLaunchDirectly())
			{
				LaunchGame();
				return;
			}

			SetContentView(BuildLayout());
			HideSystemBars();
			LoadArt();
			ShowScreen(animate: true);
		}

		protected override void OnNewIntent(Intent? intent)
		{
			base.OnNewIntent(intent);
			Intent = intent;
			if (intent?.Action == ActionSettings && root != null)
			{
				OpenSettings();
			}
		}

		protected override void OnResume()
		{
			base.OnResume();
			if (launching || root == null)
			{
				return;
			}
			HideSystemBars();
			RefreshState();

			bool installed = GameInstaller.IsInstalled(this);
			// Desenvolvimento: arquivos já copiados por scripts/deploy-android.ps1; só patch + fundo.
			// adb shell am start -n org.celesteandroid.celeste/.LauncherActivity --ez repatch true
			if (!busy && Intent!.GetBooleanExtra("repatch", false))
			{
				Intent.RemoveExtra("repatch");
				RunInstall(_ => { });
			}
			// APK pessoal: o jogo já vem dentro do APK; instala sozinho na primeira abertura.
			else if (!busy && !installed && GameInstaller.HasEmbeddedGame(this))
			{
				RunInstall(installer => installer.ImportEmbedded());
			}
			// APK novo com um módulo de patches diferente: refaz o patch e, se ninguém pediu as
			// configurações e o "abrir direto" está ligado, abre o jogo em seguida.
			else if (!busy && !repatchTried && installed && !GameInstaller.IsPatchCurrent(this))
			{
				repatchTried = true;
				RunJob(installer =>
				{
					Repatch(installer);
					return "Game updated for this version";
				}, launchAfter: !settingsRequested && LauncherPrefs.DirectStart(this));
			}
			else if (settingsRequested)
			{
				settingsRequested = false;
				OpenSettings();
			}
		}

		private static bool CanLaunchDirectly(Context context) =>
			GameInstaller.IsInstalled(context) && GameInstaller.IsPatchCurrent(context);

		private bool CanLaunchDirectly() =>
			CanLaunchDirectly(this) && !(Intent?.GetBooleanExtra("repatch", false) ?? false);

		private void LaunchGame()
		{
			launching = true;
			var intent = new Intent(this, typeof(GameActivity));
			string? driver = LauncherPrefs.Driver(this);
			if (!string.IsNullOrEmpty(driver))
				intent.PutExtra(GameActivity.ExtraDriver, driver);
			StartActivity(intent);
			// O launcher sai da pilha: voltar do jogo fecha o app, e a próxima abertura já cai no jogo.
			Finish();
			OverridePendingTransition(0, 0);
		}

		/// <summary>
		/// Atalho "Settings" no toque longo do ícone: com o jogo abrindo direto, é o caminho para as
		/// configurações (e para sair de uma configuração que impeça o jogo de abrir).
		/// </summary>
		private void PublishShortcuts()
		{
			if (!OperatingSystem.IsAndroidVersionAtLeast(25))
				return;
			try
			{
				var manager = (ShortcutManager?)GetSystemService(Java.Lang.Class.FromType(typeof(ShortcutManager)));
				if (manager == null)
					return;
				var intent = new Intent(this, typeof(LauncherActivity)).SetAction(ActionSettings)!;
				intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
				var builder = new ShortcutInfo.Builder(this, "settings")
					.SetShortLabel("Settings")!
					.SetLongLabel("Launcher settings")!
					.SetIntent(intent)!;
				int icon = Resources!.GetIdentifier("ic_celeste", "mipmap", PackageName);
				if (icon != 0)
					builder.SetIcon(Android.Graphics.Drawables.Icon.CreateWithResource(this, icon));
				manager.SetDynamicShortcuts(new[] { builder.Build()! });
			}
			catch (Exception e)
			{
				Log.Warn(GameActivity.LogTag, "atalho Settings: " + e.Message);
			}
		}

		#region Importação

		private void PickFolder() => StartActivityForResult(new Intent(Intent.ActionOpenDocumentTree), RequestFolder);

		private void PickZip()
		{
			var intent = new Intent(Intent.ActionOpenDocument);
			intent.AddCategory(Intent.CategoryOpenable);
			intent.SetType("*/*");
			intent.PutExtra(Intent.ExtraMimeTypes, new[] { "application/zip", "application/x-zip-compressed" });
			StartActivityForResult(intent, RequestZip);
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
		{
			base.OnActivityResult(requestCode, resultCode, data);
			Uri? uri = data?.Data;
			if (resultCode != Result.Ok || uri == null)
				return;
			if (requestCode == RequestFolder)
				RunInstall(installer => installer.ImportFolder(uri));
			else if (requestCode == RequestZip)
				RunInstall(installer => installer.ImportZip(uri));
			else if (requestCode == RequestSaves)
				RunJob(installer => $"Imported {installer.ImportSaves(uri)} save file(s)");
			else if (requestCode == RequestExportSaves)
				RunJob(installer => $"Exported {installer.ExportSaves(uri)} save file(s)");
		}

		/// <summary>Importação do jogo: cópia → patch → fundo. Ao terminar, a tela do Play aparece.</summary>
		private void RunInstall(Action<GameInstaller> import)
		{
			RunJob(installer =>
			{
				import(installer);
				Repatch(installer);
				return "Game imported! Ready to play";
			});
		}

		/// <summary>Gera o Celeste.dll patcheado e o fundo das faixas laterais (no aparelho).</summary>
		private static void Repatch(GameInstaller installer)
		{
			installer.Patch();
			installer.PrepareBackground();
		}

		/// <summary>Roda uma tarefa do instalador fora da thread de UI, com progresso.</summary>
		private void RunJob(Func<GameInstaller, string> job, bool launchAfter = false)
		{
			busy = true;
			ShowProgress("Working…", -1);
			RefreshState();

			var installer = new GameInstaller(this, (message, fraction) => RunOnUiThread(() => ShowProgress(message, fraction)));

			new Thread(() =>
			{
				string? error = null;
				string? success = null;
				try
				{
					success = job(installer);
				}
				catch (InstallException e)
				{
					error = e.Message;
				}
				catch (Exception e)
				{
					Log.Error(GameActivity.LogTag, e.ToString());
					error = "Something went wrong: " + e.Message;
				}

				RunOnUiThread(() =>
				{
					busy = false;
					HideProgress();
					if (error == null && launchAfter)
					{
						LaunchGame();
						return;
					}
					LoadArt();
					ShowScreen(animate: true);
					SetStatus(error ?? success, error != null);
					// O atalho "Settings" pode ter chegado durante o patch.
					if (settingsRequested && GameInstaller.IsInstalled(this))
					{
						settingsRequested = false;
						OpenSettings();
					}
				});
			}) { Name = "CelesteInstall", IsBackground = true }.Start();
		}

		#endregion

		private void HideSystemBars()
		{
			if (!OperatingSystem.IsAndroidVersionAtLeast(30))
				return;
			// A partir do Android 15 o app já é edge-to-edge por padrão.
			if (!OperatingSystem.IsAndroidVersionAtLeast(35))
				Window!.SetDecorFitsSystemWindows(false);
			IWindowInsetsController? insets = Window!.InsetsController;
			if (insets != null)
			{
				insets.Hide(WindowInsets.Type.SystemBars());
				insets.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
			}
		}
	}
}
