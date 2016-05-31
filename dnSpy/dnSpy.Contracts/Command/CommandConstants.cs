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

namespace dnSpy.Contracts.Command {
	/// <summary>
	/// Command constants
	/// </summary>
	public static class CommandConstants {
		/// <summary>
		/// Default command IDs (<see cref="DefaultIds"/>)
		/// </summary>
		public static readonly Guid DefaultGroup = new Guid("14608CB3-3965-49B2-A8A9-46CDBB4E2E30");

		/// <summary>
		/// Text editor command IDs (<see cref="TextEditorIds"/>)
		/// </summary>
		public static readonly Guid TextEditorGroup = new Guid("2313BC9A-8895-4390-87BF-FA563F35B33B");

		/// <summary>
		/// Order of default <see cref="ICommandInfoCreator"/>
		/// </summary>
		public const double CMDINFO_ORDER_DEFAULT = 10000;

		/// <summary>
		/// Order of default text editor <see cref="ICommandInfoCreator"/>
		/// </summary>
		public const double CMDINFO_ORDER_TEXT_EDITOR = 5000;

		/// <summary>
		/// Order of default text editor <see cref="ICommandTargetFilterCreator"/>
		/// </summary>
		public const double CMDFILTERCREATOR_ORDER_TEXT_EDITOR = 5000;
	}
}
