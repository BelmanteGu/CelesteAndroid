using System;
using System.IO;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Color = Android.Graphics.Color;
using Uri = Android.Net.Uri;

namespace CelesteAndroid
{
	/// <summary>
	/// Visual do launcher (ver design/launcher.html): fundo animado, tela "Select game files" e
	/// tela do Play. Fonte pixel (Pixelify Sans, OFL), botões com cantos pouco arredondados,
	/// vermelho do morango do ícone e o azul do letreiro do logo no Play.
	/// </summary>
	public partial class LauncherActivity
	{
		private static readonly Color Night = Color.ParseColor("#120C22");
		private static readonly Color Ink = Color.ParseColor("#F6EEFE");
		private static readonly Color Muted = Color.Argb(178, 246, 238, 254);
		private static readonly Color Line = Color.Argb(46, 246, 238, 254);
		private static readonly Color Glass = Color.Argb(140, 18, 12, 34);
		private static readonly Color Red = Color.ParseColor("#DD2A2A");
		private static readonly Color RedLight = Color.ParseColor("#FF5F42");
		private static readonly Color RedDeep = Color.ParseColor("#8D1035");
		private static readonly Color Ice = Color.ParseColor("#6DC8E4");
		private static readonly Color IceDeep = Color.ParseColor("#2F7FA6");
		private static readonly Color OnRed = Color.ParseColor("#FFF3EC");

		private const string ChannelUrl = "https://www.youtube.com/@belmante";

		private FrameLayout? root;
		private ImageView background = null!;
		private View veil = null!;
		private LinearLayout pickScreen = null!;
		private LinearLayout readyScreen = null!;
		private TextView pickStatus = null!;
		private TextView readyStatus = null!;
		private View selectActions = null!;
		private Button playButton = null!;
		private View settingsChip = null!;
		private TextView directChip = null!;
		private View channelBadge = null!;
		private LinearLayout progressBox = null!;
		private ProgressBar progressBar = null!;
		private TextView progressText = null!;
		private Typeface? pixel;

		private View BuildLayout()
		{
			pixel = LoadPixelFont();
			root = new FrameLayout(this);
			root.SetBackgroundColor(Night);

			background = new ImageView(this);
			background.SetScaleType(ImageView.ScaleType.CenterCrop);
			root.AddView(background, Match());

			veil = new View(this);
			root.AddView(veil, Match());

			root.AddView(BuildPickScreen(), Match());
			root.AddView(BuildReadyScreen(), Match());

			// Barra de progresso da importação/patch, embaixo no centro.
			progressBox = new LinearLayout(this) { Orientation = Orientation.Vertical, Visibility = ViewStates.Gone };
			progressBox.SetGravity(GravityFlags.CenterHorizontal);
			progressText = Label("", 12, Muted);
			progressBar = new ProgressBar(this, null, Android.Resource.Attribute.ProgressBarStyleHorizontal) { Max = 1000 };
			progressBar.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Red);
			progressBar.IndeterminateTintList = Android.Content.Res.ColorStateList.ValueOf(Red);
			progressBox.AddView(progressText);
			progressBox.AddView(progressBar, new LinearLayout.LayoutParams(Dp(300), Dp(8)) { TopMargin = Dp(8) });
			root.AddView(progressBox, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent,
				GravityFlags.Bottom | GravityFlags.CenterHorizontal) { BottomMargin = Dp(34) });


