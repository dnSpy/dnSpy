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
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	public interface IUnresolvedParameter
	{
		/// <summary>
		/// Gets the name of the variable.
		/// </summary>
		string Name { get; }
		
		/// <summary>
		/// Gets the declaration region of the variable.
		/// </summary>
		DomRegion Region { get; }
		
		/// <summary>
		/// Gets the type of the variable.
		/// </summary>
		ITypeReference Type { get; }
		
		/// <summary>
		/// Gets the list of attributes.
		/// </summary>
		IList<IUnresolvedAttribute> Attributes { get; }
		
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
		
		IParameter CreateResolvedParameter(ITypeResolveContext context);
	}
	
	public interface IParameter : IVariable
	{
		/// <summary>
		/// Gets the list of attributes.
		/// </summary>
		IList<IAttribute> Attributes { get; }
		
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
		/// The default value is given by the <see cref="IVariable.ConstantValue"/> property.
		/// </summary>
		bool IsOptional { get; }
		
		/// <summary>
		/// Gets the owner of this parameter.
		/// May return null; for example when parameters belong to lambdas or anonymous methods.
		/// </summary>
		IParameterizedMember Owner { get; }
	}
}
