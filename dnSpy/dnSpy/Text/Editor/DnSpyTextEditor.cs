/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Themes;
using dnSpy.Shared.AvalonEdit;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;

namespace dnSpy.Text.Editor {
	[ExportAutoLoaded(LoadType = AutoLoadedLoadType.BeforePlugins)]
	sealed class DnSpyTextEditorThemeInitializer : IAutoLoaded {
		readonly IThemeManager themeManager;

		[ImportingConstructor]
		DnSpyTextEditorThemeInitializer(IThemeManager themeManager) {
			this.themeManager = themeManager;
			this.themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			InitializeResources();
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) => InitializeResources();

		void InitializeResources() {
			var theme = themeManager.Theme;

			var specialBox = theme.GetTextColor(ColorType.SpecialCharacterBox);
			SpecialCharacterTextRunOptions.BackgroundBrush = specialBox.Background;
			SpecialCharacterTextRunOptions.ForegroundBrush = specialBox.Foreground;

			foreach (var f in typeof(OutputColor).GetFields()) {
				if (!f.IsLiteral)
					continue;
				var val = (OutputColor)f.GetValue(null);
				if (val != OutputColor.Last)
					UpdateTextEditorResource(val, f.Name);
			}

			UpdateDefaultHighlighter();
		}

		void UpdateTextEditorResource(OutputColor colorType, string name) {
			var theme = themeManager.Theme;

			var color = theme.GetTextColor(colorType.ToColorType());
			Application.Current.Resources[GetTextInheritedForegroundResourceKey(name)] = GetBrush(color.Foreground);
			Application.Current.Resources[GetTextInheritedBackgroundResourceKey(name)] = GetBrush(color.Background);
			Application.Current.Resources[GetTextInheritedFontStyleResourceKey(name)] = color.FontStyle ?? FontStyles.Normal;
			Application.Current.Resources[GetTextInheritedFontWeightResourceKey(name)] = color.FontWeight ?? FontWeights.Normal;

			color = theme.GetColor(colorType.ToColorType());
			Application.Current.Resources[GetInheritedForegroundResourceKey(name)] = GetBrush(color.Foreground);
			Application.Current.Resources[GetInheritedBackgroundResourceKey(name)] = GetBrush(color.Background);
			Application.Current.Resources[GetInheritedFontStyleResourceKey(name)] = color.FontStyle ?? FontStyles.Normal;
			Application.Current.Resources[GetInheritedFontWeightResourceKey(name)] = color.FontWeight ?? FontWeights.Normal;
		}

		static Brush GetBrush(Brush b) => b ?? Brushes.Transparent;
		static string GetTextInheritedForegroundResourceKey(string name) => string.Format("TETextInherited{0}Foreground", name);
		static string GetTextInheritedBackgroundResourceKey(string name) => string.Format("TETextInherited{0}Background", name);
		static string GetTextInheritedFontStyleResourceKey(string name) => string.Format("TETextInherited{0}FontStyle", name);
		static string GetTextInheritedFontWeightResourceKey(string name) => string.Format("TETextInherited{0}FontWeight", name);
		static string GetInheritedForegroundResourceKey(string name) => string.Format("TEInherited{0}Foreground", name);
		static string GetInheritedBackgroundResourceKey(string name) => string.Format("TEInherited{0}Background", name);
		static string GetInheritedFontStyleResourceKey(string name) => string.Format("TEInherited{0}FontStyle", name);
		static string GetInheritedFontWeightResourceKey(string name) => string.Format("TEInherited{0}FontWeight", name);

		static readonly Tuple<string, Dictionary<string, ColorType>>[] langFixes = new Tuple<string, Dictionary<string, ColorType>>[] {
			new Tuple<string, Dictionary<string, ColorType>>("XML",
				new Dictionary<string, ColorType>(StringComparer.Ordinal) {
					{ "Comment", ColorType.XmlComment },
					{ "CData", ColorType.XmlCDataSection },
					{ "DocType", ColorType.XmlName },
					{ "XmlDeclaration", ColorType.XmlName },
					{ "XmlTag", ColorType.XmlName },
					{ "AttributeName", ColorType.XmlAttributeName },
					{ "AttributeValue", ColorType.XmlAttributeValue },
					{ "Entity", ColorType.XmlAttributeName },
					{ "BrokenEntity", ColorType.XmlAttributeName },
				}
			)
		};
		void UpdateDefaultHighlighter() {
			foreach (var fix in langFixes)
				UpdateLanguage(fix.Item1, fix.Item2);
		}

