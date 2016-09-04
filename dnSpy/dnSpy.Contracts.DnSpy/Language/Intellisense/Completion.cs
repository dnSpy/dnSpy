/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Threading;
using dnSpy.Contracts.Images;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// A completion item
	/// </summary>
	public class Completion : IPropertyOwner {
		/// <summary>
		/// Gets the properties
		/// </summary>
		public PropertyCollection Properties {
			get {
				if (properties == null)
					Interlocked.CompareExchange(ref properties, new PropertyCollection(), null);
				return properties;
			}
		}
		PropertyCollection properties;

		/// <summary>
		/// Gets the text shown in the UI
		/// </summary>
		public string DisplayText { get; }

		/// <summary>
		/// Gets the text that is used to filter this item
		/// </summary>
		public string FilterText { get; }

		/// <summary>
		/// Gets the text that gets inserted in the text buffer or null if none
		/// </summary>
		protected string InsertionText { get; }

		/// <summary>
		/// Gets the image or the default value if there's no image
		/// </summary>
		public ImageReference Image => image ?? (image = GetImageReference()).Value;
		ImageReference? image;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text">Text shown in the UI. It's also written to <see cref="FilterText"/> and <see cref="InsertionText"/>.</param>
		/// <param name="image">Image</param>
		public Completion(string text, ImageReference? image)
			: this(text, text, text, image) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="displayText">Text shown in the UI</param>
		/// <param name="filterText">Text used to filter out items or null to use <paramref name="displayText"/></param>
		/// <param name="insertionText">Text that gets inserted in the text buffer or null to use <paramref name="displayText"/></param>
		/// <param name="image">Image</param>
		public Completion(string displayText, string filterText, string insertionText, ImageReference? image) {
			if (displayText == null)
				throw new ArgumentNullException(nameof(displayText));
			DisplayText = displayText;
			FilterText = filterText ?? displayText;
			InsertionText = insertionText ?? displayText;
			this.image = image;
		}

		/// <summary>
		/// Gets the image reference. Only called if <see cref="Image"/> hasn't been initialized.
		/// </summary>
		/// <returns></returns>
		protected virtual ImageReference GetImageReference() => default(ImageReference);

		/// <summary>
		/// Adds the new text to the text buffer
		/// </summary>
		/// <param name="replaceSpan">Span to replace with new content</param>
		public virtual void Commit(ITrackingSpan replaceSpan) {
			var buffer = replaceSpan.TextBuffer;
			var span = replaceSpan.GetSpan(buffer.CurrentSnapshot);
			buffer.Replace(span.Span, InsertionText);
		}
	}
}
