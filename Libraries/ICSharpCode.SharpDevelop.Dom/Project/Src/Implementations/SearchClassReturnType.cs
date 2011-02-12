// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Threading;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// The SearchClassReturnType is used when only a part of the class name is known and the
	/// type can only be resolved on demand (the ConvertVisitor uses SearchClassReturnTypes).
	/// </summary>
	public sealed class SearchClassReturnType : ProxyReturnType
	{
		IClass declaringClass;
		IProjectContent pc;
		int caretLine;
		int caretColumn;
		string name;
		string shortName;
		int typeParameterCount;
		bool lookForInnerClassesInDeclaringClass = true;
		
		public SearchClassReturnType(IProjectContent projectContent, IClass declaringClass, int caretLine, int caretColumn, string name, int typeParameterCount)
		{
			if (declaringClass == null)
				throw new ArgumentNullException("declaringClass");
			this.declaringClass = declaringClass;
			this.pc = projectContent;
			this.caretLine = caretLine;
			this.caretColumn = caretColumn;
			this.typeParameterCount = typeParameterCount;
			this.name = name;
			int pos = name.LastIndexOf('.');
			if (pos < 0)
				shortName = name;
			else
				shortName = name.Substring(pos + 1);
		}
		
		/// <summary>
		/// Gets/Sets whether to look for inner classes in the declaring class.
		/// The default is true.
		/// Set this property to false for return types that are used as base type references.
		/// </summary>
		public bool LookForInnerClassesInDeclaringClass {
			get { return lookForInnerClassesInDeclaringClass; }
			set {
				lookForInnerClassesInDeclaringClass = value;
				ClearCachedBaseType();
			}
		}
		
		volatile IReturnType cachedBaseType;
		
		//int isSearching; // 0=false, 1=true
		
		// Required to prevent stack overflow on inferrence cycles
		// replaces old 'isSearching' flag which wasn't thread-safe
		[ThreadStatic] static BusyManager _busyManager;
		
		static BusyManager busyManager {
			get { return _busyManager ?? (_busyManager = new BusyManager()); }
		}
		
		void ClearCachedBaseType()
		{
			cachedBaseType = null;
		}
		
		public override IReturnType BaseType {
			get {
				IReturnType type = cachedBaseType;
				if (type != null)
					return type;
				using (var l = busyManager.Enter(this)) {
					// abort if called recursively on the same thread
					if (!l.Success)
						return null;
					SearchTypeRequest request = new SearchTypeRequest(name, typeParameterCount, declaringClass, caretLine, caretColumn);
					if (!lookForInnerClassesInDeclaringClass) {
						// skip looking for inner classes by adjusting the CurrentType for the lookup
						request.CurrentType = declaringClass.DeclaringType;
					}
					type = pc.SearchType(request).Result;
					cachedBaseType = type;
					if (type != null)
						DomCache.RegisterForClear(ClearCachedBaseType);
					return type;
				}
			}
		}
		
		public override string FullyQualifiedName {
			get {
				string tmp = base.FullyQualifiedName;
				if (tmp == "?") {
					return name;
				}
				return tmp;
			}
		}
		
		public override string Name {
			get {
				return shortName;
			}
		}
		
		public override string DotNetName {
			get {
				string tmp = base.DotNetName;
				if (tmp == "?") {
					return name;
				}
				return tmp;
			}
		}
		
		public override string ToString()
		{
			return String.Format("[SearchClassReturnType: {0}]", name);
		}
	}
}
