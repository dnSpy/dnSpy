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
using System.ComponentModel.Composition;
using System.Windows.Input;

namespace dnSpy.Contracts.Command {
	/// <summary>
	/// Converts raw input to a <see cref="CommandInfo"/>. Use
	/// <see cref="ExportCommandInfoCreatorAttribute"/> to export an instance.
	/// </summary>
	public interface ICommandInfoCreator {
		/// <summary>
		/// Converts raw input to a <see cref="CommandInfo"/> or returns null
		/// </summary>
		/// <param name="e">Key event args</param>
		/// <param name="owner">Owner object</param>
		/// <returns></returns>
		CommandInfo? Create(KeyEventArgs e, object owner);
	}

	/// <summary>Metadata</summary>
	public interface ICommandInfoCreatorMetadata {
		/// <summary>See <see cref="ExportCommandInfoCreatorAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="ICommandInfoCreator"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportCommandInfoCreatorAttribute : ExportAttribute, ICommandInfoCreatorMetadata {
		/// <summary>Constructor</summary>
		public ExportCommandInfoCreatorAttribute()
			: base(typeof(ICommandInfoCreator)) {
			Order = double.MaxValue;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
