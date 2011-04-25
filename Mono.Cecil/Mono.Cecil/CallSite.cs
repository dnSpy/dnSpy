//
// CallSite.cs
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

namespace Mono.Cecil {

	public sealed class CallSite : IMethodSignature {

		readonly MethodReference signature;

		public bool HasThis {
			get { return signature.HasThis; }
			set { signature.HasThis = value; }
		}

		public bool ExplicitThis {
			get { return signature.ExplicitThis; }
			set { signature.ExplicitThis = value; }
		}

		public MethodCallingConvention CallingConvention {
			get { return signature.CallingConvention; }
			set { signature.CallingConvention = value; }
		}

		public bool HasParameters {
			get { return signature.HasParameters; }
		}

		public Collection<ParameterDefinition> Parameters {
			get { return signature.Parameters; }
		}

		public TypeReference ReturnType {
			get { return signature.MethodReturnType.ReturnType; }
			set { signature.MethodReturnType.ReturnType = value; }
		}

		public MethodReturnType MethodReturnType {
			get { return signature.MethodReturnType; }
		}

		public string Name {
			get { return string.Empty; }
			set { throw new InvalidOperationException (); }
		}

		public string Namespace {
			get { return string.Empty; }
			set { throw new InvalidOperationException (); }
		}

		public ModuleDefinition Module {
			get { return ReturnType.Module; }
		}

		public IMetadataScope Scope {
			get { return signature.ReturnType.Scope; }
		}

		public MetadataToken MetadataToken {
			get { return signature.token; }
			set { signature.token = value; }
		}

		public string FullName {
			get {
				var signature = new StringBuilder ();
				signature.Append (ReturnType.FullName);
				this.MethodSignatureFullName (signature);
				return signature.ToString ();
			}
		}

		internal CallSite ()
		{
			this.signature = new MethodReference ();
			this.signature.token = new MetadataToken (TokenType.Signature, 0);
		}

		public CallSite (TypeReference returnType)
			: this ()
		{
			if (returnType == null)
				throw new ArgumentNullException ("returnType");

			this.signature.ReturnType = returnType;
		}

		public override string ToString ()
		{
			return FullName;
		}
	}
}
