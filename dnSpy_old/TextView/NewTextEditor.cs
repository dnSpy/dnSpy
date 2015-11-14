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

// New highlighter code using the already cached colors provided by the code generators

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts;
using dnSpy.Contracts.Themes;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.AvalonEdit;
using dnSpy.Shared.UI.Highlighting;
using dnSpy.Shared.UI.Themes;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.ILSpy;
using AR = ICSharpCode.AvalonEdit.Rendering;

namespace dnSpy.TextView {
	sealed class NewTextEditor : TextEditor {
		public LanguageTokens LanguageTokens { get; set; }

		public NewTextEditor() {
			Options.AllowToggleOverstrikeMode = true;
		}

		internal static void OnThemeUpdatedStatic() {
			var theme = DnSpy.App.ThemeManager.Theme;

			var specialBox = theme.GetTextColor(ColorType.SpecialCharacterBox);
			ICSharpCode.AvalonEdit.Rendering.SpecialCharacterTextRunOptions.BackgroundBrush = specialBox.Background == null ? null : specialBox.Background;
			ICSharpCode.AvalonEdit.Rendering.SpecialCharacterTextRunOptions.ForegroundBrush = specialBox.Foreground == null ? null : specialBox.Foreground;

			foreach (var f in typeof(TextTokenType).GetFields()) {
				if (!f.IsLiteral)
					continue;
				var val = (TextTokenType)f.GetValue(null);
				if (val != TextTokenType.Last)
					UpdateTextEditorResource(val, f.Name);
			}
		}

		static void UpdateTextEditorResource(TextTokenType colorType, string name) {
			var theme = DnSpy.App.ThemeManager.Theme;

			var color = theme.GetTextColor(colorType.ToColorType());
			App.Current.Resources[GetTextInheritedForegroundResourceKey(name)] = GetBrush(color.Foreground);
			App.Current.Resources[GetTextInheritedBackgroundResourceKey(name)] = GetBrush(color.Background);
			App.Current.Resources[GetTextInheritedFontStyleResourceKey(name)] = color.FontStyle ?? FontStyles.Normal;
			App.Current.Resources[GetTextInheritedFontWeightResourceKey(name)] = color.FontWeight ?? FontWeights.Normal;

			color = theme.GetColor(colorType.ToColorType());
			App.Current.Resources[GetInheritedForegroundResourceKey(name)] = GetBrush(color.Foreground);
			App.Current.Resources[GetInheritedBackgroundResourceKey(name)] = GetBrush(color.Background);
			App.Current.Resources[GetInheritedFontStyleResourceKey(name)] = color.FontStyle ?? FontStyles.Normal;
			App.Current.Resources[GetInheritedFontWeightResourceKey(name)] = color.FontWeight ?? FontWeights.Normal;
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

		internal void OnThemeUpdated() {
			var theme = DnSpy.App.ThemeManager.Theme;
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

			var currentLine = theme.GetColor(ColorType.CurrentLine);
			TextArea.TextView.CurrentLineBackground = currentLine.Background == null ? null : currentLine.Background;
			TextArea.TextView.CurrentLineBorder = new Pen(currentLine.Foreground == null ? null : currentLine.Foreground, 2);

			UpdateDefaultHighlighter();
			TextArea.TextView.Redraw();
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
				var ourColor = DnSpy.App.ThemeManager.Theme.GetTextColor(colorType);
				color.Background = ourColor.Background.ToHighlightingBrush();
				color.Foreground = ourColor.Foreground.ToHighlightingBrush();
				color.FontWeight = ourColor.FontWeight;
				color.FontStyle = ourColor.FontStyle;
			}
		}

		protected override IVisualLineTransformer CreateColorizer(IHighlightingDefinition highlightingDefinition) {
			Debug.Assert(LanguageTokens != null);
			if (highlightingDefinition.Name == "C#" || highlightingDefinition.Name == "VB" ||
				highlightingDefinition.Name == "ILAsm")
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
				return DnSpy.App.ThemeManager.Theme.GetTextColor(tokenType.ToColorType()).ToHighlightingColor();
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
