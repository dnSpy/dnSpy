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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.TextEditor;
using dnSpy.Contracts.Themes;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.AvalonEdit;
using dnSpy.Shared.Highlighting;
using dnSpy.Shared.MVVM;
using dnSpy.Shared.Themes;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;

namespace dnSpy.TextEditor {
	[ExportAutoLoaded(LoadType = AutoLoadedLoadType.BeforePlugins)]
	sealed class DnSpyTextEditorThemeInitializer : IAutoLoaded {
		readonly IThemeManager themeManager;

		[ImportingConstructor]
		DnSpyTextEditorThemeInitializer(IThemeManager themeManager) {
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
			SpecialCharacterTextRunOptions.BackgroundBrush = specialBox.Background == null ? null : specialBox.Background;
			SpecialCharacterTextRunOptions.ForegroundBrush = specialBox.Foreground == null ? null : specialBox.Foreground;

			foreach (var f in typeof(TextTokenKind).GetFields()) {
				if (!f.IsLiteral)
					continue;
				var val = (TextTokenKind)f.GetValue(null);
				if (val != TextTokenKind.Last)
					UpdateTextEditorResource(val, f.Name);
			}

			UpdateDefaultHighlighter();
		}

		void UpdateTextEditorResource(TextTokenKind colorType, string name) {
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

	sealed class DnSpyTextEditor : ICSharpCode.AvalonEdit.TextEditor, IDisposable {
		static DnSpyTextEditor() {
			HighlightingManager.Instance.RegisterHighlighting(
				"IL", new string[] { ".il" }, () => {
					using (var s = typeof(DnSpyTextEditor).Assembly.GetManifestResourceStream(typeof(DnSpyTextEditor), "IL.xshd")) {
						using (var reader = new XmlTextReader(s))
							return HighlightingLoader.Load(reader, HighlightingManager.Instance);
					}
				}
			);
		}

		public IInputElement FocusedElement => this.TextArea;
		public MyTextBuffer TextBuffer { get; }

		internal sealed class MyTextBuffer : ITextBuffer, IDisposable {
			public IContentType ContentType {
				get { return contentType; }
				set {
					if (value == null)
						throw new ArgumentNullException(nameof(value));
					if (contentType != value) {
						contentType = value;
						RecreateColorizers();
					}
				}
			}
			IContentType contentType;

			public ITextBufferColorizer[] Colorizers { get; private set; }

			readonly DnSpyTextEditor owner;
			readonly ITextBufferColorizerCreator textBufferColorizerCreator;
			ITextBufferColorizer defaultColorizer;

			public MyTextBuffer(DnSpyTextEditor owner, ITextBufferColorizerCreator textBufferColorizerCreator, IContentType contentType) {
				if (contentType == null)
					throw new ArgumentNullException(nameof(contentType));
				this.owner = owner;
				this.textBufferColorizerCreator = textBufferColorizerCreator;
				this.contentType = contentType;
				this.defaultColorizer = null;
				this.Colorizers = Array.Empty<ITextBufferColorizer>();
				RecreateColorizers();
			}

			public void SetDefaultColorizer(ITextBufferColorizer defaultColorizer) {
				Debug.Assert(this.defaultColorizer == null);
				if (this.defaultColorizer != null)
					throw new InvalidOperationException();
				this.defaultColorizer = defaultColorizer;
				RecreateColorizers();
			}

			public void RecreateColorizers() {
				ClearColorizers();
				var list = new List<ITextBufferColorizer>();
				if (defaultColorizer != null)
					list.Add(defaultColorizer);
				list.AddRange(textBufferColorizerCreator.Create(this));
				Colorizers = list.ToArray();
			}

			void ClearColorizers() {
				foreach (var c in Colorizers)
					(c as IDisposable)?.Dispose();
				Colorizers= Array.Empty<ITextBufferColorizer>();
			}

			public void Dispose() => ClearColorizers();
		}

		readonly IThemeManager themeManager;
		readonly ITextEditorSettings textEditorSettings;
		readonly SearchPanel searchPanel;

		public DnSpyTextEditor(IThemeManager themeManager, ITextEditorSettings textEditorSettings, ITextBufferColorizerCreator textBufferColorizerCreator, IContentTypeRegistryService contentTypeRegistryService) {
			this.themeManager = themeManager;
			this.textEditorSettings = textEditorSettings;
			this.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(".il");
			this.textEditorSettings.PropertyChanged += TextEditorSettings_PropertyChanged;
			this.themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			Options.AllowToggleOverstrikeMode = true;
			Options.RequireControlModifierForHyperlinkClick = false;
			this.TextBuffer = new MyTextBuffer(this, textBufferColorizerCreator, contentTypeRegistryService.UnknownContentType);
			UpdateColors(false);

			searchPanel = SearchPanel.Install(TextArea);
			searchPanel.RegisterCommands(this.CommandBindings);
			searchPanel.Localization = new AvalonEditSearchPanelLocalization();

			TextArea.SelectionCornerRadius = 0;
			TextArea.PreviewKeyDown += TextArea_PreviewKeyDown;
			TextArea.InputBindings.Add(new KeyBinding(new RelayCommand(a => PageUp()), Key.PageUp, ModifierKeys.Control));
			TextArea.InputBindings.Add(new KeyBinding(new RelayCommand(a => PageDown()), Key.PageDown, ModifierKeys.Control));
			TextArea.InputBindings.Add(new KeyBinding(new RelayCommand(a => UpDownLine(false)), Key.Down, ModifierKeys.Control));
			TextArea.InputBindings.Add(new KeyBinding(new RelayCommand(a => UpDownLine(true)), Key.Up, ModifierKeys.Control));
			this.AddHandler(GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnGotKeyboardFocus), true);
			this.AddHandler(LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnLostKeyboardFocus), true);

			TextArea.MouseRightButtonDown += (s, e) => GoToMousePosition();

			SetBinding(FontFamilyProperty, new Binding {
				Source = textEditorSettings,
				Path = new PropertyPath("FontFamily"),
				Mode = BindingMode.OneWay,
			});
			SetBinding(FontSizeProperty, new Binding {
				Source = textEditorSettings,
				Path = new PropertyPath("FontSize"),
				Mode = BindingMode.OneWay,
			});
			SetBinding(WordWrapProperty, new Binding {
				Source = textEditorSettings,
				Path = new PropertyPath("WordWrap"),
				Mode = BindingMode.OneWay,
			});

			OnHighlightCurrentLineChanged();
			OnShowLineNumbersChanged();
		}

