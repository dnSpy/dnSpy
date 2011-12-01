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
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	public class DefaultResolvedProperty : AbstractResolvedMember, IProperty
	{
		protected new readonly IUnresolvedProperty unresolved;
		readonly IList<IParameter> parameters;
		IAccessor getter;
		IAccessor setter;
		
		public DefaultResolvedProperty(IUnresolvedProperty unresolved, ITypeResolveContext parentContext)
			: base(unresolved, parentContext)
		{
			this.unresolved = unresolved;
			this.parameters = unresolved.Parameters.CreateResolvedParameters(context);
		}
		
		public IList<IParameter> Parameters {
			get { return parameters; }
		}
		
		public bool CanGet {
			get { return unresolved.CanGet; }
		}
		
		public bool CanSet {
			get { return unresolved.CanSet; }
		}
		
		public IAccessor Getter {
			get {
				if (!unresolved.CanGet)
					return null;
				IAccessor result = this.getter;
				if (result != null) {
					LazyInit.ReadBarrier();
					return result;
				} else {
					return LazyInit.GetOrSet(ref this.getter, unresolved.Getter.CreateResolvedAccessor(context));
				}
			}
		}
		
		public IAccessor Setter {
			get {
				if (!unresolved.CanSet)
					return null;
				IAccessor result = this.setter;
				if (result != null) {
					LazyInit.ReadBarrier();
					return result;
				} else {
					return LazyInit.GetOrSet(ref this.setter, unresolved.Setter.CreateResolvedAccessor(context));
				}
			}
		}
		
		public bool IsIndexer {
			get { return unresolved.IsIndexer; }
		}
		
		public override IMemberReference ToMemberReference()
		{
			return new DefaultMemberReference(
				this.EntityType, this.DeclaringType.ToTypeReference(), this.Name, 0,
				this.Parameters.Select(p => p.Type.ToTypeReference()).ToList());
		}
	}
}
