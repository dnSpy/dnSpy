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
using dnSpy.Contracts.Resources;
using dnSpy.Contracts.Settings.HexEditor;
using dnSpy.Contracts.Settings.HexGroups;
using dnSpy.Hex.Settings;

namespace dnSpy.Hex.HexEditor {
	sealed class HexEditorOptions : CommonEditorOptions {
		public Guid Guid { get; }
		public string Name { get; }

		HexEditorOptions(HexViewOptionsGroup group, string subGroup, Guid guid, string? name)
			: base(group, subGroup) {
			Guid = guid;
			Name = name ?? throw new ArgumentOutOfRangeException(nameof(name));
		}

		public static HexEditorOptions? TryCreate(HexViewOptionsGroup group, IHexEditorOptionsDefinitionMetadata md) {
			if (group is null)
				throw new ArgumentNullException(nameof(group));
			if (md is null)
				throw new ArgumentNullException(nameof(md));

			if (md.SubGroup is null)
				return null;
			var subGroup = md.SubGroup;
			if (subGroup is null)
				return null;

			if (md.Guid is null)
				return null;
			if (!Guid.TryParse(md.Guid, out var guid))
				return null;

			if (md.Name is null)
				return null;

			return new HexEditorOptions(group, subGroup, guid, ResourceHelper.GetString(md.Type.Assembly, md.Name));
		}
	}
}
