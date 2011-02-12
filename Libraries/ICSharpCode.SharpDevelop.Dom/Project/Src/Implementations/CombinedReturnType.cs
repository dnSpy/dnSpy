// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// Combines multiple return types for use in constraints.
	/// </summary>
	public sealed class CombinedReturnType : AbstractReturnType
	{
		IList<IReturnType> baseTypes;
		
		string fullName;
		string name;
		string @namespace;
		string dotnetName;
		
		public override bool Equals(IReturnType obj)
		{
			CombinedReturnType combined = obj as CombinedReturnType;
			if (combined == null) return false;
			if (baseTypes.Count != combined.baseTypes.Count) return false;
			for (int i = 0; i < baseTypes.Count; i++) {
				if (!baseTypes[i].Equals(combined.baseTypes[i])) {
					return false;
				}
			}
			return true;
		}
		
		public override int GetHashCode()
		{
			unchecked {
				int res = 0;
				foreach (IReturnType rt in baseTypes) {
					res *= 1300027;
					res += rt.GetHashCode();
				}
				return res;
			}
		}
		
		public CombinedReturnType(IList<IReturnType> baseTypes, string fullName, string name, string @namespace, string dotnetName)
		{
			this.baseTypes = baseTypes;
			this.fullName = fullName;
			this.name = name;
			this.@namespace = @namespace;
			this.dotnetName = dotnetName;
		}
		
		public IList<IReturnType> BaseTypes {
			get {
				return baseTypes;
			}
		}
		
		List<T> Combine<T>(Converter<IReturnType, List<T>> conv) where T : IMember
		{
			int count = baseTypes.Count;
			if (count == 0)
				return null;
			List<T> list = null;
			foreach (IReturnType baseType in baseTypes) {
				List<T> newList = conv(baseType);
				if (newList == null)
					continue;
				if (list == null) {
					list = newList;
				} else {
					foreach (T element in newList) {
						bool found = false;
						foreach (T t in list) {
							if (t.CompareTo(element) == 0) {
								found = true;
								break;
							}
						}
						if (!found) {
							list.Add(element);
						}
					}
				}
			}
			return list;
		}
		
		public override List<IMethod> GetMethods()
		{
			return Combine<IMethod>(delegate(IReturnType type) { return type.GetMethods(); });
		}
		
		public override List<IProperty> GetProperties()
		{
			return Combine<IProperty>(delegate(IReturnType type) { return type.GetProperties(); });
		}
		
		public override List<IField> GetFields()
		{
			return Combine<IField>(delegate(IReturnType type) { return type.GetFields(); });
		}
		
		public override List<IEvent> GetEvents()
		{
			return Combine<IEvent>(delegate(IReturnType type) { return type.GetEvents(); });
		}
		
		public override string FullyQualifiedName {
			get {
				return fullName;
			}
		}
		
		public override string Name {
			get {
				return name;
			}
		}
		
		public override string Namespace {
			get {
				return @namespace;
			}
		}
		
		public override string DotNetName {
			get {
				return dotnetName;
			}
		}
		
		public override bool IsDefaultReturnType {
			get {
				return false;
			}
		}
		
		public override int TypeArgumentCount {
			get {
				return 0;
			}
		}
		
		public override IClass GetUnderlyingClass()
		{
			return null;
		}
	}
}
