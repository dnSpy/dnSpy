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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Interop with the .NET GAC.
	/// </summary>
	static class GacInterop
	{
		/// <summary>
		/// Gets the names of all assemblies in the GAC.
		/// </summary>
		public static IEnumerable<AssemblyNameReference> GetGacAssemblyFullNames()
		{
			IApplicationContext applicationContext = null;
			IAssemblyEnum assemblyEnum = null;
			IAssemblyName assemblyName = null;
			
			Fusion.CreateAssemblyEnum(out assemblyEnum, null, null, 2, 0);
			while (assemblyEnum.GetNextAssembly(out applicationContext, out assemblyName, 0) == 0) {
				uint nChars = 0;
				assemblyName.GetDisplayName(null, ref nChars, 0);
				
				StringBuilder name = new StringBuilder((int)nChars);
				assemblyName.GetDisplayName(name, ref nChars, 0);
				
				AssemblyNameReference r = null;
				try {
					r = AssemblyNameReference.Parse(name.ToString());
				} catch (ArgumentException) {
				} catch (FormatException) {
				} catch (OverflowException) {
				}
				if (r != null)
					yield return r;
			}
		}
		
		#region FindAssemblyInGac
		// This region is based on code from Mono.Cecil:
		
		// Author:
		//   Jb Evain (jbevain@gmail.com)
		//
		// Copyright (c) 2008 - 2010 Jb Evain
		//
		// Permission is hereby granted, free of charge, to any person obtaining
		// a copy of this software and associated documentation files (the
		// "Software"), to deal in the Software without restriction, including
		// without limitation the rights to use, copy, modify, merge, publish,
		// distribute, sublicense, and/or sell copies of the Software, and to
		// permit persons to whom the Software is furnished to do so, subject to
		// the following conditions:
		//
		// The above copyright notice and this permission notice shall be
		// included in all copies or substantial portions of the Software.
		//
		// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
		// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
		// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
		// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
		// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
		// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
		// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
		//

		static readonly string[] gac_paths = { Fusion.GetGacPath(false), Fusion.GetGacPath(true) };
		static readonly string[] gacs = { "GAC_MSIL", "GAC_32", "GAC" };
		static readonly string[] prefixes = { string.Empty, "v4.0_" };
		
		/// <summary>
		/// Gets the file name for an assembly stored in the GAC.
		/// </summary>
		public static string FindAssemblyInNetGac (AssemblyNameReference reference)
		{
			// without public key, it can't be in the GAC
			if (reference.PublicKeyToken == null)
				return null;
			
			for (int i = 0; i < 2; i++) {
				for (int j = 0; j < gacs.Length; j++) {
					var gac = Path.Combine (gac_paths [i], gacs [j]);
					var file = GetAssemblyFile (reference, prefixes [i], gac);
					if (File.Exists (file))
						return file;
				}
			}

			return null;
		}
		
		static string GetAssemblyFile (AssemblyNameReference reference, string prefix, string gac)
		{
			var gac_folder = new StringBuilder ()
				.Append (prefix)
				.Append (reference.Version)
				.Append ("__");

			for (int i = 0; i < reference.PublicKeyToken.Length; i++)
				gac_folder.Append (reference.PublicKeyToken [i].ToString ("x2"));

			return Path.Combine (
				Path.Combine (
					Path.Combine (gac, reference.Name), gac_folder.ToString ()),
				reference.Name + ".dll");
		}
		#endregion
	}
}
