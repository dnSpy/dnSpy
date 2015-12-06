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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// Creates <see cref="ISimpleAppOption"/> instances (<see cref="ISimpleAppOptionCheckBox"/>,
	/// <see cref="ISimpleAppOptionButton"/>, <see cref="ISimpleAppOptionTextBox"/>,
	/// <see cref="ISimpleAppOptionUserContent"/>). Use <see cref="ExportSimpleAppOptionCreatorAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface ISimpleAppOptionCreator {
		/// <summary>
		/// Creates new <see cref="ISimpleAppOption"/> instances
		/// </summary>
		/// <returns></returns>
		IEnumerable<ISimpleAppOption> Create();
	}

	/// <summary>Metadata</summary>
	public interface ISimpleAppOptionCreatorMetadata {
		/// <summary>See <see cref="ExportSimpleAppOptionCreatorAttribute.Guid"/></summary>
		string Guid { get; }
	}

	/// <summary>
	/// Exports a <see cref="ISimpleAppOptionCreator"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportSimpleAppOptionCreatorAttribute : ExportAttribute, ISimpleAppOptionCreatorMetadata {
		/// <summary>Constructor</summary>
		public ExportSimpleAppOptionCreatorAttribute()
			: base(typeof(ISimpleAppOptionCreator)) {
		}

		/// <summary>
		/// Gets the guid of the <see cref="IDynamicAppSettingsTab"/>, eg. <see cref="AppSettingsConstants.GUID_DYNTAB_MISC"/>
		/// </summary>
		public string Guid { get; set; }
	}
}
