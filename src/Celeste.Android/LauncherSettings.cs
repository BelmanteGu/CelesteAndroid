using System;
using Android.Content;
using Android.Views;
using Android.Widget;
using Color = Android.Graphics.Color;

namespace CelesteAndroid
{
	/// <summary>
	/// Painel "Settings" do launcher: gráficos, controles na tela (e o editor de posições),
	/// widescreen, saves e arquivos do jogo. Abre pelo botão da tela do Play ou pelo atalho do ícone.
	/// </summary>
	public partial class LauncherActivity
	{
		private View? overlay;
		private TextView? driverValue;
		private TextView? touchValue;
		private TextView? wideValue;
		private TextView? directValue;

		private void OpenSettings()
		{
			if (root == null || busy)
				return;
			CloseOverlay();

			var panel = new LinearLayout(this) { Orientation = Orientation.Vertical };
			panel.SetBackgroundColor(Color.Argb(240, 18, 12, 34));
			panel.SetPadding(Dp(36), Dp(22), Dp(36), Dp(18));
			panel.Clickable = true; // não deixa o toque passar para a tela de baixo

			var header = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			header.SetGravity(GravityFlags.CenterVertical);
			var title = Label("Settings", 24, Ink);
			title.LetterSpacing = 0.12f;
			header.AddView(title, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));
			var close = Chip("Close");
			close.Click += (_, _) => CloseOverlay();
			header.AddView(close);
			panel.AddView(header);

			var columns = new LinearLayout(this) { Orientation = Orientation.Horizontal };

			var game = Column("Game");
			driverValue = Toggle(game, "Graphics", ToggleDriver);
			touchValue = Toggle(game, "Touch controls", ToggleTouch);
			wideValue = Toggle(game, "Widescreen", ToggleWidescreen);
			directValue = Toggle(game, "Open game directly", () => LauncherPrefs.SetDirectStart(this, !LauncherPrefs.DirectStart(this)));
			AddAction(game, "Edit touch layout", OpenTouchLayout);
			columns.AddView(game, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));

