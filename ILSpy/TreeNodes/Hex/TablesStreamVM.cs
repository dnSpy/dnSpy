/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet.MD;
using dnSpy.HexEditor;

namespace dnSpy.TreeNodes.Hex {
	sealed class TablesStreamVM : HexVM {
		public override string Name {
			get { return "MiniMdSchema"; }
		}

		public UInt32HexField M_ulReservedVM {
			get { return m_ulReservedVM; }
		}
		readonly UInt32HexField m_ulReservedVM;

		public ByteHexField M_majorVM {
			get { return m_majorVM; }
		}
		readonly ByteHexField m_majorVM;

		public ByteHexField M_minorVM {
			get { return m_minorVM; }
		}
		readonly ByteHexField m_minorVM;

		public ByteHexField M_heapsVM {
			get { return m_heapsVM; }
		}
		readonly ByteHexField m_heapsVM;

		public ByteHexField M_ridVM {
			get { return m_ridVM; }
		}
		readonly ByteHexField m_ridVM;

		public UInt64HexField M_maskvalidVM {
			get { return m_maskvalidVM; }
		}
		readonly UInt64HexField m_maskvalidVM;

		public UInt64HexField M_sortedVM {
			get { return m_sortedVM; }
		}
		readonly UInt64HexField m_sortedVM;

		public UInt32HexField Rows00VM {
			get { return rowsVM[0x00]; }
		}

		public UInt32HexField Rows01VM {
			get { return rowsVM[0x01]; }
		}

		public UInt32HexField Rows02VM {
			get { return rowsVM[0x02]; }
		}

		public UInt32HexField Rows03VM {
			get { return rowsVM[0x03]; }
		}

		public UInt32HexField Rows04VM {
			get { return rowsVM[0x04]; }
		}

		public UInt32HexField Rows05VM {
			get { return rowsVM[0x05]; }
		}

		public UInt32HexField Rows06VM {
			get { return rowsVM[0x06]; }
		}

		public UInt32HexField Rows07VM {
			get { return rowsVM[0x07]; }
		}

		public UInt32HexField Rows08VM {
			get { return rowsVM[0x08]; }
		}

		public UInt32HexField Rows09VM {
			get { return rowsVM[0x09]; }
		}

		public UInt32HexField Rows0AVM {
			get { return rowsVM[0x0A]; }
		}

		public UInt32HexField Rows0BVM {
			get { return rowsVM[0x0B]; }
		}

		public UInt32HexField Rows0CVM {
			get { return rowsVM[0x0C]; }
		}

		public UInt32HexField Rows0DVM {
			get { return rowsVM[0x0D]; }
		}

		public UInt32HexField Rows0EVM {
			get { return rowsVM[0x0E]; }
		}

		public UInt32HexField Rows0FVM {
			get { return rowsVM[0x0F]; }
		}

		public UInt32HexField Rows10VM {
			get { return rowsVM[0x10]; }
		}

		public UInt32HexField Rows11VM {
			get { return rowsVM[0x11]; }
		}

		public UInt32HexField Rows12VM {
			get { return rowsVM[0x12]; }
		}

		public UInt32HexField Rows13VM {
			get { return rowsVM[0x13]; }
		}

		public UInt32HexField Rows14VM {
			get { return rowsVM[0x14]; }
		}

		public UInt32HexField Rows15VM {
			get { return rowsVM[0x15]; }
		}

		public UInt32HexField Rows16VM {
			get { return rowsVM[0x16]; }
		}

		public UInt32HexField Rows17VM {
			get { return rowsVM[0x17]; }
		}

		public UInt32HexField Rows18VM {
			get { return rowsVM[0x18]; }
		}

		public UInt32HexField Rows19VM {
			get { return rowsVM[0x19]; }
		}

		public UInt32HexField Rows1AVM {
			get { return rowsVM[0x1A]; }
		}

		public UInt32HexField Rows1BVM {
			get { return rowsVM[0x1B]; }
		}

		public UInt32HexField Rows1CVM {
			get { return rowsVM[0x1C]; }
		}

		public UInt32HexField Rows1DVM {
			get { return rowsVM[0x1D]; }
		}

		public UInt32HexField Rows1EVM {
			get { return rowsVM[0x1E]; }
		}

		public UInt32HexField Rows1FVM {
			get { return rowsVM[0x1F]; }
		}

		public UInt32HexField Rows20VM {
			get { return rowsVM[0x20]; }
		}

		public UInt32HexField Rows21VM {
			get { return rowsVM[0x21]; }
		}

		public UInt32HexField Rows22VM {
			get { return rowsVM[0x22]; }
		}

		public UInt32HexField Rows23VM {
			get { return rowsVM[0x23]; }
		}

		public UInt32HexField Rows24VM {
			get { return rowsVM[0x24]; }
		}

		public UInt32HexField Rows25VM {
			get { return rowsVM[0x25]; }
		}

