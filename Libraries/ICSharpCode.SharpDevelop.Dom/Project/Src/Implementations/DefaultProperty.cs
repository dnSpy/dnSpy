// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.SharpDevelop.Dom {

	public class DefaultProperty : AbstractMember, IProperty
	{
		DomRegion getterRegion = DomRegion.Empty;
		DomRegion setterRegion = DomRegion.Empty;
		
		IList<IParameter> parameters = null;
		internal byte accessFlags;
		const byte indexerFlag   = 0x01;
		const byte getterFlag    = 0x02;
		const byte setterFlag    = 0x04;
		const byte extensionFlag = 0x08;
		ModifierEnum getterModifiers, setterModifiers;
		
		protected override void FreezeInternal()
		{
			parameters = FreezeList(parameters);
			base.FreezeInternal();
		}
		
		public bool IsIndexer {
			get { return (accessFlags & indexerFlag) == indexerFlag; }
			set {
				CheckBeforeMutation();
				if (value) accessFlags |= indexerFlag; else accessFlags &= 255-indexerFlag;
			}
		}
		
		public bool CanGet {
			get { return (accessFlags & getterFlag) == getterFlag; }
			set {
				CheckBeforeMutation();
				if (value) accessFlags |= getterFlag; else accessFlags &= 255-getterFlag;
			}
		}

		public bool CanSet {
			get { return (accessFlags & setterFlag) == setterFlag; }
			set {
				CheckBeforeMutation();
				if (value) accessFlags |= setterFlag; else accessFlags &= 255-setterFlag;
			}
		}
		
		public bool IsExtensionMethod {
			get { return (accessFlags & extensionFlag) == extensionFlag; }
			set {
				CheckBeforeMutation();
				if (value) accessFlags |= extensionFlag; else accessFlags &= 255-extensionFlag;
			}
		}
		
		public override string DocumentationTag {
			get {
				string dotnetName = this.DotNetName;
				StringBuilder b = new StringBuilder("P:", dotnetName.Length + 2);
				b.Append(dotnetName);
				IList<IParameter> paras = this.Parameters;
				if (paras.Count > 0) {
					b.Append('(');
					for (int i = 0; i < paras.Count; ++i) {
						if (i > 0) b.Append(',');
						IReturnType rt = paras[i].ReturnType;
						if (rt != null) {
							b.Append(rt.DotNetName);
						}
					}
					b.Append(')');
				}
				return b.ToString();
			}
		}
		
		public override IMember Clone()
		{
			DefaultProperty p = new DefaultProperty(Name, ReturnType, Modifiers, Region, BodyRegion, DeclaringType);
			p.parameters = DefaultParameter.Clone(this.Parameters);
			p.getterModifiers = this.getterModifiers;
			p.setterModifiers = this.setterModifiers;
			p.getterRegion = this.getterRegion;
			p.setterRegion = this.setterRegion;
			p.CopyDocumentationFrom(this);
			p.accessFlags = this.accessFlags;
			foreach (ExplicitInterfaceImplementation eii in InterfaceImplementations) {
				p.InterfaceImplementations.Add(eii.Clone());
			}
			return p;
		}
		
		public virtual IList<IParameter> Parameters {
			get {
				if (parameters == null) {
					parameters = new List<IParameter>();
				}
				return parameters;
			}
			set {
				CheckBeforeMutation();
				parameters = value;
			}
		}
		
		public DomRegion GetterRegion {
			get { return getterRegion; }
			set {
				CheckBeforeMutation();
				getterRegion = value;
			}
		}
		
		public DomRegion SetterRegion {
			get { return setterRegion; }
			set {
				CheckBeforeMutation();
				setterRegion = value;
			}
		}
		
		public ModifierEnum GetterModifiers {
			get { return getterModifiers; }
			set {
				CheckBeforeMutation();
				getterModifiers = value;
			}
		}
		
		public ModifierEnum SetterModifiers {
			get { return setterModifiers; }
			set {
				CheckBeforeMutation();
				setterModifiers = value;
			}
		}
		
		public DefaultProperty(IClass declaringType, string name) : base(declaringType, name)
		{
		}
		
		public DefaultProperty(string name, IReturnType type, ModifierEnum m, DomRegion region, DomRegion bodyRegion, IClass declaringType) : base(declaringType, name)
		{
			this.ReturnType = type;
			this.Region = region;
			this.BodyRegion = bodyRegion;
			Modifiers = m;
		}
		
		public virtual int CompareTo(IProperty value)
		{
			int cmp = string.CompareOrdinal(this.FullyQualifiedName, value.FullyQualifiedName);
			if (cmp != 0) {
				return cmp;
			}
			
			return DiffUtility.Compare(Parameters, value.Parameters);
		}
		
		int IComparable.CompareTo(object value) {
			return CompareTo((IProperty)value);
		}
		
		public override EntityType EntityType {
			get {
				return EntityType.Property;
			}
		}
	}
}
