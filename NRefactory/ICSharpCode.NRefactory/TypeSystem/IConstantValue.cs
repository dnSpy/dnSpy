// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	[ContractClass(typeof(IConstantValueContract))]
	public interface IConstantValue : IFreezable
	{
		/// <summary>
		/// Gets the type of the constant value.
		/// </summary>
		IType GetValueType(ITypeResolveContext context);
		
		/// <summary>
		/// Gets the .NET value of the constant value.
		/// Possible return values are:
		/// - null
		/// - primitive integers
		/// - float/double
		/// - bool
		/// - string
		/// - IType (for typeof-expressions)
		/// and arrays of these values. Enum values are returned using the underlying primitive integer.
		/// 
		/// TODO: how do we represent errors (value not available?)
		/// </summary>
		object GetValue(ITypeResolveContext context);
	}
	
	[ContractClassFor(typeof(IConstantValue))]
	abstract class IConstantValueContract : IFreezableContract, IConstantValue
	{
		IType IConstantValue.GetValueType(ITypeResolveContext context)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IType>() != null);
			return null;
		}
		
		object IConstantValue.GetValue(ITypeResolveContext context)
		{
			Contract.Requires(context != null);
			return null;
		}
	}
}
