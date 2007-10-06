//
// CLIHeader.cs
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

	public sealed class CLIHeader : IHeader, IBinaryVisitable {

		public uint Cb;
		public ushort MajorRuntimeVersion;
		public ushort MinorRuntimeVersion;
		public DataDirectory Metadata;
		public RuntimeImage Flags;
		public uint EntryPointToken;
		public DataDirectory Resources;
		public DataDirectory StrongNameSignature;
		public DataDirectory CodeManagerTable;
		public DataDirectory VTableFixups;
		public DataDirectory ExportAddressTableJumps;
		public DataDirectory ManagedNativeHeader;

		public byte [] ImageHash;

		internal CLIHeader ()
		{
		}

		public void SetDefaultValues ()
		{
			Cb = 0x48;
			MajorRuntimeVersion = 2;
			MinorRuntimeVersion = 0;
			Flags = RuntimeImage.ILOnly;
			CodeManagerTable = DataDirectory.Zero;
			ExportAddressTableJumps = DataDirectory.Zero;
			ManagedNativeHeader = DataDirectory.Zero;
		}

		public void Accept (IBinaryVisitor visitor)
		{
			visitor.VisitCLIHeader (this);
		}
	}
}
