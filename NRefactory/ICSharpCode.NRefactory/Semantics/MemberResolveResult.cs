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
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Semantics
{
	/// <summary>
	/// Represents the result of a member invocation.
	/// </summary>
	public class MemberResolveResult : ResolveResult
	{
		readonly IMember member;
		readonly bool isConstant;
		readonly object constantValue;
		readonly ResolveResult targetResult;
		
		public MemberResolveResult(ResolveResult targetResult, IMember member, IType returnType) : base(returnType)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			this.targetResult = targetResult;
			this.member = member;
		}
		
		public MemberResolveResult(ResolveResult targetResult, IMember member, IType returnType, object constantValue) : base(returnType)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			this.targetResult = targetResult;
			this.member = member;
			this.isConstant = true;
			this.constantValue = constantValue;
		}
		
		public MemberResolveResult(ResolveResult targetResult, IMember member, ITypeResolveContext context) 
			: base(member.EntityType == EntityType.Constructor ? member.DeclaringType : member.ReturnType.Resolve(context))
		{
			this.targetResult = targetResult;
			this.member = member;
			IField field = member as IField;
			if (field != null) {
				isConstant = field.IsConst;
				if (isConstant)
					constantValue = field.ConstantValue.Resolve(context).ConstantValue;
			}
		}
		
		public ResolveResult TargetResult {
			get { return targetResult; }
		}
		
		public IMember Member {
			get { return member; }
		}
		
		public override bool IsCompileTimeConstant {
			get { return isConstant; }
		}
		
		public override object ConstantValue {
			get { return constantValue; }
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			if (targetResult != null)
				return new[] { targetResult };
			else
				return Enumerable.Empty<ResolveResult>();
		}
		
		public override string ToString()
		{
			return string.Format("[{0} {1}]", GetType().Name, member);
		}
		
		public override DomRegion GetDefinitionRegion()
		{
			return member.Region;
		}
	}
}
