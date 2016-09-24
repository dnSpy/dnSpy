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
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	static class SpaceReservationManagerDefinitions {
#pragma warning disable 0169
		[Export(typeof(SpaceReservationManagerDefinition))]
		[Name(PredefinedSpaceReservationManagerNames.CurrentLine)]
		[Order(Before = IntellisenseSpaceReservationManagerNames.SmartTagSpaceReservationManagerName)]
		static readonly SpaceReservationManagerDefinition currentLineSpaceReservationManagerDefinition;

		[Export(typeof(SpaceReservationManagerDefinition))]
		[Name(IntellisenseSpaceReservationManagerNames.SmartTagSpaceReservationManagerName)]
		[Order(Before = IntellisenseSpaceReservationManagerNames.QuickInfoSpaceReservationManagerName, After = PredefinedSpaceReservationManagerNames.CurrentLine)]
		static readonly SpaceReservationManagerDefinition smartTagSpaceReservationManagerDefinition;

		[Export(typeof(SpaceReservationManagerDefinition))]
		[Name(IntellisenseSpaceReservationManagerNames.QuickInfoSpaceReservationManagerName)]
		[Order(Before = IntellisenseSpaceReservationManagerNames.SignatureHelpSpaceReservationManagerName, After = IntellisenseSpaceReservationManagerNames.SmartTagSpaceReservationManagerName)]
		static readonly SpaceReservationManagerDefinition quickInfoSpaceReservationManagerDefinition;

		[Export(typeof(SpaceReservationManagerDefinition))]
		[Name(IntellisenseSpaceReservationManagerNames.SignatureHelpSpaceReservationManagerName)]
		[Order(Before = IntellisenseSpaceReservationManagerNames.CompletionSpaceReservationManagerName, After = IntellisenseSpaceReservationManagerNames.QuickInfoSpaceReservationManagerName)]
		static readonly SpaceReservationManagerDefinition signatureHelpSpaceReservationManagerDefinition;

		[Export(typeof(SpaceReservationManagerDefinition))]
		[Name(IntellisenseSpaceReservationManagerNames.CompletionSpaceReservationManagerName)]
		[Order(After = IntellisenseSpaceReservationManagerNames.SignatureHelpSpaceReservationManagerName)]
		static readonly SpaceReservationManagerDefinition completionSpaceReservationManagerDefinition;
#pragma warning restore 0169
	}
}
