// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Base class for <see cref="IVisualLineTransformer"/> that helps
	/// splitting visual elements so that colors (and other text properties) can be easily assigned
	/// to individual words/characters.
	/// </summary>
	public abstract class ColorizingTransformer : IVisualLineTransformer, ITextViewConnect
	{
		/// <summary>
		/// Gets the list of elements currently being transformed.
		/// </summary>
		protected IList<VisualLineElement> CurrentElements { get; private set; }
		
		/// <summary>
		/// <see cref="IVisualLineTransformer.Transform"/> implementation.
		/// Sets <see cref="CurrentElements"/> and calls <see cref="Colorize"/>.
		/// </summary>
		public void Transform(ITextRunConstructionContext context, IList<VisualLineElement> elements)
		{
			if (elements == null)
				throw new ArgumentNullException("elements");
			if (this.CurrentElements != null)
				throw new InvalidOperationException("Recursive Transform() call");
			this.CurrentElements = elements;
			try {
				Colorize(context);
			} finally {
				this.CurrentElements = null;
			}
		}
		
		/// <summary>
		/// Performs the colorization.
		/// </summary>
		protected abstract void Colorize(ITextRunConstructionContext context);
		
		/// <summary>
		/// Changes visual element properties.
		/// This method accesses <see cref="CurrentElements"/>, so it must be called only during
		/// a <see cref="Transform"/> call.
		/// This method splits <see cref="VisualLineElement"/>s as necessary to ensure that the region
		/// can be colored by setting the <see cref="VisualLineElement.TextRunProperties"/> of whole elements,
		/// and then calls the <paramref name="action"/> on all elements in the region.
		/// </summary>
		/// <param name="visualStartColumn">Start visual column of the region to change</param>
		/// <param name="visualEndColumn">End visual column of the region to change</param>
		/// <param name="action">Action that changes an individual <see cref="VisualLineElement"/>.</param>
		protected void ChangeVisualElements(int visualStartColumn, int visualEndColumn, Action<VisualLineElement> action)
		{
			if (action == null)
				throw new ArgumentNullException("action");
			for (int i = 0; i < CurrentElements.Count; i++) {
				VisualLineElement e = CurrentElements[i];
				if (e.VisualColumn > visualEndColumn)
					break;
				if (e.VisualColumn < visualStartColumn &&
				    e.VisualColumn + e.VisualLength > visualStartColumn)
				{
					if (e.CanSplit) {
						e.Split(visualStartColumn, CurrentElements, i--);
						continue;
					}
				}
				if (e.VisualColumn >= visualStartColumn && e.VisualColumn < visualEndColumn) {
					if (e.VisualColumn + e.VisualLength > visualEndColumn) {
						if (e.CanSplit) {
							e.Split(visualEndColumn, CurrentElements, i--);
							continue;
						}
					} else {
						action(e);
					}
				}
			}
		}
		
		/// <summary>
		/// Called when added to a text view.
		/// </summary>
		protected virtual void OnAddToTextView(TextView textView)
		{
		}
		
		/// <summary>
		/// Called when removed from a text view.
		/// </summary>
		protected virtual void OnRemoveFromTextView(TextView textView)
		{
		}
		
		void ITextViewConnect.AddToTextView(TextView textView)
		{
			OnAddToTextView(textView);
		}
		
		void ITextViewConnect.RemoveFromTextView(TextView textView)
		{
			OnRemoveFromTextView(textView);
		}
	}
}
