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

namespace dnSpy.Contracts.Command {
	/// <summary>
	/// Creates <see cref="ICommandTargetFilter"/> instances. Use
	/// <see cref="ExportCommandTargetFilterCreatorAttribute"/> to export an instance.
	/// </summary>
	public interface ICommandTargetFilterCreator {
		/// <summary>
		/// Creates a new <see cref="ICommandTargetFilter"/> instance or returns null
		/// </summary>
		/// <param name="target">Target object</param>
		/// <returns></returns>
		ICommandTargetFilter Create(object target);
	}

	/// <summary>Metadata</summary>
	public interface ICommandTargetFilterCreatorMetadata {
		/// <summary>See <see cref="ExportCommandTargetFilterCreatorAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="ICommandTargetFilterCreator"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportCommandTargetFilterCreatorAttribute : ExportAttribute, ICommandTargetFilterCreatorMetadata {
		/// <summary>Constructor</summary>
		/// <param name="order">Order of this instance, eg. <see cref="CommandConstants.CMDTARGETFILTER_ORDER_TEXT_EDITOR"/></param>
		public ExportCommandTargetFilterCreatorAttribute(double order)
			: base(typeof(ICommandTargetFilterCreator)) {
			Order = order;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
