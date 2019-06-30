/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Hex.Editor {
	sealed class HexAdornmentLayerElementImpl : HexAdornmentLayerElement {
		public override UIElement Adornment { get; }
		public override VSTE.AdornmentPositioningBehavior Behavior { get; }
		public override VSTE.AdornmentRemovedCallback? RemovedCallback { get; }
		public override object? Tag { get; }
		public override HexBufferSpan? VisualSpan { get; }

		public HexAdornmentLayerElementImpl(VSTE.AdornmentPositioningBehavior behavior, HexBufferSpan? visualSpan, object? tag, UIElement adornment, VSTE.AdornmentRemovedCallback? removedCallback) {
			Adornment = adornment;
			Behavior = behavior;
			RemovedCallback = removedCallback;
			Tag = tag;
			VisualSpan = visualSpan;
		}
	}
}
