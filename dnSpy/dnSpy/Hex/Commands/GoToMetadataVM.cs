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
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Hex.Files.PE;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;
using dnSpy.Properties;

namespace dnSpy.Hex.Commands {
	sealed class GoToMetadataVM : ViewModelBase {
		public ICommand GoToMetadataBlobCommand => new RelayCommand(a => GoToMetadataKind = GoToMetadataKind.Blob);
		public ICommand GoToMetadatatStringsCommand => new RelayCommand(a => GoToMetadataKind = GoToMetadataKind.Strings);
		public ICommand GoToMetadataUSCommand => new RelayCommand(a => GoToMetadataKind = GoToMetadataKind.US);
		public ICommand GoToMetadataGUIDCommand => new RelayCommand(a => GoToMetadataKind = GoToMetadataKind.GUID);
		public ICommand GoToMetadataTableCommand => new RelayCommand(a => GoToMetadataKind = GoToMetadataKind.Table);
		public ICommand GoToMetadataMemberRvaCommand => new RelayCommand(a => GoToMetadataKind = GoToMetadataKind.MemberRva, a => peHeaders is not null);

		public bool IsOffset {
			get {
				switch (SelectedItem.Kind) {
				case GoToMetadataKind.Blob:
				case GoToMetadataKind.Strings:
				case GoToMetadataKind.US:
					return true;
				case GoToMetadataKind.GUID:
				case GoToMetadataKind.Table:
				case GoToMetadataKind.MemberRva:
					return false;
				default:
					throw new InvalidOperationException();
				}
			}
		}

		public bool IsToken {
			get {
				switch (SelectedItem.Kind) {
				case GoToMetadataKind.Blob:
				case GoToMetadataKind.Strings:
				case GoToMetadataKind.US:
				case GoToMetadataKind.GUID:
					return false;
				case GoToMetadataKind.Table:
				case GoToMetadataKind.MemberRva:
					return true;
				default:
					throw new InvalidOperationException();
				}
			}
		}

		public bool IsIndex {
			get {
				switch (SelectedItem.Kind) {
				case GoToMetadataKind.Blob:
				case GoToMetadataKind.Strings:
				case GoToMetadataKind.US:
				case GoToMetadataKind.Table:
				case GoToMetadataKind.MemberRva:
					return false;
				case GoToMetadataKind.GUID:
					return true;
				default:
					throw new InvalidOperationException();
				}
			}
		}

		public ObservableCollection<GoToMetadataKindVM> GoToMetadataCollection { get; }

