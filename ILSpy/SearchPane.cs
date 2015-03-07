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
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Utils;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ICSharpCode.ILSpy
{
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
		
		private SearchPane()
		{
			InitializeComponent();
			searchModeComboBox.Items.Add(new { Image = Images.Class, Name = "Type" });
			searchModeComboBox.Items.Add(new { Image = Images.Property, Name = "Member" });
			searchModeComboBox.Items.Add(new { Image = Images.Literal, Name = "Constant" });
			searchModeComboBox.SelectedIndex = (int)SearchMode.Type;
			ContextMenuProvider.Add(listBox);
			
			MainWindow.Instance.CurrentAssemblyListChanged += MainWindow_Instance_CurrentAssemblyListChanged;
		}
		
		bool runSearchOnNextShow;
		
		void MainWindow_Instance_CurrentAssemblyListChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (IsVisible) {
				StartSearch(this.SearchTerm);
			} else {
				StartSearch(null);
				runSearchOnNextShow = true;
			}
		}
		
		public void Show()
		{
			if (!IsVisible) {
				MainWindow.Instance.ShowInTopPane("Search", this);
				if (runSearchOnNextShow) {
					runSearchOnNextShow = false;
					StartSearch(this.SearchTerm);
				}
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
			StartSearch(this.SearchTerm);
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
				currentSearch = new RunningSearch(mainWindow.CurrentAssemblyList.GetAllModules(), searchTerm, (SearchMode)searchModeComboBox.SelectedIndex, mainWindow.CurrentLanguage);
				listBox.ItemsSource = currentSearch.Results;
				new Thread(currentSearch.Run).Start();
			}
		}
		
		void IPane.Closed()
		{
			this.SearchTerm = string.Empty;
		}
		
		void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
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
				MainWindow.Instance.JumpToReference(result.Member);
			}
		}
		
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Key == Key.T && e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
				searchModeComboBox.SelectedIndex = (int)SearchMode.Type;
				e.Handled = true;
			} else if (e.Key == Key.M && e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
				searchModeComboBox.SelectedIndex = (int)SearchMode.Member;
				e.Handled = true;
			} else if (e.Key == Key.S && e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
				searchModeComboBox.SelectedIndex = (int)SearchMode.Literal;
				e.Handled = true;
			}
		}
		
		void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Down && listBox.HasItems) {
				e.Handled = true;
				listBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
				listBox.SelectedIndex = 0;
			}
		}

		sealed class RunningSearch
		{
			readonly Dispatcher dispatcher;
			readonly CancellationTokenSource cts = new CancellationTokenSource();
			readonly ModuleDef[] modules;
			readonly string[] searchTerm;
			readonly SearchMode searchMode;
			readonly Language language;
			public readonly ObservableCollection<SearchResult> Results = new ObservableCollection<SearchResult>();
			int resultCount;

			public RunningSearch(ModuleDef[] modules, string searchTerm, SearchMode searchMode, Language language)
			{
				this.dispatcher = Dispatcher.CurrentDispatcher;
				this.modules = modules;
				this.searchTerm = searchTerm.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
				this.language = language;
				this.searchMode = searchMode;
				
				this.Results.Add(new SearchResult { Name = "Searching..." });
			}
			
			public void Cancel()
			{
				cts.Cancel();
			}
			
			public void Run()
			{
				try {
					var searcher = GetSearchStrategy(searchMode, searchTerm);
					foreach (var module in modules) {
						if (module == null)
							continue;
						CancellationToken cancellationToken = cts.Token;

						foreach (TypeDef type in module.Types) {
							cancellationToken.ThrowIfCancellationRequested();
							searcher.Search(type, language, AddResult);
						}
					}
				} catch (OperationCanceledException) {
					// ignore cancellation
				}
				// remove the 'Searching...' entry
				dispatcher.BeginInvoke(
					DispatcherPriority.Normal,
					new Action(delegate { this.Results.RemoveAt(this.Results.Count - 1); }));
			}
			
			void AddResult(SearchResult result)
			{
				if (++resultCount == 1000) {
					result = new SearchResult { Name = "Search aborted, more than 1000 results found." };
					cts.Cancel();
				}
				dispatcher.BeginInvoke(
					DispatcherPriority.Normal,
					new Action(delegate { this.Results.Insert(this.Results.Count - 1, result); }));
				cts.Token.ThrowIfCancellationRequested();
			}

			AbstractSearchStrategy GetSearchStrategy(SearchMode mode, string[] terms)
			{
				if (terms.Length == 1) {
					if (terms[0].StartsWith("t:"))
						return new TypeSearchStrategy(terms[0].Substring(2));

					if (terms[0].StartsWith("m:"))
						return new MemberSearchStrategy(terms[0].Substring(2));

					if (terms[0].StartsWith("c:"))
						return new LiteralSearchStrategy(terms[0].Substring(2));
				}

				switch (mode) {
					case SearchMode.Type:
						return new TypeSearchStrategy(terms);
					case SearchMode.Member:
						return new MemberSearchStrategy(terms);
					case SearchMode.Literal:
						return new LiteralSearchStrategy(terms);
				}

				return null;
			}
		}
	}

	sealed class SearchResult : INotifyPropertyChanged, IMemberTreeNode
	{
		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged {
			add { }
			remove { }
		}
			
		public IMemberRef Member { get; set; }
			
		public string Location { get; set; }
		public string Name { get; set; }
		public ImageSource Image { get; set; }
		public ImageSource LocationImage { get; set; }
			
		public override string ToString()
		{
			return Name;
		}
	}

	[ExportMainMenuCommand(Menu = "_View", Header = "_Search", MenuIcon = "Images/Find.png", MenuCategory = "ShowPane", MenuOrder = 100)]
	[ExportToolbarCommand(ToolTip = "Search (Ctrl+Shift+F or Ctrl+E)", ToolbarIcon = "Images/Find.png", ToolbarCategory = "View", ToolbarOrder = 100)]
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
		Type,
		Member,
		Literal
	}
}