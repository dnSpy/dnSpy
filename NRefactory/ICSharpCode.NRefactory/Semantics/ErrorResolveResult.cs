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
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Semantics
{
	/// <summary>
	/// Represents a resolve error.
	/// 
	/// Note: some errors are represented by other classes; for example a <see cref="ConversionResolveResult"/> may
	/// be erroneous if the conversion is invalid.
	/// </summary>
	/// <seealso cref="ResolveResult.IsError"/>.
	public class ErrorResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets an ErrorResolveResult instance with <c>Type</c> = <c>SpecialType.UnknownType</c>.
		/// </summary>
		public static readonly ErrorResolveResult UnknownError = new ErrorResolveResult(SpecialType.UnknownType);
		
		public ErrorResolveResult(IType type) : base(type)
		{
		}
		
		public ErrorResolveResult(IType type, string message, TextLocation location) : base(type)
		{
			this.Message = message;
			this.Location = location;
		}
		
		public override bool IsError {
			get { return true; }
		}
		
		public string Message { get; private set; }
		
		public TextLocation Location { get; private set; }
	}
}