			var files = Column("Saves & files");
			AddAction(files, "Import saves", () => StartActivityForResult(new Intent(Intent.ActionOpenDocumentTree), RequestSaves));
			AddAction(files, "Export saves", () => StartActivityForResult(new Intent(Intent.ActionOpenDocumentTree), RequestExportSaves));
			AddAction(files, "Change game files", PickFolder);
			AddAction(files, "Import game .zip", PickZip);
			columns.AddView(files, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f) { LeftMargin = Dp(28) });

			panel.AddView(columns, Wrap(top: 14));

			var hint = Label("With \"Open game directly\" on, long-press the app icon → Settings to come back here.", 10, Muted);
			panel.AddView(hint, Wrap(top: 12));

			overlay = panel;
			root.AddView(panel, Match());
			RefreshSettings();
		}

		private void CloseOverlay()
		{
			if (overlay != null && root != null)
				root.RemoveView(overlay);
			overlay = null;
			driverValue = touchValue = wideValue = directValue = null;
			RefreshState();
		}

		public override void OnBackPressed()
		{
			if (overlay != null)
			{
				CloseOverlay();
				return;
			}
			base.OnBackPressed();
		}

		private void RefreshSettings()
		{
			if (driverValue != null)
				driverValue.Text = LauncherPrefs.Driver(this) == "OpenGL" ? "OpenGL ES" : "Vulkan";
			if (touchValue != null)
				touchValue.Text = LauncherPrefs.TouchControls(this) ? "On" : "Off";
			if (wideValue != null)
				wideValue.Text = LauncherPrefs.Widescreen(this) ? "On" : "Off";
			if (directValue != null)
				directValue.Text = LauncherPrefs.DirectStart(this) ? "On" : "Off";
		}

		private LinearLayout Column(string title)
		{
			var column = new LinearLayout(this) { Orientation = Orientation.Vertical };
			column.AddView(Label(title.ToUpperInvariant(), 10, Muted));
			return column;
		}

		/// <summary>Linha "nome ...... valor"; tocar alterna o valor.</summary>
		private TextView Toggle(LinearLayout column, string name, Action toggle)
		{
			var row = new LinearLayout(this) { Orientation = Orientation.Horizontal, Clickable = true };
			row.SetGravity(GravityFlags.CenterVertical);
			row.SetPadding(Dp(12), Dp(9), Dp(12), Dp(9));
			row.Background = new Android.Graphics.Drawables.RippleDrawable(
				Android.Content.Res.ColorStateList.ValueOf(Color.Argb(50, 255, 255, 255)), Rounded(Glass, 6, Line), null);
			row.AddView(Label(name, 13, Ink), new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));
			var value = Label("", 13, Ice);
			row.AddView(value);
			row.Click += (_, _) =>
			{
				toggle();
				RefreshSettings();
			};
			column.AddView(row, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(8) });
			return value;
		}

		private void AddAction(LinearLayout column, string name, Action action)
		{
			var row = Chip(name);
			row.SetTextColor(Ink);
			row.SetTextSize(Android.Util.ComplexUnitType.Sp, 13);
			row.SetPadding(Dp(12), Dp(9), Dp(12), Dp(9));
			row.Click += (_, _) =>
			{
				CloseOverlay();
				action();
			};
			column.AddView(row, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(8) });
		}

		private void ToggleDriver() =>
			// Vazio = deixa o FNA escolher (SDL_GPU → Vulkan); "OpenGL" força o OpenGL ES.
			LauncherPrefs.SetDriver(this, LauncherPrefs.Driver(this) == "OpenGL" ? "" : "OpenGL");

		private void ToggleTouch() => LauncherPrefs.SetTouchControls(this, !LauncherPrefs.TouchControls(this));

		private void ToggleWidescreen() => LauncherPrefs.SetWidescreen(this, !LauncherPrefs.Widescreen(this));

		/// <summary>
		/// Editor das posições dos controles de toque: mostra a área do jogo e deixa arrastar cada
		/// controle; o resultado vai para as preferências e o jogo lê ao iniciar.
		/// </summary>
		private void OpenTouchLayout()
		{
			if (root == null)
				return;
			CloseOverlay();
			TouchLayoutSpec initial = TouchLayoutSpec.TryParse(LauncherPrefs.TouchLayout(this), out TouchLayoutSpec parsed)
				? parsed
				: TouchLayoutSpec.Default;

			var editor = new TouchLayoutEditor(this, initial);
			var panel = new LinearLayout(this) { Orientation = Orientation.Vertical, Clickable = true };
			panel.SetBackgroundColor(Color.Argb(247, 18, 12, 34));
			panel.SetPadding(Dp(18), Dp(16), Dp(18), Dp(14));

			panel.AddView(Label("Touch layout", 20, Ink));
			panel.AddView(Label("Drag each control where you want it. Pause stays visible even when the pad fades.", 11, Muted), Wrap(top: 4));
			panel.AddView(editor, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1f) { TopMargin = Dp(10), BottomMargin = Dp(10) });

			var bar = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			bar.SetGravity(GravityFlags.CenterVertical);
			var size = Label("", 12, Ink);
			void ShowSize() => size.Text = $"Size {(int)Math.Round(editor.Scale * 100)}%";
			ShowSize();

			View AddChip(string text, Action click)
			{
				var chip = Chip(text);
				chip.SetTextColor(Ink);
				chip.Click += (_, _) => click();
				bar.AddView(chip, Wrap(left: 8));
				return chip;
			}

			AddChip("A-", () => { editor.Scale -= 0.05f; ShowSize(); });
			bar.AddView(size, Wrap(left: 10));
			AddChip("A+", () => { editor.Scale += 0.05f; ShowSize(); });
			bar.AddView(new View(this), new LinearLayout.LayoutParams(0, 1, 1f));
			AddChip("Reset", () => { editor.Reset(); ShowSize(); });
			AddChip("Left-handed", editor.Mirror);
			AddChip("Cancel", CloseOverlay);
			var save = BlockButton("Save", Red, RedDeep, OnRed, 13);
			save.Click += (_, _) =>
			{
				LauncherPrefs.SetTouchLayout(this, editor.Spec.Encode());
				CloseOverlay();
			};
			bar.AddView(save, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, Dp(38)) { LeftMargin = Dp(10) });
			panel.AddView(bar);

			overlay = panel;
			root.AddView(panel, Match());
		}
	}
}
