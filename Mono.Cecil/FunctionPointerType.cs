//
// FunctionPointerType.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2010 Jb Evain
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

	public sealed class FunctionPointerType : TypeSpecification, IMethodSignature {

		readonly MethodReference function;

		public bool HasThis {
			get { return function.HasThis; }
			set { function.HasThis = value; }
		}

		public bool ExplicitThis {
			get { return function.ExplicitThis; }
			set { function.ExplicitThis = value; }
		}

		public MethodCallingConvention CallingConvention {
			get { return function.CallingConvention; }
			set { function.CallingConvention = value; }
		}

		public bool HasParameters {
			get { return function.HasParameters; }
		}

		public Collection<ParameterDefinition> Parameters {
			get { return function.Parameters; }
		}

		public TypeReference ReturnType {
			get { return function.MethodReturnType.ReturnType; }
			set { function.MethodReturnType.ReturnType = value; }
		}

		public MethodReturnType MethodReturnType {
			get { return function.MethodReturnType; }
		}

		public override string Name {
			get { return function.Name; }
			set { throw new InvalidOperationException (); }
		}

		public override string Namespace {
			get { return string.Empty; }
			set { throw new InvalidOperationException (); }
		}

		public override ModuleDefinition Module {
			get { return ReturnType.Module; }
		}

		public override IMetadataScope Scope {
			get { return function.ReturnType.Scope; }
		}

		public override bool IsFunctionPointer {
			get { return true; }
		}

		internal override bool ContainsGenericParameter {
			get { return function.ContainsGenericParameter; }
		}

		public override string FullName {
			get {
				var signature = new StringBuilder ();
				signature.Append (function.Name);
				signature.Append (" ");
				signature.Append (function.ReturnType.FullName);
				signature.Append (" *");
				this.MethodSignatureFullName (signature);
				return signature.ToString ();
			}
		}

		public FunctionPointerType ()
			: base (null)
		{
			this.function = new MethodReference ();
			this.function.Name = "method";
			this.etype = MD.ElementType.FnPtr;
		}

		public override TypeDefinition Resolve ()
		{
			return null;
		}

		public override TypeReference GetElementType ()
		{
			return this;
		}
	}
}
