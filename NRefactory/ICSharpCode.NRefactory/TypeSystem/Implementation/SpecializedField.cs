// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Represents a specialized IField (e.g. after type substitution).
	/// </summary>
	public class SpecializedField : DefaultField
	{
		readonly IMember memberDefinition;
		IType declaringType;
		
		public SpecializedField(IField f) : base(f)
		{
			this.memberDefinition = f.MemberDefinition;
			this.declaringType = f.DeclaringType;
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
			SpecializedField other = obj as SpecializedField;
			if (other == null)
				return false;
			return object.Equals(this.memberDefinition, other.memberDefinition) && object.Equals(this.declaringType, other.declaringType);
		}
	}
}
