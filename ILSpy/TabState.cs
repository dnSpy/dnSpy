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
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using dnSpy.AsmEditor;
using dnSpy.AsmEditor.Hex;
using dnSpy.HexEditor;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.ILSpy.dntheme;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy {
	public enum TabStateType {
		DecompiledCode,
		HexEditor,
	}

	public abstract class TabState : IDisposable, INotifyPropertyChanged {
		public TabItem TabItem;

		public abstract string Header { get; }
		public abstract TabStateType Type { get; }
		public abstract FrameworkElement ScaleElement { get; }
		public abstract string FileName { get; }
		public abstract string Name { get; }
		public abstract UIElement FocusedElement { get; }

		public bool IsActive {
			get { return isActive; }
			set {
				if (isActive != value) {
					isActive = value;
					OnPropertyChanged("IsActive");
				}
			}
		}
		bool isActive;

		public bool IsSelected {
			get { return isSelected; }
			set {
				if (isSelected != value) {
					isSelected = value;
					OnPropertyChanged("IsSelected");
				}
			}
		}
		bool isSelected;

		public ICommand CloseCommand {
			get { return new RelayCommand(a => Close()); }
		}

		internal TabManagerBase Owner;

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propName) {
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

		const int MAX_HEADER_LENGTH = 40;
		public string ShortHeader {
			get {
				var header = Header;
				if (header.Length <= MAX_HEADER_LENGTH)
					return header;
				return header.Substring(0, MAX_HEADER_LENGTH) + "…";
			}
		}

		public virtual string ToolTip {
			get {
				var shortHeader = ShortHeader;
				var header = Header;
				return shortHeader == header ? null : header;
			}
		}

		protected TabState() {
			var tabItem = new TabItem();
			TabItem = tabItem;
			TabItem.Header = this;
			tabItem.DataContext = this;
			TabItem.Style = (Style)App.Current.FindResource("TabStateTabItemStyle");
		}

		protected void InstallMouseWheelZoomHandler() {
			ScaleElement.MouseWheel += OnMouseWheel;
		}

		void OnMouseWheel(object sender, MouseWheelEventArgs e) {
			if (Keyboard.Modifiers != ModifierKeys.Control)
				return;

			MainWindow.Instance.ZoomMouseWheel(this, e.Delta);
			e.Handled = true;
		}

		public static TabState GetTabState(FrameworkElement elem) {
			if (elem == null)
				return null;
			return (TabState)elem.Tag;
		}

		protected void UpdateHeader() {
			OnPropertyChanged("Header");
			OnPropertyChanged("ShortHeader");
			OnPropertyChanged("ToolTip");
		}

		void Close() {
			Owner.Close(this);
		}

		public virtual void FocusContent() {
			var uiel = TabItem.Content as UIElement;
			var sv = uiel as ScrollViewer;
			if (sv != null)
				uiel = sv.Content as UIElement ?? uiel;
			if (uiel != null)
				uiel.Focus();
		}

		public abstract SavedTabState CreateSavedTabState();

		public void Dispose() {
			Dispose(true);
		}

		protected virtual void Dispose(bool isDisposing) {
		}
	}

	public sealed class DecompileTabState : TabState {
		public readonly DecompilerTextView TextView = new DecompilerTextView();
		internal readonly NavigationHistory<NavigationState> History = new NavigationHistory<NavigationState>();
		internal bool ignoreDecompilationRequests;
		internal bool HasDecompiled;

		public override string FileName {
			get {
				var mod = ILSpyTreeNode.GetModule(DecompiledNodes);
				return mod == null ? null : mod.Location;
			}
		}

		public override string Name {
			get {
				var mod = ILSpyTreeNode.GetModule(DecompiledNodes);
				return mod == null ? null : mod.Name;
			}
		}

		public override UIElement FocusedElement {
			get {
				if (TabItem.Content == TextView) {
					if (TextView.waitAdornerButton.IsVisible)
						return TextView.waitAdornerButton;
					return TextView.TextEditor.TextArea;
				}

				return TabItem.Content as UIElement;
			}
		}

		public override FrameworkElement ScaleElement {
			get { return TextView.TextEditor.TextArea; }
		}

		public override TabStateType Type {
			get { return TabStateType.DecompiledCode; }
		}

		public ILSpyTreeNode[] DecompiledNodes {
			get { return decompiledNodes; }
		}
		ILSpyTreeNode[] decompiledNodes = new ILSpyTreeNode[0];

		public bool IsTextViewInVisualTree {
			get { return TabItem.Content == TextView; }
		}

		public string Title {
			get { return title; }
			set {
				if (title != value) {
					title = value;
					UpdateHeader();
				}
			}
		}
		string title;

		public Language Language {
			get { return language; }
		}
		Language language;

		public override string Header {
			get {
				var nodes = DecompiledNodes;
				if (nodes == null || nodes.Length == 0)
					return Title ?? "<empty>";

				if (nodes.Length == 1)
					return nodes[0].ToString(Language);

				var sb = new StringBuilder();
				foreach (var node in nodes) {
					if (sb.Length > 0)
						sb.Append(", ");
					sb.Append(node.ToString(Language));
				}
				return sb.ToString();
			}
		}

		internal void SetDecompileProps(Language language, ILSpyTreeNode[] nodes) {
			this.language = language;
			UnhookEvents();
			this.decompiledNodes = nodes ?? new ILSpyTreeNode[0];
			HookEvents();
			this.title = null;
			UpdateHeader();
		}

		void HookEvents() {
			foreach (var node in decompiledNodes)
				node.PropertyChanged += node_PropertyChanged;
		}

		void UnhookEvents() {
			foreach (var node in decompiledNodes)
				node.PropertyChanged -= node_PropertyChanged;
		}

		void node_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "Text")
				UpdateHeader();
		}

		public static DecompileTabState GetDecompileTabState(DecompilerTextView elem) {
			return (DecompileTabState)GetTabState(elem);
		}

		public DecompileTabState(Language language) {
			var view = TextView;
			TabItem.Content = view;
			view.Tag = this;
			this.language = language;
			ContextMenuProvider.Add(view);
			view.DragOver += view_DragOver;
			view.OnThemeUpdated();
			InstallMouseWheelZoomHandler();
		}

		void view_DragOver(object sender, DragEventArgs e) {
			// The text editor seems to allow anything
			if (e.Data.GetDataPresent(typeof(TabItem))) {
				e.Effects = DragDropEffects.None;
				e.Handled = true;
				return;
			}
		}

		public override void FocusContent() {
			if (this.TextView == TabItem.Content)
				this.TextView.TextEditor.TextArea.Focus();
			else
				base.FocusContent();
		}

		protected override void Dispose(bool isDisposing) {
			if (isDisposing)
				TextView.Dispose();
			UnhookEvents();
			decompiledNodes = new ILSpyTreeNode[0];
		}

		public bool Equals(ILSpyTreeNode[] nodes, Language language) {
			if (Language != language)
				return false;
			if (DecompiledNodes.Length != nodes.Length)
				return false;
			for (int i = 0; i < DecompiledNodes.Length; i++) {
				if (DecompiledNodes[i] != nodes[i])
					return false;
			}
			return true;
		}

		public override SavedTabState CreateSavedTabState() {
			var savedState = new SavedDecompileTabState();
			savedState.Language = Language.Name;
			savedState.Paths = new List<FullNodePathName>();
			savedState.ActiveAutoLoadedAssemblies = new List<string>();
			foreach (var node in DecompiledNodes) {
				savedState.Paths.Add(node.CreateFullNodePathName());
				var autoAsm = GetAutoLoadedAssemblyNode(node);
				if (!string.IsNullOrEmpty(autoAsm))
					savedState.ActiveAutoLoadedAssemblies.Add(autoAsm);
			}
			savedState.EditorPositionState = TextView.EditorPositionState;
			return savedState;
		}

		static string GetAutoLoadedAssemblyNode(ILSpyTreeNode node) {
			var assyNode = MainWindow.GetAssemblyTreeNode(node);
			if (assyNode == null)
				return null;
			var loadedAssy = assyNode.LoadedAssembly;
			if (!(loadedAssy.IsLoaded && loadedAssy.IsAutoLoaded))
				return null;

			return loadedAssy.FileName;
		}
	}

	public sealed class HexTabState : TabState {
		internal readonly HexBox HexBox;

		public override UIElement FocusedElement {
			get { return HexBox; }
		}

		public override string Header {
			get {
				var doc = HexBox.Document;
				if (doc == null)
					return "<NO DOC>";
				var filename = HexBox.Document.Name;
				try {
					return Path.GetFileName(filename);
				}
				catch {
				}
				return filename;
            }
		}

		public override string ToolTip {
			get {
				var doc = HexBox.Document;
				if (doc == null)
					return null;
				return doc.Name;
			}
		}

		public override FrameworkElement ScaleElement {
			get { return HexBox; }
		}

		public override TabStateType Type {
			get { return TabStateType.HexEditor; }
		}

		public override string FileName {
			get { return HexBox.Document == null ? null : HexBox.Document.Name; }
		}

		public override string Name {
			get { return HexBox.Document == null ? null : Path.GetFileName(HexBox.Document.Name); }
		}

		public HexTabState() {
			this.HexBox = new HexBox();
			this.HexBox.Tag = this;
			var scroller = new ScrollViewer();
			scroller.Content = HexBox;
			scroller.CanContentScroll = true;
			scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
			scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			this.TabItem.Content = scroller;

			this.HexBox.SetBinding(Control.FontFamilyProperty, new Binding("SelectedFont") { Source = Options.DisplaySettingsPanel.CurrentDisplaySettings });
			this.HexBox.SetBinding(Control.FontSizeProperty, new Binding("SelectedFontSize") { Source = Options.DisplaySettingsPanel.CurrentDisplaySettings });
			this.HexBox.SetResourceReference(Control.BackgroundProperty, GetBackgroundResourceKey(ColorType.HexText));
			this.HexBox.SetResourceReference(Control.ForegroundProperty, GetForegroundResourceKey(ColorType.HexText));
			this.HexBox.SetResourceReference(HexBox.OffsetForegroundProperty, GetForegroundResourceKey(ColorType.HexOffset));
			this.HexBox.SetResourceReference(HexBox.Byte0ForegroundProperty, GetForegroundResourceKey(ColorType.HexByte0));
			this.HexBox.SetResourceReference(HexBox.Byte1ForegroundProperty, GetForegroundResourceKey(ColorType.HexByte1));
			this.HexBox.SetResourceReference(HexBox.ByteErrorForegroundProperty, GetForegroundResourceKey(ColorType.HexByteError));
			this.HexBox.SetResourceReference(HexBox.AsciiForegroundProperty, GetForegroundResourceKey(ColorType.HexAscii));
			this.HexBox.SetResourceReference(HexBox.CaretForegroundProperty, GetBackgroundResourceKey(ColorType.HexCaret));
			this.HexBox.SetResourceReference(HexBox.InactiveCaretForegroundProperty, GetBackgroundResourceKey(ColorType.HexInactiveCaret));
			this.HexBox.SetResourceReference(HexBox.SelectionBackgroundProperty, GetBackgroundResourceKey(ColorType.HexSelection));
			this.HexBox.SetResourceReference(Control.FontStyleProperty, GetFontStyleResourceKey(ColorType.HexText));
			this.HexBox.SetResourceReference(Control.FontWeightProperty, GetFontWeightResourceKey(ColorType.HexText));

			ContextMenuProvider.Add(this.HexBox);

			InstallMouseWheelZoomHandler();

			BytesGroupCount = null;
			BytesPerLine = null;
			UseHexPrefix = null;
			ShowAscii = null;
			LowerCaseHex = null;
		}

		internal static void OnThemeUpdatedStatic() {
			var theme = Themes.Theme;

			var color = theme.GetColor(ColorType.HexText).InheritedColor;
			App.Current.Resources[GetBackgroundResourceKey(ColorType.HexText)] = GetBrush(color.Background);
			App.Current.Resources[GetForegroundResourceKey(ColorType.HexText)] = GetBrush(color.Foreground);
			App.Current.Resources[GetFontStyleResourceKey(ColorType.HexText)] = color.FontStyle ?? FontStyles.Normal;
			App.Current.Resources[GetFontWeightResourceKey(ColorType.HexText)] = color.FontWeight ?? FontWeights.Normal;

			UpdateForeground(theme, ColorType.HexOffset);
			UpdateForeground(theme, ColorType.HexByte0);
			UpdateForeground(theme, ColorType.HexByte1);
			UpdateForeground(theme, ColorType.HexByteError);
			UpdateForeground(theme, ColorType.HexAscii);
			UpdateBackground(theme, ColorType.HexCaret);
			UpdateBackground(theme, ColorType.HexInactiveCaret);
			UpdateBackground(theme, ColorType.HexSelection);
		}

		static void UpdateForeground(Theme theme, ColorType colorType) {
			var color = theme.GetColor(colorType).TextInheritedColor;
			App.Current.Resources[GetForegroundResourceKey(colorType)] = GetBrush(color.Foreground);
		}

		static void UpdateBackground(Theme theme, ColorType colorType) {
			var color = theme.GetColor(colorType).TextInheritedColor;
			App.Current.Resources[GetBackgroundResourceKey(colorType)] = GetBrush(color.Background);
		}

		static Brush GetBrush(HighlightingBrush b) {
			return b == null ? Brushes.Transparent : b.GetBrush(null);
		}

		static string GetBackgroundResourceKey(ColorType colorType) {
			return string.Format("HB_{0}_Background", Enum.GetName(typeof(ColorType), colorType));
		}

		static string GetForegroundResourceKey(ColorType colorType) {
			return string.Format("HB_{0}_Foreground", Enum.GetName(typeof(ColorType), colorType));
		}

		static string GetFontStyleResourceKey(ColorType colorType) {
			return string.Format("HB_{0}_FontStyle", Enum.GetName(typeof(ColorType), colorType));
		}

		static string GetFontWeightResourceKey(ColorType colorType) {
			return string.Format("HB_{0}_FontWeight", Enum.GetName(typeof(ColorType), colorType));
		}

		public void Restore(SavedHexTabState state) {
			BytesGroupCount = state.BytesGroupCount;
			BytesPerLine = state.BytesPerLine;
			UseHexPrefix = state.UseHexPrefix;
			ShowAscii = state.ShowAscii;
			LowerCaseHex = state.LowerCaseHex;

			HexBox.HexOffsetSize = state.HexOffsetSize;
			HexBox.UseRelativeOffsets = state.UseRelativeOffsets;
			HexBox.BaseOffset = state.BaseOffset;

			if (HexBox.IsLoaded)
				HexBox.State = state.HexBoxState;
			else
				new StateRestorer(HexBox, state.HexBoxState);
		}

		sealed class StateRestorer {
			readonly HexBox hexBox;
			readonly HexBoxState state;

			public StateRestorer(HexBox hexBox, HexBoxState state) {
				this.hexBox = hexBox;
				this.state = state;
				this.hexBox.Loaded += HexBox_Loaded;
			}

			private void HexBox_Loaded(object sender, RoutedEventArgs e) {
				this.hexBox.Loaded -= HexBox_Loaded;
				hexBox.UpdateLayout();
				hexBox.State = state;
			}
		}

		public override SavedTabState CreateSavedTabState() {
			var state = new SavedHexTabState();
			state.BytesGroupCount = BytesGroupCount;
			state.BytesPerLine = BytesPerLine;
			state.UseHexPrefix = UseHexPrefix;
			state.ShowAscii = ShowAscii;
			state.LowerCaseHex = LowerCaseHex;

			state.HexOffsetSize = HexBox.HexOffsetSize;
			state.UseRelativeOffsets = HexBox.UseRelativeOffsets;
			state.BaseOffset = HexBox.BaseOffset;
			state.HexBoxState = HexBox.State;
			state.FileName = HexBox.Document == null ? string.Empty : HexBox.Document.Name;
			return state;
		}

		public void SetDocument(HexDocument doc) {
			this.HexBox.Document = doc;
			UpdateHeader();
		}

		public void InitializeStartEndOffset() {
			var doc = HexBox.Document;
			if (doc == null)
				return;

			HexBox.StartOffset = 0;
			HexBox.EndOffset = doc.Size == 0 ? 0 : doc.Size - 1;
		}

		public int? BytesGroupCount {
			get { return useDefault_BytesGroupCount ? (int?)null : HexBox.BytesGroupCount; }
			set {
				if (value == null) {
					useDefault_BytesGroupCount = true;
					HexBox.ClearValue(HexBox.BytesGroupCountProperty);
					HexBox.SetBinding(HexBox.BytesGroupCountProperty, new Binding("BytesGroupCount") { Source = HexSettings.Instance });
				}
				else {
					useDefault_BytesGroupCount = false;
					HexBox.BytesGroupCount = value.Value;
				}
			}
		}
		bool useDefault_BytesGroupCount;

		public int? BytesPerLine {
			get { return useDefault_BytesPerLine ? (int?)null : HexBox.BytesPerLine; }
			set {
				if (value == null) {
					useDefault_BytesPerLine = true;
					HexBox.ClearValue(HexBox.BytesPerLineProperty);
					HexBox.SetBinding(HexBox.BytesPerLineProperty, new Binding("BytesPerLine") { Source = HexSettings.Instance });
				}
				else {
					useDefault_BytesPerLine = false;
					HexBox.BytesPerLine = Math.Min(HexSettings.MAX_BYTES_PER_LINE, value.Value);
				}
			}
		}
		bool useDefault_BytesPerLine;

		public bool? UseHexPrefix {
			get { return useDefault_UseHexPrefix ? (bool?)null : HexBox.UseHexPrefix; }
			set {
				if (value == null) {
					useDefault_UseHexPrefix = true;
					HexBox.ClearValue(HexBox.UseHexPrefixProperty);
					HexBox.SetBinding(HexBox.UseHexPrefixProperty, new Binding("UseHexPrefix") { Source = HexSettings.Instance });
				}
				else {
					useDefault_UseHexPrefix = false;
					HexBox.UseHexPrefix = value.Value;
				}
			}
		}
		bool useDefault_UseHexPrefix;

		public bool? ShowAscii {
			get { return useDefault_ShowAscii ? (bool?)null : HexBox.ShowAscii; }
			set {
				if (value == null) {
					useDefault_ShowAscii = true;
					HexBox.ClearValue(HexBox.ShowAsciiProperty);
					HexBox.SetBinding(HexBox.ShowAsciiProperty, new Binding("ShowAscii") { Source = HexSettings.Instance });
				}
				else {
					useDefault_ShowAscii = false;
					HexBox.ShowAscii = value.Value;
				}
			}
		}
		bool useDefault_ShowAscii;

		public bool? LowerCaseHex {
			get { return useDefault_LowerCaseHex ? (bool?)null : HexBox.LowerCaseHex; }
			set {
				if (value == null) {
					useDefault_LowerCaseHex = true;
					HexBox.ClearValue(HexBox.LowerCaseHexProperty);
					HexBox.SetBinding(HexBox.LowerCaseHexProperty, new Binding("LowerCaseHex") { Source = HexSettings.Instance });
				}
				else {
					useDefault_LowerCaseHex = false;
					HexBox.LowerCaseHex = value.Value;
				}
			}
		}
		bool useDefault_LowerCaseHex;

		public void SelectAndMoveCaret(ulong fileOffset, ulong length) {
			ulong end = length == 0 ? fileOffset : fileOffset + length - 1 < fileOffset ? ulong.MaxValue : fileOffset + length - 1;
			if (length == 0)
				HexBox.Selection = null;
			else
				HexBox.Selection = new HexSelection(fileOffset, end);
			HexBox.CaretPosition = new HexBoxPosition(fileOffset, HexBox.CaretPosition.Kind, 0);
		}
	}
}
