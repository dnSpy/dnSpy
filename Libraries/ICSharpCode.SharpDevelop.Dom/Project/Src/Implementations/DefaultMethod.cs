// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ICSharpCode.SharpDevelop.Dom
{
	public class Constructor : DefaultMethod
	{
		public Constructor(ModifierEnum m, DomRegion region, DomRegion bodyRegion, IClass declaringType)
			: base((m & ModifierEnum.Static) != 0 ? "#cctor" : "#ctor",
			       declaringType.DefaultReturnType,
			       m, region, bodyRegion, declaringType)
		{
		}
		
		public Constructor(ModifierEnum m, IReturnType returnType, IClass declaringType)
			: base((m & ModifierEnum.Static) != 0 ? "#cctor" : "#ctor",
			       returnType, m, DomRegion.Empty, DomRegion.Empty, declaringType)
		{
		}
		
		/// <summary>
		/// Creates a default constructor for the class.
		/// The constructor has the region of the class and a documentation comment saying
		/// it is a default constructor.
		/// </summary>
		public static Constructor CreateDefault(IClass c)
		{
			if (c == null)
				throw new ArgumentNullException("c");
			
			ModifierEnum modifiers = ModifierEnum.Synthetic;
			if (c.IsAbstract)
				modifiers |= ModifierEnum.Protected;
			else
				modifiers |= ModifierEnum.Public;
			DomRegion region = new DomRegion(c.Region.BeginLine, c.Region.BeginColumn, c.Region.BeginLine, c.Region.BeginColumn);
			Constructor con = new Constructor(modifiers, region, region, c);
			con.Documentation = "Default constructor of " + c.Name;
			return con;
		}
	}
	
	[Serializable]
	public class Destructor : DefaultMethod
	{
		public Destructor(DomRegion region, DomRegion bodyRegion, IClass declaringType)
			: base("#dtor", null, ModifierEnum.None, region, bodyRegion, declaringType)
		{
		}
	}
	
	[Serializable]
	public class DefaultMethod : AbstractMember, IMethod
	{
		IList<IParameter> parameters;
		IList<ITypeParameter> typeParameters;
		IList<string> handlesClauses;
		
		protected override void FreezeInternal()
		{
			parameters = FreezeList(parameters);
			typeParameters = FreezeList(typeParameters);
			handlesClauses = FreezeList(handlesClauses);
			base.FreezeInternal();
		}
		
		bool isExtensionMethod;
		
		public bool IsExtensionMethod {
			get {
				return isExtensionMethod;
			}
			set {
				CheckBeforeMutation();
				isExtensionMethod = value;
			}
		}
		
		public override IMember Clone()
		{
			DefaultMethod p = new DefaultMethod(Name, ReturnType, Modifiers, Region, BodyRegion, DeclaringType);
			p.parameters = DefaultParameter.Clone(this.Parameters);
			p.typeParameters = new List<ITypeParameter>(this.typeParameters);
			p.CopyDocumentationFrom(this);
			p.isExtensionMethod = this.isExtensionMethod;
			foreach (ExplicitInterfaceImplementation eii in InterfaceImplementations) {
				p.InterfaceImplementations.Add(eii.Clone());
			}
			return p;
		}
		
		public override string DotNetName {
			get {
				if (typeParameters == null || typeParameters.Count == 0)
					return base.DotNetName;
				else
					return base.DotNetName + "``" + typeParameters.Count;
			}
		}
		
		public override string DocumentationTag {
			get {
				string dotnetName = this.DotNetName;
				StringBuilder b = new StringBuilder("M:", dotnetName.Length + 2);
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
		
		public virtual IList<ITypeParameter> TypeParameters {
			get {
				if (typeParameters == null) {
					typeParameters = new List<ITypeParameter>();
				}
				return typeParameters;
			}
			set {
				CheckBeforeMutation();
				typeParameters = value;
			}
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
		
		public IList<string> HandlesClauses {
			get {
				if (handlesClauses == null) {
					handlesClauses = new List<string>();
				}
				return handlesClauses;
			}
			set {
				CheckBeforeMutation();
				handlesClauses = value;
			}
		}
		
		public virtual bool IsConstructor {
			get {
				return Name == "#ctor" || Name == "#cctor";
			}
		}
		
		public virtual bool IsOperator {
			get {
				return Name.StartsWith("op_", StringComparison.Ordinal);
			}
		}
		
		public DefaultMethod(IClass declaringType, string name) : base(declaringType, name)
		{
		}
		
		public DefaultMethod(string name, IReturnType type, ModifierEnum m, DomRegion region, DomRegion bodyRegion, IClass declaringType) : base(declaringType, name)
		{
			this.ReturnType = type;
			this.Region     = region;
			this.BodyRegion = bodyRegion;
			Modifiers = m;
		}
		
		public override string ToString()
		{
			return String.Format("[DefaultMethod: {0}]",
			                     (new Dom.CSharp.CSharpAmbience {
			                      	ConversionFlags = ConversionFlags.StandardConversionFlags
			                      		| ConversionFlags.UseFullyQualifiedMemberNames
			                      }).Convert(this));
		}
		
		public virtual int CompareTo(IMethod value)
		{
			int cmp = string.CompareOrdinal(this.FullyQualifiedName, value.FullyQualifiedName);
			if (cmp != 0) {
				return cmp;
			}
			
			cmp = this.TypeParameters.Count - value.TypeParameters.Count;
			if (cmp != 0) {
				return cmp;
			}
			
			return DiffUtility.Compare(Parameters, value.Parameters);
		}
		
		int IComparable.CompareTo(object value)
		{
			if (value == null) {
				return 0;
			}
			return CompareTo((IMethod)value);
		}
		
		public override EntityType EntityType {
			get {
				return EntityType.Method;
			}
		}
	}
}
