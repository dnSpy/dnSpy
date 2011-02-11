//
// CustomAttrib.cs
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

	using Mono.Cecil.Metadata;

	internal sealed class CustomAttrib {

		public const ushort StdProlog = 0x0001;

		public MethodReference Constructor;

		public ushort Prolog;
		public FixedArg [] FixedArgs;
		public ushort NumNamed;
		public NamedArg [] NamedArgs;
		public bool Read;

		public CustomAttrib (MethodReference ctor)
		{
			Constructor = ctor;
		}

		public struct FixedArg {

			public bool SzArray;
			public uint NumElem;
			public Elem [] Elems;
		}

		public struct Elem {

			public bool Simple;
			public bool String;
			public bool Type;
			public bool BoxedValueType;

			public ElementType FieldOrPropType;
			public object Value;

			public TypeReference ElemType;
		}

		public struct NamedArg {

			public bool Field;
			public bool Property;

			public ElementType FieldOrPropType;
			public string FieldOrPropName;
			public FixedArg FixedArg;
		}
	}
}
