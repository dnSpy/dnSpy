// 
// NullValueAnalysis.cs
//  
// Author:
//       Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis
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

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	/// <summary>
	/// Represents the null value status of a variable at a specific location.
	/// </summary>
	public enum NullValueStatus
	{
		/// <summary>
		/// The value of the variable is unknown, possibly due to limitations
		/// of the null value analysis.
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// The value of the variable is unknown and even assigning it to a
		/// value won't change its state, since it has been captured by a lambda
		/// that may change it at any time (potentially even from a different thread).
		/// Only going out of scope and creating a new variable may change the value
		/// of this variable.
		/// </summary>
		CapturedUnknown,
		/// <summary>
		/// This variable is potentially unassigned.
		/// </summary>
		Unassigned,
		/// <summary>
		/// The value of the variable is provably null.
		/// </summary>
		DefinitelyNull,
		/// <summary>
		/// The value of the variable might or might not be null
		/// </summary>
		PotentiallyNull,
		/// <summary>
		/// The value of the variable is provably not null
		/// </summary>
		DefinitelyNotNull,
		/// <summary>
		/// The position of this node is unreachable, therefore the value
		/// of the variable is not meaningful.
		/// Alternatively, it might mean no local variable exists with the requested name.
		/// </summary>
		UnreachableOrInexistent,
		/// <summary>
		/// The analyser has encountered an error when attempting to find the value
		/// of this variable.
		/// </summary>
		Error
	}

	public static class NullValueStatusExtensions
	{
		public static bool IsDefiniteValue (this NullValueStatus self) {
			return self == NullValueStatus.DefinitelyNull || self == NullValueStatus.DefinitelyNotNull;
		}
	}
}

