// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// DefaultReturnType is a reference to a normal class or a reference to a generic class where
	/// the type parameters are NOT specified.
	/// E.g. "System.Int32", "System.Void", "System.String", "System.Collections.Generic.List"
	/// </summary>
	public class DefaultReturnType : AbstractReturnType
	{
		public static bool Equals(IReturnType rt1, IReturnType rt2)
		{
			if (rt1 == rt2) return true;
			if (rt1 == null || rt2 == null) return false;
			IClass c1 = rt1.GetUnderlyingClass();
			IClass c2 = rt2.GetUnderlyingClass();
			if (c1 == null && c2 == null) {
				// guess if the classes are equal
				return rt1.FullyQualifiedName == rt2.FullyQualifiedName && rt1.TypeArgumentCount == rt2.TypeArgumentCount;
			} else {
				if (c1 == c2)
					return true;
				if (c1 == null || c2 == null)
					return false;
				return c1.FullyQualifiedName == c2.FullyQualifiedName && c1.TypeParameters.Count == c2.TypeParameters.Count;
			}
		}
		
		public static int GetHashCode(IReturnType rt)
		{
			if (rt == null)
				return 0;
			return (rt.FullyQualifiedName ?? "").GetHashCode() ^ rt.TypeArgumentCount;
		}
		
		IClass c;
		
		public DefaultReturnType(IClass c)
		{
			if (c == null)
				throw new ArgumentNullException("c");
			this.c = c;
		}
		
		public override string ToString()
		{
			return c.FullyQualifiedName;
		}
		
		public override int TypeArgumentCount {
			get {
				return c.TypeParameters.Count;
			}
		}
		
		public override IClass GetUnderlyingClass()
		{
			return c;
		}
		
		// Required to prevent stack overflow when calling GetMethods() on a class with cyclic inheritance
		// replaces old 'getMembersBusy' flag which wasn't thread-safe
		[ThreadStatic] static BusyManager _busyManager;
		
		static BusyManager busyManager {
			get { return _busyManager ?? (_busyManager = new BusyManager()); }
		}
		
		public override List<IMethod> GetMethods()
		{
			List<IMethod> l = new List<IMethod>();
			using (var busyLock = busyManager.Enter(this)) {
				if (busyLock.Success) {
					l.AddRange(c.Methods);
					if (c.AddDefaultConstructorIfRequired && !c.IsStatic) {
						// A constructor is added for classes that do not have a default constructor;
						// and for all structs.
						if (c.ClassType == ClassType.Class && !l.Exists(m => m.IsConstructor)) {
							l.Add(Constructor.CreateDefault(c));
						} else if (c.ClassType == ClassType.Struct || c.ClassType == ClassType.Enum) {
							l.Add(Constructor.CreateDefault(c));
						}
					}
					
					if (c.ClassType == ClassType.Interface) {
						if (c.BaseTypes.Count == 0) {
							AddMethodsFromBaseType(l, c.ProjectContent.SystemTypes.Object);
						} else {
							foreach (IReturnType baseType in c.BaseTypes) {
								AddMethodsFromBaseType(l, baseType);
							}
						}
					} else {
						AddMethodsFromBaseType(l, c.BaseType);
					}
				}
			}
			return l;
		}
		
		void AddMethodsFromBaseType(List<IMethod> l, IReturnType baseType)
		{
			if (baseType != null) {
				foreach (IMethod m in baseType.GetMethods()) {
					if (m.IsConstructor)
						continue;
					
					/*bool ok = true;
					if (m.IsOverridable) {
						StringComparer comparer = m.DeclaringType.ProjectContent.Language.NameComparer;
						foreach (IMethod oldMethod in c.Methods) {
							if (comparer.Equals(oldMethod.Name, m.Name)) {
								if (m.IsStatic == oldMethod.IsStatic && object.Equals(m.ReturnType, oldMethod.ReturnType)) {
									if (DiffUtility.Compare(oldMethod.Parameters, m.Parameters) == 0) {
										ok = false;
										break;
									}
								}
							}
						}
					}
					if (ok)
						l.Add(m);*/
					l.Add(m);
				}
			}
		}
		
		public override List<IProperty> GetProperties()
		{
			List<IProperty> l = new List<IProperty>();
			using (var busyLock = busyManager.Enter(this)) {
				if (busyLock.Success) {
					l.AddRange(c.Properties);
					if (c.ClassType == ClassType.Interface) {
						foreach (IReturnType baseType in c.BaseTypes) {
							AddPropertiesFromBaseType(l, baseType);
						}
					} else {
						AddPropertiesFromBaseType(l, c.BaseType);
					}
				}
			}
			return l;
		}
		
		void AddPropertiesFromBaseType(List<IProperty> l, IReturnType baseType)
		{
			if (baseType != null) {
				foreach (IProperty p in baseType.GetProperties()) {
					/*bool ok = true;
					if (p.IsOverridable) {
						StringComparer comparer = p.DeclaringType.ProjectContent.Language.NameComparer;
						foreach (IProperty oldProperty in c.Properties) {
							if (comparer.Equals(oldProperty.Name, p.Name)) {
								if (p.IsStatic == oldProperty.IsStatic && object.Equals(p.ReturnType, oldProperty.ReturnType)) {
									if (DiffUtility.Compare(oldProperty.Parameters, p.Parameters) == 0) {
										ok = false;
										break;
									}
								}
							}
						}
					}
					if (ok)
						l.Add(p);*/
					l.Add(p);
				}
			}
		}
		
		public override List<IField> GetFields()
		{
			List<IField> l = new List<IField>();
			using (var busyLock = busyManager.Enter(this)) {
				if (busyLock.Success) {
					l.AddRange(c.Fields);
					if (c.ClassType == ClassType.Interface) {
						foreach (IReturnType baseType in c.BaseTypes) {
							l.AddRange(baseType.GetFields());
						}
					} else {
						IReturnType baseType = c.BaseType;
						if (baseType != null) {
							l.AddRange(baseType.GetFields());
						}
					}
				}
			}
			return l;
		}
		
		public override List<IEvent> GetEvents()
		{
			List<IEvent> l = new List<IEvent>();
			using (var busyLock = busyManager.Enter(this)) {
				if (busyLock.Success) {
					l.AddRange(c.Events);
					if (c.ClassType == ClassType.Interface) {
						foreach (IReturnType baseType in c.BaseTypes) {
							l.AddRange(baseType.GetEvents());
						}
					} else {
						IReturnType baseType = c.BaseType;
						if (baseType != null) {
							l.AddRange(baseType.GetEvents());
						}
					}
				}
			}
			return l;
		}
		
		public override string FullyQualifiedName {
			get {
				return c.FullyQualifiedName;
			}
			set {
				throw new NotSupportedException();
			}
		}
		
		public override string Name {
			get {
				return c.Name;
			}
		}
		
		public override string Namespace {
			get {
				return c.Namespace;
			}
		}
		
		public override string DotNetName {
			get {
				return c.DotNetName;
			}
		}
		
		public override Nullable<bool> IsReferenceType {
			get {
				return (this.c.ClassType == ClassType.Class
				        || this.c.ClassType == ClassType.Interface
				        || this.c.ClassType == ClassType.Delegate);
			}
		}
	}
}
