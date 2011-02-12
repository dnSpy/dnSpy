// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// GenericReturnType is a reference to a type parameter.
	/// </summary>
	public sealed class GenericReturnType : DecoratingReturnType
	{
		ITypeParameter typeParameter;
		
		public ITypeParameter TypeParameter {
			get {
				return typeParameter;
			}
		}
		
		public override bool Equals(IReturnType rt)
		{
			if (rt == null || !rt.IsGenericReturnType)
				return false;
			GenericReturnType grt = rt.CastToGenericReturnType();
			if ((typeParameter.Method == null) != (grt.typeParameter.Method == null))
				return false;
			return typeParameter.Index == grt.typeParameter.Index;
		}
		
		public override int GetHashCode()
		{
			if (typeParameter.Method != null)
				return 17491 + typeParameter.Index;
			else
				return 81871 + typeParameter.Index;
		}
		
		public override T CastToDecoratingReturnType<T>()
		{
			if (typeof(T) == typeof(GenericReturnType)) {
				return (T)(object)this;
			} else {
				return null;
			}
		}
		
		public GenericReturnType(ITypeParameter typeParameter)
		{
			if (typeParameter == null)
				throw new ArgumentNullException("typeParameter");
			this.typeParameter = typeParameter;
		}
		
		public override string FullyQualifiedName {
			get {
				return typeParameter.Name;
			}
		}
		
		public override string Name {
			get {
				return typeParameter.Name;
			}
		}
		
		public override string Namespace {
			get {
				return "";
			}
		}
		
		public override string DotNetName {
			get {
				if (typeParameter.Method != null)
					return "``" + typeParameter.Index;
				else
					return "`" + typeParameter.Index;
			}
		}
		
		public override IClass GetUnderlyingClass()
		{
			return null;
		}
		
		public override IReturnType BaseType {
			get {
				int count = typeParameter.Constraints.Count;
				if (count == 0)
					return typeParameter.Class.ProjectContent.SystemTypes.Object;
				if (count == 1)
					return typeParameter.Constraints[0];
				return new CombinedReturnType(typeParameter.Constraints,
				                              FullyQualifiedName,
				                              Name, Namespace,
				                              DotNetName);
			}
		}
		
		// remove static methods (T.ReferenceEquals() is not possible)
		public override List<IMethod> GetMethods()
		{
			List<IMethod> list = base.GetMethods();
			if (list != null) {
				list.RemoveAll(delegate(IMethod m) { return m.IsStatic || m.IsConstructor; });
				if (typeParameter.HasConstructableConstraint || typeParameter.HasValueTypeConstraint) {
					list.Add(new Constructor(ModifierEnum.Public, this,
					                         DefaultTypeParameter.GetDummyClassForTypeParameter(typeParameter)));
				}
			}
			return list;
		}
		
		public override Nullable<bool> IsReferenceType {
			get { return null; }
		}
		
		public override string ToString()
		{
			return String.Format("[GenericReturnType: {0}]", typeParameter);
		}
	}
}
