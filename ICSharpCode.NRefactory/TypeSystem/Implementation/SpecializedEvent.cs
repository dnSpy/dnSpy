// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Represents a specialized IEvent (e.g. after type substitution).
	/// </summary>
	public class SpecializedEvent : DefaultEvent
	{
		readonly IMember memberDefinition;
		IType declaringType;
		
		public SpecializedEvent(IEvent e) : base(e)
		{
			this.memberDefinition = e.MemberDefinition;
			this.declaringType = e.DeclaringType;
		}
		
		public override IType DeclaringType {
			get { return declaringType; }
		}
		
		public void SetDeclaringType(IType declaringType)
		{
			CheckBeforeMutation();
			this.declaringType = declaringType;
		}
		
		public override IMember MemberDefinition {
			get { return memberDefinition; }
		}
		
		public override int GetHashCode()
		{
			int hashCode = 0;
			unchecked {
				if (memberDefinition != null)
					hashCode += 1000000007 * memberDefinition.GetHashCode();
				if (declaringType != null)
					hashCode += 1000000009 * declaringType.GetHashCode();
			}
			return hashCode;
		}
		
		public override bool Equals(object obj)
		{
			SpecializedEvent other = obj as SpecializedEvent;
			if (other == null)
				return false;
			return object.Equals(this.memberDefinition, other.memberDefinition) && object.Equals(this.declaringType, other.declaringType);
		}
	}
}