		public GoToMetadataKindVM SelectedItem {
			get => selectedItem;
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged(nameof(SelectedItem));
					OnPropertyChanged(nameof(IsOffset));
					OnPropertyChanged(nameof(IsToken));
					OnPropertyChanged(nameof(IsIndex));
					offsetTokenVM.GoToMetadataKind = GoToMetadataKind;
					HasErrorUpdated();
				}
			}
		}
		GoToMetadataKindVM selectedItem;

		public GoToMetadataKind GoToMetadataKind {
			get => SelectedItem.Kind;
			set => SelectedItem = GoToMetadataCollection.First(a => a.Kind == value);
		}

		public object OffsetToken => offsetTokenVM;
		OffsetTokenVM offsetTokenVM;

		public uint OffsetTokenValue => offsetTokenVM.Value;

		readonly PeHeaders? peHeaders;

		public GoToMetadataVM(HexBuffer buffer, DotNetMetadataHeaders mdHeaders, PeHeaders? peHeaders, uint value) {
			if (buffer is null)
				throw new ArgumentNullException(nameof(buffer));
			if (mdHeaders is null)
				throw new ArgumentNullException(nameof(mdHeaders));
			this.peHeaders = peHeaders;
			offsetTokenVM = new OffsetTokenVM(buffer, mdHeaders, peHeaders, value, a => HasErrorUpdated());
			GoToMetadataCollection = new ObservableCollection<GoToMetadataKindVM>();
			GoToMetadataCollection.Add(new GoToMetadataKindVM(GoToMetadataKind.Table, dnSpy_Resources.GoToMetadataToken, dnSpy_Resources.ShortCutKeyCtrl1));
			if (peHeaders is not null)
				GoToMetadataCollection.Add(new GoToMetadataKindVM(GoToMetadataKind.MemberRva, dnSpy_Resources.GoToMetadataMethodBody, dnSpy_Resources.ShortCutKeyCtrl2));
			GoToMetadataCollection.Add(new GoToMetadataKindVM(GoToMetadataKind.Blob, "#Blob", dnSpy_Resources.ShortCutKeyCtrl3));
			GoToMetadataCollection.Add(new GoToMetadataKindVM(GoToMetadataKind.Strings, "#Strings", dnSpy_Resources.ShortCutKeyCtrl4));
			GoToMetadataCollection.Add(new GoToMetadataKindVM(GoToMetadataKind.US, "#US", dnSpy_Resources.ShortCutKeyCtrl5));
			GoToMetadataCollection.Add(new GoToMetadataKindVM(GoToMetadataKind.GUID, "#GUID", dnSpy_Resources.ShortCutKeyCtrl6));
			selectedItem = GoToMetadataCollection[0];
			offsetTokenVM.GoToMetadataKind = GoToMetadataKind;
		}

		sealed class OffsetTokenVM : NumberDataFieldVM<uint, uint> {
			readonly HexBuffer buffer;
			readonly DotNetMetadataHeaders mdHeaders;
			readonly PeHeaders? peHeaders;

			public OffsetTokenVM(HexBuffer buffer, DotNetMetadataHeaders mdHeaders, PeHeaders? peHeaders, uint value, Action<DataFieldVM> onUpdated)
				: base(onUpdated, uint.MinValue, uint.MaxValue, null) {
				SetValueFromConstructor(value);
				this.buffer = buffer;
				this.mdHeaders = mdHeaders;
				this.peHeaders = peHeaders;
			}

			public GoToMetadataKind GoToMetadataKind {
				get => goToMetadataKind;
				set {
					if (goToMetadataKind != value) {
						goToMetadataKind = value;
						ForceWriteStringValue(StringValue);
					}
				}
			}
			GoToMetadataKind goToMetadataKind;

			protected override string OnNewValue(uint value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

			protected override string? ConvertToValue(out uint value) {
				value = SimpleTypeConverter.ParseUInt32(StringValue, Min, Max, out var error);
				if (error is not null)
					return error;
				return CheckOffsetToken(value) ? null : dnSpy_Resources.GoToMetadataInvalidOffsetOrToken;
			}

			bool CheckOffsetToken(uint value) {
				switch (GoToMetadataKind) {
				case GoToMetadataKind.Blob:
					if (mdHeaders.BlobStream is null)
						return false;
					return value < mdHeaders.BlobStream.Span.Span.Length;

				case GoToMetadataKind.Strings:
					if (mdHeaders.StringsStream is null)
						return false;
					return value < mdHeaders.StringsStream.Span.Span.Length;

				case GoToMetadataKind.US:
					if ((value >> 24) == 0x70)
						value &= 0x00FFFFFF;
					if (mdHeaders.USStream is null)
						return false;
					return value < mdHeaders.USStream.Span.Span.Length;

				case GoToMetadataKind.GUID:
					return value != 0 && mdHeaders.GUIDStream?.IsValidIndex(value) == true;

				case GoToMetadataKind.Table:
					return GetMDTable(value) is not null;

				case GoToMetadataKind.MemberRva:
					if (peHeaders is null)
						return false;
					var mdTable = GetMDTable(value);
					if (mdTable is null)
						return false;
					if (mdTable.Table != Table.Method && mdTable.Table != Table.FieldRVA)
						return false;
					// Column 0 is the RVA in both Method and FieldRVA tables
					var pos = mdTable.Span.Start + ((value & 0x00FFFFFF) - 1) * mdTable.RowSize;
					return buffer.ReadUInt32(pos) != 0;

				default:
					throw new InvalidOperationException();
				}
			}

			MDTable? GetMDTable(uint token) {
				var tablesStream = mdHeaders.TablesStream;
				if (tablesStream is null)
					return null;
				var table = token >> 24;
				if (table >= (uint)tablesStream.MDTables.Count)
					return null;
				var mdTable = tablesStream.MDTables[(int)table];
				return mdTable?.IsValidRID(token & 0x00FFFFFF) == true ? mdTable : null;
			}
		}

		public override bool HasError => offsetTokenVM.HasError;
	}

	sealed class GoToMetadataKindVM : ViewModelBase {
		public GoToMetadataKind Kind { get; }
		public string Text { get; }
		public string InputGestureText { get; }
		public GoToMetadataKindVM(GoToMetadataKind kind, string text, string inputGestureText) {
			Kind = kind;
			Text = text;
			InputGestureText = inputGestureText;
		}
	}
}
