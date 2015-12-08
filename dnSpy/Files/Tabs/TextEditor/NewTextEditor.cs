/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Themes;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.AvalonEdit;
using dnSpy.Shared.UI.Highlighting;
using dnSpy.Shared.UI.MVVM;
using dnSpy.Shared.UI.Themes;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using AR = ICSharpCode.AvalonEdit.Rendering;

namespace dnSpy.Files.Tabs.TextEditor {
	[ExportAutoLoaded]
	sealed class NewTextEditorThemeInitializer : IAutoLoaded {
		readonly IThemeManager themeManager;

		[ImportingConstructor]
		NewTextEditorThemeInitializer(IThemeManager themeManager) {
			this.themeManager = themeManager;
			this.themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			InitializeResources();
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			InitializeResources();
		}

		void InitializeResources() {
			var theme = themeManager.Theme;

			var specialBox = theme.GetTextColor(ColorType.SpecialCharacterBox);
			AR.SpecialCharacterTextRunOptions.BackgroundBrush = specialBox.Background == null ? null : specialBox.Background;
			AR.SpecialCharacterTextRunOptions.ForegroundBrush = specialBox.Foreground == null ? null : specialBox.Foreground;

			foreach (var f in typeof(TextTokenType).GetFields()) {
				if (!f.IsLiteral)
					continue;
				var val = (TextTokenType)f.GetValue(null);
				if (val != TextTokenType.Last)
					UpdateTextEditorResource(val, f.Name);
			}

			UpdateDefaultHighlighter();
		}

