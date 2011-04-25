//
// GenericInstanceType.cs
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
using System.Text;

using Mono.Collections.Generic;

using MD = Mono.Cecil.Metadata;

namespace Mono.Cecil {

	public sealed class GenericInstanceType : TypeSpecification, IGenericInstance, IGenericContext {

		Collection<TypeReference> arguments;

		public bool HasGenericArguments {
			get { return !arguments.IsNullOrEmpty (); }
		}

		public Collection<TypeReference> GenericArguments {
			get { return arguments ?? (arguments = new Collection<TypeReference> ()); }
		}

		public override TypeReference DeclaringType {
			get { return ElementType.DeclaringType; }
			set { throw new NotSupportedException (); }
		}

		public override string FullName {
			get {
				var name = new StringBuilder ();
				name.Append (base.FullName);
				this.GenericInstanceFullName (name);
				return name.ToString ();
			}
		}

		public override bool IsGenericInstance {
			get { return true; }
		}

		internal override bool ContainsGenericParameter {
			get { return this.ContainsGenericParameter () || base.ContainsGenericParameter; }
		}

		IGenericParameterProvider IGenericContext.Type {
			get { return ElementType; }
		}

		public GenericInstanceType (TypeReference type)
			: base (type)
		{
			base.IsValueType = type.IsValueType;
			this.etype = MD.ElementType.GenericInst;
		}
	}
}
