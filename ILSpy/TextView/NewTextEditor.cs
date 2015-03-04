// New highlighter code using the already cached colors provided by the code generators

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using ICSharpCode.NRefactory;
using ICSharpCode.ILSpy.dntheme;

using AR = ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.ILSpy.TextView
{
	sealed class NewTextEditor : TextEditor
	{
		public LanguageTokens LanguageTokens { get; set; }

		public NewTextEditor()
		{
			Loaded += NewTextEditor_Loaded;
		}

		void NewTextEditor_Loaded(object sender, RoutedEventArgs e)
		{
			OnThemeUpdated();
		}

		internal void OnThemeUpdated()
		{
			var theme = MainWindow.Instance.Theme;
			var textColor = theme.GetColor(ColorType.Text).InheritedColor;
			Background = textColor.Background == null ? null : textColor.Background.GetBrush(null);
			Foreground = textColor.Foreground == null ? null : textColor.Foreground.GetBrush(null);
			FontWeight = textColor.FontWeight ?? FontWeights.Regular;
			FontStyle = textColor.FontStyle ?? FontStyles.Normal;

			ICSharpCode.ILSpy.Debugger.Bookmarks.BreakpointBookmark.HighlightingColor = theme.GetColor(dntheme.ColorType.BreakpointStatement).TextInheritedColor;
			ICSharpCode.ILSpy.Debugger.Bookmarks.CurrentLineBookmark.HighlightingColor = theme.GetColor(dntheme.ColorType.CurrentStatement).TextInheritedColor;
			var specialBox = theme.GetColor(dntheme.ColorType.SpecialCharacterBox).TextInheritedColor;
			ICSharpCode.AvalonEdit.Rendering.SpecialCharacterTextRunOptions.Brush = specialBox.Background == null ? null : specialBox.Background.GetBrush(null);

			var ln = theme.GetColor(ColorType.LineNumber).InheritedColor;
			LineNumbersForeground = ln.Foreground == null ? null : ln.Foreground.GetBrush(null);

			var linkColor = theme.GetColor(ColorType.Link).TextInheritedColor;
			TextArea.TextView.LinkTextForegroundBrush = (linkColor.Foreground ?? textColor.Foreground).GetBrush(null);
			TextArea.TextView.LinkTextBackgroundBrush = linkColor.Background == null ? Brushes.Transparent : linkColor.Background.GetBrush(null);

			var sel = theme.GetColor(ColorType.Selection).InheritedColor;
			TextArea.SelectionBorder = null;
			TextArea.SelectionBrush = sel.Background == null ? null : sel.Background.GetBrush(null);
			TextArea.SelectionForeground = sel.Foreground == null ? null : sel.Foreground.GetBrush(null);

			var currentLine = theme.GetColor(ColorType.CurrentLine).InheritedColor;
			TextArea.TextView.CurrentLineBackground = currentLine.Background == null ? null : currentLine.Background.GetBrush(null);
			var currentLineBorder = theme.GetColor(ColorType.CurrentLineBorder).InheritedColor;
			TextArea.TextView.CurrentLineBorder = new Pen(currentLineBorder.Background == null ? null : currentLineBorder.Background.GetBrush(null), 2);

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
		void UpdateDefaultHighlighter()
		{
			foreach (var fix in langFixes)
				UpdateLanguage(fix.Item1, fix.Item2);
		}

		void UpdateLanguage(string name, Dictionary<string, ColorType> colorNames)
		{
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
				var ourColor = MainWindow.Instance.Theme.GetColor(colorType).TextInheritedColor;
				color.Background = ourColor.Background;
				color.Foreground = ourColor.Foreground;
				color.FontWeight = ourColor.FontWeight;
				color.FontStyle = ourColor.FontStyle;
			}
		}

		protected override IVisualLineTransformer CreateColorizer(IHighlightingDefinition highlightingDefinition)
		{
			Debug.Assert(LanguageTokens != null);
			if (highlightingDefinition.Name == "C#" || highlightingDefinition.Name == "VB" ||
				highlightingDefinition.Name == "ILAsm")
				return new NewHighlightingColorizer(this);
			return base.CreateColorizer(highlightingDefinition);
		}

		sealed class NewHighlightingColorizer : HighlightingColorizer
		{
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

			public NewHighlighter(NewTextEditor textEditor, TextDocument document)
			{
				this.textEditor = textEditor;
				this.document = document;
			}

			public IDocument Document
			{
				get { return document; }
			}

			public HighlightedLine HighlightLine(int lineNumber)
			{
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

			bool CanAddColor(HighlightingColor color)
			{
				return color != null &&
					(color.FontWeight != null || color.FontStyle != null ||
					color.Foreground != null || color.Background != null);
			}

			HighlightingColor GetColor(TextTokenType tokenType)
			{
				return MainWindow.Instance.Theme.GetColor(tokenType).TextInheritedColor;
			}

			public IEnumerable<HighlightingColor> GetColorStack(int lineNumber)
			{
				return new HighlightingColor[0];
			}

			public void UpdateHighlightingState(int lineNumber)
			{
			}

			public event HighlightingStateChangedEventHandler HighlightingStateChanged;

			public void BeginHighlighting()
			{
			}

			public void EndHighlighting()
			{
			}

			public HighlightingColor GetNamedColor(string name)
			{
				return null;
			}

			public HighlightingColor DefaultTextColor
			{
				get { return null; }
			}

			public void Dispose()
			{
			}
		}
	}
}
