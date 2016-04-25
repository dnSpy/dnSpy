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

using System.Collections.Generic;
using dnSpy.Contracts.Plugin;
using dnSpy.Scripting.Roslyn.Properties;

namespace dnSpy.Scripting.Roslyn {
	[ExportPlugin]
	sealed class Plugin : IPlugin {
		Plugin() {
#if DEBUG
			// Make sure the checks in the cctors are always executed in debug builds
			System.Type t;
			t = GetType().Assembly.GetType("dnSpy.Scripting.Roslyn.VisualBasic.VisualBasicControlVM");
			//TODO: System.Diagnostics.Debug.Assert(t != null);
			if (t != null)
				System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);
			t = GetType().Assembly.GetType("dnSpy.Scripting.Roslyn.CSharp.CSharpControlVM");
			System.Diagnostics.Debug.Assert(t != null);
			if (t != null)
				System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);
#endif
		}

		public IEnumerable<string> MergedResourceDictionaries {
			get { yield break; }
		}

		public PluginInfo PluginInfo {
			get {
				return new PluginInfo {
					ShortDescription = dnSpy_Scripting_Roslyn_Resources.Plugin_ShortDescription,
				};
			}
		}

		public void OnEvent(PluginEvent @event, object obj) {
		}
	}
}
