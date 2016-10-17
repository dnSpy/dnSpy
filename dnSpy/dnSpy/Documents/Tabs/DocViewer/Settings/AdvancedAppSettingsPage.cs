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
using System.ComponentModel;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;

namespace dnSpy.Documents.Tabs.DocViewer.Settings {
	sealed class AdvancedAppSettingsPage : AppSettingsPage, INotifyPropertyChanged {
		public override Guid ParentGuid => new Guid(AppSettingsConstants.GUID_DOCUMENT_VIEWER);
		public override Guid Guid => new Guid("6F00676C-2AB2-434E-9A44-D1D329D5CA4B");
		public override double Order => AppSettingsConstants.ORDER_DOCUMENT_VIEWER_DEFAULT_ADVANCED;
		public override string Title => dnSpy_Resources.AdvancedSettings;
		public override ImageReference Icon => ImageReference.None;
		public override object UIObject => this;

		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		public bool ReferenceHighlighting {
			get { return referenceHighlighting; }
			set {
				if (referenceHighlighting != value) {
					referenceHighlighting = value;
					OnPropertyChanged(nameof(ReferenceHighlighting));
				}
			}
		}
		bool referenceHighlighting;

		public bool HighlightRelatedKeywords {
			get { return highlightRelatedKeywords; }
			set {
				if (highlightRelatedKeywords != value) {
					highlightRelatedKeywords = value;
					OnPropertyChanged(nameof(HighlightRelatedKeywords));
				}
			}
		}
		bool highlightRelatedKeywords;

		public bool HighlightMatchingBrace {
			get { return highlightMatchingBrace; }
			set {
				if (highlightMatchingBrace != value) {
					highlightMatchingBrace = value;
					OnPropertyChanged(nameof(HighlightMatchingBrace));
				}
			}
		}
		bool highlightMatchingBrace;

		public bool LineSeparators {
			get { return lineSeparators; }
			set {
				if (lineSeparators != value) {
					lineSeparators = value;
					OnPropertyChanged(nameof(LineSeparators));
				}
			}
		}
		bool lineSeparators;

		public bool ShowStructureLines {
			get { return showStructureLines; }
			set {
				if (showStructureLines != value) {
					showStructureLines = value;
					OnPropertyChanged(nameof(ShowStructureLines));
				}
			}
		}
		bool showStructureLines;

		public bool CompressEmptyOrWhitespaceLines {
			get { return compressEmptyOrWhitespaceLines; }
			set {
				if (compressEmptyOrWhitespaceLines != value) {
					compressEmptyOrWhitespaceLines = value;
					OnPropertyChanged(nameof(CompressEmptyOrWhitespaceLines));
				}
			}
		}
		bool compressEmptyOrWhitespaceLines;

		public bool CompressNonLetterLines {
			get { return compressNonLetterLines; }
			set {
				if (compressNonLetterLines != value) {
					compressNonLetterLines = value;
					OnPropertyChanged(nameof(CompressNonLetterLines));
				}
			}
		}
		bool compressNonLetterLines;

		public bool MinimumLineSpacing {
			get { return minimumLineSpacing; }
			set {
				if (minimumLineSpacing != value) {
					minimumLineSpacing = value;
					OnPropertyChanged(nameof(MinimumLineSpacing));
				}
			}
		}
		bool minimumLineSpacing;

		public bool SelectionMargin {
			get { return selectionMargin; }
			set {
				if (selectionMargin != value) {
					selectionMargin = value;
					OnPropertyChanged(nameof(SelectionMargin));
				}
			}
		}
		bool selectionMargin;

		public bool GlyphMargin {
			get { return glyphMargin; }
			set {
				if (glyphMargin != value) {
					glyphMargin = value;
					OnPropertyChanged(nameof(GlyphMargin));
				}
			}
		}
		bool glyphMargin;

		public bool MouseWheelZoom {
			get { return mouseWheelZoom; }
			set {
				if (mouseWheelZoom != value) {
					mouseWheelZoom = value;
					OnPropertyChanged(nameof(MouseWheelZoom));
				}
			}
		}
		bool mouseWheelZoom;

		public bool ZoomControl {
			get { return zoomControl; }
			set {
				if (zoomControl != value) {
					zoomControl = value;
					OnPropertyChanged(nameof(ZoomControl));
				}
			}
		}
		bool zoomControl;

		readonly IDocumentViewerOptions options;

		public AdvancedAppSettingsPage(IDocumentViewerOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			this.options = options;
			ReferenceHighlighting = options.ReferenceHighlighting;
			HighlightRelatedKeywords = options.HighlightRelatedKeywords;
			HighlightMatchingBrace = options.BraceMatching;
			LineSeparators = options.LineSeparators;
			ShowStructureLines = options.ShowStructureLines;
			CompressEmptyOrWhitespaceLines = options.CompressEmptyOrWhitespaceLines;
			CompressNonLetterLines = options.CompressNonLetterLines;
			MinimumLineSpacing = options.RemoveExtraTextLineVerticalPixels;
			SelectionMargin = options.SelectionMargin;
			GlyphMargin = options.GlyphMargin;
			MouseWheelZoom = options.EnableMouseWheelZoom;
			ZoomControl = options.ZoomControl;
		}

		public override void OnApply() {
			options.ReferenceHighlighting = ReferenceHighlighting;
			options.HighlightRelatedKeywords = HighlightRelatedKeywords;
			options.BraceMatching = HighlightMatchingBrace;
			options.LineSeparators = LineSeparators;
			options.ShowStructureLines = ShowStructureLines;
			options.CompressEmptyOrWhitespaceLines = CompressEmptyOrWhitespaceLines;
			options.CompressNonLetterLines = CompressNonLetterLines;
			options.RemoveExtraTextLineVerticalPixels = MinimumLineSpacing;
			options.SelectionMargin = SelectionMargin;
			options.GlyphMargin = GlyphMargin;
			options.EnableMouseWheelZoom = MouseWheelZoom;
			options.ZoomControl = ZoomControl;
		}

		public override object GetDataTemplateObject() => UIObject;
	}
}
