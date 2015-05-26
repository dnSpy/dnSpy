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
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.ILSpy.AsmEditor;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy
{
	abstract class TabState : IDisposable, INotifyPropertyChanged
	{
		public abstract string Header { get; }
		public TabItem TabItem;

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

		protected void OnPropertyChanged(string propName)
		{
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

		public string ToolTip {
			get {
				var shortHeader = ShortHeader;
				var header = Header;
				return shortHeader == header ? null : header;
			}
		}

		protected TabState()
		{
			var tabItem = new TabItem();
			TabItem = tabItem;
			TabItem.Header = this;
			tabItem.DataContext = this;
		}

		public static TabState GetTabState(FrameworkElement elem)
		{
			if (elem == null)
				return null;
			return elem.DataContext as TabState;
		}

		protected void UpdateHeader()
		{
			OnPropertyChanged("Header");
			OnPropertyChanged("ShortHeader");
			OnPropertyChanged("ToolTip");
		}

		void Close()
		{
			Owner.Close(this);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool isDisposing)
		{
		}
	}

	sealed class TabStateDecompile : TabState
	{
		public readonly DecompilerTextView TextView = new DecompilerTextView();
		public readonly NavigationHistory<NavigationState> History = new NavigationHistory<NavigationState>();
		public bool ignoreDecompilationRequests;
		public bool HasDecompiled;

		public ILSpyTreeNode[] DecompiledNodes {
			get { return decompiledNodes; }
		}
		ILSpyTreeNode[] decompiledNodes = new ILSpyTreeNode[0];

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

		internal void SetDecompileProps(Language language, ILSpyTreeNode[] nodes)
		{
			this.language = language;
			UnhookEvents();
			this.decompiledNodes = nodes ?? new ILSpyTreeNode[0];
			HookEvents();
			this.title = null;
			UpdateHeader();
		}

		void HookEvents()
		{
			foreach (var node in decompiledNodes)
				node.PropertyChanged += node_PropertyChanged;
		}

		void UnhookEvents()
		{
			foreach (var node in decompiledNodes)
				node.PropertyChanged -= node_PropertyChanged;
		}

		void node_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Text")
				UpdateHeader();
		}

		public static TabStateDecompile GetTabStateDecompile(FrameworkElement elem)
		{
			if (elem == null)
				return null;
			// The DataContext is inherited from the owner TabItem which is set to a TabStateDecompile
			return elem.DataContext as TabStateDecompile;
		}

		public TabStateDecompile(Language language)
		{
			var view = TextView;
			TabItem.Content = view;
			TabItem.Style = App.Current.FindResource("TabStateDecompileTabItemStyle") as Style;
			this.language = language;
			UpdateHeader();
			ContextMenuProvider.Add(view);
			view.DragOver += view_DragOver;
		}

		void view_DragOver(object sender, DragEventArgs e)
		{
			// The text editor seems to allow anything
			if (e.Data.GetDataPresent(typeof(TabItem))) {
				e.Effects = DragDropEffects.None;
				e.Handled = true;
				return;
			}
		}

		protected override void Dispose(bool isDisposing)
		{
			if (isDisposing)
				TextView.Dispose();
			UnhookEvents();
			decompiledNodes = new ILSpyTreeNode[0];
		}

		public bool Equals(ILSpyTreeNode[] nodes, Language language)
		{
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
	}
}
