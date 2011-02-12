// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// Base class for return types that wrap around other return types.
	/// </summary>
	public abstract class ProxyReturnType : IReturnType
	{
		public abstract IReturnType BaseType {
			get;
		}
		
		public sealed override bool Equals(object obj)
		{
			return Equals(obj as IReturnType);
		}
		
		public virtual bool Equals(IReturnType other)
		{
			// this check is necessary because the underlying Equals implementation
			// expects to be able to retrieve the base type of "other" - which fails when
			// this==other and therefore other.busy.
			if (other == this)
				return true;
			
			IReturnType baseType = BaseType;
			using (var l = busyManager.Enter(this)) {
				return (l.Success && baseType != null) ? baseType.Equals(other) : false;
			}
		}
		
		public override int GetHashCode()
		{
			IReturnType baseType = BaseType;
			using (var l = busyManager.Enter(this)) {
				return (l.Success && baseType != null) ? baseType.GetHashCode() : 0;
			}
		}
		
		protected int GetObjectHashCode()
		{
			return base.GetHashCode();
		}
		
		// Required to prevent stack overflow on inferrence cycles
		[ThreadStatic] static BusyManager _busyManager;
		
		static BusyManager busyManager {
			get { return _busyManager ?? (_busyManager = new BusyManager()); }
		}
		
		public virtual string FullyQualifiedName {
			get {
				IReturnType baseType = BaseType;
				using (var l = busyManager.Enter(this)) {
					return (l.Success && baseType != null) ? baseType.FullyQualifiedName : "?";
				}
			}
		}
		
		public virtual string Name {
			get {
				IReturnType baseType = BaseType;
				using (var l = busyManager.Enter(this)) {
					return (l.Success && baseType != null) ? baseType.Name : "?";
				}
			}
		}
		
		public virtual string Namespace {
			get {
				IReturnType baseType = BaseType;
				using (var l = busyManager.Enter(this)) {
					return (l.Success && baseType != null) ? baseType.Namespace : "?";
				}
			}
		}
		
		public virtual string DotNetName {
			get {
				IReturnType baseType = BaseType;
				using (var l = busyManager.Enter(this)) {
					return (l.Success && baseType != null) ? baseType.DotNetName : "?";
				}
			}
		}
		
		public virtual int TypeArgumentCount {
			get {
				IReturnType baseType = BaseType;
				using (var l = busyManager.Enter(this)) {
					return (l.Success && baseType != null) ? baseType.TypeArgumentCount : 0;
				}
			}
		}
		
		public virtual IClass GetUnderlyingClass()
		{
			IReturnType baseType = BaseType;
			using (var l = busyManager.Enter(this)) {
				return (l.Success && baseType != null) ? baseType.GetUnderlyingClass() : null;
			}
		}
		
		public virtual List<IMethod> GetMethods()
		{
			IReturnType baseType = BaseType;
			using (var l = busyManager.Enter(this)) {
				return (l.Success && baseType != null) ? baseType.GetMethods() : new List<IMethod>();
			}
		}
		
		public virtual List<IProperty> GetProperties()
		{
			IReturnType baseType = BaseType;
			using (var l = busyManager.Enter(this)) {
				return (l.Success && baseType != null) ? baseType.GetProperties() : new List<IProperty>();
			}
		}
		
		public virtual List<IField> GetFields()
		{
			IReturnType baseType = BaseType;
			using (var l = busyManager.Enter(this)) {
				return (l.Success && baseType != null) ? baseType.GetFields() : new List<IField>();
			}
		}
		
		public virtual List<IEvent> GetEvents()
		{
			IReturnType baseType = BaseType;
			using (var l = busyManager.Enter(this)) {
				return (l.Success && baseType != null) ? baseType.GetEvents() : new List<IEvent>();
			}
		}
		
		public virtual bool IsDefaultReturnType {
			get {
				IReturnType baseType = BaseType;
				using (var l = busyManager.Enter(this)) {
					return (l.Success && baseType != null) ? baseType.IsDefaultReturnType : false;
				}
			}
		}
		
		public bool IsDecoratingReturnType<T>() where T : DecoratingReturnType
		{
			return CastToDecoratingReturnType<T>() != null;
		}
		
		public virtual T CastToDecoratingReturnType<T>() where T : DecoratingReturnType
		{
			IReturnType baseType = BaseType;
			using (var l = busyManager.Enter(this)) {
				return (l.Success && baseType != null) ? baseType.CastToDecoratingReturnType<T>() : null;
			}
		}
		
		
		public bool IsArrayReturnType {
			get {
				return IsDecoratingReturnType<ArrayReturnType>();
			}
		}
		public ArrayReturnType CastToArrayReturnType()
		{
			return CastToDecoratingReturnType<ArrayReturnType>();
		}
		
		public bool IsGenericReturnType {
			get {
				return IsDecoratingReturnType<GenericReturnType>();
			}
		}
		public GenericReturnType CastToGenericReturnType()
		{
			return CastToDecoratingReturnType<GenericReturnType>();
		}
		
		public bool IsConstructedReturnType {
			get {
				return IsDecoratingReturnType<ConstructedReturnType>();
			}
		}
		public ConstructedReturnType CastToConstructedReturnType()
		{
			return CastToDecoratingReturnType<ConstructedReturnType>();
		}
		
		public virtual bool? IsReferenceType {
			get {
				IReturnType baseType = BaseType;
				using (var l = busyManager.Enter(this)) {
					return (l.Success && baseType != null) ? baseType.IsReferenceType : null;
				}
			}
		}
		
		public virtual IReturnType GetDirectReturnType()
		{
			IReturnType baseType = BaseType;
			using (var l = busyManager.Enter(this)) {
				return (l.Success && baseType != null) ? baseType.GetDirectReturnType() : UnknownReturnType.Instance;
			}
		}
	}
}
