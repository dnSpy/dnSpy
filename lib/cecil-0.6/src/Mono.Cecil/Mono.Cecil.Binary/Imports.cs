//
// Imports.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
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

namespace Mono.Cecil.Binary {

	public sealed class ImportAddressTable : IBinaryVisitable {

		public RVA HintNameTableRVA;

		internal ImportAddressTable ()
		{
		}

		public void Accept (IBinaryVisitor visitor)
		{
			visitor.VisitImportAddressTable (this);
		}
	}

	public sealed class ImportTable : IBinaryVisitable {

		public RVA ImportLookupTable;
		public uint DateTimeStamp;
		public uint ForwardChain;
		public RVA Name;
		public RVA ImportAddressTable;

		internal ImportTable ()
		{
		}

		public void Accept (IBinaryVisitor visitor)
		{
			visitor.VisitImportTable (this);
		}
	}

	public sealed class ImportLookupTable : IBinaryVisitable {

		public RVA HintNameRVA;

		internal ImportLookupTable ()
		{
		}

		public void Accept (IBinaryVisitor visitor)
		{
			visitor.VisitImportLookupTable (this);
		}
	}

	public sealed class HintNameTable : IBinaryVisitable {

		public const string RuntimeMainExe = "_CorExeMain";
		public const string RuntimeMainDll = "_CorDllMain";
		public const string RuntimeCorEE = "mscoree.dll";

		public ushort Hint;
		public string RuntimeMain;
		public string RuntimeLibrary;
		public ushort EntryPoint;
		public RVA RVA;

		internal HintNameTable ()
		{
		}

		public void Accept (IBinaryVisitor visitor)
		{
			visitor.VisitHintNameTable (this);
		}
	}
}
