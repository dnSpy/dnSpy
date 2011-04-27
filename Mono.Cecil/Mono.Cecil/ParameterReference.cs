//
// ParameterReference.cs
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

	public abstract class ParameterReference : IMetadataTokenProvider {

		string name;
		internal int index = -1;
		protected TypeReference parameter_type;
		internal MetadataToken token;

		public string Name {
			get { return name; }
			set { name = value; }
		}

		public int Index {
			get { return index; }
		}

		public TypeReference ParameterType {
			get { return parameter_type; }
			set { parameter_type = value; }
		}

		public MetadataToken MetadataToken {
			get { return token; }
			set { token = value; }
		}

		internal ParameterReference (string name, TypeReference parameterType)
		{
			if (parameterType == null)
				throw new ArgumentNullException ("parameterType");

			this.name = name ?? string.Empty;
			this.parameter_type = parameterType;
		}

		public override string ToString ()
		{
			return name;
		}

		public abstract ParameterDefinition Resolve ();
	}
}
