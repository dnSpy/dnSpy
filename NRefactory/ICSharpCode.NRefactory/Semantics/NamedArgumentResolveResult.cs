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
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Semantics
{
	/// <summary>
	/// Represents a named argument.
	/// </summary>
	public class NamedArgumentResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets the member to which the parameter belongs.
		/// This field can be null.
		/// </summary>
		public readonly IParameterizedMember Member;
		
		/// <summary>
		/// Gets the parameter.
		/// This field can be null.
		/// </summary>
		public readonly IParameter Parameter;
		
		/// <summary>
		/// Gets the parameter name.
		/// </summary>
		public readonly string ParameterName;
		
		/// <summary>
		/// Gets the argument passed to the parameter.
		/// </summary>
		public readonly ResolveResult Argument;
		
		public NamedArgumentResolveResult(IParameter parameter, ResolveResult argument, IParameterizedMember member = null)
			: base(argument.Type)
		{
			if (parameter == null)
				throw new ArgumentNullException("parameter");
			if (argument == null)
				throw new ArgumentNullException("argument");
			this.Member = member;
			this.Parameter = parameter;
			this.ParameterName = parameter.Name;
			this.Argument = argument;
		}
		
		public NamedArgumentResolveResult(string parameterName, ResolveResult argument)
			: base(argument.Type)
		{
			if (parameterName == null)
				throw new ArgumentNullException("parameterName");
			if (argument == null)
				throw new ArgumentNullException("argument");
			this.ParameterName = parameterName;
			this.Argument = argument;
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return new [] { Argument };
		}
	}
}
