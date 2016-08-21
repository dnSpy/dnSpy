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
using dnSpy.Contracts.Settings;
using dnSpy.Scripting.Roslyn.Common;

namespace dnSpy.Scripting.Roslyn.CSharp {
	[Export]
	sealed class CSharpReplSettingsImpl : ReplSettingsImplBase {
		static readonly Guid SETTINGS_GUID = new Guid("13C22A09-E143-430E-B2D9-91DA7B2A5388");

		[ImportingConstructor]
		CSharpReplSettingsImpl(ISettingsManager settingsManager)
			: base(SETTINGS_GUID, settingsManager) {
		}
	}
}
