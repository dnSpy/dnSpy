// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// Represents an unmanaged pointer type.
	/// </summary>
	public class PointerReturnType : DecoratingReturnType
	{
		IReturnType baseType;
		
		public PointerReturnType(IReturnType baseType)
		{
			if (baseType == null)
				throw new ArgumentNullException("baseType");
			this.baseType = baseType;
		}
		
		public override IReturnType BaseType {
			get { return baseType; }
		}
		
		public override T CastToDecoratingReturnType<T>()
		{
			if (typeof(T) == typeof(PointerReturnType)) {
				return (T)(object)this;
			} else {
				return null;
			}
		}
		
		public override IReturnType GetDirectReturnType()
		{
			IReturnType newBaseType = baseType.GetDirectReturnType();
			if (newBaseType == baseType)
				return this;
			else
				return new PointerReturnType(newBaseType);
		}
		
		public override bool Equals(IReturnType rt)
		{
			if (rt == null) return false;
			PointerReturnType prt = rt.CastToDecoratingReturnType<PointerReturnType>();
			if (prt == null) return false;
			return baseType.Equals(prt.baseType);
		}
		
		public override int GetHashCode()
		{
			unchecked {
				return 53 * baseType.GetHashCode();
			}
		}
		
		public override string DotNetName {
			get { return baseType.DotNetName + "*"; }
		}
		
		public override string FullyQualifiedName {
			get { return baseType.FullyQualifiedName + "*"; }
		}
		
		public override List<IEvent> GetEvents()
		{
			return new List<IEvent>();
		}
		
		public override List<IField> GetFields()
		{
			return new List<IField>();
		}
		
		public override List<IMethod> GetMethods()
		{
			return new List<IMethod>();
		}
		
		public override List<IProperty> GetProperties()
		{
			return new List<IProperty>();
		}
		
		public override IClass GetUnderlyingClass()
		{
			return null;
		}
		
		public override string Name {
			get { return baseType.Name + "*"; }
		}
		
		public override string Namespace {
			get { return baseType.Namespace; }
		}
		
		public override int TypeArgumentCount {
			get { return 0; }
		}
	}
}