		void UpdateLanguage(string name, Dictionary<string, ColorType> colorNames) {
			var lang = HighlightingManager.Instance.GetDefinition(name);
			Debug.Assert(lang != null);
			if (lang == null)
				return;

			foreach (var color in lang.NamedHighlightingColors) {
				ColorType colorType;
				bool b = colorNames.TryGetValue(color.Name, out colorType);
				Debug.Assert(b);
				if (!b)
					continue;
				var ourColor = themeManager.Theme.GetTextColor(colorType);
				color.Background = ourColor.Background.ToHighlightingBrush();
				color.Foreground = ourColor.Foreground.ToHighlightingBrush();
				color.FontWeight = ourColor.FontWeight;
				color.FontStyle = ourColor.FontStyle;
			}
		}
	}

	sealed class DnSpyTextEditor : TextEditor, IDisposable {
		public FrameworkElement FocusedElement => this.TextArea;
		public FrameworkElement ScaleElement => this.TextArea;
		internal IThemeManager ThemeManager { get; }

		readonly SearchPanel searchPanel;

		public DnSpyTextEditor(IThemeManager themeManager, ITextEditorSettings textEditorSettings) {
			this.ThemeManager = themeManager;
			this.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(".il");
			this.ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;

			TextArea.TextView.DocumentChanged += TextView_DocumentChanged;
			UpdateColors(false);

			searchPanel = SearchPanel.Install(TextArea);
			searchPanel.RegisterCommands(this.CommandBindings);

			TextArea.SelectionCornerRadius = 0;
			this.AddHandler(GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnGotKeyboardFocus), true);
			this.AddHandler(LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnLostKeyboardFocus), true);

			SetBinding(FontFamilyProperty, new Binding {
				Source = textEditorSettings,
				Path = new PropertyPath(nameof(textEditorSettings.FontFamily)),
				Mode = BindingMode.OneWay,
			});
			SetBinding(FontSizeProperty, new Binding {
				Source = textEditorSettings,
				Path = new PropertyPath(nameof(textEditorSettings.FontSize)),
				Mode = BindingMode.OneWay,
			});

