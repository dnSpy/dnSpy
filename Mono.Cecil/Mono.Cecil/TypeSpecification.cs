//
// TypeSpecification.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
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

using System;

using Mono.Cecil.Metadata;

namespace Mono.Cecil {

	public abstract class TypeSpecification : TypeReference {

		readonly TypeReference element_type;

		public TypeReference ElementType {
			get { return element_type; }
		}

		public override string Name {
			get { return element_type.Name; }
			set { throw new NotSupportedException (); }
		}

		public override string Namespace {
			get { return element_type.Namespace; }
			set { throw new NotSupportedException (); }
		}

		public override IMetadataScope Scope {
			get { return element_type.Scope; }
		}

		public override ModuleDefinition Module {
			get { return element_type.Module; }
		}

		public override string FullName {
			get { return element_type.FullName; }
		}

		internal override bool ContainsGenericParameter {
			get { return element_type.ContainsGenericParameter; }
		}

		public override MetadataType MetadataType {
			get { return (MetadataType) etype; }
		}

		internal TypeSpecification (TypeReference type)
			: base (null, null)
		{
			this.element_type = type;
			this.token = new MetadataToken (TokenType.TypeSpec);
		}

		public override TypeReference GetElementType ()
		{
			return element_type.GetElementType ();
		}
	}

	static partial class Mixin {

		public static void CheckType (TypeReference type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
		}
	}
}
