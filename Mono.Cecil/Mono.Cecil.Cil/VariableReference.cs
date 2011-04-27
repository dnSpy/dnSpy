//
// VariableReference.cs
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

namespace Mono.Cecil.Cil {

	public abstract class VariableReference {

		string name;
		internal int index = -1;
		protected TypeReference variable_type;

		public string Name {
			get { return name; }
			set { name = value; }
		}

		public TypeReference VariableType {
			get { return variable_type; }
			set { variable_type = value; }
		}

		public int Index {
			get { return index; }
		}

		internal VariableReference (TypeReference variable_type)
			: this (string.Empty, variable_type)
		{
		}

		internal VariableReference (string name, TypeReference variable_type)
		{
			this.name = name;
			this.variable_type = variable_type;
		}

		public abstract VariableDefinition Resolve ();

		public override string ToString ()
		{
			if (!string.IsNullOrEmpty (name))
				return name;

			if (index >= 0)
				return "V_" + index;

			return string.Empty;
		}
	}
}
