//
// MethodSpecification.cs
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

using Mono.Collections.Generic;

namespace Mono.Cecil {

	public abstract class MethodSpecification : MethodReference {

		readonly MethodReference method;

		public MethodReference ElementMethod {
			get { return method; }
		}

		public override string Name {
			get { return method.Name; }
			set { throw new InvalidOperationException (); }
		}

		public override MethodCallingConvention CallingConvention {
			get { return method.CallingConvention; }
			set { throw new InvalidOperationException (); }
		}

		public override bool HasThis {
			get { return method.HasThis; }
			set { throw new InvalidOperationException (); }
		}

		public override bool ExplicitThis {
			get { return method.ExplicitThis; }
			set { throw new InvalidOperationException (); }
		}

		public override MethodReturnType MethodReturnType {
			get { return method.MethodReturnType; }
			set { throw new InvalidOperationException (); }
		}

		public override TypeReference DeclaringType {
			get { return method.DeclaringType; }
			set { throw new InvalidOperationException (); }
		}

		public override ModuleDefinition Module {
			get { return method.Module; }
		}

		public override bool HasParameters {
			get { return method.HasParameters; }
		}

		public override Collection<ParameterDefinition> Parameters {
			get { return method.Parameters; }
		}

		internal override bool ContainsGenericParameter {
			get { return method.ContainsGenericParameter; }
		}

		internal MethodSpecification (MethodReference method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");

			this.method = method;
			this.token = new MetadataToken (TokenType.MethodSpec);
		}

		public sealed override MethodReference GetElementMethod ()
		{
			return method.GetElementMethod ();
		}
	}
}
