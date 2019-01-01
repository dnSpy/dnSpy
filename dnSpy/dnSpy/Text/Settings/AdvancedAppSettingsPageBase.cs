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

using System;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Properties;

namespace dnSpy.Text.Settings {
	abstract class AdvancedAppSettingsPageBase : AppSettingsPage {
		public sealed override string Title => dnSpy_Resources.AdvancedSettings;
		public sealed override object UIObject => this;

		public bool ReferenceHighlighting {
			get => referenceHighlighting;
			set {
				if (referenceHighlighting != value) {
					referenceHighlighting = value;
					OnPropertyChanged(nameof(ReferenceHighlighting));
				}
			}
		}
		bool referenceHighlighting;

		public bool HighlightRelatedKeywords {
			get => highlightRelatedKeywords;
			set {
				if (highlightRelatedKeywords != value) {
					highlightRelatedKeywords = value;
					OnPropertyChanged(nameof(HighlightRelatedKeywords));
				}
			}
		}
		bool highlightRelatedKeywords;

		public bool HighlightMatchingBrace {
			get => highlightMatchingBrace;
			set {
				if (highlightMatchingBrace != value) {
					highlightMatchingBrace = value;
					OnPropertyChanged(nameof(HighlightMatchingBrace));
				}
			}
		}
		bool highlightMatchingBrace;

		public bool LineSeparators {
			get => lineSeparators;
			set {
				if (lineSeparators != value) {
					lineSeparators = value;
					OnPropertyChanged(nameof(LineSeparators));
				}
			}
		}
		bool lineSeparators;

		public bool ShowBlockStructure {
			get => showBlockStructure;
			set {
				if (showBlockStructure != value) {
					showBlockStructure = value;
					OnPropertyChanged(nameof(ShowBlockStructure));
				}
			}
		}
		bool showBlockStructure;

		public EnumListVM BlockStructureLineKindVM { get; }
		public BlockStructureLineKind BlockStructureLineKind {
			get => (BlockStructureLineKind)BlockStructureLineKindVM.SelectedItem;
			set => BlockStructureLineKindVM.SelectedItem = value;
		}
		static readonly EnumVM[] blockStructureLineKindList = new EnumVM[5] {
			new EnumVM(BlockStructureLineKind.Solid, dnSpy_Resources.BlockStructureLineKind_SolidLines),
			new EnumVM(BlockStructureLineKind.Dashed_1_1, GetDashedText(1)),
			new EnumVM(BlockStructureLineKind.Dashed_2_2, GetDashedText(2)),
			new EnumVM(BlockStructureLineKind.Dashed_3_3, GetDashedText(3)),
			new EnumVM(BlockStructureLineKind.Dashed_4_4, GetDashedText(4)),
		};
		static string GetDashedText(int px) => dnSpy_Resources.BlockStructureLineKind_DashedLines + " (" + px.ToString() + "px)";

		public bool CompressEmptyOrWhitespaceLines {
			get => compressEmptyOrWhitespaceLines;
			set {
				if (compressEmptyOrWhitespaceLines != value) {
					compressEmptyOrWhitespaceLines = value;
					OnPropertyChanged(nameof(CompressEmptyOrWhitespaceLines));
				}
			}
		}
		bool compressEmptyOrWhitespaceLines;

		public bool CompressNonLetterLines {
			get => compressNonLetterLines;
			set {
				if (compressNonLetterLines != value) {
					compressNonLetterLines = value;
					OnPropertyChanged(nameof(CompressNonLetterLines));
				}
			}
		}
		bool compressNonLetterLines;

		public bool MinimumLineSpacing {
			get => minimumLineSpacing;
			set {
				if (minimumLineSpacing != value) {
					minimumLineSpacing = value;
					OnPropertyChanged(nameof(MinimumLineSpacing));
				}
			}
		}
		bool minimumLineSpacing;

		public bool SelectionMargin {
			get => selectionMargin;
			set {
				if (selectionMargin != value) {
					selectionMargin = value;
					OnPropertyChanged(nameof(SelectionMargin));
				}
			}
		}
		bool selectionMargin;

		public bool GlyphMargin {
			get => glyphMargin;
			set {
				if (glyphMargin != value) {
					glyphMargin = value;
					OnPropertyChanged(nameof(GlyphMargin));
				}
			}
		}
		bool glyphMargin;

		public bool MouseWheelZoom {
			get => mouseWheelZoom;
			set {
				if (mouseWheelZoom != value) {
					mouseWheelZoom = value;
					OnPropertyChanged(nameof(MouseWheelZoom));
				}
			}
		}
		bool mouseWheelZoom;

		public bool ZoomControl {
			get => zoomControl;
			set {
				if (zoomControl != value) {
					zoomControl = value;
					OnPropertyChanged(nameof(ZoomControl));
				}
			}
		}
		bool zoomControl;

		public bool ForceClearTypeIfNeeded {
			get => forceClearTypeIfNeeded;
			set {
				if (forceClearTypeIfNeeded != value) {
					forceClearTypeIfNeeded = value;
					OnPropertyChanged(nameof(ForceClearTypeIfNeeded));
				}
			}
		}
		bool forceClearTypeIfNeeded;

		readonly ICommonEditorOptions options;

		protected AdvancedAppSettingsPageBase(ICommonEditorOptions options) {
			this.options = options ?? throw new ArgumentNullException(nameof(options));
			BlockStructureLineKindVM = new EnumListVM(blockStructureLineKindList);
			ReferenceHighlighting = options.ReferenceHighlighting;
			HighlightRelatedKeywords = options.HighlightRelatedKeywords;
			HighlightMatchingBrace = options.BraceMatching;
			LineSeparators = options.LineSeparators;
			ShowBlockStructure = options.ShowBlockStructure;
			BlockStructureLineKind = options.BlockStructureLineKind;
			CompressEmptyOrWhitespaceLines = options.CompressEmptyOrWhitespaceLines;
			CompressNonLetterLines = options.CompressNonLetterLines;
			MinimumLineSpacing = options.RemoveExtraTextLineVerticalPixels;
			SelectionMargin = options.SelectionMargin;
			GlyphMargin = options.GlyphMargin;
			MouseWheelZoom = options.EnableMouseWheelZoom;
			ZoomControl = options.ZoomControl;
			ForceClearTypeIfNeeded = options.ForceClearTypeIfNeeded;
		}

		public override void OnApply() {
			options.ReferenceHighlighting = ReferenceHighlighting;
			options.HighlightRelatedKeywords = HighlightRelatedKeywords;
			options.BraceMatching = HighlightMatchingBrace;
			options.LineSeparators = LineSeparators;
			options.ShowBlockStructure = ShowBlockStructure;
			options.BlockStructureLineKind = BlockStructureLineKind;
			options.CompressEmptyOrWhitespaceLines = CompressEmptyOrWhitespaceLines;
			options.CompressNonLetterLines = CompressNonLetterLines;
			options.RemoveExtraTextLineVerticalPixels = MinimumLineSpacing;
			options.SelectionMargin = SelectionMargin;
			options.GlyphMargin = GlyphMargin;
			options.EnableMouseWheelZoom = MouseWheelZoom;
			options.ZoomControl = ZoomControl;
			options.ForceClearTypeIfNeeded = ForceClearTypeIfNeeded;
		}
	}
}
