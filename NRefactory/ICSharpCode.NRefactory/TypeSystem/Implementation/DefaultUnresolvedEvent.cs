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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IUnresolvedEvent"/>.
	/// </summary>
	[Serializable]
	public class DefaultUnresolvedEvent : AbstractUnresolvedMember, IUnresolvedEvent
	{
		IUnresolvedMethod addAccessor, removeAccessor, invokeAccessor;
		
		protected override void FreezeInternal()
		{
			base.FreezeInternal();
			FreezableHelper.Freeze(addAccessor);
			FreezableHelper.Freeze(removeAccessor);
			FreezableHelper.Freeze(invokeAccessor);
		}
		
		public override void ApplyInterningProvider(IInterningProvider provider)
		{
			base.ApplyInterningProvider(provider);
			addAccessor    = provider.Intern(addAccessor);
			removeAccessor = provider.Intern(removeAccessor);
			invokeAccessor = provider.Intern(invokeAccessor);
		}
		
		public DefaultUnresolvedEvent()
		{
			this.EntityType = EntityType.Event;
		}
		
		public DefaultUnresolvedEvent(IUnresolvedTypeDefinition declaringType, string name)
		{
			this.EntityType = EntityType.Event;
			this.DeclaringTypeDefinition = declaringType;
			this.Name = name;
			if (declaringType != null)
				this.ParsedFile = declaringType.ParsedFile;
		}
		
		public bool CanAdd {
			get { return addAccessor != null; }
		}
		
		public bool CanRemove {
			get { return removeAccessor != null; }
		}
		
		public bool CanInvoke {
			get { return invokeAccessor != null; }
		}
		
		public IUnresolvedMethod AddAccessor {
			get { return addAccessor; }
			set {
				ThrowIfFrozen();
				addAccessor = value;
			}
		}
		
		public IUnresolvedMethod RemoveAccessor {
			get { return removeAccessor; }
			set {
				ThrowIfFrozen();
				removeAccessor = value;
			}
		}
		
		public IUnresolvedMethod InvokeAccessor {
			get { return invokeAccessor; }
			set {
				ThrowIfFrozen();
				invokeAccessor = value;
			}
		}
		
		public override IMember CreateResolved(ITypeResolveContext context)
		{
			return new DefaultResolvedEvent(this, context);
		}
	}
}
