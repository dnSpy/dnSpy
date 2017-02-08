/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Contracts.Settings.Repl;
using dnSpy.Contracts.Text;

namespace dnSpy.Scripting.Roslyn.CSharp {
	static class ReplOptionsDefinitions {
#pragma warning disable 0169
		[ExportReplOptionsDefinition("C#", ContentTypes.ReplCSharpRoslyn, AppSettingsConstants.GUID_REPL_CSHARP_ROSLYN)]
		static readonly ReplOptionsDefinition csharpReplOptionsDefinition;
#pragma warning restore 0169
	}
}
