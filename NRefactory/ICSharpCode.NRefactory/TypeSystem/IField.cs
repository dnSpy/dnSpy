// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents a field or constant.
	/// </summary>
	#if WITH_CONTRACTS
	[ContractClass(typeof(IFieldContract))]
	#endif
	public interface IField : IMember, IVariable
	{
		/// <summary>
		/// Gets the name of the field.
		/// </summary>
		new string Name { get; } // solve ambiguity between INamedElement.Name and IVariable.Name
		
		/// <summary>
		/// Gets the region where the field is declared.
		/// </summary>
		new DomRegion Region { get; } // solve ambiguity between IEntity.Region and IVariable.Region
		
		/// <summary>
		/// Gets whether this field is readonly.
		/// </summary>
		bool IsReadOnly { get; }
		
		/// <summary>
		/// Gets whether this field is volatile.
		/// </summary>
		bool IsVolatile { get; }
	}
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(IField))]
	abstract class IFieldContract : IMemberContract, IField
	{
		string IField.Name {
			get {
				Contract.Ensures(Contract.Result<string>() != null);
				return null;
			}
		}
		
		bool IField.IsReadOnly {
			get { return false; }
		}
		
		bool IField.IsVolatile {
			get { return false; }
		}
		
		
		string IVariable.Name {
			get { return null;  }
		}
		
		ITypeReference IVariable.Type {
			get { return null; }
		}
		
		bool IVariable.IsConst {
			get { return false; }
		}
		
		IConstantValue IVariable.ConstantValue {
			get { return null; }
		}
	}
	#endif
}
