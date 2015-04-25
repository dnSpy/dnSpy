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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Represents a specialized IEvent (event after type substitution).
	/// </summary>
	public class SpecializedEvent : SpecializedMember, IEvent
	{
		readonly IEvent eventDefinition;
		
		public SpecializedEvent(IEvent eventDefinition, TypeParameterSubstitution substitution)
			: base(eventDefinition)
		{
			this.eventDefinition = eventDefinition;
			AddSubstitution(substitution);
		}
		
		public bool CanAdd {
			get { return eventDefinition.CanAdd; }
		}
		
		public bool CanRemove {
			get { return eventDefinition.CanRemove; }
		}
		
		public bool CanInvoke {
			get { return eventDefinition.CanInvoke; }
		}
		
		IMethod addAccessor, removeAccessor, invokeAccessor;
		
		public IMethod AddAccessor {
			get { return WrapAccessor(ref this.addAccessor, eventDefinition.AddAccessor); }
		}
		
		public IMethod RemoveAccessor {
			get { return WrapAccessor(ref this.removeAccessor, eventDefinition.RemoveAccessor); }
		}
		
		public IMethod InvokeAccessor {
			get { return WrapAccessor(ref this.invokeAccessor, eventDefinition.InvokeAccessor); }
		}
	}
}
