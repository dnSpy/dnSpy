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

using System.Collections.Generic;
using dnSpy.Contracts.HexEditor;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class ImageDosHeaderVM : HexVM {
		public override string Name => "IMAGE_DOS_HEADER";
		public UInt16HexField MagicVM { get; }
		public UInt16HexField CblpVM { get; }
		public UInt16HexField CpVM { get; }
		public UInt16HexField CrlcVM { get; }
		public UInt16HexField CparhdrVM { get; }
		public UInt16HexField MinallocVM { get; }
		public UInt16HexField MaxallocVM { get; }
		public UInt16HexField SsVM { get; }
		public UInt16HexField SpVM { get; }
		public UInt16HexField CsumVM { get; }
		public UInt16HexField IpVM { get; }
		public UInt16HexField CsVM { get; }
		public UInt16HexField LfarlcVM { get; }
		public UInt16HexField OvnoVM { get; }
		public UInt16HexField Res_0VM { get; }
		public UInt16HexField Res_1VM { get; }
		public UInt16HexField Res_2VM { get; }
		public UInt16HexField Res_3VM { get; }
		public UInt16HexField OemidVM { get; }
		public UInt16HexField OeminfoVM { get; }
		public UInt16HexField Res2_0VM { get; }
		public UInt16HexField Res2_1VM { get; }
		public UInt16HexField Res2_2VM { get; }
		public UInt16HexField Res2_3VM { get; }
		public UInt16HexField Res2_4VM { get; }
		public UInt16HexField Res2_5VM { get; }
		public UInt16HexField Res2_6VM { get; }
		public UInt16HexField Res2_7VM { get; }
		public UInt16HexField Res2_8VM { get; }
		public UInt16HexField Res2_9VM { get; }
		public Int32HexField LfanewVM { get; }

		public override IEnumerable<HexField> HexFields => hexFields;
		readonly HexField[] hexFields;

		public ImageDosHeaderVM(object owner, HexDocument doc, ulong startOffset)
			: base(owner) {
			this.MagicVM = new UInt16HexField(doc, Name, "e_magic", startOffset + 0);
			this.CblpVM = new UInt16HexField(doc, Name, "e_cblp", startOffset + 2);
			this.CpVM = new UInt16HexField(doc, Name, "e_cp", startOffset + 4);
			this.CrlcVM = new UInt16HexField(doc, Name, "e_crlc", startOffset + 6);
			this.CparhdrVM = new UInt16HexField(doc, Name, "e_cparhdr", startOffset + 8);
			this.MinallocVM = new UInt16HexField(doc, Name, "e_minalloc", startOffset + 0x0A);
			this.MaxallocVM = new UInt16HexField(doc, Name, "e_maxalloc", startOffset + 0x0C);
			this.SsVM = new UInt16HexField(doc, Name, "e_ss", startOffset + 0x0E);
			this.SpVM = new UInt16HexField(doc, Name, "e_sp", startOffset + 0x10);
			this.CsumVM = new UInt16HexField(doc, Name, "e_csum", startOffset + 0x12);
			this.IpVM = new UInt16HexField(doc, Name, "e_ip", startOffset + 0x14);
			this.CsVM = new UInt16HexField(doc, Name, "e_cs", startOffset + 0x16);
			this.LfarlcVM = new UInt16HexField(doc, Name, "e_lfarlc", startOffset + 0x18);
			this.OvnoVM = new UInt16HexField(doc, Name, "e_ovno", startOffset + 0x1A);
			this.Res_0VM = new UInt16HexField(doc, Name, "e_res[0]", startOffset + 0x1C);
			this.Res_1VM = new UInt16HexField(doc, Name, "e_res[1]", startOffset + 0x1E);
			this.Res_2VM = new UInt16HexField(doc, Name, "e_res[2]", startOffset + 0x20);
			this.Res_3VM = new UInt16HexField(doc, Name, "e_res[3]", startOffset + 0x22);
			this.OemidVM = new UInt16HexField(doc, Name, "e_oemid", startOffset + 0x24);
			this.OeminfoVM = new UInt16HexField(doc, Name, "e_oeminfo", startOffset + 0x26);
			this.Res2_0VM = new UInt16HexField(doc, Name, "e_res2[0]", startOffset + 0x28);
			this.Res2_1VM = new UInt16HexField(doc, Name, "e_res2[1]", startOffset + 0x2A);
			this.Res2_2VM = new UInt16HexField(doc, Name, "e_res2[2]", startOffset + 0x2C);
			this.Res2_3VM = new UInt16HexField(doc, Name, "e_res2[3]", startOffset + 0x2E);
			this.Res2_4VM = new UInt16HexField(doc, Name, "e_res2[4]", startOffset + 0x30);
			this.Res2_5VM = new UInt16HexField(doc, Name, "e_res2[5]", startOffset + 0x32);
			this.Res2_6VM = new UInt16HexField(doc, Name, "e_res2[6]", startOffset + 0x34);
			this.Res2_7VM = new UInt16HexField(doc, Name, "e_res2[7]", startOffset + 0x36);
			this.Res2_8VM = new UInt16HexField(doc, Name, "e_res2[8]", startOffset + 0x38);
			this.Res2_9VM = new UInt16HexField(doc, Name, "e_res2[9]", startOffset + 0x3A);
			this.LfanewVM = new Int32HexField(doc, Name, "e_lfanew", startOffset + 0x3C);

			this.hexFields = new HexField[] {
				MagicVM,
				CblpVM,
				CpVM,
				CrlcVM,
				CparhdrVM,
				MinallocVM,
				MaxallocVM,
				SsVM,
				SpVM,
				CsumVM,
				IpVM,
				CsVM,
				LfarlcVM,
				OvnoVM,
				Res_0VM,
				Res_1VM,
				Res_2VM,
				Res_3VM,
				OemidVM,
				OeminfoVM,
				Res2_0VM,
				Res2_1VM,
				Res2_2VM,
				Res2_3VM,
				Res2_4VM,
				Res2_5VM,
				Res2_6VM,
				Res2_7VM,
				Res2_8VM,
				Res2_9VM,
				LfanewVM,
			};
		}
	}
}
