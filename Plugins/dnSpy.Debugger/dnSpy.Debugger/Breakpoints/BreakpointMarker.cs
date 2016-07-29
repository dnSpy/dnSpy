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
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.Breakpoints {
	[Export(typeof(IBreakpointListener))]
	sealed class BreakpointMarker : IBreakpointListener {
		readonly IGlyphTextMarkerService glyphTextMarkerService;
		readonly IClassificationType classificationTypeEnabledBreakpoint;
		readonly Dictionary<ILCodeBreakpoint, IGlyphTextMethodMarker> toMethodMarkers;

		[ImportingConstructor]
		BreakpointMarker(IBreakpointManager breakpointManager, IGlyphTextMarkerService glyphTextMarkerService, IClassificationTypeRegistryService classificationTypeRegistryService) {
			this.glyphTextMarkerService = glyphTextMarkerService;
			this.classificationTypeEnabledBreakpoint = classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.BreakpointStatement);
			this.toMethodMarkers = new Dictionary<ILCodeBreakpoint, IGlyphTextMethodMarker>();
			breakpointManager.BreakpointsAdded += BreakpointManager_BreakpointsAdded;
			breakpointManager.BreakpointsRemoved += BreakpointManager_BreakpointsRemoved;
		}

		void BreakpointManager_BreakpointsAdded(object sender, BreakpointsAddedEventArgs e) {
			foreach (var bp in e.Breakpoints) {
				var ilbp = bp as ILCodeBreakpoint;
				if (ilbp != null)
					BreakpointAdded(ilbp);
			}
		}

		void BreakpointManager_BreakpointsRemoved(object sender, BreakpointsRemovedEventArgs e) {
			foreach (var bp in e.Breakpoints) {
				var ilbp = bp as ILCodeBreakpoint;
				if (ilbp != null)
					BreakpointRemoved(ilbp);
			}
		}

		void BreakpointAdded(ILCodeBreakpoint ilbp) {
			Debug.Assert(!toMethodMarkers.ContainsKey(ilbp));
			if (toMethodMarkers.ContainsKey(ilbp))
				return;
			ilbp.PropertyChanged += ILCodeBreakpoint_PropertyChanged;
			UpdateMarker(ilbp);
		}

		void ILCodeBreakpoint_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var ilbp = (ILCodeBreakpoint)sender;
			if (e.PropertyName == nameof(ilbp.IsEnabled))
				UpdateMarker(ilbp);
		}

		void RemoveMarker(ILCodeBreakpoint ilbp) {
			IGlyphTextMethodMarker marker;
			if (toMethodMarkers.TryGetValue(ilbp, out marker)) {
				glyphTextMarkerService.Remove(marker);
				toMethodMarkers.Remove(ilbp);
			}
		}

		void UpdateMarker(ILCodeBreakpoint ilbp) {
			RemoveMarker(ilbp);
			var info = GetBreakpointMarkerInfo(ilbp);
			var marker = glyphTextMarkerService.AddMarker(ilbp.MethodToken, ilbp.ILOffset, info.ImageReference, info.MarkerTypeName, info.ClassificationType, info.ZIndex, textViewFilter);
			toMethodMarkers.Add(ilbp, marker);
		}
		static readonly Func<ITextView, bool> textViewFilter = textView => textView.Roles.Contains(PredefinedTextViewRoles.Debuggable);

		void BreakpointRemoved(ILCodeBreakpoint ilbp) {
			Debug.Assert(toMethodMarkers.ContainsKey(ilbp));
			if (!toMethodMarkers.ContainsKey(ilbp))
				return;
			RemoveMarker(ilbp);
			ilbp.PropertyChanged -= ILCodeBreakpoint_PropertyChanged;
		}

		struct BreakpointMarkerInfo {
			public ImageReference? ImageReference { get; }
			public string MarkerTypeName { get; }
			public IClassificationType ClassificationType { get; }
			public int ZIndex { get; }
			public BreakpointMarkerInfo(ImageReference? imageReference, string markerTypeName, IClassificationType classificationType, int zIndex) {
				ImageReference = imageReference;
				MarkerTypeName = markerTypeName;
				ClassificationType = classificationType;
				ZIndex = zIndex;
			}
		}

		BreakpointMarkerInfo GetBreakpointMarkerInfo(ILCodeBreakpoint ilbp) {
			ImageReference imgRef;
			string markerTypeName;
			IClassificationType classificationType;
			if (ilbp.IsEnabled) {
				imgRef = new ImageReference(GetType().Assembly, "Breakpoint");
				markerTypeName = ThemeClassificationTypeNameKeys.BreakpointStatementMarker;
				classificationType = classificationTypeEnabledBreakpoint;
			}
			else {
				imgRef = new ImageReference(GetType().Assembly, "DisabledBreakpoint");
				markerTypeName = ThemeClassificationTypeNameKeys.DisabledBreakpointStatementMarker;
				classificationType = null;
			}
			return new BreakpointMarkerInfo(imgRef, markerTypeName, classificationType, GlyphTextMarkerServiceZIndexes.Breakpoint);
		}
	}
}
