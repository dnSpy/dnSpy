//
// FieldReference.cs
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

namespace Mono.Cecil {

	public class FieldReference : MemberReference {

		TypeReference field_type;

		public TypeReference FieldType {
			get { return field_type; }
			set { field_type = value; }
		}

		public override string FullName {
			get { return field_type.FullName + " " + MemberFullName (); }
		}

		internal override bool ContainsGenericParameter {
			get { return field_type.ContainsGenericParameter || base.ContainsGenericParameter; }
		}

		internal FieldReference ()
		{
			this.token = new MetadataToken (TokenType.MemberRef);
		}

		public FieldReference (string name, TypeReference fieldType)
			: base (name)
		{
			if (fieldType == null)
				throw new ArgumentNullException ("fieldType");

			this.field_type = fieldType;
			this.token = new MetadataToken (TokenType.MemberRef);
		}

		public FieldReference (string name, TypeReference fieldType, TypeReference declaringType)
			: this (name, fieldType)
		{
			if (declaringType == null)
				throw new ArgumentNullException("declaringType");

			this.DeclaringType = declaringType;
		}

		public virtual FieldDefinition Resolve ()
		{
			var module = this.Module;
			if (module == null)
				throw new NotSupportedException ();

			return module.Resolve (this);
		}
	}
}
