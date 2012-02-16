// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

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
		public SearchInputHandler(TextArea textArea)
			: base(textArea)
		{
			RegisterCommands(this.CommandBindings);
		}
		
		void RegisterCommands(ICollection<CommandBinding> commandBindings)
		{
			commandBindings.Add(new CommandBinding(ApplicationCommands.Find, ExecuteFind));
			commandBindings.Add(new CommandBinding(SearchCommands.FindNext, ExecuteFindNext));
			commandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, ExecuteFindPrevious));
			commandBindings.Add(new CommandBinding(SearchCommands.CloseSearchPanel, ExecuteCloseSearchPanel));
		}
		
		SearchPanel panel;
		
		void ExecuteFind(object sender, ExecutedRoutedEventArgs e)
		{
			if (panel == null || panel.IsClosed) {
				panel = new SearchPanel();
				panel.Attach(TextArea);
			}
			panel.SearchPattern = TextArea.Selection.GetText();
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Input, (Action)delegate { panel.Reactivate(); });
		}
		
		void ExecuteFindNext(object sender, ExecutedRoutedEventArgs e)
		{
			if (panel != null)
				panel.FindNext();
		}
		
		void ExecuteFindPrevious(object sender, ExecutedRoutedEventArgs e)
		{
			if (panel != null)
				panel.FindPrevious();
		}
		
		void ExecuteCloseSearchPanel(object sender, ExecutedRoutedEventArgs e)
		{
			if (panel != null)
				panel.Close();
			panel = null;
		}
	}
}
