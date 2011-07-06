// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents a variable (name/return type pair).
	/// </summary>
	#if WITH_CONTRACTS
	[ContractClass(typeof(IVariableContract))]
	#endif
	public interface IVariable
	{
		/// <summary>
		/// Gets the name of the variable.
		/// </summary>
		string Name { get; }
		
		/// <summary>
		/// Gets the declaration region of the variable.
		/// </summary>
		DomRegion DeclarationRegion { get; }
		
		/// <summary>
		/// Gets the type of the variable.
		/// </summary>
		ITypeReference Type { get; }
		
		/// <summary>
		/// Gets whether this field is a constant (C#-like const).
		/// </summary>
		bool IsConst { get; }
		
		/// <summary>
		/// If this field is a constant, retrieves the value.
		/// </summary>
		IConstantValue ConstantValue { get; }
	}
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(IVariable))]
	abstract class IVariableContract : IVariable
	{
		string IVariable.Name {
			get {
				Contract.Ensures(Contract.Result<string>() != null);
				return null;
			}
		}
		
		ITypeReference IVariable.Type {
			get {
				Contract.Ensures(Contract.Result<ITypeReference>() != null);
				return null;
			}
		}
		
		bool IVariable.IsConst {
			get {
				IVariable @this = this;
				Contract.Ensures(Contract.Result<bool>() == (@this.ConstantValue != null));
				return false;
			}
		}
		
		IConstantValue IVariable.ConstantValue {
			get { return null; }
		}
	}
	#endif
}
