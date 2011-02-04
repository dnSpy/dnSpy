// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.Windows;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Base class for margins.
	/// Margins don't have to derive from this class, it just helps maintaining a reference to the TextView
	/// and the TextDocument.
	/// AbstractMargin derives from FrameworkElement, so if you don't want to handle visual children and rendering
	/// on your own, choose another base class for your margin!
	/// </summary>
	public abstract class AbstractMargin : FrameworkElement, ITextViewConnect
	{
		/// <summary>
		/// TextView property.
		/// </summary>
		public static readonly DependencyProperty TextViewProperty =
			DependencyProperty.Register("TextView", typeof(TextView), typeof(AbstractMargin),
			                            new FrameworkPropertyMetadata(OnTextViewChanged));
		
		/// <summary>
		/// Gets/sets the text view for which line numbers are displayed.
		/// </summary>
		/// <remarks>Adding a margin to <see cref="TextArea.LeftMargins"/> will automatically set this property to the text area's TextView.</remarks>
		public TextView TextView {
			get { return (TextView)GetValue(TextViewProperty); }
			set { SetValue(TextViewProperty, value); }
		}
		
		static void OnTextViewChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
		{
			AbstractMargin margin = (AbstractMargin)dp;
			margin.wasAutoAddedToTextView = false;
			margin.OnTextViewChanged((TextView)e.OldValue, (TextView)e.NewValue);
		}
		
		// automatically set/unset TextView property using ITextViewConnect
		bool wasAutoAddedToTextView;
		
		void ITextViewConnect.AddToTextView(TextView textView)
		{
			if (this.TextView == null) {
				this.TextView = textView;
				wasAutoAddedToTextView = true;
			} else if (this.TextView != textView) {
				throw new InvalidOperationException("This margin belongs to a different TextView.");
			}
		}
		
		void ITextViewConnect.RemoveFromTextView(TextView textView)
		{
			if (wasAutoAddedToTextView && this.TextView == textView) {
				this.TextView = null;
				Debug.Assert(!wasAutoAddedToTextView); // setting this.TextView should have unset this flag
			}
		}
		
		TextDocument document;
		
		/// <summary>
		/// Gets the document associated with the margin.
		/// </summary>
		public TextDocument Document {
			get { return document; }
		}
		
		/// <summary>
		/// Called when the <see cref="TextView"/> is changing.
		/// </summary>
		protected virtual void OnTextViewChanged(TextView oldTextView, TextView newTextView)
		{
			if (oldTextView != null) {
				oldTextView.DocumentChanged -= TextViewDocumentChanged;
			}
			if (newTextView != null) {
				newTextView.DocumentChanged += TextViewDocumentChanged;
			}
			TextViewDocumentChanged(null, null);
		}
		
		void TextViewDocumentChanged(object sender, EventArgs e)
		{
			OnDocumentChanged(document, TextView != null ? TextView.Document : null);
		}
		
		/// <summary>
		/// Called when the <see cref="Document"/> is changing.
		/// </summary>
		protected virtual void OnDocumentChanged(TextDocument oldDocument, TextDocument newDocument)
		{
			document = newDocument;
		}
	}
}
