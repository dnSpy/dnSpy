// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// VisualLineElement that represents a piece of text and is a clickable link.
	/// </summary>
	public class VisualLineLinkText : VisualLineText
	{
		/// <summary>
		/// Gets/Sets the URL that is navigated to when the link is clicked.
		/// </summary>
		public Uri NavigateUri { get; set; }
		
		/// <summary>
		/// Gets/Sets the window name where the URL will be opened.
		/// </summary>
		public string TargetName { get; set; }
		
		/// <summary>
		/// Gets/Sets whether the user needs to press Control to click the link.
		/// The default value is true.
		/// </summary>
		public bool RequireControlModifierForClick { get; set; }
		
		/// <summary>
		/// Creates a visual line text element with the specified length.
		/// It uses the <see cref="ITextRunConstructionContext.VisualLine"/> and its
		/// <see cref="VisualLineElement.RelativeTextOffset"/> to find the actual text string.
		/// </summary>
		public VisualLineLinkText(VisualLine parentVisualLine, int length) : base(parentVisualLine, length)
		{
			this.RequireControlModifierForClick = true;
		}
		
		/// <inheritdoc/>
		public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
		{
			this.TextRunProperties.SetForegroundBrush(Brushes.Blue);
			this.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
			return base.CreateTextRun(startVisualColumn, context);
		}
		
		/// <summary>
		/// Gets whether the link is currently clickable.
		/// </summary>
		/// <remarks>Returns true when control is pressed; or when
		/// <see cref="RequireControlModifierForClick"/> is disabled.</remarks>
		protected bool LinkIsClickable()
		{
			if (NavigateUri == null)
				return false;
			if (RequireControlModifierForClick)
				return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
			else
				return true;
		}
		
		/// <inheritdoc/>
		protected internal override void OnQueryCursor(QueryCursorEventArgs e)
		{
			if (LinkIsClickable()) {
				e.Handled = true;
				e.Cursor = Cursors.Hand;
			}
		}
		
		/// <inheritdoc/>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
		                                                 Justification = "I've seen Process.Start throw undocumented exceptions when the mail client / web browser is installed incorrectly")]
		protected internal override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left && !e.Handled && LinkIsClickable()) {
				RequestNavigateEventArgs args = new RequestNavigateEventArgs(this.NavigateUri, this.TargetName);
				args.RoutedEvent = Hyperlink.RequestNavigateEvent;
				FrameworkElement element = e.Source as FrameworkElement;
				if (element != null) {
					// allow user code to handle the navigation request
					element.RaiseEvent(args);
				}
				if (!args.Handled) {
					try {
						Process.Start(this.NavigateUri.ToString());
					} catch {
						// ignore all kinds of errors during web browser start
					}
				}
				e.Handled = true;
			}
		}
		
		/// <inheritdoc/>
		protected override VisualLineText CreateInstance(int length)
		{
			return new VisualLineLinkText(ParentVisualLine, length) {
				NavigateUri = this.NavigateUri,
				TargetName = this.TargetName,
				RequireControlModifierForClick = this.RequireControlModifierForClick
			};
		}
	}
}
