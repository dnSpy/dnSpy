// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Represents a specialized IField (field after type substitution).
	/// </summary>
	public class SpecializedField : SpecializedMember, IField
	{
		readonly IField fieldDefinition;
		
		public SpecializedField(IField fieldDefinition, TypeParameterSubstitution substitution)
			: base(fieldDefinition)
		{
			this.fieldDefinition = fieldDefinition;
			AddSubstitution(substitution);
		}
		
		public bool IsReadOnly {
			get { return fieldDefinition.IsReadOnly; }
		}
		
		public bool IsVolatile {
			get { return fieldDefinition.IsVolatile; }
		}
		
		IType IVariable.Type {
			get { return this.ReturnType; }
		}
		
		public bool IsConst {
			get { return fieldDefinition.IsConst; }
		}

		public bool IsFixed {
			get { return fieldDefinition.IsFixed; }
		}

		public object ConstantValue {
			get { return fieldDefinition.ConstantValue; }
		}
	}
}
