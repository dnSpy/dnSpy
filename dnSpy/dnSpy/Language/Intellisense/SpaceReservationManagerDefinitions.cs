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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	static class SpaceReservationManagerDefinitions {
#pragma warning disable 0169
		[Export(typeof(SpaceReservationManagerDefinition))]
		[Name(PredefinedSpaceReservationManagerNames.CurrentLine)]
		[Order(Before = PredefinedSpaceReservationManagerNames.SmartTag)]
		static readonly SpaceReservationManagerDefinition currentLineSpaceReservationManagerDefinition;

		[Export(typeof(SpaceReservationManagerDefinition))]
		[Name(PredefinedSpaceReservationManagerNames.SmartTag)]
		[Order(Before = PredefinedSpaceReservationManagerNames.QuickInfo, After = PredefinedSpaceReservationManagerNames.CurrentLine)]
		static readonly SpaceReservationManagerDefinition smartTagSpaceReservationManagerDefinition;

		[Export(typeof(SpaceReservationManagerDefinition))]
		[Name(PredefinedSpaceReservationManagerNames.QuickInfo)]
		[Order(Before = PredefinedSpaceReservationManagerNames.SignatureHelp, After = PredefinedSpaceReservationManagerNames.SmartTag)]
		static readonly SpaceReservationManagerDefinition quickInfoSpaceReservationManagerDefinition;

		[Export(typeof(SpaceReservationManagerDefinition))]
		[Name(PredefinedSpaceReservationManagerNames.SignatureHelp)]
		[Order(Before = PredefinedSpaceReservationManagerNames.Completion, After = PredefinedSpaceReservationManagerNames.QuickInfo)]
		static readonly SpaceReservationManagerDefinition signatureHelpSpaceReservationManagerDefinition;

		[Export(typeof(SpaceReservationManagerDefinition))]
		[Name(PredefinedSpaceReservationManagerNames.Completion)]
		[Order(After = PredefinedSpaceReservationManagerNames.SignatureHelp)]
		static readonly SpaceReservationManagerDefinition completionSpaceReservationManagerDefinition;
#pragma warning restore 0169
	}
}
