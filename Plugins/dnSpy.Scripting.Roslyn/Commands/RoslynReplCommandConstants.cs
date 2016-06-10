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
using dnSpy.Contracts.Command;

namespace dnSpy.Scripting.Roslyn.Commands {
	static class RoslynReplCommandConstants {
		/// <summary>
		/// Roslyn REPL command IDs (<see cref="RoslynReplIds"/>)
		/// </summary>
		public static readonly Guid RoslynReplGroup = new Guid("75758152-7214-4C5C-8F5C-180441233B46");

		/// <summary>
		/// Order of Roslyn REPL editor <see cref="ICommandInfoCreator"/>
		/// </summary>
		public const double CMDINFO_ORDER_ROSLYN_REPL = CommandConstants.CMDINFO_ORDER_REPL - 100;

		/// <summary>
		/// Order of Roslyn REPL editor <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_ROSLYN_REPL = CommandConstants.CMDTARGETFILTER_ORDER_REPL - 100;
	}
}
