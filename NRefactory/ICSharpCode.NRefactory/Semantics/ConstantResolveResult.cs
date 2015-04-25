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
using System.Globalization;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Semantics
{
	/// <summary>
	/// ResolveResult representing a compile-time constant.
	/// Note: this class is mainly used for literals; there may be other ResolveResult classes
	/// which are compile-time constants as well.
	/// For example, a reference to a <c>const</c> field results in a <see cref="MemberResolveResult"/>.
	/// 
	/// Check <see cref="ResolveResult.IsCompileTimeConstant"/> to determine is a resolve result is a constant.
	/// </summary>
	public class ConstantResolveResult : ResolveResult
	{
		object constantValue;
		
		public ConstantResolveResult(IType type, object constantValue) : base(type)
		{
			this.constantValue = constantValue;
		}
		
		public override bool IsCompileTimeConstant {
			get { return true; }
		}
		
		public override object ConstantValue {
			get { return constantValue; }
		}
		
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} {1} = {2}]", GetType().Name, this.Type, constantValue);
		}
	}
}
