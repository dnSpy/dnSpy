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

		public IInputElement FocusedElement {
			get { return this.TextArea; }
		}

		readonly TextTokenInfos infos;
		readonly IThemeManager themeManager;
		readonly ITextEditorSettings textEditorSettings;
		readonly SearchPanel searchPanel;

		public DnSpyTextEditor(IThemeManager themeManager, ITextEditorSettings textEditorSettings) {
			this.infos = new TextTokenInfos();
			this.themeManager = themeManager;
			this.textEditorSettings = textEditorSettings;
			this.textEditorSettings.PropertyChanged += TextEditorSettings_PropertyChanged;
			this.themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			Options.AllowToggleOverstrikeMode = true;
			Options.RequireControlModifierForHyperlinkClick = false;
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
		}

		protected override void OnDragOver(DragEventArgs e) {
			base.OnDragOver(e);

			if (!e.Handled) {
				// The text editor seems to allow anything
				if (e.Data.GetDataPresent(typeof(dnSpy.Tabs.TabItemImpl))) {
					e.Effects = DragDropEffects.None;
					e.Handled = true;
					return;
				}
			}
		}

		void TextEditorSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
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

			protected override IHighlighter CreateHighlighter(TextView textView, TextDocument document) {
				return new NewHighlighter(textEditor, document);
			}
		}

		sealed class NewHighlighter : IHighlighter {
			readonly DnSpyTextEditor textEditor;
			readonly TextDocument document;

			public NewHighlighter(DnSpyTextEditor textEditor, TextDocument document) {
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
				if (offs >= endOffs)
					return hl;

				var infoPart = textEditor.Find(offs);
				while (offs < endOffs) {
					int defaultTextLength, tokenLength;
					TextTokenKind tokenKind;
					if (!infoPart.FindByDocOffset(offs, out defaultTextLength, out tokenKind, out tokenLength))
						return hl;

					HighlightingColor color;
					if (tokenLength != 0 && CanAddColor(color = GetColor(tokenKind))) {
						hl.Sections.Add(new HighlightedSection {
							Offset = offs + defaultTextLength,
							Length = tokenLength,
							Color = color,
						});
					}

					offs += defaultTextLength + tokenLength;
				}
				Debug.Assert(offs == endOffs);

				return hl;
			}

			bool CanAddColor(HighlightingColor color) {
				return color != null &&
					(color.FontWeight != null || color.FontStyle != null ||
					color.Foreground != null || color.Background != null);
			}

			HighlightingColor GetColor(TextTokenKind tokenKind) {
				return textEditor.themeManager.Theme.GetTextColor(tokenKind.ToColorType()).ToHighlightingColor();
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

		struct TextTokenInfoPart {
			public int Offset { get; }
			public TextTokenInfo Info { get; }

			static TextTokenInfoPart() {
				var info = new TextTokenInfo();
				info.Finish();
				Default = new TextTokenInfoPart(0, info);
			}
			public static readonly TextTokenInfoPart Default;

			public TextTokenInfoPart(int offset, TextTokenInfo info) {
				Offset = offset;
				Info = info;
			}

			public int DocOffsetToRelativeOffset(int docOffset) {
				Debug.Assert(Info == Default.Info || (Offset <= docOffset && docOffset < Offset + Info.Length));
				return docOffset - Offset;
			}

			public bool FindByDocOffset(int docOffset, out int defaultTextLength, out TextTokenKind tokenKind, out int tokenLength) {
				return Info.Find(DocOffsetToRelativeOffset(docOffset), out defaultTextLength, out tokenKind, out tokenLength);
			}
		}

		sealed class TextTokenInfos {
			readonly List<TextTokenInfoPart> infos = new List<TextTokenInfoPart>();

			public TextTokenInfoPart Find(int docOffset) {
				for (int i = 0; i < infos.Count; i++) {
					var info = infos[(previousReturnedIndex + i) % infos.Count];
					if ((info.Info.Length == 0 && info.Offset == docOffset) || (info.Offset <= docOffset && docOffset < info.Offset + info.Info.Length)) {
						previousReturnedIndex = i;
						return info;
					}
				}

				return TextTokenInfoPart.Default;
			}
			int previousReturnedIndex;

			public void Add(int offset, TextTokenInfo info) {
				Debug.Assert((infos.Count == 0 && offset == 0) || (infos.Count > 0 && infos.Last().Offset + infos.Last().Info.Length <= offset));
				infos.Add(new TextTokenInfoPart(offset, info));
			}

			public void SetAsyncUpdatingAfterChanges(int docOffset) {
				AddOrUpdate(docOffset, TextTokenInfoPart.Default.Info);
			}

			public void AddOrUpdate(int docOffset, TextTokenInfo newInfo) {
				for (int i = 0; i < infos.Count; i++) {
					int mi = (previousReturnedIndex + i) % infos.Count;
					var info = infos[mi];
					if (info.Offset == docOffset) {
						infos[mi] = new TextTokenInfoPart(docOffset, newInfo);
						return;
					}
				}
				Add(docOffset, newInfo);
			}

			public TextTokenInfo RemoveLastTextTokenInfo() {
				Debug.Assert(infos.Count > 0);
				if (infos.Count == 0)
					return null;
				int index = infos.Count - 1;
				var info = infos[index];
				infos.RemoveAt(index);
				return info.Info;
			}

			public void Clear() {
				infos.Clear();
			}
		}

		public void SetDocumentColorInfo(TextTokenInfo info, bool finish = true) {
			if (info == null)
				info = new TextTokenInfo();
			if (finish)
				info.Finish();
			infos.Clear();
			infos.Add(0, info);
		}

		TextTokenInfoPart Find(int docOffset) {
			return infos.Find(docOffset);
		}

		public void SetAsyncUpdatingAfterChanges(int docOffset) {
			infos.SetAsyncUpdatingAfterChanges(docOffset);
		}

		public void AddOrUpdate(int docOffset, TextTokenInfo info) {
			infos.AddOrUpdate(docOffset, info);
			TextArea.TextView.Redraw(docOffset, info.Length);
		}

		public TextTokenInfo RemoveLastTextTokenInfo() {
			return infos.RemoveLastTextTokenInfo();
		}

		public void ClearTextTokenInfos() {
			infos.Clear();
		}
	}
}
