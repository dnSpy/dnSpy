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

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using dnSpy.Roslyn.Internal.SignatureHelp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace dnSpy.Roslyn.Shared.Text {
	/// <summary>
	/// Shared <see cref="MefHostServices"/> instance
	/// </summary>
	public sealed class RoslynMefHostServices {
		/// <summary>
		/// Gets the shared <see cref="MefHostServices"/> instance
		/// </summary>
		public static MefHostServices DefaultServices {
			get {
				if (defaultServices == null)
					Interlocked.CompareExchange(ref defaultServices, CreateDefaultServices(), null);
				return defaultServices;
			}
		}
		static MefHostServices defaultServices;

		static MefHostServices CreateDefaultServices() {
			var asms = new HashSet<Assembly>(MefHostServices.DefaultAssemblies);
			var version = typeof(Compilation).Assembly.GetName().Version;
			foreach (var asmNameTmp in otherAssemblies) {
				var asmName = string.Format(asmNameTmp, version);
				try {
					asms.Add(Assembly.Load(asmName));
				}
				catch {
					Debug.Fail($"Couldn't load Roslyn MEF assembly: {asmName}");
				}
			}
			// dnSpy.Roslyn.Internal exports some stuff too
			asms.Add(typeof(SignatureHelpService).Assembly);
			asms.Add(typeof(Microsoft.CodeAnalysis.Editor.VisualBasic.QuickInfo.SemanticQuickInfoProvider).Assembly);
			return MefHostServices.Create(asms);
		}
		static readonly string[] otherAssemblies = new string[] {
			// Don't include Microsoft.CodeAnalysis.Workspaces.Desktop, it contains refs to Microsoft.Build
			// and other files that aren't included.
			// "Microsoft.CodeAnalysis.Workspaces.Desktop, Version={0}, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"Microsoft.CodeAnalysis.Features, Version={0}, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"Microsoft.CodeAnalysis.CSharp.Features, Version={0}, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"Microsoft.CodeAnalysis.VisualBasic.Features, Version={0}, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
		};
	}
}
