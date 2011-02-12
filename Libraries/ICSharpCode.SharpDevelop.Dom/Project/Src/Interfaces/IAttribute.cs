// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	public interface IAttribute : IFreezable
	{
		/// <summary>
		/// Gets the compilation unit in which this attribute is defined.
		/// </summary>
		ICompilationUnit CompilationUnit {
			get;
		}
		
		/// <summary>
		/// Gets the code region of this attribute.
		/// </summary>
		DomRegion Region {
			get;
		}
		
		AttributeTarget AttributeTarget {
			get;
		}
		
		IReturnType AttributeType {
			get;
		}
		
		IList<object> PositionalArguments {
			get;
		}
		
		IDictionary<string, object> NamedArguments {
			get;
		}
	}
	
	public enum AttributeTarget
	{
		None,
		Assembly,
		Field,
		Event,
		Method,
		Module,
		Param,
		Property,
		Return,
		Type
	}
}
