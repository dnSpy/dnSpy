//
// SectionCharacteristics.cs
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

	using System;

	[Flags]
	public enum SectionCharacteristics : uint {
		TypeNoPad = 0x0000008,
		ContainsCode = 0x00000020,
		ContainsInitializedData = 0x00000040,
		ContainsUninitializedData = 0x00000080,
		LnkOther = 0x0000100,
		LnkInfo = 0x000200,
		LnkRemove = 0x0000800,
		LnkCOMDAT = 0x00001000,
		GPRel = 0x00008000,
		MemPurgeable = 0x00020000,
		MemLocked = 0x00040000,
		MemPreload = 0x00080000,
		Align1Bytes = 0x00100000,
		Align2Bytes = 0x00200000,
		Align4Bytes = 0x00300000,
		Align8Bytes = 0x00400000,
		Align16Bytes = 0x00500000,
		Align32Bytes = 0x00600000,
		Align64Bytes = 0x00700000,
		Align128Bytes = 0x00800000,
		Align256Bytes = 0x00900000,
		Align512Bytes = 0x00a00000,
		Align1024Bytes = 0x00b00000,
		Align2048Bytes = 0x00c00000,
		Align4096Bytes = 0x00d00000,
		Align8192Bytes = 0x00e00000,
		LnkNRelocOvfl = 0x01000000,
		MemDiscardable = 0x02000000,
		MemNotCached = 0x04000000,
		MemNotPaged = 0x08000000,
		MemShared = 0x10000000,
		MemExecute = 0x20000000,
		MemoryRead = 0x40000000,
		MemoryWrite = 0x80000000
	}
}
