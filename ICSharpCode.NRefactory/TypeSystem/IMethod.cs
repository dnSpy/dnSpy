// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents a method, constructor, destructor or operator.
	/// </summary>
	[ContractClass(typeof(IMethodContract))]
	public interface IMethod : IParameterizedMember
	{
		/// <summary>
		/// Gets the attributes associated with the return type.
		/// </summary>
		IList<IAttribute> ReturnTypeAttributes { get; }
		
		IList<ITypeParameter> TypeParameters { get; }
		
		// handles is VB-specific and not part of the public API, so
		// we don't really need it
		//IList<string> HandlesClauses { get; }
		
		bool IsExtensionMethod { get; }
		bool IsConstructor { get; }
		bool IsDestructor { get; }
		bool IsOperator { get; }
	}
	
	[ContractClassFor(typeof(IMethod))]
	abstract class IMethodContract : IParameterizedMemberContract, IMethod
	{
		IList<IAttribute> IMethod.ReturnTypeAttributes {
			get {
				Contract.Ensures(Contract.Result<IList<IAttribute>>() != null);
				return null;
			}
		}
		
		IList<ITypeParameter> IMethod.TypeParameters {
			get {
				Contract.Ensures(Contract.Result<IList<ITypeParameter>>() != null);
				return null;
			}
		}
		
//		IList<string> IMethod.HandlesClauses {
//			get {
//				Contract.Ensures(Contract.Result<IList<string>>() != null);
//				return null;
//			}
//		}
		
		bool IMethod.IsExtensionMethod {
			get { return false; }
		}
		
		bool IMethod.IsConstructor {
			get { return false; }
		}
		
		bool IMethod.IsDestructor {
			get { return false; }
		}
		
		bool IMethod.IsOperator {
			get { return false; }
		}
	}
}