		public UInt32HexField Rows26VM {
			get { return rowsVM[0x26]; }
		}

		public UInt32HexField Rows27VM {
			get { return rowsVM[0x27]; }
		}

		public UInt32HexField Rows28VM {
			get { return rowsVM[0x28]; }
		}

		public UInt32HexField Rows29VM {
			get { return rowsVM[0x29]; }
		}

		public UInt32HexField Rows2AVM {
			get { return rowsVM[0x2A]; }
		}

		public UInt32HexField Rows2BVM {
			get { return rowsVM[0x2B]; }
		}

		public UInt32HexField Rows2CVM {
			get { return rowsVM[0x2C]; }
		}

		public UInt32HexField Rows2DVM {
			get { return rowsVM[0x2D]; }
		}

		public UInt32HexField Rows2EVM {
			get { return rowsVM[0x2E]; }
		}

		public UInt32HexField Rows2FVM {
			get { return rowsVM[0x2F]; }
		}

		public UInt32HexField Rows30VM {
			get { return rowsVM[0x30]; }
		}

		public UInt32HexField Rows31VM {
			get { return rowsVM[0x31]; }
		}

		public UInt32HexField Rows32VM {
			get { return rowsVM[0x32]; }
		}

		public UInt32HexField Rows33VM {
			get { return rowsVM[0x33]; }
		}

		public UInt32HexField Rows34VM {
			get { return rowsVM[0x34]; }
		}

		public UInt32HexField Rows35VM {
			get { return rowsVM[0x35]; }
		}

		public UInt32HexField Rows36VM {
			get { return rowsVM[0x36]; }
		}

		public UInt32HexField Rows37VM {
			get { return rowsVM[0x37]; }
		}

		public UInt32HexField Rows38VM {
			get { return rowsVM[0x38]; }
		}

		public UInt32HexField Rows39VM {
			get { return rowsVM[0x39]; }
		}

		public UInt32HexField Rows3AVM {
			get { return rowsVM[0x3A]; }
		}

		public UInt32HexField Rows3BVM {
			get { return rowsVM[0x3B]; }
		}

		public UInt32HexField Rows3CVM {
			get { return rowsVM[0x3C]; }
		}

		public UInt32HexField Rows3DVM {
			get { return rowsVM[0x3D]; }
		}

		public UInt32HexField Rows3EVM {
			get { return rowsVM[0x3E]; }
		}

		public UInt32HexField Rows3FVM {
			get { return rowsVM[0x3F]; }
		}

		readonly UInt32HexField[] rowsVM;

		public UInt32HexField M_ulExtraVM {
			get { return m_ulExtraVM; }
		}
		readonly UInt32HexField m_ulExtraVM;

		public override IEnumerable<HexField> HexFields {
			get { return hexFields; }
		}
		readonly HexField[] hexFields;

		public TablesStreamVM(HexDocument doc, TablesStream tblStream) {
			ulong startOffset = (ulong)tblStream.StartOffset;
			this.m_ulReservedVM = new UInt32HexField(doc, Name, "m_ulReserved", startOffset + 0);
			this.m_majorVM = new ByteHexField(doc, Name, "m_major", startOffset + 4);
			this.m_minorVM = new ByteHexField(doc, Name, "m_minor", startOffset + 5);
			this.m_heapsVM = new ByteHexField(doc, Name, "m_heaps", startOffset + 6);
			this.m_ridVM = new ByteHexField(doc, Name, "m_rid", startOffset + 7);
			this.m_maskvalidVM = new UInt64HexField(doc, Name, "m_maskvalid", startOffset + 8);
			this.m_sortedVM = new UInt64HexField(doc, Name, "m_sorted", startOffset + 0x10);

			var list = new List<HexField> {
				m_ulReservedVM,
				m_majorVM,
				m_minorVM,
				m_heapsVM,
				m_ridVM,
				m_maskvalidVM,
				m_sortedVM,
			};

			this.rowsVM = new UInt32HexField[64];
			ulong valid = tblStream.ValidMask;
			ulong offs = startOffset + 0x18;
			for (int i = 0; i < this.rowsVM.Length; i++) {
				this.rowsVM[i] = new UInt32HexField(doc, Name, string.Format("rows[{0:X2}]", i), offs);
				if ((valid & 1) != 0) {
					list.Add(this.rowsVM[i]);
					offs += 4;
				}
				else
					this.rowsVM[i].IsVisible = false;

				valid >>= 1;
			}

			this.m_ulExtraVM = new UInt32HexField(doc, Name, "m_ulExtraVM", offs);
			this.m_ulExtraVM.IsVisible = tblStream.HasExtraData;
			if (tblStream.HasExtraData)
				list.Add(this.m_ulExtraVM);

			Debug.Assert(offs == (ulong)tblStream.MDTables[0].StartOffset);

			this.hexFields = list.ToArray();
		}
	}
}
