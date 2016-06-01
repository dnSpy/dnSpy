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
using System.ComponentModel.Composition;

namespace dnSpy.Contracts.Command {
	/// <summary>
	/// Converts raw input to a <see cref="CommandInfo"/>. Use
	/// <see cref="ExportCommandInfoCreatorAttribute"/> to export an instance.
	/// </summary>
	public interface ICommandInfoCreator {
		/// <summary>
		/// Gets all keyboard shortcuts
		/// </summary>
		/// <param name="target">Target object</param>
		/// <returns></returns>
		IEnumerable<CommandShortcut> GetCommandShortcuts(object target);

		/// <summary>
		/// Returns a <see cref="CommandInfo"/> created from user text
		/// </summary>
		/// <param name="target">Target object</param>
		/// <param name="text">Text typed by the user</param>
		/// <returns></returns>
		CommandInfo? CreateFromTextInput(object target, string text);
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
		/// <param name="order">Order of this instance, eg. <see cref="CommandConstants.CMDINFO_ORDER_DEFAULT"/></param>
		public ExportCommandInfoCreatorAttribute(double order)
			: base(typeof(ICommandInfoCreator)) {
			Order = order;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
