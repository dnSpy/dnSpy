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

namespace ICSharpCode.NRefactory.TypeSystem
{
	[Flags]
	public enum ConversionFlags
	{
		/// <summary>
		/// Convert only the name.
		/// </summary>
		None = 0,
		/// <summary>
		/// Show the parameter list
		/// </summary>
		ShowParameterList      = 1,
		/// <summary>
		/// Show names for parameters
		/// </summary>
		ShowParameterNames     = 2,
		/// <summary>
		/// Show the accessibility (private, public, etc.)
		/// </summary>
		ShowAccessibility      = 4,
		/// <summary>
		/// Show the definition key word (class, struct, Sub, Function, etc.)
		/// </summary>
		ShowDefinitionKeyword  = 8,
		/// <summary>
		/// Show the declaring type for the member
		/// </summary>
		ShowDeclaringType = 0x10,
		/// <summary>
		/// Show modifiers (virtual, override, etc.)
		/// </summary>
		ShowModifiers          = 0x20,
		/// <summary>
		/// Show the return type
		/// </summary>
		ShowReturnType = 0x40,
		/// <summary>
		/// Use fully qualified names for types.
		/// </summary>
		UseFullyQualifiedTypeNames = 0x80,
		/// <summary>
		/// Show the list of type parameters on method and class declarations.
		/// Type arguments for parameter/return types are always shown.
		/// </summary>
		ShowTypeParameterList = 0x100,
		/// <summary>
		/// For fields, events and methods: adds a semicolon at the end.
		/// For properties: shows "{ get; }" or similar.
		/// </summary>
		ShowBody = 0x200,
		
		/// <summary>
		/// Use fully qualified names for members.
		/// </summary>
		UseFullyQualifiedEntityNames = 0x400,
		
		StandardConversionFlags = ShowParameterNames |
			ShowAccessibility |
			ShowParameterList |
			ShowReturnType |
			ShowModifiers |
			ShowTypeParameterList |
			ShowDefinitionKeyword |
			ShowBody,
		
		All = 0x7ff,
	}
	
	/// <summary>
	/// Ambiences are used to convert type system symbols to text (usually for displaying the symbol to the user; e.g. in editor tooltips).
	/// </summary>
	public interface IAmbience
	{
		ConversionFlags ConversionFlags { get; set; }
		
		[Obsolete("Use ConvertSymbol() instead")]
		string ConvertEntity(IEntity entity);
		string ConvertSymbol(ISymbol symbol);
		string ConvertType(IType type);
		[Obsolete("Use ConvertSymbol() instead")]
		string ConvertVariable(IVariable variable);
		string ConvertConstantValue(object constantValue);
		
		string WrapComment(string comment);
	}
}
