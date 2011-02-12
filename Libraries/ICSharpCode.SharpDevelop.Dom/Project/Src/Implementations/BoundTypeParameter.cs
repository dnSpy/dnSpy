// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// A type parameter that was bound to a concrete type.
	/// </summary>
	public sealed class BoundTypeParameter : AbstractFreezable, ITypeParameter
	{
		readonly ITypeParameter baseTypeParameter;
		readonly IMethod owningMethod;
		readonly IClass owningClass;
		IReturnType boundTo;
		
		public BoundTypeParameter(ITypeParameter baseTypeParameter, IClass owningClass)
			: this(baseTypeParameter, owningClass, null)
		{
		}
		
		public BoundTypeParameter(ITypeParameter baseTypeParameter, IClass owningClass, IMethod owningMethod)
		{
			if (owningClass == null)
				throw new ArgumentNullException("owningClass");
			if (baseTypeParameter == null)
				throw new ArgumentNullException("baseTypeParameter");
			this.baseTypeParameter = baseTypeParameter;
			this.owningMethod = owningMethod;
			this.owningClass = owningClass;
		}
		
		protected override void FreezeInternal()
		{
			base.FreezeInternal();
			baseTypeParameter.Freeze();
			owningMethod.Freeze();
			owningClass.Freeze();
		}
		
		public string Name {
			get { return baseTypeParameter.Name; }
		}
		
		public int Index {
			get { return baseTypeParameter.Index; }
		}
		
		public IList<IAttribute> Attributes {
			get { return baseTypeParameter.Attributes; }
		}
		
		public IMethod Method {
			get { return owningMethod; }
		}
		
		public IClass Class {
			get { return owningClass; }
		}
		
		public IList<IReturnType> Constraints {
			get { return baseTypeParameter.Constraints; }
		}
		
		public bool HasConstructableConstraint {
			get { return baseTypeParameter.HasConstructableConstraint; }
		}
		
		public bool HasReferenceTypeConstraint {
			get { return baseTypeParameter.HasReferenceTypeConstraint; }
		}
		
		public bool HasValueTypeConstraint {
			get { return baseTypeParameter.HasValueTypeConstraint; }
		}
		
		public IReturnType BoundTo {
			get { return boundTo; }
			set {
				CheckBeforeMutation();
				boundTo = value;
			}
		}
		
		public ITypeParameter UnboundTypeParameter {
			get { return baseTypeParameter.UnboundTypeParameter; }
		}
	}
}
