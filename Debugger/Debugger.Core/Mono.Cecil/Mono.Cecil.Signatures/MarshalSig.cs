//
// MarshalSig.cs
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

namespace Mono.Cecil.Signatures {

	using System;

	using Mono.Cecil;

	internal sealed class MarshalSig {

		public NativeType NativeInstrinsic;
		public IMarshalSigSpec Spec;

		public MarshalSig (NativeType nt)
		{
			this.NativeInstrinsic = nt;
		}

		public interface IMarshalSigSpec {
		}

		public sealed class Array : IMarshalSigSpec {

			public NativeType ArrayElemType;
			public int ParamNum;
			public int ElemMult;
			public int NumElem;

			public Array ()
			{
				this.ParamNum = 0;
				this.ElemMult = 0;
				this.NumElem = 0;
			}
		}

		public sealed class CustomMarshaler : IMarshalSigSpec {

			public string Guid;
			public string UnmanagedType;
			public string ManagedType;
			public string Cookie;
		}

		public sealed class FixedArray : IMarshalSigSpec {

			public int NumElem;
			public NativeType ArrayElemType;

			public FixedArray ()
			{
				this.NumElem = 0;
				this.ArrayElemType = NativeType.NONE;
			}
		}

		public sealed class SafeArray : IMarshalSigSpec {

			public VariantType ArrayElemType;
		}

		public sealed class FixedSysString : IMarshalSigSpec {

			public int Size;
		}
	}
}
