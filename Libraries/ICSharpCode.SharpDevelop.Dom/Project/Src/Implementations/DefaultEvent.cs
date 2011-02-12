// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom
{
	public class DefaultEvent : AbstractMember, IEvent
	{
		IMethod  addMethod;
		IMethod  removeMethod;
		IMethod  raiseMethod;
		
		protected override void FreezeInternal()
		{
			if (addMethod != null)
				addMethod.Freeze();
			if (removeMethod != null)
				removeMethod.Freeze();
			if (raiseMethod != null)
				raiseMethod.Freeze();
			base.FreezeInternal();
		}
		
		public override string DocumentationTag {
			get {
				return "E:" + this.DotNetName;
			}
		}
		
		public override IMember Clone()
		{
			DefaultEvent de = new DefaultEvent(Name, ReturnType, Modifiers, Region, BodyRegion, DeclaringType);
			de.CopyDocumentationFrom(this);
			foreach (ExplicitInterfaceImplementation eii in InterfaceImplementations) {
				de.InterfaceImplementations.Add(eii.Clone());
			}
			if (addMethod != null)
				de.addMethod = (IMethod)addMethod.Clone();
			if (removeMethod != null)
				de.removeMethod = (IMethod)removeMethod.Clone();
			if (raiseMethod != null)
				de.raiseMethod = (IMethod)raiseMethod.Clone();
			return de;
		}
		
		public DefaultEvent(IClass declaringType, string name) : base(declaringType, name)
		{
		}
		
		public DefaultEvent(string name, IReturnType type, ModifierEnum m, DomRegion region, DomRegion bodyRegion, IClass declaringType) : base(declaringType, name)
		{
			this.ReturnType = type;
			this.Region     = region;
			this.BodyRegion = bodyRegion;
			Modifiers       = (ModifierEnum)m;
			if (Modifiers == ModifierEnum.None) {
				Modifiers = ModifierEnum.Private;
			}
		}
		
		public virtual int CompareTo(IEvent value)
		{
			int cmp;
			
			if(0 != (cmp = base.CompareTo((IEntity)value)))
				return cmp;
			
			if (FullyQualifiedName != null) {
				return FullyQualifiedName.CompareTo(value.FullyQualifiedName);
			}
			
			return 0;
		}
		
		int IComparable.CompareTo(object value)
		{
			return CompareTo((IEvent)value);
		}
		
		public virtual IMethod AddMethod {
			get {
				return addMethod;
			}
			set {
				CheckBeforeMutation();
				addMethod = value;
			}
		}
		
		public virtual IMethod RemoveMethod {
			get {
				return removeMethod;
			}
			set {
				CheckBeforeMutation();
				removeMethod = value;
			}
		}
		
		public virtual IMethod RaiseMethod {
			get {
				return raiseMethod;
			}
			set {
				CheckBeforeMutation();
				raiseMethod = value;
			}
		}
		
		public override EntityType EntityType {
			get {
				return EntityType.Event;
			}
		}
	}
}
