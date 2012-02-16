// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit
{
	/// <summary>
	/// The text editor control.
	/// Contains a scrollable TextArea.
	/// </summary>
	[Localizability(LocalizationCategory.Text), ContentProperty("Text")]
	public class TextEditor : Control, ITextEditorComponent, IServiceProvider, IWeakEventListener
	{
		#region Constructors
		static TextEditor()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TextEditor),
			                                         new FrameworkPropertyMetadata(typeof(TextEditor)));
			FocusableProperty.OverrideMetadata(typeof(TextEditor),
			                                   new FrameworkPropertyMetadata(Boxes.True));
		}
		
		/// <summary>
		/// Creates a new TextEditor instance.
		/// </summary>
		public TextEditor() : this(new TextArea())
		{
		}
		
		/// <summary>
		/// Creates a new TextEditor instance.
		/// </summary>
		protected TextEditor(TextArea textArea)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
			this.textArea = textArea;
			
			textArea.TextView.Services.AddService(typeof(TextEditor), this);
			
			SetCurrentValue(OptionsProperty, textArea.Options);
			SetCurrentValue(DocumentProperty, new TextDocument());
		}
		
		#if !DOTNET4
		void SetCurrentValue(DependencyProperty property, object value)
		{
			SetValue(property, value);
		}
		#endif
		#endregion
		
		/// <inheritdoc/>
		protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
		{
			return new TextEditorAutomationPeer(this);
		}
		
		/// Forward focus to TextArea.
		/// <inheritdoc/>
		protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			base.OnGotKeyboardFocus(e);
			if (e.NewFocus == this) {
				Keyboard.Focus(this.TextArea);
				e.Handled = true;
			}
		}
		
		#region Document property
		/// <summary>
		/// Document property.
		/// </summary>
		public static readonly DependencyProperty DocumentProperty
			= TextView.DocumentProperty.AddOwner(
				typeof(TextEditor), new FrameworkPropertyMetadata(OnDocumentChanged));
		
		/// <summary>
		/// Gets/Sets the document displayed by the text editor.
		/// This is a dependency property.
		/// </summary>
		public TextDocument Document {
			get { return (TextDocument)GetValue(DocumentProperty); }
			set { SetValue(DocumentProperty, value); }
		}
		
		/// <summary>
		/// Occurs when the document property has changed.
		/// </summary>
		public event EventHandler DocumentChanged;
		
		/// <summary>
		/// Raises the <see cref="DocumentChanged"/> event.
		/// </summary>
		protected virtual void OnDocumentChanged(EventArgs e)
		{
			if (DocumentChanged != null) {
				DocumentChanged(this, e);
			}
		}
		
		static void OnDocumentChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
		{
			((TextEditor)dp).OnDocumentChanged((TextDocument)e.OldValue, (TextDocument)e.NewValue);
		}
		
		void OnDocumentChanged(TextDocument oldValue, TextDocument newValue)
		{
			if (oldValue != null) {
				TextDocumentWeakEventManager.TextChanged.RemoveListener(oldValue, this);
				PropertyChangedEventManager.RemoveListener(oldValue.UndoStack, this, "IsOriginalFile");
			}
			textArea.Document = newValue;
			if (newValue != null) {
				TextDocumentWeakEventManager.TextChanged.AddListener(newValue, this);
				PropertyChangedEventManager.AddListener(newValue.UndoStack, this, "IsOriginalFile");
			}
			OnDocumentChanged(EventArgs.Empty);
			OnTextChanged(EventArgs.Empty);
		}
		#endregion
		
		#region Options property
		/// <summary>
		/// Options property.
		/// </summary>
		public static readonly DependencyProperty OptionsProperty
			= TextView.OptionsProperty.AddOwner(typeof(TextEditor), new FrameworkPropertyMetadata(OnOptionsChanged));
		
		/// <summary>
		/// Gets/Sets the options currently used by the text editor.
		/// </summary>
		public TextEditorOptions Options {
			get { return (TextEditorOptions)GetValue(OptionsProperty); }
			set { SetValue(OptionsProperty, value); }
		}
		
		/// <summary>
		/// Occurs when a text editor option has changed.
		/// </summary>
		public event PropertyChangedEventHandler OptionChanged;
		
		/// <summary>
		/// Raises the <see cref="OptionChanged"/> event.
		/// </summary>
		protected virtual void OnOptionChanged(PropertyChangedEventArgs e)
		{
			if (OptionChanged != null) {
				OptionChanged(this, e);
			}
		}
		
		static void OnOptionsChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
		{
			((TextEditor)dp).OnOptionsChanged((TextEditorOptions)e.OldValue, (TextEditorOptions)e.NewValue);
		}
		
		void OnOptionsChanged(TextEditorOptions oldValue, TextEditorOptions newValue)
		{
			if (oldValue != null) {
				PropertyChangedWeakEventManager.RemoveListener(oldValue, this);
			}
			textArea.Options = newValue;
			if (newValue != null) {
				PropertyChangedWeakEventManager.AddListener(newValue, this);
			}
			OnOptionChanged(new PropertyChangedEventArgs(null));
		}
		
		/// <inheritdoc cref="IWeakEventListener.ReceiveWeakEvent"/>
		protected virtual bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			if (managerType == typeof(PropertyChangedWeakEventManager)) {
				OnOptionChanged((PropertyChangedEventArgs)e);
				return true;
			} else if (managerType == typeof(TextDocumentWeakEventManager.TextChanged)) {
				OnTextChanged(e);
				return true;
			} else if (managerType == typeof(PropertyChangedEventManager)) {
				return HandleIsOriginalChanged((PropertyChangedEventArgs)e);
			}
			return false;
		}
		
		bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			return ReceiveWeakEvent(managerType, sender, e);
		}
		#endregion
		
		#region Text property
		/// <summary>
		/// Gets/Sets the text of the current document.
		/// </summary>
		[Localizability(LocalizationCategory.Text), DefaultValue("")]
		public string Text {
			get {
				TextDocument document = this.Document;
				return document != null ? document.Text : string.Empty;
			}
			set {
				TextDocument document = GetDocument();
				document.Text = value ?? string.Empty;
				// after replacing the full text, the caret is positioned at the end of the document
				// - reset it to the beginning.
				this.CaretOffset = 0;
				document.UndoStack.ClearAll();
			}
		}
		
		TextDocument GetDocument()
		{
			TextDocument document = this.Document;
			if (document == null)
				throw ThrowUtil.NoDocumentAssigned();
			return document;
		}
		
		/// <summary>
		/// Occurs when the Text property changes.
		/// </summary>
		public event EventHandler TextChanged;
		
		/// <summary>
		/// Raises the <see cref="TextChanged"/> event.
		/// </summary>
		protected virtual void OnTextChanged(EventArgs e)
		{
			if (TextChanged != null) {
				TextChanged(this, e);
			}
		}
		#endregion
		
		#region TextArea / ScrollViewer properties
		readonly TextArea textArea;
		ScrollViewer scrollViewer;
		
		/// <summary>
		/// Is called after the template was applied.
		/// </summary>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			scrollViewer = (ScrollViewer)Template.FindName("PART_ScrollViewer", this);
		}
		
		/// <summary>
		/// Gets the text area.
		/// </summary>
		public TextArea TextArea {
			get {
				return textArea;
			}
		}
		
		/// <summary>
		/// Gets the scroll viewer used by the text editor.
		/// This property can return null if the template has not been applied / does not contain a scroll viewer.
		/// </summary>
		internal ScrollViewer ScrollViewer {
			get { return scrollViewer; }
		}
		
		bool CanExecute(RoutedUICommand command)
		{
			TextArea textArea = this.TextArea;
			if (textArea == null)
				return false;
			else
				return command.CanExecute(null, textArea);
		}
		
		void Execute(RoutedUICommand command)
		{
			TextArea textArea = this.TextArea;
			if (textArea != null)
				command.Execute(null, textArea);
		}
		#endregion
		
		#region Syntax highlighting
		/// <summary>
		/// The <see cref="SyntaxHighlighting"/> property.
		/// </summary>
		public static readonly DependencyProperty SyntaxHighlightingProperty =
			DependencyProperty.Register("SyntaxHighlighting", typeof(IHighlightingDefinition), typeof(TextEditor),
			                            new FrameworkPropertyMetadata(OnSyntaxHighlightingChanged));
		
		
		/// <summary>
		/// Gets/sets the syntax highlighting definition used to colorize the text.
		/// </summary>
		public IHighlightingDefinition SyntaxHighlighting {
			get { return (IHighlightingDefinition)GetValue(SyntaxHighlightingProperty); }
			set { SetValue(SyntaxHighlightingProperty, value); }
		}
		
		IVisualLineTransformer colorizer;
		
		static void OnSyntaxHighlightingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((TextEditor)d).OnSyntaxHighlightingChanged(e.NewValue as IHighlightingDefinition);
		}
		
		void OnSyntaxHighlightingChanged(IHighlightingDefinition newValue)
		{
			if (colorizer != null) {
				this.TextArea.TextView.LineTransformers.Remove(colorizer);
				colorizer = null;
			}
			if (newValue != null) {
				colorizer = CreateColorizer(newValue);
				this.TextArea.TextView.LineTransformers.Insert(0, colorizer);
			}
		}
		
		/// <summary>
		/// Creates the highlighting colorizer for the specified highlighting definition.
		/// Allows derived classes to provide custom colorizer implementations for special highlighting definitions.
		/// </summary>
		/// <returns></returns>
		protected virtual IVisualLineTransformer CreateColorizer(IHighlightingDefinition highlightingDefinition)
		{
			if (highlightingDefinition == null)
				throw new ArgumentNullException("highlightingDefinition");
			return new HighlightingColorizer(highlightingDefinition.MainRuleSet);
		}
		#endregion
		
		#region WordWrap
		/// <summary>
		/// Word wrap dependency property.
		/// </summary>
		public static readonly DependencyProperty WordWrapProperty =
			DependencyProperty.Register("WordWrap", typeof(bool), typeof(TextEditor),
			                            new FrameworkPropertyMetadata(Boxes.False));
		
		/// <summary>
		/// Specifies whether the text editor uses word wrapping.
		/// </summary>
		/// <remarks>
		/// Setting WordWrap=true has the same effect as setting HorizontalScrollBarVisibility=Disabled and will override the
		/// HorizontalScrollBarVisibility setting.
		/// </remarks>
		public bool WordWrap {
			get { return (bool)GetValue(WordWrapProperty); }
			set { SetValue(WordWrapProperty, Boxes.Box(value)); }
		}
		#endregion
		
		#region IsReadOnly
		/// <summary>
		/// IsReadOnly dependency property.
		/// </summary>
		public static readonly DependencyProperty IsReadOnlyProperty =
			DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(TextEditor),
			                            new FrameworkPropertyMetadata(Boxes.False, OnIsReadOnlyChanged));
		
		/// <summary>
		/// Specifies whether the user can change the text editor content.
		/// Setting this property will replace the
		/// <see cref="Editing.TextArea.ReadOnlySectionProvider">TextArea.ReadOnlySectionProvider</see>.
		/// </summary>
		public bool IsReadOnly {
			get { return (bool)GetValue(IsReadOnlyProperty); }
			set { SetValue(IsReadOnlyProperty, Boxes.Box(value)); }
		}
		
		static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TextEditor editor = d as TextEditor;
			if (editor != null) {
				if ((bool)e.NewValue)
					editor.TextArea.ReadOnlySectionProvider = ReadOnlyDocument.Instance;
				else
					editor.TextArea.ReadOnlySectionProvider = NoReadOnlySections.Instance;
				
				TextEditorAutomationPeer peer = TextEditorAutomationPeer.FromElement(editor) as TextEditorAutomationPeer;
				if (peer != null) {
					peer.RaiseIsReadOnlyChanged((bool)e.OldValue, (bool)e.NewValue);
				}
			}
		}
		#endregion
		
		#region IsModified
		/// <summary>
		/// Dependency property for <see cref="IsModified"/>
		/// </summary>
		public static readonly DependencyProperty IsModifiedProperty =
			DependencyProperty.Register("IsModified", typeof(bool), typeof(TextEditor),
			                            new FrameworkPropertyMetadata(Boxes.False, OnIsModifiedChanged));
		
		/// <summary>
		/// Gets/Sets the 'modified' flag.
		/// </summary>
		public bool IsModified {
			get { return (bool)GetValue(IsModifiedProperty); }
			set { SetValue(IsModifiedProperty, Boxes.Box(value)); }
		}
		
		static void OnIsModifiedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TextEditor editor = d as TextEditor;
			if (editor != null) {
				TextDocument document = editor.Document;
				if (document != null) {
					UndoStack undoStack = document.UndoStack;
					if ((bool)e.NewValue) {
						if (undoStack.IsOriginalFile)
							undoStack.DiscardOriginalFileMarker();
					} else {
						undoStack.MarkAsOriginalFile();
					}
				}
			}
		}
		
		bool HandleIsOriginalChanged(PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IsOriginalFile") {
				TextDocument document = this.Document;
				if (document != null) {
					SetCurrentValue(IsModifiedProperty, Boxes.Box(!document.UndoStack.IsOriginalFile));
				}
				return true;
			} else {
				return false;
			}
		}
		#endregion
		
		#region ShowLineNumbers
		/// <summary>
		/// ShowLineNumbers dependency property.
		/// </summary>
		public static readonly DependencyProperty ShowLineNumbersProperty =
			DependencyProperty.Register("ShowLineNumbers", typeof(bool), typeof(TextEditor),
			                            new FrameworkPropertyMetadata(Boxes.False, OnShowLineNumbersChanged));
		
		/// <summary>
		/// Specifies whether line numbers are shown on the left to the text view.
		/// </summary>
		public bool ShowLineNumbers {
			get { return (bool)GetValue(ShowLineNumbersProperty); }
			set { SetValue(ShowLineNumbersProperty, Boxes.Box(value)); }
		}
		
		static void OnShowLineNumbersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TextEditor editor = (TextEditor)d;
			var leftMargins = editor.TextArea.LeftMargins;
			if ((bool)e.NewValue) {
				LineNumberMargin lineNumbers = new LineNumberMargin();
				Line line = (Line)DottedLineMargin.Create();
				leftMargins.Insert(0, lineNumbers);
				leftMargins.Insert(1, line);
				var lineNumbersForeground = new Binding("LineNumbersForeground") { Source = editor };
				line.SetBinding(Line.StrokeProperty, lineNumbersForeground);
				lineNumbers.SetBinding(Control.ForegroundProperty, lineNumbersForeground);
			} else {
				for (int i = 0; i < leftMargins.Count; i++) {
					if (leftMargins[i] is LineNumberMargin) {
						leftMargins.RemoveAt(i);
						if (i < leftMargins.Count && DottedLineMargin.IsDottedLineMargin(leftMargins[i])) {
							leftMargins.RemoveAt(i);
						}
						break;
					}
				}
			}
		}
		#endregion
		
		#region LineNumbersForeground
		/// <summary>
		/// LineNumbersForeground dependency property.
		/// </summary>
		public static readonly DependencyProperty LineNumbersForegroundProperty =
			DependencyProperty.Register("LineNumbersForeground", typeof(Brush), typeof(TextEditor),
			                            new FrameworkPropertyMetadata(Brushes.Gray, OnLineNumbersForegroundChanged));
		
		/// <summary>
		/// Gets/sets the Brush used for displaying the foreground color of line numbers.
		/// </summary>
		public Brush LineNumbersForeground {
			get { return (Brush)GetValue(LineNumbersForegroundProperty); }
			set { SetValue(LineNumbersForegroundProperty, value); }
		}
		
		static void OnLineNumbersForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TextEditor editor = (TextEditor)d;
			var lineNumberMargin = editor.TextArea.LeftMargins.FirstOrDefault(margin => margin is LineNumberMargin) as LineNumberMargin;;
			
			if (lineNumberMargin != null) {
				lineNumberMargin.SetValue(Control.ForegroundProperty, e.NewValue);
			}
		}
		#endregion
		
		#region TextBoxBase-like methods
		/// <summary>
		/// Appends text to the end of the document.
		/// </summary>
		public void AppendText(string textData)
		{
			var document = GetDocument();
			document.Insert(document.TextLength, textData);
		}
		
		/// <summary>
		/// Begins a group of document changes.
		/// </summary>
		public void BeginChange()
		{
			GetDocument().BeginUpdate();
		}
		
		/// <summary>
		/// Copies the current selection to the clipboard.
		/// </summary>
		public void Copy()
		{
			Execute(ApplicationCommands.Copy);
		}
		
		/// <summary>
		/// Removes the current selection and copies it to the clipboard.
		/// </summary>
		public void Cut()
		{
			Execute(ApplicationCommands.Cut);
		}
		
		/// <summary>
		/// Begins a group of document changes and returns an object that ends the group of document
		/// changes when it is disposed.
		/// </summary>
		public IDisposable DeclareChangeBlock()
		{
			return GetDocument().RunUpdate();
		}
		
		/// <summary>
		/// Ends the current group of document changes.
		/// </summary>
		public void EndChange()
		{
			GetDocument().EndUpdate();
		}
		
		/// <summary>
		/// Scrolls one line down.
		/// </summary>
		public void LineDown()
		{
			if (scrollViewer != null)
				scrollViewer.LineDown();
		}
		
		/// <summary>
		/// Scrolls to the left.
		/// </summary>
		public void LineLeft()
		{
			if (scrollViewer != null)
				scrollViewer.LineLeft();
		}
		
		/// <summary>
		/// Scrolls to the right.
		/// </summary>
		public void LineRight()
		{
			if (scrollViewer != null)
				scrollViewer.LineRight();
		}
		
		/// <summary>
		/// Scrolls one line up.
		/// </summary>
		public void LineUp()
		{
			if (scrollViewer != null)
				scrollViewer.LineUp();
		}
		
		/// <summary>
		/// Scrolls one page down.
		/// </summary>
		public void PageDown()
		{
			if (scrollViewer != null)
				scrollViewer.PageDown();
		}
		
		/// <summary>
		/// Scrolls one page up.
		/// </summary>
		public void PageUp()
		{
			if (scrollViewer != null)
				scrollViewer.PageUp();
		}
		
		/// <summary>
		/// Scrolls one page left.
		/// </summary>
		public void PageLeft()
		{
			if (scrollViewer != null)
				scrollViewer.PageLeft();
		}
		
		/// <summary>
		/// Scrolls one page right.
		/// </summary>
		public void PageRight()
		{
			if (scrollViewer != null)
				scrollViewer.PageRight();
		}
		
		/// <summary>
		/// Pastes the clipboard content.
		/// </summary>
		public void Paste()
		{
			Execute(ApplicationCommands.Paste);
		}
		
		/// <summary>
		/// Redoes the most recent undone command.
		/// </summary>
		/// <returns>True is the redo operation was successful, false is the redo stack is empty.</returns>
		public bool Redo()
		{
			if (CanExecute(ApplicationCommands.Redo)) {
				Execute(ApplicationCommands.Redo);
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// Scrolls to the end of the document.
		/// </summary>
		public void ScrollToEnd()
		{
			ApplyTemplate(); // ensure scrollViewer is created
			if (scrollViewer != null)
				scrollViewer.ScrollToEnd();
		}
		
		/// <summary>
		/// Scrolls to the start of the document.
		/// </summary>
		public void ScrollToHome()
		{
			ApplyTemplate(); // ensure scrollViewer is created
			if (scrollViewer != null)
				scrollViewer.ScrollToHome();
		}
		
		/// <summary>
		/// Scrolls to the specified position in the document.
		/// </summary>
		public void ScrollToHorizontalOffset(double offset)
		{
			ApplyTemplate(); // ensure scrollViewer is created
			if (scrollViewer != null)
				scrollViewer.ScrollToHorizontalOffset(offset);
		}
		
		/// <summary>
		/// Scrolls to the specified position in the document.
		/// </summary>
		public void ScrollToVerticalOffset(double offset)
		{
			ApplyTemplate(); // ensure scrollViewer is created
			if (scrollViewer != null)
				scrollViewer.ScrollToVerticalOffset(offset);
		}
		
		/// <summary>
		/// Selects the entire text.
		/// </summary>
		public void SelectAll()
		{
			Execute(ApplicationCommands.SelectAll);
		}
		
		/// <summary>
		/// Undoes the most recent command.
		/// </summary>
		/// <returns>True is the undo operation was successful, false is the undo stack is empty.</returns>
		public bool Undo()
		{
			if (CanExecute(ApplicationCommands.Undo)) {
				Execute(ApplicationCommands.Undo);
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// Gets if the most recent undone command can be redone.
		/// </summary>
		public bool CanRedo {
			get { return CanExecute(ApplicationCommands.Redo); }
		}
		
		/// <summary>
		/// Gets if the most recent command can be undone.
		/// </summary>
		public bool CanUndo {
			get { return CanExecute(ApplicationCommands.Undo); }
		}
		
		/// <summary>
		/// Gets the vertical size of the document.
		/// </summary>
		public double ExtentHeight {
			get {
				return scrollViewer != null ? scrollViewer.ExtentHeight : 0;
			}
		}
		
		/// <summary>
		/// Gets the horizontal size of the current document region.
		/// </summary>
		public double ExtentWidth {
			get {
				return scrollViewer != null ? scrollViewer.ExtentWidth : 0;
			}
		}
		
		/// <summary>
		/// Gets the horizontal size of the viewport.
		/// </summary>
		public double ViewportHeight {
			get {
				return scrollViewer != null ? scrollViewer.ViewportHeight : 0;
			}
		}
		
		/// <summary>
		/// Gets the horizontal size of the viewport.
		/// </summary>
		public double ViewportWidth {
			get {
				return scrollViewer != null ? scrollViewer.ViewportWidth : 0;
			}
		}
		
		/// <summary>
		/// Gets the vertical scroll position.
		/// </summary>
		public double VerticalOffset {
			get {
				return scrollViewer != null ? scrollViewer.VerticalOffset : 0;
			}
		}
		
		/// <summary>
		/// Gets the horizontal scroll position.
		/// </summary>
		public double HorizontalOffset {
			get {
				return scrollViewer != null ? scrollViewer.HorizontalOffset : 0;
			}
		}
		#endregion
		
		#region TextBox methods
		/// <summary>
		/// Gets/Sets the selected text.
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string SelectedText {
			get {
				TextArea textArea = this.TextArea;
				// We'll get the text from the whole surrounding segment.
				// This is done to ensure that SelectedText.Length == SelectionLength.
				if (textArea != null && textArea.Document != null && !textArea.Selection.IsEmpty)
					return textArea.Document.GetText(textArea.Selection.SurroundingSegment);
				else
					return string.Empty;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				TextArea textArea = this.TextArea;
				if (textArea != null && textArea.Document != null) {
					int offset = this.SelectionStart;
					int length = this.SelectionLength;
					textArea.Document.Replace(offset, length, value);
					// keep inserted text selected
					textArea.Selection = SimpleSelection.Create(textArea, offset, offset + value.Length);
				}
			}
		}
		
		/// <summary>
		/// Gets/sets the caret position.
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int CaretOffset {
			get {
				TextArea textArea = this.TextArea;
				if (textArea != null)
					return textArea.Caret.Offset;
				else
					return 0;
			}
			set {
				TextArea textArea = this.TextArea;
				if (textArea != null)
					textArea.Caret.Offset = value;
			}
		}
		
		/// <summary>
		/// Gets/sets the start position of the selection.
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int SelectionStart {
			get {
				TextArea textArea = this.TextArea;
				if (textArea != null) {
					if (textArea.Selection.IsEmpty)
						return textArea.Caret.Offset;
					else
						return textArea.Selection.SurroundingSegment.Offset;
				} else {
					return 0;
				}
			}
			set {
				Select(value, SelectionLength);
			}
		}
		
		/// <summary>
		/// Gets/sets the length of the selection.
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int SelectionLength {
			get {
				TextArea textArea = this.TextArea;
				if (textArea != null && !textArea.Selection.IsEmpty)
					return textArea.Selection.SurroundingSegment.Length;
				else
					return 0;
			}
			set {
				Select(SelectionStart, value);
			}
		}
		
		/// <summary>
		/// Selects the specified text section.
		/// </summary>
		public void Select(int start, int length)
		{
			int documentLength = Document != null ? Document.TextLength : 0;
			if (start < 0 || start > documentLength)
				throw new ArgumentOutOfRangeException("start", start, "Value must be between 0 and " + documentLength);
			if (length < 0 || start + length > documentLength)
				throw new ArgumentOutOfRangeException("length", length, "Value must be between 0 and " + (documentLength - length));
			textArea.Selection = SimpleSelection.Create(textArea, start, start + length);
			textArea.Caret.Offset = start + length;
		}
		
		/// <summary>
		/// Gets the number of lines in the document.
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int LineCount {
			get {
				TextDocument document = this.Document;
				if (document != null)
					return document.LineCount;
				else
					return 1;
			}
		}
		
		/// <summary>
		/// Clears the text.
		/// </summary>
		public void Clear()
		{
			this.Text = string.Empty;
		}
		#endregion
		
		#region Loading from stream
		/// <summary>
		/// Loads the text from the stream, auto-detecting the encoding.
		/// </summary>
		/// <remarks>
		/// This method sets <see cref="IsModified"/> to false.
		/// </remarks>
		public void Load(Stream stream)
		{
			using (StreamReader reader = FileReader.OpenStream(stream, this.Encoding ?? Encoding.UTF8)) {
				this.Text = reader.ReadToEnd();
				this.Encoding = reader.CurrentEncoding; // assign encoding after ReadToEnd() so that the StreamReader can autodetect the encoding
			}
			this.IsModified = false;
		}
		
		/// <summary>
		/// Loads the text from the stream, auto-detecting the encoding.
		/// </summary>
		public void Load(string fileName)
		{
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				Load(fs);
			}
		}
		
		/// <summary>
		/// Gets/sets the encoding used when the file is saved.
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Encoding Encoding { get; set; }
		
		/// <summary>
		/// Saves the text to the stream.
		/// </summary>
		/// <remarks>
		/// This method sets <see cref="IsModified"/> to false.
		/// </remarks>
		public void Save(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");
			StreamWriter writer = new StreamWriter(stream, this.Encoding ?? Encoding.UTF8);
			writer.Write(this.Text);
			writer.Flush();
			// do not close the stream
			this.IsModified = false;
		}
		
		/// <summary>
		/// Saves the text to the file.
		/// </summary>
		public void Save(string fileName)
		{
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None)) {
				Save(fs);
			}
		}
		#endregion
		
		#region MouseHover events
		/// <summary>
		/// The PreviewMouseHover event.
		/// </summary>
		public static readonly RoutedEvent PreviewMouseHoverEvent =
			TextView.PreviewMouseHoverEvent.AddOwner(typeof(TextEditor));
		
		/// <summary>
		/// The MouseHover event.
		/// </summary>
		public static readonly RoutedEvent MouseHoverEvent =
			TextView.MouseHoverEvent.AddOwner(typeof(TextEditor));
		
		
		/// <summary>
		/// The PreviewMouseHoverStopped event.
		/// </summary>
		public static readonly RoutedEvent PreviewMouseHoverStoppedEvent =
			TextView.PreviewMouseHoverStoppedEvent.AddOwner(typeof(TextEditor));
		
		/// <summary>
		/// The MouseHoverStopped event.
		/// </summary>
		public static readonly RoutedEvent MouseHoverStoppedEvent =
			TextView.MouseHoverStoppedEvent.AddOwner(typeof(TextEditor));
		
		
		/// <summary>
		/// Occurs when the mouse has hovered over a fixed location for some time.
		/// </summary>
		public event MouseEventHandler PreviewMouseHover {
			add { AddHandler(PreviewMouseHoverEvent, value); }
			remove { RemoveHandler(PreviewMouseHoverEvent, value); }
		}
		
		/// <summary>
		/// Occurs when the mouse has hovered over a fixed location for some time.
		/// </summary>
		public event MouseEventHandler MouseHover {
			add { AddHandler(MouseHoverEvent, value); }
			remove { RemoveHandler(MouseHoverEvent, value); }
		}
		
		/// <summary>
		/// Occurs when the mouse had previously hovered but now started moving again.
		/// </summary>
		public event MouseEventHandler PreviewMouseHoverStopped {
			add { AddHandler(PreviewMouseHoverStoppedEvent, value); }
			remove { RemoveHandler(PreviewMouseHoverStoppedEvent, value); }
		}
		
		/// <summary>
		/// Occurs when the mouse had previously hovered but now started moving again.
		/// </summary>
		public event MouseEventHandler MouseHoverStopped {
			add { AddHandler(MouseHoverStoppedEvent, value); }
			remove { RemoveHandler(MouseHoverStoppedEvent, value); }
		}
		#endregion
		
		#region ScrollBarVisibility
		/// <summary>
		/// Dependency property for <see cref="HorizontalScrollBarVisibility"/>
		/// </summary>
		public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty = ScrollViewer.HorizontalScrollBarVisibilityProperty.AddOwner(typeof(TextEditor), new FrameworkPropertyMetadata(ScrollBarVisibility.Visible));
		
		/// <summary>
		/// Gets/Sets the horizontal scroll bar visibility.
		/// </summary>
		public ScrollBarVisibility HorizontalScrollBarVisibility {
			get { return (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty); }
			set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
		}
		
		/// <summary>
		/// Dependency property for <see cref="VerticalScrollBarVisibility"/>
		/// </summary>
		public static readonly DependencyProperty VerticalScrollBarVisibilityProperty = ScrollViewer.VerticalScrollBarVisibilityProperty.AddOwner(typeof(TextEditor), new FrameworkPropertyMetadata(ScrollBarVisibility.Visible));
		
		/// <summary>
		/// Gets/Sets the vertical scroll bar visibility.
		/// </summary>
		public ScrollBarVisibility VerticalScrollBarVisibility {
			get { return (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); }
			set { SetValue(VerticalScrollBarVisibilityProperty, value); }
		}
		#endregion
		
		object IServiceProvider.GetService(Type serviceType)
		{
			return textArea.GetService(serviceType);
		}
		
		/// <summary>
		/// Gets the text view position from a point inside the editor.
		/// </summary>
		/// <param name="point">The position, relative to top left
		/// corner of TextEditor control</param>
		/// <returns>The text view position, or null if the point is outside the document.</returns>
		public TextViewPosition? GetPositionFromPoint(Point point)
		{
			if (this.Document == null)
				return null;
			TextView textView = this.TextArea.TextView;
			return textView.GetPosition(TranslatePoint(point, textView) + textView.ScrollOffset);
		}
		
		/// <summary>
		/// Scrolls to the specified line.
		/// This method requires that the TextEditor was already assigned a size (WPF layout must have run prior).
		/// </summary>
		public void ScrollToLine(int line)
		{
			ScrollTo(line, -1);
		}
		
		/// <summary>
		/// Scrolls to the specified line/column.
		/// This method requires that the TextEditor was already assigned a size (WPF layout must have run prior).
		/// </summary>
		public void ScrollTo(int line, int column)
		{
			const double MinimumScrollPercentage = 0.3;
			
			TextView textView = textArea.TextView;
			TextDocument document = textView.Document;
			if (scrollViewer != null && document != null) {
				if (line < 1)
					line = 1;
				if (line > document.LineCount)
					line = document.LineCount;
				
				IScrollInfo scrollInfo = textView;
				if (!scrollInfo.CanHorizontallyScroll) {
					// Word wrap is enabled. Ensure that we have up-to-date info about line height so that we scroll
					// to the correct position.
					// This avoids that the user has to repeat the ScrollTo() call several times when there are very long lines.
					VisualLine vl = textView.GetOrConstructVisualLine(document.GetLineByNumber(line));
					double remainingHeight = scrollViewer.ViewportHeight / 2;
					while (remainingHeight > 0) {
						DocumentLine prevLine = vl.FirstDocumentLine.PreviousLine;
						if (prevLine == null)
							break;
						vl = textView.GetOrConstructVisualLine(prevLine);
						remainingHeight -= vl.Height;
					}
				}
				
				Point p = textArea.TextView.GetVisualPosition(new TextViewPosition(line, Math.Max(1, column)), VisualYPosition.LineMiddle);
				double verticalPos = p.Y - scrollViewer.ViewportHeight / 2;
				if (Math.Abs(verticalPos - scrollViewer.VerticalOffset) > MinimumScrollPercentage * scrollViewer.ViewportHeight) {
					scrollViewer.ScrollToVerticalOffset(Math.Max(0, verticalPos));
				}
				if (column > 0) {
					if (p.X > scrollViewer.ViewportWidth - Caret.MinimumDistanceToViewBorder * 2) {
						double horizontalPos = Math.Max(0, p.X - scrollViewer.ViewportWidth / 2);
						if (Math.Abs(horizontalPos - scrollViewer.HorizontalOffset) > MinimumScrollPercentage * scrollViewer.ViewportWidth) {
							scrollViewer.ScrollToHorizontalOffset(horizontalPos);
						}
					} else {
						scrollViewer.ScrollToHorizontalOffset(0);
					}
				}
			}
		}
	}
}