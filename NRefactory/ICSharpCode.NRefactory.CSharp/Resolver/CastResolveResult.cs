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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Represents an explicitly applied conversion (CastExpression or AsExpression)
	/// (a result belonging to an AST node; not implicitly inserted 'between' nodes).
	/// </summary>
	class CastResolveResult : ConversionResolveResult
	{
		// The reason this class exists is that for code like this:
		//    int i = ...;
		//    long n = 0;
		//    n = n + (long)i;
		// The resolver will produce (and process) an CastResolveResult for the cast,
		// (with Conversion = implicit numeric conversion)
		// and then pass it into CSharpResolver.ResolveBinaryOperator().
		// That method normally wraps input arguments into another conversion
		// (the implicit conversion applied by the operator).
		// However, identity conversions do not cause the creation of ConversionResolveResult instances,
		// so the OperatorResolveResult's argument will be the CastResolveResult
		// of the cast.
		// Without this class (and instead using ConversionResolveResult for both purposes),
		// it would be hard for the conversion-processing code
		// in the ResolveVisitor to distinguish the existing conversion from the CastExpression
		// from an implicit conversion introduced by the binary operator.
		// This would cause the conversion to be processed yet again.
		// The following unit tests would fail without this class:
		//  * CastTests.ExplicitConversion_In_Assignment
		//  * FindReferencesTest.FindReferencesForOpImplicitInAssignment_ExplicitCast
		//  * CS0029InvalidConversionIssueTests.ExplicitConversionFromUnknownType
		
		public CastResolveResult(ConversionResolveResult rr)
			: base(rr.Type, rr.Input, rr.Conversion, rr.CheckForOverflow)
		{
		}
		
		public CastResolveResult(IType targetType, ResolveResult input, Conversion conversion, bool checkForOverflow)
			: base(targetType, input, conversion, checkForOverflow)
		{
		}
	}
}
