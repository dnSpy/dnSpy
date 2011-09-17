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
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Semantics
{
	/// <summary>
	/// Represents a local variable.
	/// </summary>
	public class LocalResolveResult : ResolveResult
	{
		readonly IVariable variable;
		readonly object constantValue;
		
		public LocalResolveResult(IVariable variable, IType type, object constantValue = null)
			: base(UnpackTypeIfByRefParameter(type, variable))
		{
			if (variable == null)
				throw new ArgumentNullException("variable");
			this.variable = variable;
			this.constantValue = constantValue;
		}
		
		static IType UnpackTypeIfByRefParameter(IType type, IVariable v)
		{
			if (type.Kind == TypeKind.ByReference) {
				IParameter p = v as IParameter;
				if (p != null && (p.IsRef || p.IsOut))
					return ((ByReferenceType)type).ElementType;
			}
			return type;
		}
		
		public IVariable Variable {
			get { return variable; }
		}
		
		public bool IsParameter {
			get { return variable is IParameter; }
		}
		
		public override bool IsCompileTimeConstant {
			get { return variable.IsConst; }
		}
		
		public override object ConstantValue {
			get { return constantValue; }
		}
		
		public override string ToString()
		{
			return string.Format("[LocalResolveResult {0}]", variable);
		}
		
		public override DomRegion GetDefinitionRegion()
		{
			return variable.Region;
		}
	}
}
