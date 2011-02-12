// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// The type reference that is the element of an enumerable.
	/// This class is used in combination with an InferredReturnType to
	/// represent the implicitly typed loop variable <c>v</c> in
	/// "<c>foreach (var v in enumerableInstance) {}</c>"
	/// </summary>
	public class ElementReturnType : ProxyReturnType
	{
		IReturnType enumerableType;
		IProjectContent pc;
		
		public ElementReturnType(IProjectContent pc, IReturnType enumerableType)
		{
			if (pc == null)
				throw new ArgumentNullException("pc");
			this.enumerableType = enumerableType;
			this.pc = pc;
		}
		
		[ThreadStatic] static BusyManager _busyManager;
		
		static BusyManager busyManager {
			get { return _busyManager ?? (_busyManager = new BusyManager()); }
		}
		
		public override IReturnType BaseType {
			get {
				using (var l = busyManager.Enter(this)) {
					if (!l.Success)
						return null;
					
					// get element type from enumerableType
					if (enumerableType.IsArrayReturnType)
						return enumerableType.CastToArrayReturnType().ArrayElementType;
					
					IClass c = enumerableType.GetUnderlyingClass();
					if (c == null)
						return null;
					IClass genumerable = pc.GetClass("System.Collections.Generic.IEnumerable", 1);
					if (c.IsTypeInInheritanceTree(genumerable)) {
						return MemberLookupHelper.GetTypeParameterPassedToBaseClass(enumerableType, genumerable, 0);
					}
					IClass enumerable = pc.GetClass("System.Collections.IEnumerable", 0);
					if (c.IsTypeInInheritanceTree(enumerable)) {
						return pc.SystemTypes.Object;
					}
					return null;
				}
			}
		}
		
		public override string ToString()
		{
			return "[ElementReturnType " + enumerableType + "]";
		}
	}
}
