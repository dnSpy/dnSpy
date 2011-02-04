//
// MethodReturnType.cs
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

using Mono.Collections.Generic;

namespace Mono.Cecil {

	public sealed class MethodReturnType : IConstantProvider, ICustomAttributeProvider, IMarshalInfoProvider {

		internal IMethodSignature method;
		internal ParameterDefinition parameter;
		TypeReference return_type;

		public IMethodSignature Method {
			get { return method; }
		}

		public TypeReference ReturnType {
			get { return return_type; }
			set { return_type = value; }
		}

		internal ParameterDefinition Parameter {
			get { return parameter ?? (parameter = new ParameterDefinition (return_type)); }
			set { parameter = value; }
		}

		public MetadataToken MetadataToken {
			get { return Parameter.MetadataToken; }
			set { Parameter.MetadataToken = value; }
		}

		public bool HasCustomAttributes {
			get { return parameter != null && parameter.HasCustomAttributes; }
		}

		public Collection<CustomAttribute> CustomAttributes {
			get { return Parameter.CustomAttributes; }
		}

		public bool HasDefault {
			get { return parameter != null && parameter.HasDefault; }
			set { Parameter.HasDefault = value; }
		}

		public bool HasConstant {
			get { return parameter != null && parameter.HasConstant; }
			set { Parameter.HasConstant = value; }
		}

		public object Constant {
			get { return Parameter.Constant; }
			set { Parameter.Constant = value; }
		}

		public bool HasFieldMarshal {
			get { return parameter != null && parameter.HasFieldMarshal; }
			set { Parameter.HasFieldMarshal = value; }
		}

		public bool HasMarshalInfo {
			get { return parameter != null && parameter.HasMarshalInfo; }
		}

		public MarshalInfo MarshalInfo {
			get { return Parameter.MarshalInfo; }
			set { Parameter.MarshalInfo = value; }
		}

		public MethodReturnType (IMethodSignature method)
		{
			this.method = method;
		}
	}
}
