// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Text;

using dnlib.DotNet;

namespace ICSharpCode.ILSpy {
	/// <summary>
	/// Interop with the .NET GAC.
	/// </summary>
	static class GacInterop {
		/// <summary>
		/// Gets the names of all assemblies in the GAC.
		/// </summary>
		public static IEnumerable<AssemblyNameInfo> GetGacAssemblyFullNames() {
			IApplicationContext applicationContext = null;
			IAssemblyEnum assemblyEnum = null;
			IAssemblyName assemblyName = null;

			Fusion.CreateAssemblyEnum(out assemblyEnum, null, null, 2, 0);
			while (assemblyEnum.GetNextAssembly(out applicationContext, out assemblyName, 0) == 0) {
				uint nChars = 0;
				assemblyName.GetDisplayName(null, ref nChars, 0);

				StringBuilder name = new StringBuilder((int)nChars);
				assemblyName.GetDisplayName(name, ref nChars, 0);

				var r = new AssemblyNameInfo(name.ToString());
				if (r.Version != null)
					yield return r;
			}
		}
	}
}
