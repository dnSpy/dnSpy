// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// ConstructedReturnType is a reference to generic class that specifies the type parameters.
	/// When getting the Members, this return type modifies the lists in such a way that the
	/// <see cref="GenericReturnType"/>s are replaced with the return types in the type parameters
	/// collection.
	/// Example: List&lt;string&gt;
	/// </summary>
	public sealed class ConstructedReturnType : DecoratingReturnType
	{
		// Return types that should be substituted for the generic types
		// If a substitution is unknown (type could not be resolved), the list
		// contains a null entry.
		IList<IReturnType> typeArguments;
		IReturnType baseType;
		
		public IList<IReturnType> TypeArguments {
			get {
				return typeArguments;
			}
		}
		
		public ConstructedReturnType(IReturnType baseType, IList<IReturnType> typeArguments)
		{
			if (baseType == null)
				throw new ArgumentNullException("baseType");
			if (typeArguments == null)
				throw new ArgumentNullException("typeArguments");
			this.typeArguments = typeArguments;
			this.baseType = baseType;
		}
		
		public override T CastToDecoratingReturnType<T>()
		{
			if (typeof(T) == typeof(ConstructedReturnType)) {
				return (T)(object)this;
			} else {
				return null;
			}
		}
		
		public override bool Equals(IReturnType rt)
		{
			return rt != null
				&& rt.IsConstructedReturnType
				&& this.DotNetName == rt.DotNetName;
		}
		
		public override int GetHashCode()
		{
			return this.DotNetName.GetHashCode();
		}
		
		public override IReturnType GetDirectReturnType()
		{
			IReturnType newBaseType = baseType.GetDirectReturnType();
			IReturnType[] newTypeArguments = new IReturnType[typeArguments.Count];
			bool typeArgumentsChanged = false;
			for (int i = 0; i < typeArguments.Count; i++) {
				if (typeArguments[i] != null)
					newTypeArguments[i] = typeArguments[i].GetDirectReturnType();
				if (typeArguments[i] != newTypeArguments[i])
					typeArgumentsChanged = true;
			}
			if (baseType == newBaseType && !typeArgumentsChanged)
				return this;
			else
				return new ConstructedReturnType(newBaseType, newTypeArguments);
		}
		
		public override IReturnType BaseType {
			get {
				return baseType;
			}
		}
		
		public IReturnType UnboundType {
			get {
				return baseType;
			}
		}
		
		/// <summary>
		/// Gets if <paramref name="t"/> is/contains a generic return type referring to a class type parameter.
		/// </summary>
		bool CheckReturnType(IReturnType t)
		{
			if (t == null) {
				return false;
			}
			if (t.IsGenericReturnType) {
				return t.CastToGenericReturnType().TypeParameter.Method == null;
			} else if (t.IsArrayReturnType) {
				return CheckReturnType(t.CastToArrayReturnType().ArrayElementType);
			} else if (t.IsConstructedReturnType) {
				foreach (IReturnType para in t.CastToConstructedReturnType().TypeArguments) {
					if (CheckReturnType(para)) return true;
				}
				return false;
			} else {
				return false;
			}
		}
		
		bool CheckParameters(IList<IParameter> l)
		{
			foreach (IParameter p in l) {
				if (CheckReturnType(p.ReturnType)) return true;
			}
			return false;
		}
		
		public override string DotNetName {
			get {
				string baseName = baseType.DotNetName;
				int pos = baseName.LastIndexOf('`');
				StringBuilder b;
				if (pos < 0)
					b = new StringBuilder(baseName);
				else
					b = new StringBuilder(baseName, 0, pos, pos + 20);
				b.Append('{');
				for (int i = 0; i < typeArguments.Count; ++i) {
					if (i > 0) b.Append(',');
					if (typeArguments[i] != null) {
						b.Append(typeArguments[i].DotNetName);
					}
				}
				b.Append('}');
				return b.ToString();
			}
		}
		
		public static IReturnType TranslateType(IReturnType input, IList<IReturnType> typeParameters, bool convertForMethod)
		{
			if (input == null || typeParameters == null || typeParameters.Count == 0) {
				return input; // nothing to do when there are no type parameters specified
			}
			if (input.IsGenericReturnType) {
				GenericReturnType rt = input.CastToGenericReturnType();
				if (convertForMethod ? (rt.TypeParameter.Method != null) : (rt.TypeParameter.Method == null)) {
					if (rt.TypeParameter.Index < typeParameters.Count) {
						IReturnType newType = typeParameters[rt.TypeParameter.Index];
						if (newType != null) {
							return newType;
						}
					}
				}
			} else if (input.IsArrayReturnType) {
				ArrayReturnType arInput = input.CastToArrayReturnType();
				IReturnType e = arInput.ArrayElementType;
				IReturnType t = TranslateType(e, typeParameters, convertForMethod);
				if (e != t && t != null)
					return new ArrayReturnType(arInput.ProjectContent, t, arInput.ArrayDimensions);
			} else if (input.IsConstructedReturnType) {
				ConstructedReturnType cinput = input.CastToConstructedReturnType();
				List<IReturnType> para = new List<IReturnType>(cinput.TypeArguments.Count);
				foreach (IReturnType argument in cinput.TypeArguments) {
					para.Add(TranslateType(argument, typeParameters, convertForMethod));
				}
				return new ConstructedReturnType(cinput.UnboundType, para);
			}
			return input;
		}
		
		IReturnType TranslateType(IReturnType input)
		{
			return TranslateType(input, typeArguments, false);
		}
		
		public override List<IMethod> GetMethods()
		{
			List<IMethod> l = baseType.GetMethods();
			for (int i = 0; i < l.Count; ++i) {
				if (CheckReturnType(l[i].ReturnType) || CheckParameters(l[i].Parameters)) {
					l[i] = (IMethod)l[i].CreateSpecializedMember();
					if (l[i].DeclaringType == baseType.GetUnderlyingClass()) {
						l[i].DeclaringTypeReference = this;
					}
					l[i].ReturnType = TranslateType(l[i].ReturnType);
					for (int j = 0; j < l[i].Parameters.Count; ++j) {
						l[i].Parameters[j].ReturnType = TranslateType(l[i].Parameters[j].ReturnType);
					}
				}
			}
			return l;
		}
		
		public override List<IProperty> GetProperties()
		{
			List<IProperty> l = baseType.GetProperties();
			for (int i = 0; i < l.Count; ++i) {
				if (CheckReturnType(l[i].ReturnType) || CheckParameters(l[i].Parameters)) {
					l[i] = (IProperty)l[i].CreateSpecializedMember();
					if (l[i].DeclaringType == baseType.GetUnderlyingClass()) {
						l[i].DeclaringTypeReference = this;
					}
					l[i].ReturnType = TranslateType(l[i].ReturnType);
					for (int j = 0; j < l[i].Parameters.Count; ++j) {
						l[i].Parameters[j].ReturnType = TranslateType(l[i].Parameters[j].ReturnType);
					}
				}
			}
			return l;
		}
		
		public override List<IField> GetFields()
		{
			List<IField> l = baseType.GetFields();
			for (int i = 0; i < l.Count; ++i) {
				if (CheckReturnType(l[i].ReturnType)) {
					l[i] = (IField)l[i].CreateSpecializedMember();
					if (l[i].DeclaringType == baseType.GetUnderlyingClass()) {
						l[i].DeclaringTypeReference = this;
					}
					l[i].ReturnType = TranslateType(l[i].ReturnType);
				}
			}
			return l;
		}
		
		public override List<IEvent> GetEvents()
		{
			List<IEvent> l = baseType.GetEvents();
			for (int i = 0; i < l.Count; ++i) {
				if (CheckReturnType(l[i].ReturnType)) {
					l[i] = (IEvent)l[i].CreateSpecializedMember();
					if (l[i].DeclaringType == baseType.GetUnderlyingClass()) {
						l[i].DeclaringTypeReference = this;
					}
					l[i].ReturnType = TranslateType(l[i].ReturnType);
				}
			}
			return l;
		}
		
		public override string ToString()
		{
			string r = "[ConstructedReturnType: ";
			r += baseType;
			r += "<";
			for (int i = 0; i < typeArguments.Count; i++) {
				if (i > 0) r += ",";
				if (typeArguments[i] != null) {
					r += typeArguments[i];
				}
			}
			return r + ">]";
		}
	}
}
