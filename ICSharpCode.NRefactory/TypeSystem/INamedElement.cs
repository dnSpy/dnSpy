// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	#if WITH_CONTRACTS
	[ContractClass(typeof(INamedElementContract))]
	#endif
	public interface INamedElement
	{
		/// <summary>
		/// Gets the fully qualified name of the class the return type is pointing to.
		/// </summary>
		/// <returns>
		/// "System.Int32[]" for int[]<br/>
		/// "System.Collections.Generic.List" for List&lt;string&gt;
		/// "System.Environment.SpecialFolder" for Environment.SpecialFolder
		/// </returns>
		string FullName {
			get;
		}
		
		/// <summary>
		/// Gets the short name of the class the return type is pointing to.
		/// </summary>
		/// <returns>
		/// "Int32[]" for int[]<br/>
		/// "List" for List&lt;string&gt;
		/// "SpecialFolder" for Environment.SpecialFolder
		/// </returns>
		string Name {
			get;
		}
		
		/// <summary>
		/// Gets the namespace of the class the return type is pointing to.
		/// </summary>
		/// <returns>
		/// "System" for int[]<br/>
		/// "System.Collections.Generic" for List&lt;string&gt;
		/// "System" for Environment.SpecialFolder
		/// </returns>
		string Namespace {
			get;
		}
		
		/// <summary>
		/// Gets the full reflection name of the element.
		/// </summary>
		/// <remarks>
		/// For types, the reflection name can be parsed back into a ITypeReference by using
		/// <see cref="ReflectionHelper.ParseReflectionName(string,IEntity)"/>.
		/// </remarks>
		/// <returns>
		/// "System.Int32[]" for int[]<br/>
		/// "System.Int32[][,]" for C# int[,][]<br/>
		/// "System.Collections.Generic.List`1[[System.String]]" for List&lt;string&gt;
		/// "System.Environment+SpecialFolder" for Environment.SpecialFolder
		/// </returns>
		string ReflectionName {
			get;
		}
	}
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(INamedElement))]
	abstract class INamedElementContract : INamedElement
	{
		string INamedElement.FullName {
			get {
				Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));
				return null;
			}
		}
		
		string INamedElement.Name {
			get {
				Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));
				return null;
			}
		}
		
		string INamedElement.Namespace {
			get {
				Contract.Ensures(Contract.Result<string>() != null);
				return null;
			}
		}
		
		string INamedElement.ReflectionName {
			get {
				Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));
				return null;
			}
		}
	}
	#endif
}
