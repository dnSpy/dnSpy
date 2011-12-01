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
		ShowDefinitionKeyWord  = 8,
		/// <summary>
		/// Show the fully qualified name for the member
		/// </summary>
		UseFullyQualifiedMemberNames = 0x10,
		/// <summary>
		/// Show modifiers (virtual, override, etc.)
		/// </summary>
		ShowModifiers          = 0x20,
		/// <summary>
		/// Show the return type
		/// </summary>
		ShowReturnType = 0x40,
		/// <summary>
		/// Use fully qualified names for return type and parameters.
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
		
		StandardConversionFlags = ShowParameterNames |
			ShowAccessibility |
			ShowParameterList |
			ShowReturnType |
			ShowModifiers |
			ShowTypeParameterList |
			ShowDefinitionKeyWord |
			ShowBody,
		
		All = 0xfff,
	}
	
	public interface IAmbience
	{
		ConversionFlags ConversionFlags { get; set; }
		
		string ConvertEntity(IEntity e);
		string ConvertType(IType type);
		string ConvertVariable(IVariable variable);
		
		string WrapAttribute(string attribute);
		string WrapComment(string comment);
	}
}