		void UpdateTextEditorResource(TextTokenType colorType, string name) {
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

		static Brush GetBrush(Brush b) {
			return b ?? Brushes.Transparent;
		}

		static string GetTextInheritedForegroundResourceKey(string name) {
			return string.Format("TETextInherited{0}Foreground", name);
		}

		static string GetTextInheritedBackgroundResourceKey(string name) {
			return string.Format("TETextInherited{0}Background", name);
		}

		static string GetTextInheritedFontStyleResourceKey(string name) {
			return string.Format("TETextInherited{0}FontStyle", name);
		}

		static string GetTextInheritedFontWeightResourceKey(string name) {
			return string.Format("TETextInherited{0}FontWeight", name);
		}

		static string GetInheritedForegroundResourceKey(string name) {
			return string.Format("TEInherited{0}Foreground", name);
		}

		static string GetInheritedBackgroundResourceKey(string name) {
			return string.Format("TEInherited{0}Background", name);
		}

		static string GetInheritedFontStyleResourceKey(string name) {
			return string.Format("TEInherited{0}FontStyle", name);
		}

		static string GetInheritedFontWeightResourceKey(string name) {
			return string.Format("TEInherited{0}FontWeight", name);
		}

		static readonly Tuple<string, Dictionary<string, ColorType>>[] langFixes = new Tuple<string, Dictionary<string, ColorType>>[] {
			new Tuple<string, Dictionary<string, ColorType>>("XML",
				new Dictionary<string, ColorType>(StringComparer.Ordinal) {
					{ "Comment", ColorType.XmlComment },
					{ "CData", ColorType.XmlCData },
					{ "DocType", ColorType.XmlDocType },
					{ "XmlDeclaration", ColorType.XmlDeclaration },
					{ "XmlTag", ColorType.XmlTag },
					{ "AttributeName", ColorType.XmlAttributeName },
					{ "AttributeValue", ColorType.XmlAttributeValue },
					{ "Entity", ColorType.XmlEntity },
					{ "BrokenEntity", ColorType.XmlBrokenEntity },
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

	sealed class NewTextEditor : ICSharpCode.AvalonEdit.TextEditor {
		readonly IThemeManager themeManager;

		public LanguageTokens LanguageTokens { get; set; }

		public NewTextEditor(IThemeManager themeManager) {
			this.themeManager = themeManager;
			this.themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			Options.AllowToggleOverstrikeMode = true;
			UpdateColors(false);

			TextArea.SelectionCornerRadius = 0;
			TextArea.PreviewKeyDown += TextArea_PreviewKeyDown;
			TextArea.InputBindings.Add(new KeyBinding(new RelayCommand(a => PageUp()), Key.PageUp, ModifierKeys.Control));
			TextArea.InputBindings.Add(new KeyBinding(new RelayCommand(a => PageDown()), Key.PageDown, ModifierKeys.Control));
			TextArea.InputBindings.Add(new KeyBinding(new RelayCommand(a => UpDownLine(false)), Key.Down, ModifierKeys.Control));
			TextArea.InputBindings.Add(new KeyBinding(new RelayCommand(a => UpDownLine(true)), Key.Up, ModifierKeys.Control));
			this.AddHandler(GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnGotKeyboardFocus), true);
			this.AddHandler(LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnLostKeyboardFocus), true);
		}

		public void ShowLineMargin(bool enable) {
			foreach (var margin in TextArea.LeftMargins) {
				if (margin is LineNumberMargin)
					margin.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
				else if (margin is System.Windows.Shapes.Line)
					margin.Visibility = Visibility.Collapsed;
			}
		}

		void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			TextArea.Caret.Show();
			UpdateCurrentLineColors(true);
		}

		void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			TextArea.Caret.Hide();
			UpdateCurrentLineColors(true);
		}

		public void GoToMousePosition() {
			var pos = GetPositionFromMousePosition();
			if (pos != null) {
				TextArea.Caret.Position = pos.Value;
				TextArea.Caret.DesiredXPos = double.NaN;
			}
		}

		public TextViewPosition? GetPositionFromMousePosition() {
			return TextArea.TextView.GetPosition(Mouse.GetPosition(TextArea.TextView) + TextArea.TextView.ScrollOffset);
		}

		public void SetCaretPosition(int line, int column, double desiredXPos = double.NaN) {
			TextArea.Caret.Location = new TextLocation(line, column);
			TextArea.Caret.DesiredXPos = desiredXPos;
		}

		static Point FilterCaretPos(AR.TextView textView, Point pt) {
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

		void TextArea_PreviewKeyDown(object sender, KeyEventArgs e) {
			if (Keyboard.Modifiers == ModifierKeys.None && (e.Key == Key.PageDown || e.Key == Key.PageUp)) {
				var textView = TextArea.TextView;
				var si = (System.Windows.Controls.Primitives.IScrollInfo)textView;

				// Re-use the existing code in AvalonEdit
				var cmd = e.Key == Key.PageDown ? EditingCommands.MoveDownByPage : EditingCommands.MoveUpByPage;
				var target = textView;
				bool canExec = cmd.CanExecute(null, target);
				Debug.Assert(canExec);
				if (canExec) {
					if (e.Key == Key.PageDown)
						si.PageDown();
					else
						si.PageUp();

					cmd.Execute(null, target);
					e.Handled = true;
				}
				return;
			}
		}

		new void PageUp() {
			var textView = TextArea.TextView;
			textView.EnsureVisualLines();
			if (textView.VisualLines.Count > 0) {
				var line = textView.VisualLines[0];
				// If the full height isn't visible, pick the next one
				if (line.VisualTop < textView.VerticalOffset && textView.VisualLines.Count > 1)
					line = textView.VisualLines[1];
				var docLine = line.FirstDocumentLine;
				var caret = TextArea.Caret;
				SetCaretPosition(docLine.LineNumber, caret.Location.Column);
			}
		}

		new void PageDown() {
			var textView = TextArea.TextView;
			textView.EnsureVisualLines();
			if (textView.VisualLines.Count > 0) {
				var line = textView.VisualLines[textView.VisualLines.Count - 1];
				// If the full height isn't visible, pick the previous one
				if (line.VisualTop - textView.VerticalOffset + line.Height > textView.ActualHeight && textView.VisualLines.Count > 1)
					line = textView.VisualLines[textView.VisualLines.Count - 2];
				var docLine = line.LastDocumentLine;
				var caret = TextArea.Caret;
				SetCaretPosition(docLine.LineNumber, caret.Location.Column);
			}
		}

		void UpDownLine(bool up) {
			var textView = TextArea.TextView;
			var scrollViewer = ((System.Windows.Controls.Primitives.IScrollInfo)textView).ScrollOwner;
			textView.EnsureVisualLines();

			var currPos = FilterCaretPos(textView, textView.GetVisualPosition(TextArea.Caret.Position, VisualYPosition.LineMiddle));

			if (!up)
				scrollViewer.LineDown();
			else
				scrollViewer.LineUp();
			textView.UpdateLayout();
			textView.EnsureVisualLines();

			var newPos = FilterCaretPos(textView, currPos);
			var newVisPos = textView.GetPosition(newPos);
			Debug.Assert(newVisPos != null);
			if (newVisPos != null)
				TextArea.Caret.Position = newVisPos.Value;
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			UpdateColors(true);
		}

		void UpdateColors(bool redraw = true) {
			var theme = themeManager.Theme;
			var textColor = theme.GetColor(ColorType.Text);
			Background = textColor.Background == null ? null : textColor.Background;
			Foreground = textColor.Foreground == null ? null : textColor.Foreground;
			FontWeight = textColor.FontWeight ?? FontWeights.Regular;
			FontStyle = textColor.FontStyle ?? FontStyles.Normal;

			var ln = theme.GetColor(ColorType.LineNumber);
			LineNumbersForeground = ln.Foreground == null ? null : ln.Foreground;

			var linkColor = theme.GetTextColor(ColorType.Link);
			TextArea.TextView.LinkTextForegroundBrush = (linkColor.Foreground ?? textColor.Foreground);
			TextArea.TextView.LinkTextBackgroundBrush = linkColor.Background == null ? Brushes.Transparent : linkColor.Background;

			var sel = theme.GetColor(ColorType.Selection);
			TextArea.SelectionBorder = null;
			TextArea.SelectionBrush = sel.Background == null ? null : sel.Background;
			TextArea.SelectionForeground = sel.Foreground == null ? null : sel.Foreground;

			UpdateCurrentLineColors(false);

			if (redraw)
				TextArea.TextView.Redraw();
		}

		void UpdateCurrentLineColors(bool redraw) {
			var theme = themeManager.Theme;
			bool hasFocus = this.IsKeyboardFocusWithin;
			var currentLine = theme.GetColor(hasFocus ? ColorType.CurrentLine : ColorType.CurrentLineNoFocus);
			TextArea.TextView.CurrentLineBackground = currentLine.Background == null ? null : currentLine.Background;
			TextArea.TextView.CurrentLineBorder = new Pen(currentLine.Foreground == null ? null : currentLine.Foreground, 2);
			if (redraw)
				TextArea.TextView.Redraw();
		}

		protected override IVisualLineTransformer CreateColorizer(IHighlightingDefinition highlightingDefinition) {
			Debug.Assert(LanguageTokens != null);
			if (highlightingDefinition.Name == "C#" || highlightingDefinition.Name == "VB" ||
				highlightingDefinition.Name == "IL")
				return new NewHighlightingColorizer(this);
			return base.CreateColorizer(highlightingDefinition);
		}

		sealed class NewHighlightingColorizer : HighlightingColorizer {
			readonly NewTextEditor textEditor;

			public NewHighlightingColorizer(NewTextEditor textEditor) {
				this.textEditor = textEditor;
			}

			protected override IHighlighter CreateHighlighter(AR.TextView textView, TextDocument document) {
				return new NewHighlighter(textEditor, document);
			}
		}

		sealed class NewHighlighter : IHighlighter {
			readonly NewTextEditor textEditor;
			readonly TextDocument document;

			public NewHighlighter(NewTextEditor textEditor, TextDocument document) {
				this.textEditor = textEditor;
				this.document = document;
			}

			public IDocument Document {
				get { return document; }
			}

			public HighlightedLine HighlightLine(int lineNumber) {
				var line = document.GetLineByNumber(lineNumber);
				int offs = line.Offset;
				int endOffs = line.EndOffset;
				var hl = new HighlightedLine(document, line);

				while (offs < endOffs) {
					int defaultTextLength, tokenLength;
					TextTokenType tokenType;
					if (!textEditor.LanguageTokens.Find(offs, out defaultTextLength, out tokenType, out tokenLength)) {
						Debug.Fail("Could not find token info");
						break;
					}

					HighlightingColor color;
					if (tokenLength != 0 && CanAddColor(color = GetColor(tokenType))) {
						hl.Sections.Add(new HighlightedSection {
							Offset = offs + defaultTextLength,
							Length = tokenLength,
							Color = color,
						});
					}

					offs += defaultTextLength + tokenLength;
				}

				return hl;
			}

			bool CanAddColor(HighlightingColor color) {
				return color != null &&
					(color.FontWeight != null || color.FontStyle != null ||
					color.Foreground != null || color.Background != null);
			}

			HighlightingColor GetColor(TextTokenType tokenType) {
				return textEditor.themeManager.Theme.GetTextColor(tokenType.ToColorType()).ToHighlightingColor();
			}

			public IEnumerable<HighlightingColor> GetColorStack(int lineNumber) {
				return new HighlightingColor[0];
			}

			public void UpdateHighlightingState(int lineNumber) {
			}

			public event HighlightingStateChangedEventHandler HighlightingStateChanged {
				add { }
				remove { }
			}

			public void BeginHighlighting() {
			}

			public void EndHighlighting() {
			}

			public HighlightingColor GetNamedColor(string name) {
				return null;
			}

			public HighlightingColor DefaultTextColor {
				get { return null; }
			}

			public void Dispose() {
			}
		}
	}
}
