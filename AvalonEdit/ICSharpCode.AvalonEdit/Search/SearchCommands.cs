// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Search
{
	/// <summary>
	/// Search commands for AvalonEdit.
	/// </summary>
	public static class SearchCommands
	{
		/// <summary>
		/// Finds the next occurrence in the file.
		/// </summary>
		public static readonly RoutedCommand FindNext = new RoutedCommand(
			"FindNext", typeof(SearchPanel),
			new InputGestureCollection { new KeyGesture(Key.F3) }
		);
		
		/// <summary>
		/// Finds the previous occurrence in the file.
		/// </summary>
		public static readonly RoutedCommand FindPrevious = new RoutedCommand(
			"FindPrevious", typeof(SearchPanel),
			new InputGestureCollection { new KeyGesture(Key.F3, ModifierKeys.Shift) }
		);
		
		/// <summary>
		/// Closes the SearchPanel.
		/// </summary>
		public static readonly RoutedCommand CloseSearchPanel = new RoutedCommand(
			"CloseSearchPanel", typeof(SearchPanel),
			new InputGestureCollection { new KeyGesture(Key.Escape) }
		);
	}
	
	/// <summary>
	/// TextAreaInputHandler that registers all search-related commands.
	/// </summary>
	public class SearchInputHandler : TextAreaInputHandler
	{
		/// <summary>
		/// Creates a new SearchInputHandler and registers the search-related commands.
		/// </summary>
		[Obsolete("Use SearchPanel.Install instead")]
		public SearchInputHandler(TextArea textArea)
			: base(textArea)
		{
			RegisterCommands(this.CommandBindings);
			panel = SearchPanel.Install(textArea);
		}
		
		internal SearchInputHandler(TextArea textArea, SearchPanel panel)
			: base(textArea)
		{
			RegisterCommands(this.CommandBindings);
			this.panel = panel;
		}
		
		void RegisterCommands(ICollection<CommandBinding> commandBindings)
		{
			commandBindings.Add(new CommandBinding(ApplicationCommands.Find, ExecuteFind));
			commandBindings.Add(new CommandBinding(SearchCommands.FindNext, ExecuteFindNext, CanExecuteWithOpenSearchPanel));
			commandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, ExecuteFindPrevious, CanExecuteWithOpenSearchPanel));
			commandBindings.Add(new CommandBinding(SearchCommands.CloseSearchPanel, ExecuteCloseSearchPanel, CanExecuteWithOpenSearchPanel));
		}
		
		SearchPanel panel;
		
		void ExecuteFind(object sender, ExecutedRoutedEventArgs e)
		{
			panel.Open();
			if (!(TextArea.Selection.IsEmpty || TextArea.Selection.IsMultiline))
				panel.SearchPattern = TextArea.Selection.GetText();
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Input, (Action)delegate { panel.Reactivate(); });
		}

		void CanExecuteWithOpenSearchPanel(object sender, CanExecuteRoutedEventArgs e)
		{
			if (panel.IsClosed) {
				e.CanExecute = false;
				// Continue routing so that the key gesture can be consumed by another component.
				e.ContinueRouting = true;
			} else {
				e.CanExecute = true;
				e.Handled = true;
			}
		}
		
		void ExecuteFindNext(object sender, ExecutedRoutedEventArgs e)
		{
			if (!panel.IsClosed) {
				panel.FindNext();
				e.Handled = true;
			}
		}
		
		void ExecuteFindPrevious(object sender, ExecutedRoutedEventArgs e)
		{
			if (!panel.IsClosed) {
				panel.FindPrevious();
				e.Handled = true;
			}
		}
		
		void ExecuteCloseSearchPanel(object sender, ExecutedRoutedEventArgs e)
		{
			if (!panel.IsClosed) {
				panel.Close();
				e.Handled = true;
			}
		}
		
		/// <summary>
		/// Fired when SearchOptions are modified inside the SearchPanel.
		/// </summary>
		public event EventHandler<SearchOptionsChangedEventArgs> SearchOptionsChanged {
			add { panel.SearchOptionsChanged += value; }
			remove { panel.SearchOptionsChanged -= value; }
		}
	}
}
