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
		/// REPL command IDs (<see cref="ReplIds"/>)
		/// </summary>
		public static readonly Guid ReplGroup = new Guid("8DBB0C94-6B10-4AC3-A715-CC4D478F7B67");

		/// <summary>
		/// Output logger text pane command IDs (<see cref="OutputTextPaneIds"/>)
		/// </summary>
		public static readonly Guid OutputTextPaneGroup = new Guid("091D1F2F-175A-4BD9-A0F3-C5F052D22D75");

		/// <summary>
		/// Order of default <see cref="ICommandInfoCreator"/>
		/// </summary>
		public const double CMDINFO_ORDER_DEFAULT = 10000;

		/// <summary>
		/// Order of default text editor <see cref="ICommandInfoCreator"/>
		/// </summary>
		public const double CMDINFO_ORDER_TEXT_EDITOR = 5000;

		/// <summary>
		/// Order of default text editor <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_TEXT_EDITOR = 5000;

		/// <summary>
		/// Order of REPL editor <see cref="ICommandInfoCreator"/>
		/// </summary>
		public const double CMDINFO_ORDER_REPL = CMDINFO_ORDER_TEXT_EDITOR - 100;

		/// <summary>
		/// Order of REPL editor <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_REPL = CMDTARGETFILTER_ORDER_TEXT_EDITOR - 100;

		/// <summary>
		/// Order of output logger text pane <see cref="ICommandInfoCreator"/>
		/// </summary>
		public const double CMDINFO_ORDER_OUTPUT_TEXTPANE = CMDINFO_ORDER_TEXT_EDITOR - 100;

		/// <summary>
		/// Order of output logger text pane <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_OUTPUT_TEXTPANE = CMDTARGETFILTER_ORDER_TEXT_EDITOR - 100;
	}
}
