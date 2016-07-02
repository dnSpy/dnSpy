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

using System.Windows;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class AdornmentLayerElement : IAdornmentLayerElement {
		public UIElement Adornment { get; }
		public AdornmentPositioningBehavior Behavior { get; }
		public AdornmentRemovedCallback RemovedCallback { get; }
		public object Tag { get; }
		public SnapshotSpan? VisualSpan { get; private set; }

		public AdornmentLayerElement(AdornmentPositioningBehavior behavior, SnapshotSpan? visualSpan, object tag, UIElement adornment, AdornmentRemovedCallback removedCallback) {
			Adornment = adornment;
			Behavior = behavior;
			RemovedCallback = removedCallback;
			Tag = tag;
			VisualSpan = visualSpan;
		}

		public void OnLayoutChanged(ITextSnapshot textSnapshot) {
			if (VisualSpan == null)
				return;
			VisualSpan = VisualSpan.Value.TranslateTo(textSnapshot, SpanTrackingMode.EdgeInclusive);
		}
	}
}
