// 
// Error.cs
//  
// Author:
//       Mike Krüger <mike@icsharpcode.net>
// 
// Copyright (c) 2011 Mike Krüger <mike@icsharpcode.net>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Enum that describes the type of an error.
	/// </summary>
	public enum ErrorType
	{
		Unknown,
		Error,
		Warning
	}
	
	/// <summary>
	/// Descibes an error during parsing.
	/// </summary>
	public class Error
	{
		/// <summary>
		/// The type of the error.
		/// </summary>
		public ErrorType ErrorType { get; private set; }
		
		/// <summary>
		/// The error description.
		/// </summary>
		public string Message { get; private set; }
		
		/// <summary>
		/// The region of the error.
		/// </summary>
		public DomRegion Region { get; private set; }
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
		/// </summary>
		/// <param name='errorType'>
		/// The error type.
		/// </param>
		/// <param name='message'>
		/// The description of the error.
		/// </param>
		/// <param name='region'>
		/// The region of the error.
		/// </param>
		public Error (ErrorType errorType, string message, DomRegion region)
		{
			this.ErrorType = errorType;
			this.Message = message;
			this.Region = region;
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
		/// </summary>
		/// <param name='errorType'>
		/// The error type.
		/// </param>
		/// <param name='message'>
		/// The description of the error.
		/// </param>
		/// <param name='location'>
		/// The location of the error.
		/// </param>
		public Error (ErrorType errorType, string message, AstLocation location)
		{
			this.ErrorType = errorType;
			this.Message = message;
			this.Region = new DomRegion (location, location);
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
		/// </summary>
		/// <param name='errorType'>
		/// The error type.
		/// </param>
		/// <param name='message'>
		/// The description of the error.
		/// </param>
		/// <param name='line'>
		/// The line of the error.
		/// </param>
		/// <param name='col'>
		/// The column of the error.
		/// </param>
		public Error (ErrorType errorType, string message, int line, int col) : this (errorType, message, new AstLocation (line, col))
		{
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
		/// </summary>
		/// <param name='errorType'>
		/// The error type.
		/// </param>
		/// <param name='message'>
		/// The description of the error.
		/// </param>
		public Error (ErrorType errorType, string message)
		{
			this.ErrorType = errorType;
			this.Message = message;
			this.Region = DomRegion.Empty;
		}
	}
}
