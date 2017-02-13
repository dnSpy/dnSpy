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
using System.ComponentModel;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;

namespace dnSpy.Hex.Settings {
	abstract class AdvancedAppSettingsPageBase : AppSettingsPage, INotifyPropertyChanged {
		public sealed override string Title => dnSpy_Resources.AdvancedSettings;
		public sealed override object UIObject => this;

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		public bool ShowColumnLines {
			get { return showColumnLines; }
			set {
				if (showColumnLines != value) {
					showColumnLines = value;
					OnPropertyChanged(nameof(ShowColumnLines));
				}
			}
		}
		bool showColumnLines;

		public bool RemoveExtraTextLineVerticalPixels {
			get { return removeExtraTextLineVerticalPixels; }
			set {
				if (removeExtraTextLineVerticalPixels != value) {
					removeExtraTextLineVerticalPixels = value;
					OnPropertyChanged(nameof(RemoveExtraTextLineVerticalPixels));
				}
			}
		}
		bool removeExtraTextLineVerticalPixels;

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

		public bool EnableMouseWheelZoom {
			get { return enableMouseWheelZoom; }
			set {
				if (enableMouseWheelZoom != value) {
					enableMouseWheelZoom = value;
					OnPropertyChanged(nameof(EnableMouseWheelZoom));
				}
			}
		}
		bool enableMouseWheelZoom;

		public EnumListVM ColumnLine0VM { get; }
		public HexColumnLineKind ColumnLine0 {
			get { return (HexColumnLineKind)ColumnLine0VM.SelectedItem; }
			set { ColumnLine0VM.SelectedItem = value; }
		}
		public EnumListVM ColumnLine1VM { get; }
		public HexColumnLineKind ColumnLine1 {
			get { return (HexColumnLineKind)ColumnLine1VM.SelectedItem; }
			set { ColumnLine1VM.SelectedItem = value; }
		}
		public EnumListVM ColumnGroupLine0VM { get; }
		public HexColumnLineKind ColumnGroupLine0 {
			get { return (HexColumnLineKind)ColumnGroupLine0VM.SelectedItem; }
			set { ColumnGroupLine0VM.SelectedItem = value; }
		}
		public EnumListVM ColumnGroupLine1VM { get; }
		public HexColumnLineKind ColumnGroupLine1 {
			get { return (HexColumnLineKind)ColumnGroupLine1VM.SelectedItem; }
			set { ColumnGroupLine1VM.SelectedItem = value; }
		}
		static readonly EnumVM[] hexColumnLineKindList = new EnumVM[6] {
			new EnumVM(HexColumnLineKind.None, dnSpy_Resources.BlockStructureLineKind_None),
			new EnumVM(HexColumnLineKind.Solid, dnSpy_Resources.BlockStructureLineKind_SolidLines),
			new EnumVM(HexColumnLineKind.Dashed_1_1, GetDashedText(1)),
			new EnumVM(HexColumnLineKind.Dashed_2_2, GetDashedText(2)),
			new EnumVM(HexColumnLineKind.Dashed_3_3, GetDashedText(3)),
			new EnumVM(HexColumnLineKind.Dashed_4_4, GetDashedText(4)),
		};
		static string GetDashedText(int px) => dnSpy_Resources.BlockStructureLineKind_DashedLines + " (" + px.ToString() + "px)";

		readonly CommonEditorOptions options;

		protected AdvancedAppSettingsPageBase(CommonEditorOptions options) {
			this.options = options ?? throw new ArgumentNullException(nameof(options));
			ColumnLine0VM = new EnumListVM(hexColumnLineKindList);
			ColumnLine1VM = new EnumListVM(hexColumnLineKindList);
			ColumnGroupLine0VM = new EnumListVM(hexColumnLineKindList);
			ColumnGroupLine1VM = new EnumListVM(hexColumnLineKindList);

			ShowColumnLines = options.ShowColumnLines;
			RemoveExtraTextLineVerticalPixels = options.RemoveExtraTextLineVerticalPixels;
			SelectionMargin = options.SelectionMargin;
			GlyphMargin = options.GlyphMargin;
			ZoomControl = options.ZoomControl;
			EnableMouseWheelZoom = options.EnableMouseWheelZoom;
			ColumnLine0 = options.ColumnLine0;
			ColumnLine1 = options.ColumnLine1;
			ColumnGroupLine0 = options.ColumnGroupLine0;
			ColumnGroupLine1 = options.ColumnGroupLine1;
		}

		public override void OnApply() {
			options.ShowColumnLines = ShowColumnLines;
			options.RemoveExtraTextLineVerticalPixels = RemoveExtraTextLineVerticalPixels;
			options.SelectionMargin = SelectionMargin;
			options.GlyphMargin = GlyphMargin;
			options.ZoomControl = ZoomControl;
			options.EnableMouseWheelZoom = EnableMouseWheelZoom;
			options.ColumnLine0 = ColumnLine0;
			options.ColumnLine1 = ColumnLine1;
			options.ColumnGroupLine0 = ColumnGroupLine0;
			options.ColumnGroupLine1 = ColumnGroupLine1;
		}
	}
}
