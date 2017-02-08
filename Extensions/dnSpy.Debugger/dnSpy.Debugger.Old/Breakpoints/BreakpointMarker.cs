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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.Breakpoints {
	//[Export(typeof(IBreakpointListener))]
	sealed class BreakpointMarker : IBreakpointListener {
		readonly IGlyphTextMarkerService glyphTextMarkerService;
		readonly IClassificationType classificationTypeEnabledBreakpoint;
		readonly Dictionary<ILCodeBreakpoint, IGlyphTextMethodMarker> toMethodMarkers;
		readonly ILCodeBreakpointGlyphTextMarkerHandler ilCodeBreakpointGlyphTextMarkerHandler;

		[ImportingConstructor]
		BreakpointMarker(IBreakpointService breakpointService, IGlyphTextMarkerService glyphTextMarkerService, IClassificationTypeRegistryService classificationTypeRegistryService, ILCodeBreakpointGlyphTextMarkerHandler ilCodeBreakpointGlyphTextMarkerHandler) {
			this.glyphTextMarkerService = glyphTextMarkerService;
			classificationTypeEnabledBreakpoint = classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.BreakpointStatement);
			toMethodMarkers = new Dictionary<ILCodeBreakpoint, IGlyphTextMethodMarker>();
			this.ilCodeBreakpointGlyphTextMarkerHandler = ilCodeBreakpointGlyphTextMarkerHandler;
			breakpointService.BreakpointsAdded += BreakpointService_BreakpointsAdded;
			breakpointService.BreakpointsRemoved += BreakpointService_BreakpointsRemoved;
		}

		void BreakpointService_BreakpointsAdded(object sender, BreakpointsAddedEventArgs e) {
			foreach (var bp in e.Breakpoints) {
				var ilbp = bp as ILCodeBreakpoint;
				if (ilbp != null)
					BreakpointAdded(ilbp);
			}
		}

		void BreakpointService_BreakpointsRemoved(object sender, BreakpointsRemovedEventArgs e) {
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
			var marker = glyphTextMarkerService.AddMarker(ilbp.MethodToken, ilbp.ILOffset, info.ImageReference, info.MarkerTypeName, info.SelectedMarkerTypeName, info.ClassificationType, info.ZIndex, GlyphTextMarkerHelper.GetTag(ilbp), ilCodeBreakpointGlyphTextMarkerHandler, textViewFilter);
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
			public string SelectedMarkerTypeName { get; }
			public IClassificationType ClassificationType { get; }
			public int ZIndex { get; }
			public BreakpointMarkerInfo(ImageReference? imageReference, string markerTypeName, string selectedMarkerTypeName, IClassificationType classificationType, int zIndex) {
				ImageReference = imageReference;
				MarkerTypeName = markerTypeName;
				SelectedMarkerTypeName = selectedMarkerTypeName;
				ClassificationType = classificationType;
				ZIndex = zIndex;
			}
		}

		BreakpointMarkerInfo GetBreakpointMarkerInfo(ILCodeBreakpoint ilbp) {
			ImageReference imgRef;
			string markerTypeName, selectedMarkerTypeName;
			IClassificationType classificationType;
			int zIndex;
			if (ilbp.IsEnabled) {
				imgRef = DsImages.BreakpointEnabled;
				markerTypeName = ThemeClassificationTypeNameKeys.BreakpointStatementMarker;
				selectedMarkerTypeName = ThemeClassificationTypeNameKeys.SelectedBreakpointStatementMarker;
				classificationType = classificationTypeEnabledBreakpoint;
				zIndex = GlyphTextMarkerServiceZIndexes.EnabledBreakpoint;
			}
			else {
				imgRef = DsImages.BreakpointDisabled;
				markerTypeName = ThemeClassificationTypeNameKeys.DisabledBreakpointStatementMarker;
				selectedMarkerTypeName = null;
				classificationType = null;
				zIndex = GlyphTextMarkerServiceZIndexes.DisabledBreakpoint;
			}
			return new BreakpointMarkerInfo(imgRef, markerTypeName, selectedMarkerTypeName, classificationType, zIndex);
		}
	}
}
