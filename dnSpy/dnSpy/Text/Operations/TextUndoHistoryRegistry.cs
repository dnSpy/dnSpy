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
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Operations {
	[Export(typeof(ITextUndoHistoryRegistry))]
	sealed class TextUndoHistoryRegistry : ITextUndoHistoryRegistry {
		static readonly object textUndoHistoryKey = typeof(ITextUndoHistory);

		public void AttachHistory(object context, ITextUndoHistory history) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (history == null)
				throw new ArgumentNullException(nameof(history));
			var propertyOwner = context as IPropertyOwner;
			if (propertyOwner == null)
				throw new ArgumentException();
			if (!(history is TextUndoHistory))
				throw new ArgumentException();
			// VS also doesn't support it
			throw new NotSupportedException();
		}

		public ITextUndoHistory GetHistory(object context) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			var propertyOwner = context as IPropertyOwner;
			if (propertyOwner == null)
				throw new ArgumentException();
			if (propertyOwner.Properties.TryGetProperty(textUndoHistoryKey, out TextUndoHistory history))
				return history;
			throw new ArgumentException();
		}

		public ITextUndoHistory RegisterHistory(object context) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			var propertyOwner = context as IPropertyOwner;
			if (propertyOwner == null)
				throw new ArgumentException();
			return propertyOwner.Properties.GetOrCreateSingletonProperty(textUndoHistoryKey, () => new TextUndoHistory(propertyOwner));
		}

		public void RemoveHistory(ITextUndoHistory history) {
			if (history == null)
				throw new ArgumentException();
			var historyImpl = history as TextUndoHistory;
			if (historyImpl == null)
				throw new ArgumentException();
			historyImpl.PropertyOwner.Properties.RemoveProperty(textUndoHistoryKey);
			historyImpl.Dispose();
		}

		public bool TryGetHistory(object context, out ITextUndoHistory history) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			var propertyOwner = context as IPropertyOwner;
			if (propertyOwner == null)
				throw new ArgumentException();
			return propertyOwner.Properties.TryGetProperty(textUndoHistoryKey, out history);
		}
	}
}
