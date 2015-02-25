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
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Search
{
	/// <summary>
	/// Provides search functionality for AvalonEdit. It is displayed in the top-right corner of the TextArea.
	/// </summary>
	public class SearchPanel : Control
	{
		TextArea textArea;
		SearchInputHandler handler;
		TextDocument currentDocument;
		SearchResultBackgroundRenderer renderer;
		TextBox searchTextBox;
		SearchPanelAdorner adorner;
		
		#region DependencyProperties
		/// <summary>
		/// Dependency property for <see cref="UseRegex"/>.
		/// </summary>
		public static readonly DependencyProperty UseRegexProperty =
			DependencyProperty.Register("UseRegex", typeof(bool), typeof(SearchPanel),
			                            new FrameworkPropertyMetadata(false, SearchPatternChangedCallback));
		
		/// <summary>
		/// Gets/sets whether the search pattern should be interpreted as regular expression.
		/// </summary>
		public bool UseRegex {
			get { return (bool)GetValue(UseRegexProperty); }
			set { SetValue(UseRegexProperty, value); }
		}
		
		/// <summary>
		/// Dependency property for <see cref="MatchCase"/>.
		/// </summary>
		public static readonly DependencyProperty MatchCaseProperty =
			DependencyProperty.Register("MatchCase", typeof(bool), typeof(SearchPanel),
			                            new FrameworkPropertyMetadata(false, SearchPatternChangedCallback));
		
		/// <summary>
		/// Gets/sets whether the search pattern should be interpreted case-sensitive.
		/// </summary>
		public bool MatchCase {
			get { return (bool)GetValue(MatchCaseProperty); }
			set { SetValue(MatchCaseProperty, value); }
		}
		
		/// <summary>
		/// Dependency property for <see cref="WholeWords"/>.
		/// </summary>
		public static readonly DependencyProperty WholeWordsProperty =
			DependencyProperty.Register("WholeWords", typeof(bool), typeof(SearchPanel),
			                            new FrameworkPropertyMetadata(false, SearchPatternChangedCallback));
		
		/// <summary>
		/// Gets/sets whether the search pattern should only match whole words.
		/// </summary>
		public bool WholeWords {
			get { return (bool)GetValue(WholeWordsProperty); }
			set { SetValue(WholeWordsProperty, value); }
		}
		
		/// <summary>
		/// Dependency property for <see cref="SearchPattern"/>.
		/// </summary>
		public static readonly DependencyProperty SearchPatternProperty =
			DependencyProperty.Register("SearchPattern", typeof(string), typeof(SearchPanel),
			                            new FrameworkPropertyMetadata("", SearchPatternChangedCallback));
		
		/// <summary>
		/// Gets/sets the search pattern.
		/// </summary>
		public string SearchPattern {
			get { return (string)GetValue(SearchPatternProperty); }
			set { SetValue(SearchPatternProperty, value); }
		}
		
		/// <summary>
		/// Dependency property for <see cref="MarkerBrush"/>.
		/// </summary>
		public static readonly DependencyProperty MarkerBrushProperty =
			DependencyProperty.Register("MarkerBrush", typeof(Brush), typeof(SearchPanel),
			                            new FrameworkPropertyMetadata(Brushes.LightGreen, MarkerBrushChangedCallback));
		
		/// <summary>
		/// Gets/sets the Brush used for marking search results in the TextView.
		/// </summary>
		public Brush MarkerBrush {
			get { return (Brush)GetValue(MarkerBrushProperty); }
			set { SetValue(MarkerBrushProperty, value); }
		}
		
		/// <summary>
		/// Dependency property for <see cref="Localization"/>.
		/// </summary>
		public static readonly DependencyProperty LocalizationProperty =
			DependencyProperty.Register("Localization", typeof(Localization), typeof(SearchPanel),
			                            new FrameworkPropertyMetadata(new Localization()));
		
		/// <summary>
		/// Gets/sets the localization for the SearchPanel.
		/// </summary>
		public Localization Localization {
			get { return (Localization)GetValue(LocalizationProperty); }
			set { SetValue(LocalizationProperty, value); }
		}
		#endregion
		
		static void MarkerBrushChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			SearchPanel panel = d as SearchPanel;
			if (panel != null) {
				panel.renderer.MarkerBrush = (Brush)e.NewValue;
			}
		}
		
		static SearchPanel()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchPanel), new FrameworkPropertyMetadata(typeof(SearchPanel)));
		}
		
		ISearchStrategy strategy;
		
		static void SearchPatternChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			SearchPanel panel = d as SearchPanel;
			if (panel != null) {
				panel.ValidateSearchText();
				panel.UpdateSearch();
			}
		}

		void UpdateSearch()
		{
			// only reset as long as there are results
			// if no results are found, the "no matches found" message should not flicker.
			// if results are found by the next run, the message will be hidden inside DoSearch ...
			if (renderer.CurrentResults.Any())
				messageView.IsOpen = false;
			strategy = SearchStrategyFactory.Create(SearchPattern ?? "", !MatchCase, WholeWords, UseRegex ? SearchMode.RegEx : SearchMode.Normal);
			OnSearchOptionsChanged(new SearchOptionsChangedEventArgs(SearchPattern, MatchCase, UseRegex, WholeWords));
			DoSearch(true);
		}
		
		/// <summary>
		/// Creates a new SearchPanel.
		/// </summary>
		[Obsolete("Use the Install method instead")]
		public SearchPanel()
		{
		}
		
		/// <summary>
		/// Attaches this SearchPanel to a TextArea instance.
		/// </summary>
		[Obsolete("Use the Install method instead")]
		public void Attach(TextArea textArea)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
			AttachInternal(textArea);
		}
		
		/// <summary>
		/// Creates a SearchPanel and installs it to the TextEditor's TextArea.
		/// </summary>
		/// <remarks>This is a convenience wrapper.</remarks>
		public static SearchPanel Install(TextEditor editor)
		{
			if (editor == null)
				throw new ArgumentNullException("editor");
			return Install(editor.TextArea);
		}
		
		/// <summary>
		/// Creates a SearchPanel and installs it to the TextArea.
		/// </summary>
		public static SearchPanel Install(TextArea textArea)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
			#pragma warning disable 618
			SearchPanel panel = new SearchPanel();
			panel.AttachInternal(textArea);
			panel.handler = new SearchInputHandler(textArea, panel);
			textArea.DefaultInputHandler.NestedInputHandlers.Add(panel.handler);
			return panel;
		}
		
		/// <summary>
		/// Removes the SearchPanel from the TextArea.
		/// </summary>
		public void Uninstall()
		{
			CloseAndRemove();
			textArea.DefaultInputHandler.NestedInputHandlers.Remove(handler);
		}
		
		void AttachInternal(TextArea textArea)
		{
			this.textArea = textArea;
			adorner = new SearchPanelAdorner(textArea, this);
			DataContext = this;
			
			renderer = new SearchResultBackgroundRenderer();
			currentDocument = textArea.Document;
			if (currentDocument != null)
				currentDocument.TextChanged += textArea_Document_TextChanged;
			textArea.DocumentChanged += textArea_DocumentChanged;
			KeyDown += SearchLayerKeyDown;
			
			this.CommandBindings.Add(new CommandBinding(SearchCommands.FindNext, (sender, e) => FindNext()));
			this.CommandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, (sender, e) => FindPrevious()));
			this.CommandBindings.Add(new CommandBinding(SearchCommands.CloseSearchPanel, (sender, e) => Close()));
			IsClosed = true;
		}

		void textArea_DocumentChanged(object sender, EventArgs e)
		{
			if (currentDocument != null)
				currentDocument.TextChanged -= textArea_Document_TextChanged;
			currentDocument = textArea.Document;
			if (currentDocument != null) {
				currentDocument.TextChanged += textArea_Document_TextChanged;
				DoSearch(false);
			}
		}

		void textArea_Document_TextChanged(object sender, EventArgs e)
		{
			DoSearch(false);
		}
		
		/// <inheritdoc/>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			searchTextBox = Template.FindName("PART_searchTextBox", this) as TextBox;
		}
		
		void ValidateSearchText()
		{
			if (searchTextBox == null)
				return;
			var be = searchTextBox.GetBindingExpression(TextBox.TextProperty);
			try {
				Validation.ClearInvalid(be);
				UpdateSearch();
			} catch (SearchPatternException ex) {
				var ve = new ValidationError(be.ParentBinding.ValidationRules[0], be, ex.Message, ex);
				Validation.MarkInvalid(be, ve);
			}
		}
		
		/// <summary>
		/// Reactivates the SearchPanel by setting the focus on the search box and selecting all text.
		/// </summary>
		public void Reactivate()
		{
			if (searchTextBox == null)
				return;
			searchTextBox.Focus();
			searchTextBox.SelectAll();
		}
		
		/// <summary>
		/// Moves to the next occurrence in the file.
		/// </summary>
		public void FindNext()
		{
			SearchResult result = renderer.CurrentResults.FindFirstSegmentWithStartAfter(textArea.Caret.Offset + 1);
			if (result == null)
				result = renderer.CurrentResults.FirstSegment;
			if (result != null) {
				SelectResult(result);
			}
		}

		/// <summary>
		/// Moves to the previous occurrence in the file.
		/// </summary>
		public void FindPrevious()
		{
			SearchResult result = renderer.CurrentResults.FindFirstSegmentWithStartAfter(textArea.Caret.Offset);
			if (result != null)
				result = renderer.CurrentResults.GetPreviousSegment(result);
			if (result == null)
				result = renderer.CurrentResults.LastSegment;
			if (result != null) {
				SelectResult(result);
			}
		}
		
		ToolTip messageView = new ToolTip { Placement = PlacementMode.Bottom, StaysOpen = false };

		void DoSearch(bool changeSelection)
		{
			if (IsClosed)
				return;
			renderer.CurrentResults.Clear();
			
			if (!string.IsNullOrEmpty(SearchPattern)) {
				int offset = textArea.Caret.Offset;
				if (changeSelection) {
					textArea.ClearSelection();
				}
				// We cast from ISearchResult to SearchResult; this is safe because we always use the built-in strategy
				foreach (SearchResult result in strategy.FindAll(textArea.Document, 0, textArea.Document.TextLength)) {
					if (changeSelection && result.StartOffset >= offset) {
						SelectResult(result);
						changeSelection = false;
					}
					renderer.CurrentResults.Add(result);
				}
				if (!renderer.CurrentResults.Any()) {
					messageView.IsOpen = true;
					messageView.Content = Localization.NoMatchesFoundText;
					messageView.PlacementTarget = searchTextBox;
				} else
					messageView.IsOpen = false;
			}
			textArea.TextView.InvalidateLayer(KnownLayer.Selection);
		}

		void SelectResult(SearchResult result)
		{
			textArea.Caret.Offset = result.StartOffset;
			textArea.Selection = Selection.Create(textArea, result.StartOffset, result.EndOffset);
			textArea.Caret.BringCaretToView();
			// show caret even if the editor does not have the Keyboard Focus
			textArea.Caret.Show();
		}
		
		void SearchLayerKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key) {
				case Key.Enter:
					e.Handled = true;
					messageView.IsOpen = false;
					if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
						FindPrevious();
					else
						FindNext();
					if (searchTextBox != null) {
						var error = Validation.GetErrors(searchTextBox).FirstOrDefault();
						if (error != null) {
							messageView.Content = Localization.ErrorText + " " + error.ErrorContent;
							messageView.PlacementTarget = searchTextBox;
							messageView.IsOpen = true;
						}
					}
					break;
				case Key.Escape:
					e.Handled = true;
					Close();
					break;
			}
		}
		
		/// <summary>
		/// Gets whether the Panel is already closed.
		/// </summary>
		public bool IsClosed { get; private set; }
		
		/// <summary>
		/// Closes the SearchPanel.
		/// </summary>
		public void Close()
		{
			bool hasFocus = this.IsKeyboardFocusWithin;
			
			var layer = AdornerLayer.GetAdornerLayer(textArea);
			if (layer != null)
				layer.Remove(adorner);
			messageView.IsOpen = false;
			textArea.TextView.BackgroundRenderers.Remove(renderer);
			if (hasFocus)
				textArea.Focus();
			IsClosed = true;
			
			// Clear existing search results so that the segments don't have to be maintained
			renderer.CurrentResults.Clear();
		}
		
		/// <summary>
		/// Closes the SearchPanel and removes it.
		/// </summary>
		[Obsolete("Use the Uninstall method instead!")]
		public void CloseAndRemove()
		{
			Close();
			textArea.DocumentChanged -= textArea_DocumentChanged;
			if (currentDocument != null)
				currentDocument.TextChanged -= textArea_Document_TextChanged;
		}
		
		/// <summary>
		/// Opens the an existing search panel.
		/// </summary>
		public void Open()
		{
			if (!IsClosed) return;
			var layer = AdornerLayer.GetAdornerLayer(textArea);
			if (layer != null)
				layer.Add(adorner);
			textArea.TextView.BackgroundRenderers.Add(renderer);
			IsClosed = false;
			DoSearch(false);
		}
		
		/// <summary>
		/// Fired when SearchOptions are changed inside the SearchPanel.
		/// </summary>
		public event EventHandler<SearchOptionsChangedEventArgs> SearchOptionsChanged;
		
		/// <summary>
		/// Raises the <see cref="SearchPanel.SearchOptionsChanged" /> event.
		/// </summary>
		protected virtual void OnSearchOptionsChanged(SearchOptionsChangedEventArgs e)
		{
			if (SearchOptionsChanged != null) {
				SearchOptionsChanged(this, e);
			}
		}
	}
	
	/// <summary>
	/// EventArgs for <see cref="SearchPanel.SearchOptionsChanged"/> event.
	/// </summary>
	public class SearchOptionsChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the search pattern.
		/// </summary>
		public string SearchPattern { get; private set; }
		
		/// <summary>
		/// Gets whether the search pattern should be interpreted case-sensitive.
		/// </summary>
		public bool MatchCase { get; private set; }
		
		/// <summary>
		/// Gets whether the search pattern should be interpreted as regular expression.
		/// </summary>
		public bool UseRegex { get; private set; }
		
		/// <summary>
		/// Gets whether the search pattern should only match whole words.
		/// </summary>
		public bool WholeWords { get; private set; }
		
		/// <summary>
		/// Creates a new SearchOptionsChangedEventArgs instance.
		/// </summary>
		public SearchOptionsChangedEventArgs(string searchPattern, bool matchCase, bool useRegex, bool wholeWords)
		{
			this.SearchPattern = searchPattern;
			this.MatchCase = matchCase;
			this.UseRegex = useRegex;
			this.WholeWords = wholeWords;
		}
	}
	
	class SearchPanelAdorner : Adorner
	{
		SearchPanel panel;
		
		public SearchPanelAdorner(TextArea textArea, SearchPanel panel)
			: base(textArea)
		{
			this.panel = panel;
			AddVisualChild(panel);
		}
		
		protected override int VisualChildrenCount {
			get { return 1; }
		}

		protected override Visual GetVisualChild(int index)
		{
			if (index != 0)
				throw new ArgumentOutOfRangeException();
			return panel;
		}
		
		protected override Size ArrangeOverride(Size finalSize)
		{
			panel.Arrange(new Rect(new Point(0, 0), finalSize));
			return new Size(panel.ActualWidth, panel.ActualHeight);
		}
	}
}
