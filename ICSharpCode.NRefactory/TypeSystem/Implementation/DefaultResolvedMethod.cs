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
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IMethod"/> that resolves an unresolved method.
	/// </summary>
	public class DefaultResolvedMethod : AbstractResolvedMember, IMethod
	{
		public DefaultResolvedMethod(DefaultUnresolvedMethod unresolved, ITypeResolveContext parentContext)
			: this(unresolved, parentContext, unresolved.IsExtensionMethod)
		{
		}
		
		public DefaultResolvedMethod(IUnresolvedMethod unresolved, ITypeResolveContext parentContext, bool isExtensionMethod)
			: base(unresolved, parentContext)
		{
			this.Parameters = unresolved.Parameters.CreateResolvedParameters(context);
			this.ReturnTypeAttributes = unresolved.ReturnTypeAttributes.CreateResolvedAttributes(parentContext);
			this.TypeParameters = unresolved.TypeParameters.CreateResolvedTypeParameters(context);
			this.IsExtensionMethod = isExtensionMethod;
		}
		
		public IList<IParameter> Parameters { get; private set; }
		public IList<IAttribute> ReturnTypeAttributes { get; private set; }
		public IList<ITypeParameter> TypeParameters { get; private set; }
		
		public bool IsExtensionMethod { get; private set; }
		
		public bool IsConstructor {
			get { return ((IUnresolvedMethod)unresolved).IsConstructor; }
		}
		
		public bool IsDestructor {
			get { return ((IUnresolvedMethod)unresolved).IsDestructor; }
		}
		
		public bool IsOperator {
			get { return ((IUnresolvedMethod)unresolved).IsOperator; }
		}
		
		public override IMemberReference ToMemberReference()
		{
			return new DefaultMemberReference(
				this.EntityType, this.DeclaringType.ToTypeReference(), this.Name, this.TypeParameters.Count,
				this.Parameters.Select(p => p.Type.ToTypeReference()).ToList());
		}
	}
}
