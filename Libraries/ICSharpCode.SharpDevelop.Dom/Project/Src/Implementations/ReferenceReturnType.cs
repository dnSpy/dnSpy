// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// Return type used for "ref" or "out" expressions.
	/// </summary>
	public class ReferenceReturnType : DecoratingReturnType
	{
		IReturnType baseType;
		
		public ReferenceReturnType(IReturnType baseType)
		{
			this.baseType = baseType;
		}
		
		public override IReturnType GetDirectReturnType()
		{
			if (baseType == null)
				return this;
			IReturnType newBaseType = baseType.GetDirectReturnType();
			if (newBaseType == baseType)
				return this;
			else
				return new ReferenceReturnType(newBaseType);
		}
		
		public override IReturnType BaseType {
			get { return baseType; }
		}
		
		public override bool Equals(IReturnType other)
		{
			return object.Equals(baseType, other);
		}
		
		public override int GetHashCode()
		{
			return baseType != null ? baseType.GetHashCode() : 0;
		}
		
		public override T CastToDecoratingReturnType<T>()
		{
			if (typeof(T) == typeof(ReferenceReturnType)) {
				return (T)(object)this;
			} else if (baseType != null) {
				return baseType.CastToDecoratingReturnType<T>();
			} else {
				return null;
			}
		}
	}
}
