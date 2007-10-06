//
// PInvokeInfo.cs
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

namespace Mono.Cecil {

	public sealed class PInvokeInfo : IReflectionVisitable {

		MethodDefinition m_meth;

		PInvokeAttributes m_attributes;
		string m_entryPoint;
		ModuleReference m_module;

		public MethodDefinition Method {
			get { return m_meth; }
		}

		public PInvokeAttributes Attributes {
			get { return m_attributes; }
			set { m_attributes = value; }
		}

		public string EntryPoint {
			get { return m_entryPoint; }
			set { m_entryPoint = value; }
		}

		public ModuleReference Module {
			get { return m_module; }
			set { m_module = value; }
		}

		#region PInvokeAttributes

		public bool IsNoMangle {
			get { return (m_attributes & PInvokeAttributes.NoMangle) != 0; }
			set {
				if (value)
					m_attributes |= PInvokeAttributes.NoMangle;
				else
					m_attributes &= ~PInvokeAttributes.NoMangle;
			}
		}

		public bool IsCharSetNotSpec {
			get { return (m_attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetNotSpec; }
			set {
				if (value) {
					m_attributes &= ~PInvokeAttributes.CharSetMask;
					m_attributes |= PInvokeAttributes.CharSetNotSpec;
				} else
					m_attributes &= ~(PInvokeAttributes.CharSetMask & PInvokeAttributes.CharSetNotSpec);
			}
		}

		public bool IsCharSetAnsi {
			get { return (m_attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetAnsi; }
			set {
				if (value) {
					m_attributes &= ~PInvokeAttributes.CharSetMask;
					m_attributes |= PInvokeAttributes.CharSetAnsi;
				} else
					m_attributes &= ~(PInvokeAttributes.CharSetMask & PInvokeAttributes.CharSetAnsi);
			}
		}

		public bool IsCharSetUnicode {
			get { return (m_attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetUnicode; }
			set {
				if (value) {
					m_attributes &= ~PInvokeAttributes.CharSetMask;
					m_attributes |= PInvokeAttributes.CharSetUnicode;
				} else
					m_attributes &= ~(PInvokeAttributes.CharSetMask & PInvokeAttributes.CharSetUnicode);
			}
		}

		public bool IsCharSetAuto {
			get { return (m_attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetAuto; }
			set {
				if (value) {
					m_attributes &= ~PInvokeAttributes.CharSetMask;
					m_attributes |= PInvokeAttributes.CharSetAuto;
				} else
					m_attributes &= ~(PInvokeAttributes.CharSetMask & PInvokeAttributes.CharSetAuto);
			}
		}

		public bool SupportsLastError {
			get { return (m_attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.SupportsLastError; }
			set {
				if (value) {
					m_attributes &= ~PInvokeAttributes.CharSetMask;
					m_attributes |= PInvokeAttributes.SupportsLastError;
				} else
					m_attributes &= ~(PInvokeAttributes.CharSetMask & PInvokeAttributes.SupportsLastError);
			}
		}

		public bool IsCallConvWinapi {
			get { return (m_attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvWinapi; }
			set {
				if (value) {
					m_attributes &= ~PInvokeAttributes.CallConvMask;
					m_attributes |= PInvokeAttributes.CallConvWinapi;
				} else
					m_attributes &= ~(PInvokeAttributes.CallConvMask & PInvokeAttributes.CallConvWinapi);
			}
		}

		public bool IsCallConvCdecl {
			get { return (m_attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvCdecl; }
			set {
				if (value) {
					m_attributes &= ~PInvokeAttributes.CallConvMask;
					m_attributes |= PInvokeAttributes.CallConvCdecl;
				} else
					m_attributes &= ~(PInvokeAttributes.CallConvMask & PInvokeAttributes.CallConvCdecl);
			}
		}

		public bool IsCallConvStdCall {
			get { return (m_attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvStdCall; }
			set {
				if (value) {
					m_attributes &= ~PInvokeAttributes.CallConvMask;
					m_attributes |= PInvokeAttributes.CallConvStdCall;
				} else
					m_attributes &= ~(PInvokeAttributes.CallConvMask & PInvokeAttributes.CallConvStdCall);
			}
		}

		public bool IsCallConvThiscall {
			get { return (m_attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvThiscall; }
			set {
				if (value) {
					m_attributes &= ~PInvokeAttributes.CallConvMask;
					m_attributes |= PInvokeAttributes.CallConvThiscall;
				} else
					m_attributes &= ~(PInvokeAttributes.CallConvMask & PInvokeAttributes.CallConvThiscall);
			}
		}

		public bool IsCallConvFastcall {
			get { return (m_attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvFastcall; }
			set {
				if (value) {
					m_attributes &= ~PInvokeAttributes.CallConvMask;
					m_attributes |= PInvokeAttributes.CallConvFastcall;
				} else
					m_attributes &= ~(PInvokeAttributes.CallConvMask & PInvokeAttributes.CallConvFastcall);
			}
		}

		#endregion

		public PInvokeInfo (MethodDefinition meth)
		{
			m_meth = meth;
		}

		public PInvokeInfo (MethodDefinition meth, PInvokeAttributes attrs,
			string entryPoint, ModuleReference mod) : this (meth)
		{
			m_attributes = attrs;
			m_entryPoint = entryPoint;
			m_module = mod;
		}

		public void Accept (IReflectionVisitor visitor)
		{
			visitor.VisitPInvokeInfo (this);
		}
	}
}
