// 
// AffectedEntity.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[Flags]
	public enum AffectedEntity
	{
		None,

		Namespace = 1 << 0,

		Class     = 1 << 1,
		Struct    = 1 << 2,
		Enum      = 1 << 3,
		Interface = 1 << 4,
		Delegate  = 1 << 5,

		CustomAttributes = 1 << 6,
		CustomEventArgs  = 1 << 7,
		CustomExceptions = 1 << 8,

		Property      = 1 << 9,
		Method        = 1 << 10,
		AsyncMethod   = 1 << 11,
		Field         = 1 << 12,
		ReadonlyField = 1 << 13,
		ConstantField = 1 << 14,

		Event      = 1 << 15,
		EnumMember = 1 << 16,

		Parameter     = 1 << 17,
		TypeParameter = 1 << 18,

		// Unit test special case
		TestType   = 1 << 19,
		TestMethod = 1 << 20,

		// private entities
		LambdaParameter = 1 << 21,
		LocalVariable   = 1 << 22,
		LocalConstant   = 1 << 23,
		Label           = 1 << 24,

		LocalVars = LocalVariable | Parameter | LambdaParameter | LocalConstant,
		Methods = Method | AsyncMethod,
		Fields = Field | ReadonlyField | ConstantField,
		Member = Property | Methods | Fields | Event | EnumMember,
		
		Type = Class | Struct | Enum | Interface | Delegate,

	}
}
