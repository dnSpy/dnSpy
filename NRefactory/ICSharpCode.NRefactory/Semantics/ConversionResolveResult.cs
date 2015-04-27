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
	/// Represents an implicit or explicit type conversion.
	/// <c>conversionResolveResult.Input.Type</c> is the source type;
	/// <c>conversionResolveResult.Type</c> is the target type.
	/// The <see cref="Conversion"/> property provides details about the kind of conversion.
	/// </summary>
	public class ConversionResolveResult : ResolveResult
	{
		public readonly ResolveResult Input;
		public readonly Conversion Conversion;
		
		/// <summary>
		/// For numeric conversions, specifies whether overflow checking is enabled.
		/// </summary>
		public readonly bool CheckForOverflow;
		
		public ConversionResolveResult(IType targetType, ResolveResult input, Conversion conversion)
			: base(targetType)
		{
			if (input == null)
				throw new ArgumentNullException("input");
			if (conversion == null)
				throw new ArgumentNullException("conversion");
			this.Input = input;
			this.Conversion = conversion;
		}
		
		public ConversionResolveResult(IType targetType, ResolveResult input, Conversion conversion, bool checkForOverflow)
			: this(targetType, input, conversion)
		{
			this.CheckForOverflow = checkForOverflow;
		}
		
		public override bool IsError {
			get { return !Conversion.IsValid; }
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return new [] { Input };
		}

	}
}
