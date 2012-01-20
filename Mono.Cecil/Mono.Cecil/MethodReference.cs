//
// MethodReference.cs
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

	public class MethodReference : MemberReference, IMethodSignature, IGenericParameterProvider, IGenericContext {

		internal ParameterDefinitionCollection parameters;
		MethodReturnType return_type;

		bool has_this;
		bool explicit_this;
		MethodCallingConvention calling_convention;
		internal Collection<GenericParameter> generic_parameters;

		public virtual bool HasThis {
			get { return has_this; }
			set { has_this = value; }
		}

		public virtual bool ExplicitThis {
			get { return explicit_this; }
			set { explicit_this = value; }
		}

		public virtual MethodCallingConvention CallingConvention {
			get { return calling_convention; }
			set { calling_convention = value; }
		}

		public virtual bool HasParameters {
			get { return !parameters.IsNullOrEmpty (); }
		}

		public virtual Collection<ParameterDefinition> Parameters {
			get {
				if (parameters == null)
					parameters = new ParameterDefinitionCollection (this);

				return parameters;
			}
		}

		IGenericParameterProvider IGenericContext.Type {
			get {
				var declaring_type = this.DeclaringType;
				var instance = declaring_type as GenericInstanceType;
				if (instance != null)
					return instance.ElementType;

				return declaring_type;
			}
		}

		IGenericParameterProvider IGenericContext.Method {
			get { return this; }
		}

		GenericParameterType IGenericParameterProvider.GenericParameterType {
			get { return GenericParameterType.Method; }
		}

		public virtual bool HasGenericParameters {
			get { return !generic_parameters.IsNullOrEmpty (); }
		}

		public virtual Collection<GenericParameter> GenericParameters {
			get {
				if (generic_parameters != null)
					return generic_parameters;

				return generic_parameters = new GenericParameterCollection (this);
			}
		}

		public TypeReference ReturnType {
			get {
				var return_type = MethodReturnType;
				return return_type != null ? return_type.ReturnType : null;
			}
			set {
				var return_type = MethodReturnType;
				if (return_type != null)
					return_type.ReturnType = value;
			}
		}

		public virtual MethodReturnType MethodReturnType {
			get { return return_type; }
			set { return_type = value; }
		}

		public override string FullName {
			get {
				var builder = new StringBuilder ();
				builder.Append (ReturnType.FullName)
					.Append (" ")
					.Append (MemberFullName ());
				this.MethodSignatureFullName (builder);
				return builder.ToString ();
			}
		}

		public virtual bool IsGenericInstance {
			get { return false; }
		}

		internal override bool ContainsGenericParameter {
			get {
				if (this.ReturnType.ContainsGenericParameter || base.ContainsGenericParameter)
					return true;

				var parameters = this.Parameters;

				for (int i = 0; i < parameters.Count; i++)
					if (parameters [i].ParameterType.ContainsGenericParameter)
						return true;

				return false;
			}
		}

		internal MethodReference ()
		{
			this.return_type = new MethodReturnType (this);
			this.token = new MetadataToken (TokenType.MemberRef);
		}

		public MethodReference (string name, TypeReference returnType)
			: base (name)
		{
			if (returnType == null)
				throw new ArgumentNullException ("returnType");

			this.return_type = new MethodReturnType (this);
			this.return_type.ReturnType = returnType;
			this.token = new MetadataToken (TokenType.MemberRef);
		}

		public MethodReference (string name, TypeReference returnType, TypeReference declaringType)
			: this (name, returnType)
		{
			if (declaringType == null)
				throw new ArgumentNullException ("declaringType");

			this.DeclaringType = declaringType;
		}

		public virtual MethodReference GetElementMethod ()
		{
			return this;
		}

		public virtual MethodDefinition Resolve ()
		{
			var module = this.Module;
			if (module == null)
				throw new NotSupportedException ();

			return module.Resolve (this);
		}
	}

	static partial class Mixin {

		public static bool IsVarArg (this IMethodSignature self)
		{
			return (self.CallingConvention & MethodCallingConvention.VarArg) != 0;
		}

		public static int GetSentinelPosition (this IMethodSignature self)
		{
			if (!self.HasParameters)
				return -1;

			var parameters = self.Parameters;
			for (int i = 0; i < parameters.Count; i++)
				if (parameters [i].ParameterType.IsSentinel)
					return i;

			return -1;
		}
	}
}
