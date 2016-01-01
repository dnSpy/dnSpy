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

using dnSpy.Contracts.Settings;
using dnSpy.Shared.UI.HexEditor;

namespace dnSpy.AsmEditor.Hex {
	static class HexBoxUIStateSerializer {
		public static HexBoxUIState Write(ISettingsSection section, HexBoxUIState s) {
			if (s.HexBoxState == null)
				return s;

			section.Attribute("BytesGroupCount", s.BytesGroupCount);
			section.Attribute("BytesPerLine", s.BytesPerLine);
			section.Attribute("UseHexPrefix", s.UseHexPrefix);
			section.Attribute("ShowAscii", s.ShowAscii);
			section.Attribute("LowerCaseHex", s.LowerCaseHex);
			section.Attribute("AsciiEncoding", s.AsciiEncoding);

			section.Attribute("HexOffsetSize", s.HexOffsetSize);
			section.Attribute("UseRelativeOffsets", s.UseRelativeOffsets);
			section.Attribute("BaseOffset", s.BaseOffset);

			section.Attribute("HexBoxState-TopOffset", s.HexBoxState.TopOffset);
			section.Attribute("HexBoxState-Column", s.HexBoxState.Column);
			section.Attribute("HexBoxState-StartOffset", s.HexBoxState.StartOffset);
			section.Attribute("HexBoxState-EndOffset", s.HexBoxState.EndOffset);
			section.Attribute("HexBoxState-HexBoxPosition-Offset", s.HexBoxState.CaretPosition.Offset);
			section.Attribute("HexBoxState-HexBoxPosition-Kind", s.HexBoxState.CaretPosition.Kind);
			section.Attribute("HexBoxState-HexBoxPosition-KindPosition", s.HexBoxState.CaretPosition.KindPosition);
			if (s.HexBoxState.Selection != null) {
				section.Attribute("HexBoxState-Selection-From", s.HexBoxState.Selection.Value.From);
				section.Attribute("HexBoxState-Selection-To", s.HexBoxState.Selection.Value.To);
			}

			return s;
		}

		public static HexBoxUIState Read(ISettingsSection section, HexBoxUIState s) {
			if (s.HexBoxState == null)
				return s;

			s.BytesGroupCount = section.Attribute<int?>("BytesGroupCount");
			s.BytesPerLine = section.Attribute<int?>("BytesPerLine");
			s.UseHexPrefix = section.Attribute<bool?>("UseHexPrefix");
			s.ShowAscii = section.Attribute<bool?>("ShowAscii");
			s.LowerCaseHex = section.Attribute<bool?>("LowerCaseHex");
			s.AsciiEncoding = section.Attribute<AsciiEncoding?>("AsciiEncoding");

			s.HexOffsetSize = section.Attribute<int?>("HexOffsetSize") ?? 0;
			s.UseRelativeOffsets = section.Attribute<bool?>("UseRelativeOffsets") ?? false;
			s.BaseOffset = section.Attribute<ulong?>("BaseOffset") ?? 0;

			s.HexBoxState.TopOffset = section.Attribute<ulong?>("HexBoxState-TopOffset") ?? 0;
			s.HexBoxState.Column = section.Attribute<int?>("HexBoxState-Column") ?? 0;
			s.HexBoxState.StartOffset = section.Attribute<ulong?>("HexBoxState-StartOffset") ?? 0;
			s.HexBoxState.EndOffset = section.Attribute<ulong?>("HexBoxState-EndOffset") ?? 0;
			s.HexBoxState.CaretPosition.Offset = section.Attribute<ulong?>("HexBoxState-HexBoxPosition-Offset") ?? 0;
			s.HexBoxState.CaretPosition.Kind = section.Attribute<HexBoxPositionKind?>("HexBoxState-HexBoxPosition-Kind") ?? 0;
			s.HexBoxState.CaretPosition.KindPosition = section.Attribute<byte?>("HexBoxState-HexBoxPosition-KindPosition") ?? 0;

			var from = section.Attribute<ulong?>("HexBoxState-Selection-From");
			var to = section.Attribute<ulong?>("HexBoxState-Selection-To");
			if (from != null && to != null)
				s.HexBoxState.Selection = new HexSelection((ulong)from, (ulong)to);

			return s;
		}
	}
}
