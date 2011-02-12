// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// A return type that modifies the base return type and is not regarded equal to its base type.
	/// </summary>
	public abstract class DecoratingReturnType : ProxyReturnType
	{
		public abstract override bool Equals(IReturnType other);
		public abstract override int GetHashCode();
		
		public sealed override bool IsDefaultReturnType {
			get {
				return false;
			}
		}
		
		public abstract override T CastToDecoratingReturnType<T>();
		
		public override IReturnType GetDirectReturnType()
		{
			return this;
		}
	}
}
