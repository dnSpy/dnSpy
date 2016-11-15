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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using dnSpy.Contracts.Hex.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.MEF {

	// All interfaces must be public or MEF will complain

	public interface INameAndReplacesMetadata {
		[DefaultValue(null)]
		string Name { get; }

		[DefaultValue(null)]
		IEnumerable<string> Replaces { get; }
	}

	public interface IAdornmentLayersMetadata : IOrderable {
		[DefaultValue(false)]
		bool IsOverlayLayer { get; }

		[DefaultValue(HexLayerKind.Normal)]
		HexLayerKind LayerKind { get; }
	}

	public interface ITaggerMetadata {
		IEnumerable<Type> TagTypes { get; }
	}

	public interface INamedTaggerMetadata : ITaggerMetadata, INameAndReplacesMetadata {
	}

	public interface IViewTaggerMetadata : INamedTaggerMetadata {
		[DefaultValue(null)]
		IEnumerable<string> TextViewRoles { get; }
	}
}
