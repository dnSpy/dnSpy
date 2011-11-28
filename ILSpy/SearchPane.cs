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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

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
		
		const int SearchMode_Type = 0;
		const int SearchMode_Member = 1;
		const int SearchMode_Literal = 2;
		
		private SearchPane()
		{
			InitializeComponent();
			searchModeComboBox.Items.Add(new { Image = Images.Class, Name = "Type" });
			searchModeComboBox.Items.Add(new { Image = Images.Property, Name = "Member" });
			searchModeComboBox.Items.Add(new { Image = Images.Literal, Name = "Constant" });
			searchModeComboBox.SelectedIndex = SearchMode_Type;
			
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
			} else if (e.Key == Key.S && e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
				searchModeComboBox.SelectedIndex = SearchMode_Literal;
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
			
			TypeCode searchTermLiteralType = TypeCode.Empty;
			object searchTermLiteralValue;
			
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
					if (searchMode == SearchMode_Literal) {
						CSharpParser parser = new CSharpParser();
						PrimitiveExpression pe = parser.ParseExpression(new StringReader(searchTerm)) as PrimitiveExpression;
						if (pe != null && pe.Value != null) {
							TypeCode peValueType = Type.GetTypeCode(pe.Value.GetType());
							switch (peValueType) {
								case TypeCode.Byte:
								case TypeCode.SByte:
								case TypeCode.Int16:
								case TypeCode.UInt16:
								case TypeCode.Int32:
								case TypeCode.UInt32:
								case TypeCode.Int64:
								case TypeCode.UInt64:
									searchTermLiteralType = TypeCode.Int64;
									searchTermLiteralValue = CSharpPrimitiveCast.Cast(TypeCode.Int64, pe.Value, false);
									break;
								case TypeCode.Single:
								case TypeCode.Double:
								case TypeCode.String:
									searchTermLiteralType = peValueType;
									searchTermLiteralValue = pe.Value;
									break;
							}
						}
					}
					
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
					if (IsMatch(field)) {
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
					if (IsMatch(property)) {
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
					if (IsMatch(ev)) {
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
					switch (method.SemanticsAttributes) {
						case MethodSemanticsAttributes.Setter:
						case MethodSemanticsAttributes.Getter:
						case MethodSemanticsAttributes.AddOn:
						case MethodSemanticsAttributes.RemoveOn:
						case MethodSemanticsAttributes.Fire:
							continue;
					}
					if (IsMatch(method)) {
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
			
			bool IsMatch(FieldDefinition field)
			{
				if (searchMode == SearchMode_Literal)
					return IsLiteralMatch(field.Constant);
				else
					return IsMatch(field.Name);
			}
			
			bool IsMatch(PropertyDefinition property)
			{
				if (searchMode == SearchMode_Literal)
					return MethodIsLiteralMatch(property.GetMethod) || MethodIsLiteralMatch(property.SetMethod);
				else
					return IsMatch(property.Name);
			}
			
			bool IsMatch(EventDefinition ev)
			{
				if (searchMode == SearchMode_Literal)
					return MethodIsLiteralMatch(ev.AddMethod) || MethodIsLiteralMatch(ev.RemoveMethod) || MethodIsLiteralMatch(ev.InvokeMethod);
				else
					return IsMatch(ev.Name);
			}
			
			bool IsMatch(MethodDefinition m)
			{
				if (searchMode == SearchMode_Literal)
					return MethodIsLiteralMatch(m);
				else
					return IsMatch(m.Name);
			}
			
			bool IsLiteralMatch(object val)
			{
				if (val == null)
					return false;
				switch (searchTermLiteralType) {
					case TypeCode.Int64:
						TypeCode tc = Type.GetTypeCode(val.GetType());
						if (tc >= TypeCode.SByte && tc <= TypeCode.UInt64)
							return CSharpPrimitiveCast.Cast(TypeCode.Int64, val, false).Equals(searchTermLiteralValue);
						else
							return false;
					case TypeCode.Single:
					case TypeCode.Double:
					case TypeCode.String:
						return searchTermLiteralValue.Equals(val);
					default:
						// substring search with searchTerm
						return IsMatch(val.ToString());
				}
			}
			
			bool MethodIsLiteralMatch(MethodDefinition m)
			{
				if (m == null)
					return false;
				var body = m.Body;
				if (body == null)
					return false;
				if (searchTermLiteralType == TypeCode.Int64) {
					long val = (long)searchTermLiteralValue;
					foreach (var inst in body.Instructions) {
						switch (inst.OpCode.Code) {
							case Code.Ldc_I8:
								if (val == (long)inst.Operand)
									return true;
								break;
							case Code.Ldc_I4:
								if (val == (int)inst.Operand)
									return true;
								break;
							case Code.Ldc_I4_S:
								if (val == (sbyte)inst.Operand)
									return true;
								break;
							case Code.Ldc_I4_M1:
								if (val == -1)
									return true;
								break;
							case Code.Ldc_I4_0:
								if (val == 0)
									return true;
								break;
							case Code.Ldc_I4_1:
								if (val == 1)
									return true;
								break;
							case Code.Ldc_I4_2:
								if (val == 2)
									return true;
								break;
							case Code.Ldc_I4_3:
								if (val == 3)
									return true;
								break;
							case Code.Ldc_I4_4:
								if (val == 4)
									return true;
								break;
							case Code.Ldc_I4_5:
								if (val == 5)
									return true;
								break;
							case Code.Ldc_I4_6:
								if (val == 6)
									return true;
								break;
							case Code.Ldc_I4_7:
								if (val == 7)
									return true;
								break;
							case Code.Ldc_I4_8:
								if (val == 8)
									return true;
								break;
						}
					}
				} else if (searchTermLiteralType != TypeCode.Empty) {
					Code expectedCode;
					switch (searchTermLiteralType) {
						case TypeCode.Single:
							expectedCode = Code.Ldc_R4;
							break;
						case TypeCode.Double:
							expectedCode = Code.Ldc_R8;
							break;
						case TypeCode.String:
							expectedCode = Code.Ldstr;
							break;
						default:
							throw new InvalidOperationException();
					}
					foreach (var inst in body.Instructions) {
						if (inst.OpCode.Code == expectedCode && searchTermLiteralValue.Equals(inst.Operand))
							return true;
					}
				} else {
					foreach (var inst in body.Instructions) {
						if (inst.OpCode.Code == Code.Ldstr && IsMatch((string)inst.Operand))
							return true;
					}
				}
				return false;
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