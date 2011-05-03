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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.ILSpy.TreeNodes;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Notifies panes when they are closed.
	/// </summary>
	public interface IPane
	{
		void Closed();
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
		
		const int SearchMode_Type = 0;
		const int SearchMode_Member = 1;
		
		private SearchPane()
		{
			InitializeComponent();
			searchModeComboBox.Items.Add(new { Image = Images.Class, Name = "Type" });
			searchModeComboBox.Items.Add(new { Image = Images.Property, Name = "Member" });
			searchModeComboBox.SelectedIndex = SearchMode_Type;
		}
		
		public void Show()
		{
			if (!IsVisible)
				MainWindow.Instance.ShowInTopPane("Search", this);
			Dispatcher.BeginInvoke(
				DispatcherPriority.Background,
				new Func<bool>(searchBox.Focus));
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
				currentSearch = new RunningSearch(mainWindow.CurrentAssemblyList.GetAssemblies(), searchTerm, searchModeComboBox.SelectedIndex, mainWindow.CurrentLanguage);
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
				searchModeComboBox.SelectedIndex = SearchMode_Type;
				e.Handled = true;
			} else if (e.Key == Key.M && e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
				searchModeComboBox.SelectedIndex = SearchMode_Member;
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
			readonly LoadedAssembly[] assemblies;
			readonly string searchTerm;
			readonly int searchMode;
			readonly Language language;
			public readonly ObservableCollection<SearchResult> Results = new ObservableCollection<SearchResult>();
			int resultCount;
			
			public RunningSearch(LoadedAssembly[] assemblies, string searchTerm, int searchMode, Language language)
			{
				this.dispatcher = Dispatcher.CurrentDispatcher;
				this.assemblies = assemblies;
				this.searchTerm = searchTerm;
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
					foreach (var loadedAssembly in assemblies) {
						AssemblyDefinition asm = loadedAssembly.AssemblyDefinition;
						if (asm == null)
							continue;
						CancellationToken cancellationToken = cts.Token;
						foreach (TypeDefinition type in asm.MainModule.Types) {
							cancellationToken.ThrowIfCancellationRequested();
							PerformSearch(type);
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
			
			bool IsMatch(string text)
			{
				if (text.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
				else
					return false;
			}
			
			void PerformSearch(TypeDefinition type)
			{
				if (searchMode == SearchMode_Type && IsMatch(type.Name)) {
					AddResult(new SearchResult {
					          	Member = type,
					          	Image = TypeTreeNode.GetIcon(type),
					          	Name = language.TypeToString(type, includeNamespace: false),
					          	LocationImage = type.DeclaringType != null ? TypeTreeNode.GetIcon(type.DeclaringType) : Images.Namespace,
					          	Location = type.DeclaringType != null ? language.TypeToString(type.DeclaringType, includeNamespace: true) : type.Namespace
					          });
				}
				
				foreach (TypeDefinition nestedType in type.NestedTypes) {
					PerformSearch(nestedType);
				}
				
				if (searchMode == SearchMode_Type)
					return;
				
				foreach (FieldDefinition field in type.Fields) {
					if (IsMatch(field.Name)) {
						AddResult(new SearchResult {
						          	Member = field,
						          	Image = FieldTreeNode.GetIcon(field),
						          	Name = field.Name,
						          	LocationImage = TypeTreeNode.GetIcon(type),
						          	Location = language.TypeToString(type, includeNamespace: true)
						          });
					}
				}
				foreach (PropertyDefinition property in type.Properties) {
					if (IsMatch(property.Name)) {
						AddResult(new SearchResult {
						          	Member = property,
						          	Image = PropertyTreeNode.GetIcon(property),
						          	Name = property.Name,
						          	LocationImage = TypeTreeNode.GetIcon(type),
						          	Location = language.TypeToString(type, includeNamespace: true)
						          });
					}
				}
				foreach (EventDefinition ev in type.Events) {
					if (IsMatch(ev.Name)) {
						AddResult(new SearchResult {
						          	Member = ev,
						          	Image = EventTreeNode.GetIcon(ev),
						          	Name = ev.Name,
						          	LocationImage = TypeTreeNode.GetIcon(type),
						          	Location = language.TypeToString(type, includeNamespace: true)
						          });
					}
				}
				foreach (MethodDefinition method in type.Methods) {
					if (IsMatch(method.Name)) {
						AddResult(new SearchResult {
						          	Member = method,
						          	Image = MethodTreeNode.GetIcon(method),
						          	Name = method.Name,
						          	LocationImage = TypeTreeNode.GetIcon(type),
						          	Location = language.TypeToString(type, includeNamespace: true)
						          });
					}
				}
			}
		}
		
		sealed class SearchResult : INotifyPropertyChanged, IMemberTreeNode
		{
			event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged {
				add { }
				remove { }
			}
			
			public MemberReference Member { get; set; }
			
			public string Location { get; set; }
			public string Name { get; set; }
			public ImageSource Image { get; set; }
			public ImageSource LocationImage { get; set; }
			
			public override string ToString()
			{
				return Name;
			}
		}
	}
	
	[ExportMainMenuCommand(Menu = "_View", Header = "_Search", MenuIcon="Images/Find.png", MenuCategory = "ShowPane", MenuOrder = 100)]
	[ExportToolbarCommand(ToolTip = "Search (F3)", ToolbarIcon = "Images/Find.png", ToolbarCategory = "View", ToolbarOrder = 100)]
	sealed class ShowSearchCommand : CommandWrapper
	{
		public ShowSearchCommand()
			: base(NavigationCommands.Search)
		{
		}
	}
}