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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.AsmEditor.Hex.PE {
	sealed class ImageDosHeaderVM : HexVM {
		public override string Name { get; }
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
		public UInt32HexField LfanewVM { get; }

		public override IEnumerable<HexField> HexFields => hexFields;
		readonly HexField[] hexFields;

		public ImageDosHeaderVM(HexBuffer buffer, PeDosHeaderData dosHeader)
			: base(dosHeader.Span.Span) {
			Name = dosHeader.Name;
			MagicVM = new UInt16HexField(dosHeader.Magic);
			CblpVM = new UInt16HexField(dosHeader.Cblp);
			CpVM = new UInt16HexField(dosHeader.Cp);
			CrlcVM = new UInt16HexField(dosHeader.Crlc);
			CparhdrVM = new UInt16HexField(dosHeader.Cparhdr);
			MinallocVM = new UInt16HexField(dosHeader.Minalloc);
			MaxallocVM = new UInt16HexField(dosHeader.Maxalloc);
			SsVM = new UInt16HexField(dosHeader.Ss);
			SpVM = new UInt16HexField(dosHeader.Sp);
			CsumVM = new UInt16HexField(dosHeader.Csum);
			IpVM = new UInt16HexField(dosHeader.Ip);
			CsVM = new UInt16HexField(dosHeader.Cs);
			LfarlcVM = new UInt16HexField(dosHeader.Lfarlc);
			OvnoVM = new UInt16HexField(dosHeader.Ovno);
			Res_0VM = new UInt16HexField(dosHeader.Res.Data[0].Data, dosHeader.Res.Name + "[0]");
			Res_1VM = new UInt16HexField(dosHeader.Res.Data[1].Data, dosHeader.Res.Name + "[1]");
			Res_2VM = new UInt16HexField(dosHeader.Res.Data[2].Data, dosHeader.Res.Name + "[2]");
			Res_3VM = new UInt16HexField(dosHeader.Res.Data[3].Data, dosHeader.Res.Name + "[3]");
			OemidVM = new UInt16HexField(dosHeader.Oemid);
			OeminfoVM = new UInt16HexField(dosHeader.Oeminfo);
			Res2_0VM = new UInt16HexField(dosHeader.Res2.Data[0].Data, dosHeader.Res2.Name + "[0]");
			Res2_1VM = new UInt16HexField(dosHeader.Res2.Data[1].Data, dosHeader.Res2.Name + "[1]");
			Res2_2VM = new UInt16HexField(dosHeader.Res2.Data[2].Data, dosHeader.Res2.Name + "[2]");
			Res2_3VM = new UInt16HexField(dosHeader.Res2.Data[3].Data, dosHeader.Res2.Name + "[3]");
			Res2_4VM = new UInt16HexField(dosHeader.Res2.Data[4].Data, dosHeader.Res2.Name + "[4]");
			Res2_5VM = new UInt16HexField(dosHeader.Res2.Data[5].Data, dosHeader.Res2.Name + "[5]");
			Res2_6VM = new UInt16HexField(dosHeader.Res2.Data[6].Data, dosHeader.Res2.Name + "[6]");
			Res2_7VM = new UInt16HexField(dosHeader.Res2.Data[7].Data, dosHeader.Res2.Name + "[7]");
			Res2_8VM = new UInt16HexField(dosHeader.Res2.Data[8].Data, dosHeader.Res2.Name + "[8]");
			Res2_9VM = new UInt16HexField(dosHeader.Res2.Data[9].Data, dosHeader.Res2.Name + "[9]");
			LfanewVM = new UInt32HexField(dosHeader.Lfanew);

			hexFields = new HexField[] {
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