		public void Dispose() {
			this.textEditorSettings.PropertyChanged -= TextEditorSettings_PropertyChanged;
			this.themeManager.ThemeChanged -= ThemeManager_ThemeChanged;
			TextBuffer.Dispose();
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

		void TextEditorSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "HighlightCurrentLine")
				OnHighlightCurrentLineChanged();
			else if (e.PropertyName == "ShowLineNumbers")
				OnShowLineNumbersChanged();
		}

		void OnHighlightCurrentLineChanged() {
			Options.HighlightCurrentLine = textEditorSettings.HighlightCurrentLine;
		}

		public void OnShowLineNumbersChanged() {
			ShowLineMargin(textEditorSettings.ShowLineNumbers);
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
			OnHighlightCurrentLineChanged();
		}

		void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			TextArea.Caret.Hide();
			UpdateCurrentLineColors(true);
			// Do as VS: don't hide the highlighted line
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
			var theme = themeManager.Theme;
			var marker = theme.GetColor(ColorType.SearchResultMarker);
			searchPanel.MarkerBrush = marker.Background == null ? Brushes.LightGreen : marker.Background;
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
			if (highlightingDefinition.Name == "C#" || highlightingDefinition.Name == "VB" ||
				highlightingDefinition.Name == "IL")
				return new NewHighlightingColorizer(this);
			return base.CreateColorizer(highlightingDefinition);
		}

		sealed class NewHighlightingColorizer : HighlightingColorizer {
			readonly DnSpyTextEditor textEditor;

			public NewHighlightingColorizer(DnSpyTextEditor textEditor) {
				this.textEditor = textEditor;
			}

			protected override IHighlighter CreateHighlighter(TextView textView, TextDocument document) =>
				new NewHighlighter(textEditor, document);
		}

		sealed class NewHighlighter : IHighlighter {
			readonly DnSpyTextEditor textEditor;
			readonly TextDocument document;

			public NewHighlighter(DnSpyTextEditor textEditor, TextDocument document) {
				this.textEditor = textEditor;
				this.document = document;
			}

			public IDocument Document => document;

			struct ColorInfo {
				public readonly Contracts.TextEditor.Span Span;
				public readonly ITextColor Foreground;
				public readonly ITextColor Background;
				public readonly double Priority;
				public ITextColor TextColor {
					get {
						if (Foreground == Background)
							return Foreground ?? Contracts.Themes.TextColor.Null;
						return new TextColor(Foreground?.Foreground, Background?.Background, Foreground?.FontWeight, Foreground?.FontStyle);
					}
				}
				public ColorInfo(Contracts.TextEditor.Span span, ITextColor color, double priority) {
					Span = span;
					Foreground = color;
					Background = color;
					Priority = priority;
				}
				public ColorInfo(Contracts.TextEditor.Span span, ITextColor fg, ITextColor bg, double priority) {
					Span = span;
					Foreground = fg;
					Background = bg;
					Priority = priority;
				}
			}

			public HighlightedLine HighlightLine(int lineNumber) {
				var line = document.GetLineByNumber(lineNumber);
				int lineStartOffs = line.Offset;
				int lineEndOffs = line.EndOffset;
				var hl = new HighlightedLine(document, line);
				if (lineStartOffs >= lineEndOffs)
					return hl;

				var span = Contracts.TextEditor.Span.FromBounds(lineStartOffs, lineEndOffs);
				var theme = textEditor.themeManager.Theme;
				var allInfos = new List<ColorInfo>();
				foreach (var colorizer in textEditor.TextBuffer.Colorizers) {
					foreach (var cspan in colorizer.GetColorSpans(span)) {
						var colorSpan = cspan.Span.Intersection(span);
						if (colorSpan == null || colorSpan.Value.IsEmpty)
							continue;
						var color = cspan.Color.ToTextColor(theme);
						if (color.Foreground == null && color.Background == null)
							continue;
						allInfos.Add(new ColorInfo(colorSpan.Value, color, cspan.Priority));
					}
				}

				allInfos.Sort((a, b) => a.Span.Start - b.Span.Start);

				List<ColorInfo> list;
				// Check if it's the common case
				if (!HasOverlaps(allInfos))
					list = allInfos;
				else {
					Debug.Assert(allInfos.Count != 0);

					list = new List<ColorInfo>(allInfos.Count);
					var stack = new List<ColorInfo>();
					int currOffs = 0;
					for (int i = 0; i < allInfos.Count;) {
						if (stack.Count == 0)
							currOffs = allInfos[i].Span.Start;
						for (; i < allInfos.Count; i++) {
							var curr = allInfos[i];
							if (curr.Span.Start != currOffs)
								break;
							stack.Add(curr);
						}
						Debug.Assert(stack.Count != 0);
						Debug.Assert(stack.All(a => a.Span.Start == currOffs));
						stack.Sort((a, b) => b.Priority.CompareTo(a.Priority));
						int end = stack.Min(a => a.Span.End);
						end = Math.Min(end, i < allInfos.Count ? allInfos[i].Span.Start : lineEndOffs);
						var fgColor = stack.FirstOrDefault(a => a.Foreground?.Foreground != null);
						var bgColor = stack.FirstOrDefault(a => a.Background?.Background != null);
						var newInfo = new ColorInfo(Contracts.TextEditor.Span.FromBounds(currOffs, end), fgColor.Foreground, bgColor.Background, 0);
						Debug.Assert(list.Count == 0 || list[list.Count - 1].Span.End <= newInfo.Span.Start);
						list.Add(newInfo);
						for (int j = stack.Count - 1; j >= 0; j--) {
							var info = stack[j];
							if (newInfo.Span.End >= info.Span.End)
								stack.RemoveAt(j);
							else
								stack[j] = new ColorInfo(Contracts.TextEditor.Span.FromBounds(newInfo.Span.End, info.Span.End), info.Foreground, info.Background, info.Priority);
						}
						currOffs = newInfo.Span.End;
					}
				}
				Debug.Assert(!HasOverlaps(list));

				foreach (var info in list) {
					hl.Sections.Add(new HighlightedSection {
						Offset = info.Span.Start,
						Length = info.Span.Length,
						Color = info.TextColor.ToHighlightingColor(),
					});
				}

				return hl;
			}

			bool HasOverlaps(List<ColorInfo> sortedList) {
				for (int i = 1; i < sortedList.Count; i++) {
					if (sortedList[i - 1].Span.End > sortedList[i].Span.Start)
						return true;
				}
				return false;
			}

			public event HighlightingStateChangedEventHandler HighlightingStateChanged {
				add { }
				remove { }
			}

			public IEnumerable<HighlightingColor> GetColorStack(int lineNumber) => new HighlightingColor[0];
			public void UpdateHighlightingState(int lineNumber) { }
			public void BeginHighlighting() { }
			public void EndHighlighting() { }
			public HighlightingColor GetNamedColor(string name) => null;
			public HighlightingColor DefaultTextColor => null;
			public void Dispose() { }
		}
	}

	sealed class DnSpyTextEditorColorizerHelper {
		readonly DnSpyTextEditor dnSpyTextEditor;

		public DnSpyTextEditorColorizerHelper(DnSpyTextEditor dnSpyTextEditor) {
			this.dnSpyTextEditor = dnSpyTextEditor;
			this.cachedColorsList = new CachedColorsList();
		}

		public void SetDocumentCachedColors(CachedTextTokenColors cachedColors, bool finish = true) {
			if (cachedColors == null)
				cachedColors = new CachedTextTokenColors();
			if (finish)
				cachedColors.Finish();
			cachedColorsList.Clear();
			cachedColorsList.Add(0, cachedColors);
		}
		readonly CachedColorsList cachedColorsList;

		public ITextBufferColorizer CreateTextBufferColorizer() =>
			new CachedColorsListColorizer(cachedColorsList, ColorPriority.Normal);
		public void SetAsyncUpdatingAfterChanges(int docOffset) => cachedColorsList.SetAsyncUpdatingAfterChanges(docOffset);
		public CachedTextTokenColors RemoveLastCachedTextTokenColors() => cachedColorsList.RemoveLastCachedTextTokenColors();
		public void ClearCachedColors() => cachedColorsList.Clear();
		public void AddOrUpdate(int docOffset, CachedTextTokenColors cachedColors) {
			cachedColorsList.AddOrUpdate(docOffset, cachedColors);
			dnSpyTextEditor.TextArea.TextView.Redraw(docOffset, cachedColors.Length);
		}
	}
}