			this.lineNumberMargin = new ICSharpCode.AvalonEdit.Editing.LineNumberMargin { Visibility = Visibility.Collapsed };
			this.lineNumberMargin.SetBinding(ForegroundProperty, new Binding(nameof(LineNumbersForeground)) { Source = this });
			TextArea.LeftMargins.Insert(0, this.lineNumberMargin);
			OnHighlightCurrentLineChanged();
		}

		void TextView_DocumentChanged(object sender, EventArgs e) {
			// Document must not change
			throw new InvalidOperationException();
		}

		public void Dispose() {
			ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
		}

		protected override void OnDragOver(DragEventArgs e) {
			base.OnDragOver(e);

			if (!e.Handled) {
				// The text editor seems to allow anything
				if (e.Data.GetDataPresent(typeof(Tabs.TabItemImpl))) {
					e.Effects = DragDropEffects.None;
					e.Handled = true;
					return;
				}
			}
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
			base.OnPropertyChanged(e);
			Debug.Assert(e.Property != ShowLineNumbersProperty, "Don't call base.ShowLineNumbers");
		}

		public new bool ShowLineNumbers {
			get { return lineNumberMargin.Visibility != Visibility.Collapsed; }
			set {
				if (value)
					lineNumberMargin.Visibility = Visibility.Visible;
				else
					lineNumberMargin.Visibility = Visibility.Collapsed;
			}
		}
		readonly ICSharpCode.AvalonEdit.Editing.LineNumberMargin lineNumberMargin;

		public bool HighlightCurrentLine {
			get { return highlightCurrentLine; }
			set {
				if (highlightCurrentLine != value) {
					highlightCurrentLine = value;
					Options.HighlightCurrentLine = highlightCurrentLine;
					OnHighlightCurrentLineChanged();
				}
			}
		}
		bool highlightCurrentLine;

		void OnHighlightCurrentLineChanged() => Options.HighlightCurrentLine = HighlightCurrentLine;

		void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			TextArea.Caret.Show();
			UpdateCurrentLineColors(HighlightCurrentLine);
			OnHighlightCurrentLineChanged();
		}

		void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			TextArea.Caret.Hide();
			UpdateCurrentLineColors(HighlightCurrentLine);
			// Do as VS: don't hide the highlighted line
		}

		public void GoToMousePosition() {
			var pos = GetPositionFromMousePosition();
			if (pos != null) {
				TextArea.Caret.Position = pos.Value;
				TextArea.Caret.DesiredXPos = double.NaN;
			}
		}

		public TextViewPosition? GetPositionFromMousePosition() =>
			TextArea.TextView.GetPosition(Mouse.GetPosition(TextArea.TextView) + TextArea.TextView.ScrollOffset);

		public void SetCaretPosition(int line, int column, double desiredXPos = double.NaN) {
			TextArea.Caret.Location = new TextLocation(line, column);
			TextArea.Caret.DesiredXPos = desiredXPos;
		}

		static Point FilterCaretPos(TextView textView, Point pt) {
			Point firstPos;
			if (textView.VisualLines.Count == 0)
				firstPos = new Point(0, 0);
			else {
				var line = textView.VisualLines[0];
				if (line.VisualTop < textView.VerticalOffset && textView.VisualLines.Count > 1)
					line = textView.VisualLines[1];
				firstPos = line.GetVisualPosition(0, VisualYPosition.LineMiddle);
			}

			Point lastPos;
			if (textView.VisualLines.Count == 0)
				lastPos = new Point(0, 0);
			else {
				var line = textView.VisualLines[textView.VisualLines.Count - 1];
				if (line.VisualTop - textView.VerticalOffset + line.Height > textView.ActualHeight && textView.VisualLines.Count > 1)
					line = textView.VisualLines[textView.VisualLines.Count - 2];
				lastPos = line.GetVisualPosition(0, VisualYPosition.LineMiddle);
			}

			if (pt.Y < firstPos.Y)
				return new Point(pt.X, firstPos.Y);
			else if (pt.Y > lastPos.Y)
				return new Point(pt.X, lastPos.Y);
			return pt;
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			var theme = ThemeManager.Theme;
			var marker = theme.GetColor(ColorType.SearchResultMarker);
			searchPanel.MarkerBrush = marker.Background ?? Brushes.LightGreen;
			UpdateColors(true);
		}

		void UpdateColors(bool redraw = true) {
			var theme = ThemeManager.Theme;
			var textColor = theme.GetColor(ColorType.Text);
			Background = textColor.Background;
			Foreground = textColor.Foreground;
			FontWeight = textColor.FontWeight ?? FontWeights.Regular;
			FontStyle = textColor.FontStyle ?? FontStyles.Normal;

			LineNumbersForeground = theme.GetColor(ColorType.LineNumber).Foreground;

			var linkColor = theme.GetTextColor(ColorType.Link);
			TextArea.TextView.LinkTextForegroundBrush = (linkColor.Foreground ?? textColor.Foreground);
			TextArea.TextView.LinkTextBackgroundBrush = linkColor.Background ?? Brushes.Transparent;

			var visWsColor = theme.GetTextColor(ColorType.VisibleWhitespace);
			TextArea.TextView.NonPrintableCharacterBrush = visWsColor.Foreground ?? textColor.Foreground;

			var sel = theme.GetColor(ColorType.SelectedText);
			TextArea.SelectionBorder = null;
			TextArea.SelectionBrush = sel.Background;
			TextArea.SelectionForeground = sel.Foreground;

			UpdateCurrentLineColors(false);

			if (redraw)
				TextArea.TextView.Redraw();
		}

		void UpdateCurrentLineColors(bool redraw) {
			var theme = ThemeManager.Theme;
			bool hasFocus = this.IsKeyboardFocusWithin;
			var currentLine = theme.GetColor(hasFocus ? ColorType.CurrentLine : ColorType.CurrentLineNoFocus);
			TextArea.TextView.CurrentLineBackground = currentLine.Background;
			TextArea.TextView.CurrentLineBorder = new Pen(currentLine.Foreground, 2);
			if (redraw) {
				// Redraw the highlighted line
				int oldLine = TextArea.TextView.HighlightedLine;
				TextArea.TextView.HighlightedLine = -1;
				TextArea.TextView.HighlightedLine = oldLine;
			}
		}

		public IEnumerable<GuidObject> GetGuidObjects(bool openedFromKeyboard) {
			var position = openedFromKeyboard ? TextArea.Caret.Position : GetPositionFromMousePosition();
			if (position != null)
				yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTEDITORLOCATION_GUID, new TextEditorLocation(position.Value.Line - 1, position.Value.Column - 1));
		}
	}
}