			channelBadge = BuildChannelBadge();
			root.AddView(channelBadge, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent,
				GravityFlags.Bottom | GravityFlags.End) { RightMargin = Dp(20), BottomMargin = Dp(18) });

			return root;
		}

		private View BuildPickScreen()
		{
			pickScreen = new LinearLayout(this) { Orientation = Orientation.Vertical };
			pickScreen.SetGravity(GravityFlags.Center);
			pickScreen.SetPadding(Dp(40), Dp(16), Dp(40), Dp(16));

			var icon = new ImageView(this);
			int iconId = Resources!.GetIdentifier("launcher_icon", "drawable", PackageName);
			icon.SetImageResource(iconId != 0 ? iconId : Resource.Mipmap.app_icon);
			icon.SetScaleType(ImageView.ScaleType.FitCenter);
			icon.Background = Rounded(Color.Transparent, 12);
			icon.ClipToOutline = true;
			pickScreen.AddView(icon, new LinearLayout.LayoutParams(Dp(58), Dp(58)));

			var title = Label("Welcome to Celeste", 28, Ink);
			title.LetterSpacing = 0.12f;
			pickScreen.AddView(title, Wrap(top: 10));

			var text = Label("Select your copy of the game.\nIt must be the FNA version from Steam.", 14, Muted);
			text.Gravity = GravityFlags.CenterHorizontal;
			text.SetLineSpacing(Dp(3), 1f);
			pickScreen.AddView(text, Wrap(top: 6));

			var steps = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			steps.AddView(StepChip("Steam → Celeste → Properties → Betas → fna"));
			steps.AddView(StepChip("Copy the game folder to your phone"), Wrap(left: 10));
			pickScreen.AddView(steps, Wrap(top: 12));

			var actions = new LinearLayout(this) { Orientation = Orientation.Vertical };
			actions.SetGravity(GravityFlags.CenterHorizontal);
			var select = BlockButton("Select game files", Red, RedDeep, OnRed, 15);
			select.Click += (_, _) => PickFolder();
			actions.AddView(select, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, Dp(46)));
			var zip = Label("or import a .zip", 12, Muted);
			zip.PaintFlags |= PaintFlags.UnderlineText;
			zip.SetPadding(Dp(8), Dp(8), Dp(8), Dp(4));
			zip.Click += (_, _) => PickZip();
			actions.AddView(zip, Wrap(top: 4));
			selectActions = actions;
			pickScreen.AddView(actions, Wrap(top: 16));

			pickStatus = Label("", 12, RedLight);
			pickStatus.Gravity = GravityFlags.CenterHorizontal;
			pickScreen.AddView(pickStatus, Wrap(top: 6));
			return pickScreen;
		}

		private View BuildReadyScreen()
		{
			readyScreen = new LinearLayout(this) { Orientation = Orientation.Vertical, Visibility = ViewStates.Gone };
			readyScreen.SetGravity(GravityFlags.Center);

			var hero = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			hero.SetGravity(GravityFlags.CenterVertical);

			int logoId = Resources!.GetIdentifier("celeste_logo", "drawable", PackageName);
			if (logoId != 0)
			{
				var logo = new ImageView(this);
				logo.SetImageResource(logoId);
				logo.SetAdjustViewBounds(true);
				logo.SetScaleType(ImageView.ScaleType.FitCenter);
				logo.Elevation = Dp(8);
				hero.AddView(logo, new LinearLayout.LayoutParams(Dp(250), Dp(200)));
			}
			else
			{
				var title = Label("CELESTE", 48, Ink);
				title.LetterSpacing = 0.18f;
				title.SetShadowLayer(Dp(10), 0, Dp(2), Color.Argb(160, 0, 0, 0));
				hero.AddView(title);
			}

			playButton = BlockButton("▶  Play", Ice, IceDeep, Night, 24);
			playButton.LetterSpacing = 0.14f;
			playButton.SetPadding(Dp(34), 0, Dp(38), 0);
			playButton.Click += (_, _) => LaunchGame();
			hero.AddView(playButton, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, Dp(64)) { LeftMargin = Dp(44) });
			// Sem parâmetros explícitos, o LinearLayout vertical estica o filho na largura toda e o
			// conteúdo encostava na esquerda.
			readyScreen.AddView(hero, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));

			// Saves e opções sempre à vista na tela inicial: backup, saves do PC e o "abrir direto".
			var home = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			home.SetGravity(GravityFlags.CenterHorizontal);
			var importPc = Chip("Import PC saves");
			importPc.Click += (_, _) => StartActivityForResult(new Intent(Intent.ActionOpenDocumentTree), RequestSaves);
			var backup = Chip("Backup saves");
			backup.Click += (_, _) => StartActivityForResult(new Intent(Intent.ActionOpenDocumentTree), RequestExportSaves);
			var settings = Chip("Settings");
			settings.Click += (_, _) => OpenSettings();
			directChip = Chip("");
			directChip.Click += (_, _) =>
			{
				LauncherPrefs.SetDirectStart(this, !LauncherPrefs.DirectStart(this));
				RefreshState();
			};
			home.AddView(importPc);
			home.AddView(backup, Wrap(left: 8));
			home.AddView(directChip, Wrap(left: 8));
			home.AddView(settings, Wrap(left: 8));
			settingsChip = home;
			readyScreen.AddView(home, Wrap(top: 22));

			readyStatus = Label("", 12, Muted);
			readyStatus.Gravity = GravityFlags.CenterHorizontal;
			readyScreen.AddView(readyStatus, Wrap(top: 10));
			return readyScreen;
		}

		private View BuildChannelBadge()
		{
			var badge = new LinearLayout(this) { Orientation = Orientation.Horizontal, Clickable = true };
			badge.SetGravity(GravityFlags.CenterVertical);
			badge.SetPadding(Dp(5), Dp(5), Dp(12), Dp(5));
			badge.Background = new RippleDrawable(Android.Content.Res.ColorStateList.ValueOf(Color.Argb(60, 255, 140, 40)),
				Rounded(Glass, 6, Line), null);
			var logo = new ImageView(this);
			logo.SetImageResource(Resource.Drawable.channel_logo);
			logo.Background = Rounded(Color.Black, 4);
			logo.ClipToOutline = true;
			badge.AddView(logo, new LinearLayout.LayoutParams(Dp(30), Dp(30)));
			badge.AddView(Label("Projeto Belmante", 11, Muted), Wrap(left: 8));
			badge.Click += (_, _) =>
			{
				try
				{
					StartActivity(new Intent(Intent.ActionView, Uri.Parse(ChannelUrl)));
				}
				catch (ActivityNotFoundException)
				{
					SetStatus(ChannelUrl, error: false);
				}
			};
			return badge;
		}

		/// <summary>Fundo: a arte animada (Android 9+) ou o primeiro quadro; no build público, a key art importada.</summary>
		private void LoadArt()
		{
			int animated = Resources!.GetIdentifier("launcher_bg", "drawable", PackageName);
			if (animated != 0 && OperatingSystem.IsAndroidVersionAtLeast(28))
			{
				try
				{
					Drawable drawable = ImageDecoder.DecodeDrawable(ImageDecoder.CreateSource(Resources, animated));
					background.SetImageDrawable(drawable);
					if (drawable is AnimatedImageDrawable anim)
					{
						anim.RepeatCount = AnimatedImageDrawable.RepeatInfinite;
						anim.Start();
					}
					return;
				}
				catch (Exception e)
				{
					Log.Warn(GameActivity.LogTag, "fundo animado: " + e.Message);
				}
			}
			if (Resources.GetIdentifier("celeste_art", "drawable", PackageName) != 0)
			{
				background.SetImageResource(Resource.Drawable.launcher_art);
				return;
			}
			string imported = System.IO.Path.Combine(GameInstaller.GameDir(this), "Content", "Graphics", "SplashScreen.png");
			if (File.Exists(imported))
				background.SetImageBitmap(BitmapFactory.DecodeFile(imported, new BitmapFactory.Options { InSampleSize = 2 }));
			else
				background.SetImageResource(Resource.Drawable.launcher_art);
		}

		/// <summary>Mostra a tela certa para o estado: sem jogo → seleção; com jogo → Play.</summary>
		private void ShowScreen(bool animate)
		{
			if (root == null)
				return;
			bool installed = GameInstaller.IsInstalled(this);
			View show = installed ? readyScreen : pickScreen;
			View hide = installed ? pickScreen : readyScreen;
			hide.Visibility = ViewStates.Gone;
			show.Visibility = ViewStates.Visible;

			// Na seleção o véu é mais escuro (texto em cima da arte); no Play a arte aparece mais.
			veil.Background = installed
				? new GradientDrawable(GradientDrawable.Orientation.TopBottom!, new int[] { Color.Argb(25, 18, 12, 34), Color.Argb(90, 18, 12, 34), Color.Argb(215, 18, 12, 34) })
				: new GradientDrawable(GradientDrawable.Orientation.TopBottom!, new int[] { Color.Argb(140, 18, 12, 34), Color.Argb(225, 18, 12, 34) });

			if (animate)
			{
				show.Alpha = 0f;
				show.TranslationY = Dp(14);
				show.Animate()!.Alpha(1f).TranslationY(0f).SetDuration(450).SetInterpolator(new DecelerateInterpolator(2f))!.Start();
			}
			RefreshState();
		}

		private void RefreshState()
		{
			if (root == null)
				return;
			selectActions.Visibility = busy ? ViewStates.Invisible : ViewStates.Visible;
			playButton.Enabled = !busy;
			playButton.Alpha = busy ? 0.45f : 1f;
			settingsChip.Visibility = busy ? ViewStates.Invisible : ViewStates.Visible;
			directChip.Text = "Start directly: " + (LauncherPrefs.DirectStart(this) ? "On" : "Off");
			directChip.SetTextColor(LauncherPrefs.DirectStart(this) ? Ice : Muted);
			RefreshSettings();
		}

		private void SetStatus(string? text, bool error)
		{
			if (root == null)
				return;
			pickStatus.Text = text;
			readyStatus.Text = text;
			pickStatus.SetTextColor(error ? RedLight : Muted);
			readyStatus.SetTextColor(error ? RedLight : Muted);
		}

		private void ShowProgress(string message, float fraction)
		{
			if (root == null)
				return;
			progressBox.Visibility = ViewStates.Visible;
			progressText.Text = message;
			progressBar.Indeterminate = fraction < 0;
			if (fraction >= 0)
				progressBar.Progress = (int)(fraction * 1000);
		}

		private void HideProgress()
		{
			if (root != null)
				progressBox.Visibility = ViewStates.Gone;
		}

		#region Peças

		private Typeface? LoadPixelFont()
		{
			try
			{
				return Resources!.GetFont(Resource.Font.pixelify_sans);
			}
			catch (Exception e)
			{
				Log.Warn(GameActivity.LogTag, "fonte: " + e.Message);
				return null;
			}
		}

		/// <summary>Texto na fonte pixel, com espaço entre letras (a fonte fica grudada sem ele) e sem ligaduras.</summary>
		private TextView Label(string text, float sp, Color color)
		{
			var view = new TextView(this) { Text = text };
			view.SetTextSize(ComplexUnitType.Sp, sp);
			view.SetTextColor(color);
			view.SetTypeface(pixel ?? Typeface.Monospace, TypefaceStyle.Normal);
			view.LetterSpacing = 0.08f;
			view.FontFeatureSettings = "'liga' 0, 'clig' 0";
			view.SetIncludeFontPadding(false);
			return view;
		}

		/// <summary>Botão em bloco: cor cheia com um degrau mais escuro embaixo, cantos pequenos.</summary>
		private Button BlockButton(string text, Color face, Color edge, Color textColor, float sp)
		{
			var button = new Button(this) { Text = text, StateListAnimator = null };
			button.SetAllCaps(false);
			button.SetTextSize(ComplexUnitType.Sp, sp);
			button.SetTextColor(textColor);
			button.SetTypeface(pixel ?? Typeface.Monospace, TypefaceStyle.Normal);
			button.LetterSpacing = 0.1f;
			button.FontFeatureSettings = "'liga' 0, 'clig' 0";
			button.SetPadding(Dp(26), 0, Dp(26), 0);
			button.Elevation = Dp(4);

			var layers = new LayerDrawable(new Drawable[] { Rounded(edge, 6), Rounded(face, 6) });
			layers.SetLayerInset(1, 0, 0, 0, Dp(4));
			button.Background = new RippleDrawable(Android.Content.Res.ColorStateList.ValueOf(Color.Argb(70, 255, 255, 255)), layers, null);
			return button;
		}

		private TextView Chip(string text)
		{
			var chip = Label(text, 11, Muted);
			chip.SetPadding(Dp(12), Dp(8), Dp(12), Dp(8));
			chip.Background = new RippleDrawable(Android.Content.Res.ColorStateList.ValueOf(Color.Argb(50, 255, 255, 255)),
				Rounded(Glass, 6, Line), null);
			chip.Clickable = true;
			return chip;
		}

		private TextView StepChip(string text)
		{
			var chip = Label(text, 11, Muted);
			chip.SetPadding(Dp(10), Dp(6), Dp(10), Dp(6));
			chip.Background = Rounded(Glass, 4, Line);
			return chip;
		}

		private GradientDrawable Rounded(Color color, float radiusDp, Color? stroke = null)
		{
			var shape = new GradientDrawable();
			shape.SetColor(color);
			shape.SetCornerRadius(Dp(radiusDp));
			if (stroke is Color s)
				shape.SetStroke(Math.Max(1, Dp(1)), s);
			return shape;
		}

		private static FrameLayout.LayoutParams Match() =>
			new(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

		private LinearLayout.LayoutParams Wrap(int top = 0, int left = 0) =>
			new(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(top), LeftMargin = Dp(left) };

		private int Dp(float dp) => (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, Resources!.DisplayMetrics);

		#endregion
	}
}
