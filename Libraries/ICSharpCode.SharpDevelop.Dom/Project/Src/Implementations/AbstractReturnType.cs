// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// Abstract return type for return types that are not a <see cref="ProxyReturnType"/>.
	/// </summary>
	public abstract class AbstractReturnType : IReturnType
	{
		public abstract IClass GetUnderlyingClass();
		public abstract List<IMethod>   GetMethods();
		public abstract List<IProperty> GetProperties();
		public abstract List<IField>    GetFields();
		public abstract List<IEvent>    GetEvents();
		
		public virtual int TypeArgumentCount {
			get {
				return 0;
			}
		}
		
		public virtual bool Equals(IReturnType other)
		{
			if (other == null)
				return false;
			return other.IsDefaultReturnType && DefaultReturnType.Equals(this, other);
		}
		
		public sealed override bool Equals(object o)
		{
			return Equals(o as IReturnType);
		}
		
		public override int GetHashCode()
		{
			return DefaultReturnType.GetHashCode(this);
		}
		
		string fullyQualifiedName = null;
		
		public virtual string FullyQualifiedName {
			get {
				if (fullyQualifiedName == null) {
					return String.Empty;
				}
				return fullyQualifiedName;
			}
			set {
				fullyQualifiedName = value;
			}
		}
		
		public virtual string Name {
			get {
				if (FullyQualifiedName == null) {
					return null;
				}
				int index = FullyQualifiedName.LastIndexOf('.');
				return index < 0 ? FullyQualifiedName : FullyQualifiedName.Substring(index + 1);
			}
		}

		public virtual string Namespace {
			get {
				if (FullyQualifiedName == null) {
					return null;
				}
				int index = FullyQualifiedName.LastIndexOf('.');
				return index < 0 ? String.Empty : FullyQualifiedName.Substring(0, index);
			}
		}
		
		public virtual string DotNetName {
			get {
				return FullyQualifiedName;
			}
		}
		
		public virtual bool IsDefaultReturnType {
			get {
				return true;
			}
		}
		
		public virtual bool IsArrayReturnType {
			get {
				return false;
			}
		}
		public virtual ArrayReturnType CastToArrayReturnType()
		{
			return null;
		}
		
		public virtual bool IsGenericReturnType {
			get {
				return false;
			}
		}
		public virtual GenericReturnType CastToGenericReturnType()
		{
			return null;
		}
		
		public virtual bool IsConstructedReturnType {
			get {
				return false;
			}
		}
		public virtual ConstructedReturnType CastToConstructedReturnType()
		{
			return null;
		}
		
		public bool IsDecoratingReturnType<T>() where T : DecoratingReturnType
		{
			return false;
		}
		
		public T CastToDecoratingReturnType<T>() where T : DecoratingReturnType
		{
			return null;
		}
		
		public virtual bool? IsReferenceType { get { return null; } }
		
		public virtual IReturnType GetDirectReturnType()
		{
			return this;
		}
	}
}
