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

using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Settings;

namespace dnSpy.AsmEditor.Hex {
	static class HexViewUIStateSerializer {
		public static HexViewUIState Write(ISettingsSection section, HexViewUIState s) {
			section.Attribute("ShowOffsetColumn", s.ShowOffsetColumn);
			section.Attribute("ShowValuesColumn", s.ShowValuesColumn);
			section.Attribute("ShowAsciiColumn", s.ShowAsciiColumn);
			section.Attribute("StartPosition", s.StartPosition);
			section.Attribute("EndPosition", s.EndPosition);
			section.Attribute("BasePosition", s.BasePosition);
			section.Attribute("UseRelativePositions", s.UseRelativePositions);
			section.Attribute("OffsetBitSize", s.OffsetBitSize);
			section.Attribute("HexValuesDisplayFormat", s.HexValuesDisplayFormat);
			section.Attribute("BytesPerLine", s.BytesPerLine);

			section.Attribute("ActiveColumn", s.ActiveColumn);
			section.Attribute("ValuesPosition", s.ValuesPosition);
			section.Attribute("ValuesCellPosition", s.ValuesCellPosition);
			section.Attribute("AsciiPosition", s.AsciiPosition);
			section.Attribute("ViewportLeft", s.ViewportLeft);
			section.Attribute("TopLinePosition", s.TopLinePosition);
			section.Attribute("TopLineVerticalDistance", s.TopLineVerticalDistance);

			return s;
		}

		public static HexViewUIState Read(ISettingsSection section, HexViewUIState s) {
			bool failed = false;
			s.ShowOffsetColumn = GetValue<bool>(section, "ShowOffsetColumn", ref failed);
			s.ShowValuesColumn = GetValue<bool>(section, "ShowValuesColumn", ref failed);
			s.ShowAsciiColumn = GetValue<bool>(section, "ShowAsciiColumn", ref failed);
			s.StartPosition = GetValue<HexPosition>(section, "StartPosition", ref failed);
			s.EndPosition = GetValue<HexPosition>(section, "EndPosition", ref failed);
			s.BasePosition = GetValue<HexPosition>(section, "BasePosition", ref failed);
			s.UseRelativePositions = GetValue<bool>(section, "UseRelativePositions", ref failed);
			s.OffsetBitSize = GetValue<int>(section, "OffsetBitSize", ref failed);
			s.HexValuesDisplayFormat = GetValue<HexValuesDisplayFormat>(section, "HexValuesDisplayFormat", ref failed);
			s.BytesPerLine = GetValue<int>(section, "BytesPerLine", ref failed);

			s.ActiveColumn = GetValue<HexColumnType>(section, "ActiveColumn", ref failed);
			s.ValuesPosition = GetValue<HexPosition>(section, "ValuesPosition", ref failed);
			s.ValuesCellPosition = GetValue<int>(section, "ValuesCellPosition", ref failed);
			s.AsciiPosition = GetValue<HexPosition>(section, "AsciiPosition", ref failed);
			s.ViewportLeft = GetValue<double>(section, "ViewportLeft", ref failed);
			s.TopLinePosition = GetValue<HexPosition>(section, "TopLinePosition", ref failed);
			s.TopLineVerticalDistance = GetValue<double>(section, "TopLineVerticalDistance", ref failed);

			if (failed)
				return null;
			return s;
		}

		static T GetValue<T>(ISettingsSection section, string name, ref bool failed) where T : struct {
			var v = section.Attribute<T?>(name);
			if (v == null) {
				failed = true;
				return default(T);
			}
			return v.Value;
		}
	}
}
