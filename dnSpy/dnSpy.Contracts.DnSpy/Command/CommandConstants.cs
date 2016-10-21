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
		/// Standard command IDs (<see cref="StandardIds"/>)
		/// </summary>
		public static readonly Guid StandardGroup = new Guid("14608CB3-3965-49B2-A8A9-46CDBB4E2E30");

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
		/// Text reference command IDs
		/// </summary>
		public static readonly Guid TextReferenceGroup = new Guid("8D5BC6C7-C013-4401-9ADC-62B411573F3C");

		/// <summary>
		/// Order of default <see cref="ICommandInfoProvider"/>
		/// </summary>
		public const double CMDINFO_ORDER_DEFAULT = 100000;

		/// <summary>
		/// Order of default text editor <see cref="ICommandInfoProvider"/>
		/// </summary>
		public const double CMDINFO_ORDER_TEXT_EDITOR = 50000;

		/// <summary>
		/// Order of default text editor <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_TEXT_EDITOR = 50000;

		/// <summary>
		/// Order of undo/redo <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_UNDO = CMDTARGETFILTER_ORDER_TEXT_EDITOR - 1;

		/// <summary>
		/// Order of text editor search service's <see cref="ICommandTargetFilter"/> when search UI is visible
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_SEARCH_FOCUS = CMDTARGETFILTER_ORDER_TEXT_EDITOR - 1000000;

		/// <summary>
		/// Order of text editor search service's <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_SEARCH = CMDTARGETFILTER_ORDER_TEXT_EDITOR - 1000;

		/// <summary>
		/// Order of text references <see cref="ICommandInfoProvider"/>
		/// </summary>
		public const double CMDINFO_ORDER_TEXTREFERENCES = CMDINFO_ORDER_TEXT_EDITOR - 2000;

		/// <summary>
		/// Order of document viewer <see cref="ICommandInfoProvider"/>
		/// </summary>
		public const double CMDINFO_ORDER_DOCUMENTVIEWER = CMDINFO_ORDER_TEXT_EDITOR - 3000;

		/// <summary>
		/// Order of document viewer <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_DOCUMENTVIEWER = CMDTARGETFILTER_ORDER_TEXT_EDITOR - 3000;

		/// <summary>
		/// Order of REPL editor <see cref="ICommandInfoProvider"/>
		/// </summary>
		public const double CMDINFO_ORDER_REPL = CMDINFO_ORDER_TEXT_EDITOR - 3000;

		/// <summary>
		/// Order of REPL editor <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_REPL = CMDTARGETFILTER_ORDER_TEXT_EDITOR - 3000;

		/// <summary>
		/// Order of output logger text pane <see cref="ICommandInfoProvider"/>
		/// </summary>
		public const double CMDINFO_ORDER_OUTPUT_TEXTPANE = CMDINFO_ORDER_TEXT_EDITOR - 3000;

		/// <summary>
		/// Order of output logger text pane <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_OUTPUT_TEXTPANE = CMDTARGETFILTER_ORDER_TEXT_EDITOR - 3000;

		/// <summary>
		/// Order of edit code <see cref="ICommandInfoProvider"/>
		/// </summary>
		public const double CMDINFO_ORDER_EDITCODE = CMDTARGETFILTER_ORDER_TEXT_EDITOR - 3000;

		/// <summary>
		/// Order of edit code <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_EDITCODE = CMDTARGETFILTER_ORDER_TEXT_EDITOR - 3000;

		/// <summary>
		/// Order of intellisense session stack <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_SESSIONSTACK = CMDTARGETFILTER_ORDER_TEXT_EDITOR - 4000;

		/// <summary>
		/// Order of default statement completion <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_DEFAULT_STATEMENTCOMPLETION = CMDTARGETFILTER_ORDER_SESSIONSTACK - 1000;

		/// <summary>
		/// Order of Roslyn statement completion <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_ROSLYN_STATEMENTCOMPLETION = CMDTARGETFILTER_ORDER_DEFAULT_STATEMENTCOMPLETION - 1000;

		/// <summary>
		/// Order of Roslyn signature help <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_ROSLYN_SIGNATUREHELP = CMDTARGETFILTER_ORDER_ROSLYN_STATEMENTCOMPLETION - 1000;

		/// <summary>
		/// Order of Roslyn quick info <see cref="ICommandTargetFilter"/>
		/// </summary>
		public const double CMDTARGETFILTER_ORDER_ROSLYN_QUICKINFO = CMDTARGETFILTER_ORDER_ROSLYN_SIGNATUREHELP - 1000;
	}
}
