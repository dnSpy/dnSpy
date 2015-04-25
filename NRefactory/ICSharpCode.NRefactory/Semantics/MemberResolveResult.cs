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
using System.Globalization;
using System.Linq;

using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Semantics
{
	/// <summary>
	/// Represents the result of a member invocation.
	/// Used for field/property/event access.
	/// Also, <see cref="InvocationResolveResult"/> derives from MemberResolveResult.
	/// </summary>
	public class MemberResolveResult : ResolveResult
	{
		readonly IMember member;
		readonly bool isConstant;
		readonly object constantValue;
		readonly ResolveResult targetResult;
		readonly bool isVirtualCall;
		
		public MemberResolveResult(ResolveResult targetResult, IMember member, IType returnTypeOverride = null)
			: base(returnTypeOverride ?? ComputeType(member))
		{
			this.targetResult = targetResult;
			this.member = member;
			var thisRR = targetResult as ThisResolveResult;
			this.isVirtualCall = member.IsOverridable && !(thisRR != null && thisRR.CausesNonVirtualInvocation);
			
			IField field = member as IField;
			if (field != null) {
				isConstant = field.IsConst;
				if (isConstant)
					constantValue = field.ConstantValue;
			}
		}
		
		public MemberResolveResult(ResolveResult targetResult, IMember member, bool isVirtualCall, IType returnTypeOverride = null)
			: base(returnTypeOverride ?? ComputeType(member))
		{
			this.targetResult = targetResult;
			this.member = member;
			this.isVirtualCall = isVirtualCall;
			IField field = member as IField;
			if (field != null) {
				isConstant = field.IsConst;
				if (isConstant)
					constantValue = field.ConstantValue;
			}
		}
		
		static IType ComputeType(IMember member)
		{
			switch (member.SymbolKind) {
				case SymbolKind.Constructor:
					return member.DeclaringType;
				case SymbolKind.Field:
					if (((IField)member).IsFixed)
						return new PointerType(member.ReturnType);
					break;
			}
			return member.ReturnType;
		}
		
		public MemberResolveResult(ResolveResult targetResult, IMember member, IType returnType, bool isConstant, object constantValue)
			: base(returnType)
		{
			this.targetResult = targetResult;
			this.member = member;
			this.isConstant = isConstant;
			this.constantValue = constantValue;
		}
		
		public MemberResolveResult(ResolveResult targetResult, IMember member, IType returnType, bool isConstant, object constantValue, bool isVirtualCall)
			: base(returnType)
		{
			this.targetResult = targetResult;
			this.member = member;
			this.isConstant = isConstant;
			this.constantValue = constantValue;
			this.isVirtualCall = isVirtualCall;
		}
		
		public ResolveResult TargetResult {
			get { return targetResult; }
		}
		
		/// <summary>
		/// Gets the member.
		/// This property never returns null.
		/// </summary>
		public IMember Member {
			get { return member; }
		}
		
		/// <summary>
		/// Gets whether this MemberResolveResult is a virtual call.
		/// </summary>
		public bool IsVirtualCall {
			get { return isVirtualCall; }
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
			return string.Format(CultureInfo.InvariantCulture, "[{0} {1}]", GetType().Name, member);
		}
		
		public override DomRegion GetDefinitionRegion()
		{
			return member.Region;
		}
	}
}
