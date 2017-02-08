/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Collections.Generic;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// A completion item
	/// </summary>
	public class DsCompletion : Completion4, IDsCompletion {
		/// <summary>
		/// Gets the text that is used to filter this item
		/// </summary>
		public string FilterText { get; protected set; }

		/// <summary>
		/// Gets the icon
		/// </summary>
		public override ImageMoniker IconMoniker => iconMoniker ?? (iconMoniker = GetIconMoniker()).Value;
		ImageMoniker? iconMoniker;

		/// <summary>
		/// Constructor
		/// </summary>
		public DsCompletion() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="displayText">Text shown in the UI</param>
		/// <param name="filterText">Text used to filter out items or null to use <paramref name="displayText"/></param>
		/// <param name="insertionText">Text that gets inserted in the text buffer or null to use <paramref name="displayText"/></param>
		/// <param name="description">Description or null</param>
		/// <param name="iconMoniker">Icon moniker or null</param>
		/// <param name="iconAutomationText">Icon automation text or null</param>
		/// <param name="attributeIcons">Attribute icons shown on the right side</param>
		/// <param name="suffix">Text shown after the normal completion text</param>
		public DsCompletion(string displayText, string filterText = null, string insertionText = null, string description = null, ImageMoniker iconMoniker = default(ImageMoniker), string iconAutomationText = null, IEnumerable<CompletionIcon2> attributeIcons = null, string suffix = null)
			: base(displayText, insertionText, description, default(ImageMoniker), iconAutomationText, attributeIcons, suffix) {
			if (displayText == null)
				throw new ArgumentNullException(nameof(displayText));
			FilterText = filterText ?? displayText;
			InsertionText = insertionText ?? displayText;
			this.iconMoniker = iconMoniker.Id == 0 && iconMoniker.Guid == Guid.Empty ? (ImageMoniker?)null : iconMoniker;
		}

		/// <summary>
		/// Gets the image reference. Only called if <see cref="IconMoniker"/> hasn't been initialized.
		/// </summary>
		/// <returns></returns>
		protected virtual ImageMoniker GetIconMoniker() => default(ImageMoniker);

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
