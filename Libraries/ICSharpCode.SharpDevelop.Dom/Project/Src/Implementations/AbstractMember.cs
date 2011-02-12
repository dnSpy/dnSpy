// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	public abstract class AbstractMember : AbstractEntity, IMember
	{
		IReturnType returnType;
		DomRegion region;
		IList<ExplicitInterfaceImplementation> interfaceImplementations;
		IReturnType declaringTypeReference;
		
		protected override void FreezeInternal()
		{
			interfaceImplementations = FreezeList(interfaceImplementations);
			base.FreezeInternal();
		}
		
		public sealed override ICompilationUnit CompilationUnit {
			[System.Diagnostics.DebuggerStepThrough]
			get {
				return this.DeclaringType.CompilationUnit;
			}
		}
		
		public virtual DomRegion Region {
			get {
				return region;
			}
			set {
				CheckBeforeMutation();
				region = value;
			}
		}
		
		public virtual IReturnType ReturnType {
			get {
				return returnType;
			}
			set {
				CheckBeforeMutation();
				returnType = value;
			}
		}
		
		/// <summary>
		/// Gets the declaring type reference (declaring type incl. type arguments)
		/// </summary>
		public virtual IReturnType DeclaringTypeReference {
			get {
				return declaringTypeReference ?? this.DeclaringType.DefaultReturnType;
			}
			set {
				CheckBeforeMutation();
				declaringTypeReference = value;
			}
		}
		
		public IList<ExplicitInterfaceImplementation> InterfaceImplementations {
			get {
				return interfaceImplementations ?? (interfaceImplementations = new List<ExplicitInterfaceImplementation>());
			}
		}
		
		public AbstractMember(IClass declaringType, string name) : base(declaringType, name)
		{
			// members must have a parent class
			if (declaringType == null)
				throw new ArgumentNullException("declaringType");
		}
		
		public abstract IMember Clone();
		
		object ICloneable.Clone()
		{
			return this.Clone();
		}
		
		IMember genericMember;
		
		public virtual IMember GenericMember {
			get { return genericMember; }
		}
		
		public virtual IMember CreateSpecializedMember()
		{
			AbstractMember copy = Clone() as AbstractMember;
			if (copy == null)
				throw new Exception("Clone() must return an AbstractMember instance, or CreateSpecializedMember must also be overridden.");
			copy.genericMember = this;
			return copy;
		}
	}
}
