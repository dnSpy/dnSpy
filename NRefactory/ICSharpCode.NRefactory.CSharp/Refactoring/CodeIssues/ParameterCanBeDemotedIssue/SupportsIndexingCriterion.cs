//
// SupportsIndexingCriterion.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public class SupportsIndexingCriterion : ITypeCriterion
	{
		IType returnType;

		IList<IType> argumentTypes;
		
		CSharpConversions conversions;

		bool isWriteAccess;

		public SupportsIndexingCriterion(IType returnType, IEnumerable<IType> argumentTypes, CSharpConversions conversions, bool isWriteAccess = false)
		{
			if (returnType == null)
				throw new ArgumentNullException("returnType");
			if (argumentTypes == null)
				throw new ArgumentNullException("argumentTypes");
			if (conversions == null)
				throw new ArgumentNullException("conversions");

			this.returnType = returnType;
			this.argumentTypes = argumentTypes.ToList();
			this.conversions = conversions;
			this.isWriteAccess = isWriteAccess;
		}

		#region ITypeCriterion implementation

		public bool SatisfiedBy(IType type)
		{
			var accessors = type.GetAccessors().ToList();
			return accessors.Any(member => {
				var parameterizedMember = member as IParameterizedMember;
				if (parameterizedMember == null)
					return false;

				if (isWriteAccess) {
					var parameterCount = member.Parameters.Count;
					if (member.Name != "set_Item" || parameterCount < 2)
						return false;
					var indexerElementType = parameterizedMember.Parameters.Last().Type;
					var indexerParameterTypes = parameterizedMember.Parameters.Take(parameterCount - 1).Select(p => p.Type).ToList();
					return IsSignatureMatch(indexerElementType, indexerParameterTypes);
				} else {
					if (member.Name != "get_Item" || member.Parameters.Count < 1)
						return false;
					var indexerElementType = parameterizedMember.ReturnType;
					var indexerParameterTypes = parameterizedMember.Parameters.Select(p => p.Type).ToList();
					return IsSignatureMatch(indexerElementType, indexerParameterTypes);
				}
			});
		}

		#endregion

		bool IsSignatureMatch(IType indexerElementType, IList<IType> indexerParameterTypes)
		{
			indexerElementType.GetAllBaseTypes();
			if (indexerParameterTypes.Count != argumentTypes.Count)
				return false;
			var returnConversion = conversions.ImplicitConversion(indexerElementType, returnType);
			if (!returnConversion.IsValid)
				return false;
			for (int i = 0; i < argumentTypes.Count; i++) {
				var conversion = conversions.ImplicitConversion(indexerParameterTypes[i], argumentTypes[i]);
				if (!conversion.IsValid)
					return false;
			}
			return true;
		}
	}
}

