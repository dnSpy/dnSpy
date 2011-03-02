// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	#if WITH_CONTRACTS
	[ContractClass(typeof(IParameterContract))]
	#endif
	public interface IParameter : IVariable, IFreezable
	{
		/// <summary>
		/// Gets the list of attributes.
		/// </summary>
		IList<IAttribute> Attributes { get; }
		
		/// <summary>
		/// Gets the default value of optional parameters.
		/// </summary>
		IConstantValue DefaultValue { get; }
		
		/// <summary>
		/// Gets the code region where the parameter is defined.
		/// </summary>
		DomRegion Region { get; }
		
		/// <summary>
		/// Gets whether this parameter is a C# 'ref' parameter.
		/// </summary>
		bool IsRef { get; }
		
		/// <summary>
		/// Gets whether this parameter is a C# 'out' parameter.
		/// </summary>
		bool IsOut { get; }
		
		/// <summary>
		/// Gets whether this parameter is a C# 'params' parameter.
		/// </summary>
		bool IsParams { get; }
		
		/// <summary>
		/// Gets whether this parameter is optional.
		/// </summary>
		bool IsOptional { get; }
	}
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(IParameter))]
	abstract class IParameterContract : IVariableContract, IParameter
	{
		IList<IAttribute> IParameter.Attributes {
			get {
				Contract.Ensures(Contract.Result<IList<IAttribute>>() != null);
				return null;
			}
		}
		
		IConstantValue IParameter.DefaultValue {
			get { return null; }
		}
		
		DomRegion IParameter.Region {
			get { return DomRegion.Empty; }
		}
		
		bool IParameter.IsRef {
			get { return false; }
		}
		
		bool IParameter.IsOut {
			get { return false; }
		}
		
		bool IParameter.IsParams {
			get { return false; }
		}
		
		bool IParameter.IsOptional {
			get {
				IParameter @this = this;
				Contract.Ensures(Contract.Result<bool>() == (@this.DefaultValue != null));
				return false;
			}
		}
		
		bool IFreezable.IsFrozen {
			get { return false; }
		}
		
		void IFreezable.Freeze()
		{
		}
	}
	#endif
}
