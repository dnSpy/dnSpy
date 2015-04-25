// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// References an existing assembly by name.
	/// </summary>
	[Serializable]
	public sealed class DefaultAssemblyReference : IAssemblyReference, ISupportsInterning
	{
		public static readonly IAssemblyReference CurrentAssembly = new CurrentAssemblyReference();
		
		[Obsolete("The corlib is not always called 'mscorlib' (as returned by this property), but might be 'System.Runtime'.")]
		public static readonly IAssemblyReference Corlib = new DefaultAssemblyReference("mscorlib");
		
		readonly string shortName;
		
		public DefaultAssemblyReference(string assemblyName)
		{
			int pos = assemblyName != null ? assemblyName.IndexOf(',') : -1;
			if (pos >= 0)
				shortName = assemblyName.Substring(0, pos);
			else
				shortName = assemblyName;
		}
		
		public IAssembly Resolve(ITypeResolveContext context)
		{
			IAssembly current = context.CurrentAssembly;
			if (current != null && string.Equals(shortName, current.AssemblyName, StringComparison.OrdinalIgnoreCase))
				return current;
			foreach (IAssembly asm in context.Compilation.Assemblies) {
				if (string.Equals(shortName, asm.AssemblyName, StringComparison.OrdinalIgnoreCase))
					return asm;
			}
			return null;
		}
		
		public override string ToString()
		{
			return shortName;
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				return shortName.GetHashCode();
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			DefaultAssemblyReference o = other as DefaultAssemblyReference;
			return o != null && shortName == o.shortName;
		}
		
		[Serializable]
		sealed class CurrentAssemblyReference : IAssemblyReference
		{
			public IAssembly Resolve(ITypeResolveContext context)
			{
				IAssembly asm = context.CurrentAssembly;
				if (asm == null)
					throw new ArgumentException("A reference to the current assembly cannot be resolved in the compilation's global type resolve context.");
				return asm;
			}
		}
	}
}
