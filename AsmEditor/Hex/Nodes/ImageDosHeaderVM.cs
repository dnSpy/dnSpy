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
using dnSpy.Shared.UI.HexEditor;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class ImageDosHeaderVM : HexVM {
		public override string Name {
			get { return "IMAGE_DOS_HEADER"; }
		}

		public UInt16HexField MagicVM {
			get { return magicVM; }
		}
		readonly UInt16HexField magicVM;

		public UInt16HexField CblpVM {
			get { return cblpVM; }
		}
		readonly UInt16HexField cblpVM;

		public UInt16HexField CpVM {
			get { return cpVM; }
		}
		readonly UInt16HexField cpVM;

		public UInt16HexField CrlcVM {
			get { return crlcVM; }
		}
		readonly UInt16HexField crlcVM;

		public UInt16HexField CparhdrVM {
			get { return cparhdrVM; }
		}
		readonly UInt16HexField cparhdrVM;

		public UInt16HexField MinallocVM {
			get { return minallocVM; }
		}
		readonly UInt16HexField minallocVM;

		public UInt16HexField MaxallocVM {
			get { return maxallocVM; }
		}
		readonly UInt16HexField maxallocVM;

		public UInt16HexField SsVM {
			get { return ssVM; }
		}
		readonly UInt16HexField ssVM;

		public UInt16HexField SpVM {
			get { return spVM; }
		}
		readonly UInt16HexField spVM;

		public UInt16HexField CsumVM {
			get { return csumVM; }
		}
		readonly UInt16HexField csumVM;

		public UInt16HexField IpVM {
			get { return ipVM; }
		}
		readonly UInt16HexField ipVM;

		public UInt16HexField CsVM {
			get { return csVM; }
		}
		readonly UInt16HexField csVM;

		public UInt16HexField LfarlcVM {
			get { return lfarlcVM; }
		}
		readonly UInt16HexField lfarlcVM;

		public UInt16HexField OvnoVM {
			get { return ovnoVM; }
		}
		readonly UInt16HexField ovnoVM;

		public UInt16HexField Res_0VM {
			get { return res_0VM; }
		}
		readonly UInt16HexField res_0VM;

		public UInt16HexField Res_1VM {
			get { return res_1VM; }
		}
		readonly UInt16HexField res_1VM;

		public UInt16HexField Res_2VM {
			get { return res_2VM; }
		}
		readonly UInt16HexField res_2VM;

		public UInt16HexField Res_3VM {
			get { return res_3VM; }
		}
		readonly UInt16HexField res_3VM;

		public UInt16HexField OemidVM {
			get { return oemidVM; }
		}
		readonly UInt16HexField oemidVM;

		public UInt16HexField OeminfoVM {
			get { return oeminfoVM; }
		}
		readonly UInt16HexField oeminfoVM;

		public UInt16HexField Res2_0VM {
			get { return res2_0VM; }
		}
		readonly UInt16HexField res2_0VM;

		public UInt16HexField Res2_1VM {
			get { return res2_1VM; }
		}
		readonly UInt16HexField res2_1VM;

		public UInt16HexField Res2_2VM {
			get { return res2_2VM; }
		}
		readonly UInt16HexField res2_2VM;

		public UInt16HexField Res2_3VM {
			get { return res2_3VM; }
		}
		readonly UInt16HexField res2_3VM;

		public UInt16HexField Res2_4VM {
			get { return res2_4VM; }
		}
		readonly UInt16HexField res2_4VM;

		public UInt16HexField Res2_5VM {
			get { return res2_5VM; }
		}
		readonly UInt16HexField res2_5VM;

		public UInt16HexField Res2_6VM {
			get { return res2_6VM; }
		}
		readonly UInt16HexField res2_6VM;

		public UInt16HexField Res2_7VM {
			get { return res2_7VM; }
		}
		readonly UInt16HexField res2_7VM;

		public UInt16HexField Res2_8VM {
			get { return res2_8VM; }
		}
		readonly UInt16HexField res2_8VM;

		public UInt16HexField Res2_9VM {
			get { return res2_9VM; }
		}
		readonly UInt16HexField res2_9VM;

		public Int32HexField LfanewVM {
			get { return lfanewVM; }
		}
		readonly Int32HexField lfanewVM;

		public override IEnumerable<HexField> HexFields {
			get { return hexFields; }
		}
		readonly HexField[] hexFields;

		public ImageDosHeaderVM(object owner, HexDocument doc, ulong startOffset)
			: base(owner) {
			this.magicVM = new UInt16HexField(doc, Name, "e_magic", startOffset + 0);
			this.cblpVM = new UInt16HexField(doc, Name, "e_cblp", startOffset + 2);
			this.cpVM = new UInt16HexField(doc, Name, "e_cp", startOffset + 4);
			this.crlcVM = new UInt16HexField(doc, Name, "e_crlc", startOffset + 6);
			this.cparhdrVM = new UInt16HexField(doc, Name, "e_cparhdr", startOffset + 8);
			this.minallocVM = new UInt16HexField(doc, Name, "e_minalloc", startOffset + 0x0A);
			this.maxallocVM = new UInt16HexField(doc, Name, "e_maxalloc", startOffset + 0x0C);
			this.ssVM = new UInt16HexField(doc, Name, "e_ss", startOffset + 0x0E);
			this.spVM = new UInt16HexField(doc, Name, "e_sp", startOffset + 0x10);
			this.csumVM = new UInt16HexField(doc, Name, "e_csum", startOffset + 0x12);
			this.ipVM = new UInt16HexField(doc, Name, "e_ip", startOffset + 0x14);
			this.csVM = new UInt16HexField(doc, Name, "e_cs", startOffset + 0x16);
			this.lfarlcVM = new UInt16HexField(doc, Name, "e_lfarlc", startOffset + 0x18);
			this.ovnoVM = new UInt16HexField(doc, Name, "e_ovno", startOffset + 0x1A);
			this.res_0VM = new UInt16HexField(doc, Name, "e_res[0]", startOffset + 0x1C);
			this.res_1VM = new UInt16HexField(doc, Name, "e_res[1]", startOffset + 0x1E);
			this.res_2VM = new UInt16HexField(doc, Name, "e_res[2]", startOffset + 0x20);
			this.res_3VM = new UInt16HexField(doc, Name, "e_res[3]", startOffset + 0x22);
			this.oemidVM = new UInt16HexField(doc, Name, "e_oemid", startOffset + 0x24);
			this.oeminfoVM = new UInt16HexField(doc, Name, "e_oeminfo", startOffset + 0x26);
			this.res2_0VM = new UInt16HexField(doc, Name, "e_res2[0]", startOffset + 0x28);
			this.res2_1VM = new UInt16HexField(doc, Name, "e_res2[1]", startOffset + 0x2A);
			this.res2_2VM = new UInt16HexField(doc, Name, "e_res2[2]", startOffset + 0x2C);
			this.res2_3VM = new UInt16HexField(doc, Name, "e_res2[3]", startOffset + 0x2E);
			this.res2_4VM = new UInt16HexField(doc, Name, "e_res2[4]", startOffset + 0x30);
			this.res2_5VM = new UInt16HexField(doc, Name, "e_res2[5]", startOffset + 0x32);
			this.res2_6VM = new UInt16HexField(doc, Name, "e_res2[6]", startOffset + 0x34);
			this.res2_7VM = new UInt16HexField(doc, Name, "e_res2[7]", startOffset + 0x36);
			this.res2_8VM = new UInt16HexField(doc, Name, "e_res2[8]", startOffset + 0x38);
			this.res2_9VM = new UInt16HexField(doc, Name, "e_res2[9]", startOffset + 0x3A);
			this.lfanewVM = new Int32HexField(doc, Name, "e_lfanew", startOffset + 0x3C);

			this.hexFields = new HexField[] {
				magicVM,
				cblpVM,
				cpVM,
				crlcVM,
				cparhdrVM,
				minallocVM,
				maxallocVM,
				ssVM,
				spVM,
				csumVM,
				ipVM,
				csVM,
				lfarlcVM,
				ovnoVM,
				res_0VM,
				res_1VM,
				res_2VM,
				res_3VM,
				oemidVM,
				oeminfoVM,
				res2_0VM,
				res2_1VM,
				res2_2VM,
				res2_3VM,
				res2_4VM,
				res2_5VM,
				res2_6VM,
				res2_7VM,
				res2_8VM,
				res2_9VM,
				lfanewVM,
			};
		}
	}
}
