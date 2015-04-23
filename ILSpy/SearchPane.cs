// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.ILSpy.TreeNodes.Filters;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Utils;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ICSharpCode.ILSpy
{
	[Export(typeof(IPaneCreator))]
	public class SearchPaneCreator : IPaneCreator
	{
		public IPane Create(string name)
		{
			if (name == SearchPane.Instance.PaneName)
				return SearchPane.Instance;
			return null;
		}
	}

	/// <summary>
	/// Search pane
	/// </summary>
	public partial class SearchPane : UserControl, IPane
	{
		static SearchPane instance;
		RunningSearch currentSearch;
		
		public static SearchPane Instance {
			get {
				if (instance == null) {
					App.Current.VerifyAccess();
					instance = new SearchPane();
				}
				return instance;
			}
		}

		public string PaneName {
			get { return "search window"; }
		}

		public string PaneTitle {
			get { return "Search"; }
		}

		sealed class SearchType
		{
			public string Name { get; private set; }
			public ImageSource Image { get; private set; }
			public SearchMode SearchMode { get; private set; }
			public VisibleMembersFlags Flags { get; private set; }

			public SearchType(string name, ImageSource image, SearchMode searchMode, VisibleMembersFlags flags)
			{
				this.Name = name;
				this.Image = image;
				this.SearchMode = searchMode;
				this.Flags = flags;
			}
		}

		static readonly SearchType[] searchTypes = new SearchType[] {
			new SearchType("Assembly", Images.Assembly, SearchMode.AssemblyDef, VisibleMembersFlags.AssemblyDef),
			new SearchType("Module", Images.AssemblyModule, SearchMode.ModuleDef, VisibleMembersFlags.ModuleDef),
			new SearchType("Namespace", Images.Namespace, SearchMode.Namespace, VisibleMembersFlags.Namespace),
			new SearchType("Type", Images.Class, SearchMode.TypeDef, VisibleMembersFlags.TypeDef),
			new SearchType("Field", Images.Field, SearchMode.FieldDef, VisibleMembersFlags.FieldDef),
			new SearchType("Method", Images.Method, SearchMode.MethodDef, VisibleMembersFlags.MethodDef),
			new SearchType("Property", Images.Property, SearchMode.PropertyDef, VisibleMembersFlags.PropertyDef),
			new SearchType("Event", Images.Event, SearchMode.EventDef, VisibleMembersFlags.EventDef),
			new SearchType("AssemblyRef", Images.Assembly, SearchMode.AssemblyRef, VisibleMembersFlags.AssemblyRef),
			new SearchType("ModuleRef", Images.AssemblyModule, SearchMode.ModuleRef, VisibleMembersFlags.ModuleRef),
			new SearchType("Generic Type", Images.Class, SearchMode.GenericTypeDef, VisibleMembersFlags.GenericTypeDef),
			new SearchType("Non-Generic Type", Images.Class, SearchMode.NonGenericTypeDef, VisibleMembersFlags.NonGenericTypeDef),
			new SearchType("Enum", Images.Enum, SearchMode.EnumTypeDef, VisibleMembersFlags.EnumTypeDef),
			new SearchType("Interface", Images.Interface, SearchMode.InterfaceTypeDef, VisibleMembersFlags.InterfaceTypeDef),
			new SearchType("Class", Images.Class, SearchMode.ClassTypeDef, VisibleMembersFlags.ClassTypeDef),
			new SearchType("Value Type", Images.Struct, SearchMode.ValueTypeDef, VisibleMembersFlags.ValueTypeDef),
			new SearchType("Delegate", Images.Delegate, SearchMode.DelegateTypeDef, VisibleMembersFlags.DelegateTypeDef),
			new SearchType("Member", Images.Property, SearchMode.Member, VisibleMembersFlags.MethodDef | VisibleMembersFlags.FieldDef | VisibleMembersFlags.PropertyDef | VisibleMembersFlags.EventDef),
			new SearchType("All Above", Images.Class, SearchMode.Any, VisibleMembersFlags.TreeViewAll),
			new SearchType("Number/String", Images.Literal, SearchMode.Literal, VisibleMembersFlags.MethodBody | VisibleMembersFlags.FieldDef | VisibleMembersFlags.ParamDef | VisibleMembersFlags.PropertyDef),
		};
		Dictionary<SearchMode, int> searchModeToIndex = new Dictionary<SearchMode, int>();
		
		private SearchPane()
		{
			InitializeComponent();
			foreach (var type in searchTypes) {
				searchModeComboBox.Items.Add(type);
				searchModeToIndex[type.SearchMode] = searchModeComboBox.Items.Count - 1;
			}
			searchModeComboBox.SelectedIndex = searchModeToIndex[SearchMode.TypeDef];
			ContextMenuProvider.Add(listBox);

			MainWindow.Instance.CurrentAssemblyListChanged += MainWindow_Instance_CurrentAssemblyListChanged;
			var checkBoxes = new[] { matchWholeWordsCheckBox, caseSensitiveCheckBox, matchAnyWordsCheckBox };
			foreach (var cb in checkBoxes) {
				cb.Checked += (s, e) => RestartSearch();
				cb.Unchecked += (s, e) => RestartSearch();
			}
		}
		
		bool runSearchOnNextShow;
		
		void MainWindow_Instance_CurrentAssemblyListChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (MainWindow.Instance.CurrentAssemblyList.IsReArranging)
				return;
			if (IsVisible) {
				RestartSearch();
			} else {
				StartSearch(null);
				runSearchOnNextShow = true;
			}
		}

		void RestartSearch()
		{
			StartSearch(this.SearchTerm);
		}
		
		public void Show()
		{
			if (!IsVisible)
				MainWindow.Instance.ShowInTopPane(PaneTitle, this);
			else
				searchBox.Focus();
		}

		public void Opened()
		{
			if (runSearchOnNextShow) {
				runSearchOnNextShow = false;
				RestartSearch();
			}
			Dispatcher.BeginInvoke(
				DispatcherPriority.Background,
				new Action(
					delegate {
						searchBox.Focus();
						searchBox.SelectAll();
					}));
		}
		
		public static readonly DependencyProperty SearchTermProperty =
			DependencyProperty.Register("SearchTerm", typeof(string), typeof(SearchPane),
			                            new FrameworkPropertyMetadata(string.Empty, OnSearchTermChanged));
		
		public string SearchTerm {
			get { return (string)GetValue(SearchTermProperty); }
			set { SetValue(SearchTermProperty, value); }
		}
		
		static void OnSearchTermChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			((SearchPane)o).StartSearch((string)e.NewValue);
		}
		
		void SearchModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			RestartSearch();
		}
		
		void StartSearch(string searchTerm)
		{
			if (currentSearch != null) {
				currentSearch.Cancel();
			}
			if (string.IsNullOrEmpty(searchTerm)) {
				currentSearch = null;
				listBox.ItemsSource = null;
			} else {
				MainWindow mainWindow = MainWindow.Instance;
				var searchType = (SearchType)searchModeComboBox.SelectedItem;
				currentSearch = new RunningSearch(
					mainWindow.AssemblyListTreeNode.Children.Cast<AssemblyTreeNode>(),
					CreateSearchComparer(searchType, searchTerm),
					new FlagsTreeViewNodeFilter(searchType.Flags),
					mainWindow.CurrentLanguage);
				listBox.ItemsSource = currentSearch.Results;
				new Thread(currentSearch.Run).Start();
			}
		}

		ISearchComparer CreateSearchComparer(SearchType searchType, string searchTerm)
		{
			if (searchType.SearchMode == SearchMode.Literal) {
				var s = searchTerm.Trim();

				var val64 = TryParseInt64(s);
				if (val64 != null)
					return new IntegerLiteralSearchComparer(val64.Value);
				var uval64 = TryParseUInt64(s);
				if (uval64 != null)
					return new IntegerLiteralSearchComparer(unchecked((long)uval64.Value));
				double dbl;
				if (double.TryParse(s, out dbl))
					return new DoubleLiteralSearchComparer(dbl);

				if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
					s = s.Substring(1, s.Length - 2);
				else {
					var regex = RunningSearch.TryCreateRegEx(s, caseSensitiveCheckBox.IsChecked.Value);
					if (regex != null)
						return new RegExStringLiteralSearchComparer(regex);
				}
				return new StringLiteralSearchComparer(s, caseSensitiveCheckBox.IsChecked.Value, matchWholeWordsCheckBox.IsChecked.Value);
			}

			return RunningSearch.CreateSearchComparer(
					searchTerm,
					caseSensitiveCheckBox.IsChecked.Value,
					matchWholeWordsCheckBox.IsChecked.Value,
					matchAnyWordsCheckBox.IsChecked.Value);
		}

		static long? TryParseInt64(string s)
		{
			bool isSigned = s.StartsWith("-", StringComparison.OrdinalIgnoreCase);
			if (isSigned)
				s = s.Substring(1);
			if (s != s.Trim())
				return null;
			ulong val;
			if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
				s = s.Substring(2);
				if (s != s.Trim())
					return null;
				if (!ulong.TryParse(s, NumberStyles.HexNumber, null, out val))
					return null;
			}
			else {
				if (!ulong.TryParse(s, out val))
					return null;
			}
			if (isSigned) {
				if (val > (ulong)long.MaxValue + 1)
					return null;
				return unchecked(-(long)val);
			}
			else {
				if (val > long.MaxValue)
					return null;
				return (long)val;
			}
		}

		static ulong? TryParseUInt64(string s)
		{
			ulong val;
			if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
				s = s.Substring(2);
				if (s != s.Trim())
					return null;
				if (!ulong.TryParse(s, NumberStyles.HexNumber, null, out val))
					return null;
			}
			else {
				if (!ulong.TryParse(s, out val))
					return null;
			}
			return val;
		}
		
		void IPane.Closed()
		{
			this.SearchTerm = string.Empty;
		}

		ListBoxItem GetListBoxItem(object o)
		{
			var depo = o as DependencyObject;
			while (depo != null && !(depo is ListBoxItem) && depo != listBox)
				depo = VisualTreeHelper.GetParent(depo);
			return depo as ListBoxItem;
		}

		void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (MouseButton.Left != e.ChangedButton)
				return;
			if (GetListBoxItem(e.OriginalSource) == null)
				return;
			JumpToSelectedItem();
			e.Handled = true;
		}
		
		void ListBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return) {
				e.Handled = true;
				JumpToSelectedItem();
			}
		}
		
		void JumpToSelectedItem()
		{
			SearchResult result = listBox.SelectedItem as SearchResult;
			if (result != null) {
				var obj = result.Object;
				if (obj is AssemblyTreeNode)
					obj = ((AssemblyTreeNode)obj).LoadedAssembly.AssemblyDefinition;
				else if (obj is LoadedAssembly)
					obj = ((LoadedAssembly)obj).ModuleDefinition;
				else if (obj is string) {
					var ns = (string)obj;
					MainWindow.Instance.JumpToNamespace(result.LoadedAssembly, ns);
					return;
				}
				MainWindow.Instance.JumpToReference(obj);
			}
		}
		
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Key == Key.T && e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
				searchModeComboBox.SelectedIndex = searchModeToIndex[SearchMode.TypeDef];
				e.Handled = true;
			} else if (e.Key == Key.M && e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
				searchModeComboBox.SelectedIndex = searchModeToIndex[SearchMode.Member];
				e.Handled = true;
			} else if (e.Key == Key.S && e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
				searchModeComboBox.SelectedIndex = searchModeToIndex[SearchMode.Literal];
				e.Handled = true;
			}
		}
		
		void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Down && listBox.HasItems) {
				e.Handled = true;
				listBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
				listBox.SelectedIndex = searchModeToIndex[SearchMode.TypeDef];
			}
		}
	}

	internal sealed class RunningSearch
	{
		readonly Dispatcher dispatcher;
		readonly CancellationTokenSource cts = new CancellationTokenSource();
		readonly AssemblyTreeNode[] asmNodes;
		readonly ISearchComparer searchComparer;
		readonly ITreeViewNodeFilter filter;
		readonly Language language;
		public readonly ObservableCollection<SearchResult> Results = new ObservableCollection<SearchResult>();
		int resultCount;

		public static Regex TryCreateRegEx(string s, bool caseSensitive)
		{
			s = s.Trim();
			if (s.Length > 2 && s[0] == '/' && s[s.Length - 1] == '/') {
				var regexOpts = RegexOptions.Compiled;
				if (!caseSensitive)
					regexOpts |= RegexOptions.IgnoreCase;
				try {
					return new Regex(s.Substring(1, s.Length - 2), regexOpts);
				}
				catch (ArgumentException) {
				}
			}
			return null;
		}

		public static ISearchComparer CreateSearchComparer(string searchTerm, bool caseSensitive = false, bool matchWholeWords = false, bool matchAnySearchTerm = false)
		{
			var regex = TryCreateRegEx(searchTerm, caseSensitive);
			if (regex != null)
				return new RegExSearchComparer(regex);

			var searchTerms = searchTerm.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (matchAnySearchTerm)
				return new OrSearchComparer(searchTerms, caseSensitive, matchWholeWords);
			return new AndSearchComparer(searchTerms, caseSensitive, matchWholeWords);
		}

		public RunningSearch(IEnumerable<AssemblyTreeNode> asmNodes, ISearchComparer searchComparer, ITreeViewNodeFilter filter, Language language)
		{
			this.dispatcher = Dispatcher.CurrentDispatcher;
			this.asmNodes = asmNodes.ToArray();
			foreach (var asmNode in this.asmNodes)
				asmNode.EnsureChildrenFiltered();
			this.searchComparer = searchComparer;
			this.language = language;
			this.filter = filter;

			this.Results.Add(new SearchResult { Name = "Searching..." });
		}

		public void Cancel()
		{
			cts.Cancel();
		}

		public void Run()
		{
			try {
				var searcher = new FilterSearcher(filter, searchComparer, AddResult, language, cts.Token);
				searcher.SearchAssemblies(asmNodes);
			}
			catch (OperationCanceledException) {
				// ignore cancellation
			}
			// remove the 'Searching...' entry
			dispatcher.BeginInvoke(
				DispatcherPriority.Normal,
				new Action(delegate { this.Results.RemoveAt(this.Results.Count - 1); }));
		}

		void AddResult(SearchResult result)
		{
			if (++resultCount > 1000) {
				result = new SearchResult { Name = "Search aborted, more than 1000 results found." };
				cts.Cancel();
			}
			dispatcher.BeginInvoke(
				DispatcherPriority.Normal,
				new Action(delegate { this.Results.Insert(this.Results.Count - 1, result); }));
			cts.Token.ThrowIfCancellationRequested();
		}
	}

	sealed class SearchResult : IMemberTreeNode
	{
		public IMemberRef Member {
			get { return MDTokenProvider as IMemberRef; }
		}

		public IMDTokenProvider MDTokenProvider {
			get {
				var obj = Object;
				var asmNode = obj as AssemblyTreeNode;
				if (asmNode != null)
					obj = asmNode.LoadedAssembly.AssemblyDefinition;
				var asm = obj as LoadedAssembly;
				if (asm != null)
					obj = asm.ModuleDefinition;
				return obj as IMDTokenProvider;	// returns null if it's a namespace (a string)
			}
		}

		/// <summary>
		/// <see cref="AssemblyTreeNode"/> if it's an assembly. If it's a module, it's a
		/// <see cref="LoadedAssembly"/> reference. If it's a <see cref="IMemberRef"/>,
		/// <see cref="AssemblyRef"/> or a <see cref="ModuleRef"/>, it's that reference. If it's a
		/// namespace, it's a <see cref="string"/>.
		/// </summary>
		public object Object { get; set; }
		public string Location { get; set; }
		public string Name { get; set; }
		public ImageSource Image { get; set; }
		public ImageSource LocationImage { get; set; }
		public LoadedAssembly LoadedAssembly { get; set; }
		public string ToolTip {
			get {
				var loadedAsm = LoadedAssembly;
				if (loadedAsm == null)
					return null;
				var module = loadedAsm.ModuleDefinition;
				if (module == null)
					return loadedAsm.FileName;
				if (!string.IsNullOrWhiteSpace(module.Location))
					return module.Location;
				if (!string.IsNullOrWhiteSpace(module.Name))
					return module.Name;
				if (module.Assembly != null && !string.IsNullOrWhiteSpace(module.Assembly.Name))
					return module.Assembly.Name;
				return null;
			}
		}
			
		public override string ToString()
		{
			return Name;
		}
	}

	[ExportMainMenuCommand(Menu = "_View", MenuHeader = "_Search", MenuIcon = "Images/Find.png", MenuCategory = "ShowPane", MenuOrder = 3200)]
	[ExportToolbarCommand(ToolTip = "Search (Ctrl+Shift+F or Ctrl+E)", ToolbarIcon = "Images/Find.png", ToolbarCategory = "View", ToolbarOrder = 9000)]
	sealed class ShowSearchCommand : CommandWrapper
	{
		public ShowSearchCommand()
			: base(NavigationCommands.Search)
		{
			NavigationCommands.Search.InputGestures.Clear();
			NavigationCommands.Search.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Shift));
			NavigationCommands.Search.InputGestures.Add(new KeyGesture(Key.E, ModifierKeys.Control));
		}
	}

	public enum SearchMode
	{
		AssemblyDef,
		ModuleDef,
		Namespace,
		TypeDef,
		FieldDef,
		MethodDef,
		PropertyDef,
		EventDef,
		AssemblyRef,
		BaseTypes,
		DerivedTypes,
		ModuleRef,
		ResourceList,
		NonNetFile,
		GenericTypeDef,
		NonGenericTypeDef,
		EnumTypeDef,
		InterfaceTypeDef,
		ClassTypeDef,
		ValueTypeDef,
		DelegateTypeDef,
		Member,
		Any,
		Literal,
	}
}