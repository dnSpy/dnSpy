//
// PropertyDefinition.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace Mono.Cecil {

	using System;
	using System.Text;

	public sealed class PropertyDefinition : PropertyReference,
		IMemberDefinition, ICustomAttributeProvider, IHasConstant {

		PropertyAttributes m_attributes;

		CustomAttributeCollection m_customAttrs;

		MethodDefinition m_getMeth;
		MethodDefinition m_setMeth;

		bool m_hasConstant;
		object m_const;

		public PropertyAttributes Attributes {
			get { return m_attributes; }
			set { m_attributes = value; }
		}

		public CustomAttributeCollection CustomAttributes {
			get {
				if (m_customAttrs == null)
					m_customAttrs = new CustomAttributeCollection (this);

				return m_customAttrs;
			}
		}

		public override ParameterDefinitionCollection Parameters {
			get {
				if (this.GetMethod != null)
					return CloneParameterCollection (this.GetMethod.Parameters);
				else if (this.SetMethod != null) {
					ParameterDefinitionCollection parameters =
						CloneParameterCollection (this.SetMethod.Parameters);
					if (parameters.Count > 0)
						parameters.RemoveAt (parameters.Count - 1);
					return parameters;
				}

				if (m_parameters == null)
					m_parameters = new ParameterDefinitionCollection (this);

				return m_parameters;
			}
		}

		public MethodDefinition GetMethod {
			get { return m_getMeth; }
			set { m_getMeth = value; }
		}

		public MethodDefinition SetMethod {
			get { return m_setMeth; }
			set { m_setMeth = value; }
		}

		ParameterDefinitionCollection CloneParameterCollection (ParameterDefinitionCollection original)
		{
			ParameterDefinitionCollection clone = new ParameterDefinitionCollection (
				original.Container);
			foreach (ParameterDefinition param in original)
				clone.Add (param);
			return clone;
		}

		public bool HasConstant {
			get { return m_hasConstant; }
		}

		public object Constant {
			get { return m_const; }
			set {
				m_hasConstant = true;
				m_const = value;
			}
		}

		#region PropertyAttributes

		public bool IsSpecialName {
			get { return (m_attributes & PropertyAttributes.SpecialName) != 0; }
			set {
				if (value)
					m_attributes |= PropertyAttributes.SpecialName;
				else
					m_attributes &= ~PropertyAttributes.SpecialName;
			}
		}

		public bool IsRuntimeSpecialName {
			get { return (m_attributes & PropertyAttributes.RTSpecialName) != 0; }
			set {
				if (value)
					m_attributes |= PropertyAttributes.RTSpecialName;
				else
					m_attributes &= ~PropertyAttributes.RTSpecialName;
			}
		}

		public bool HasDefault {
			get { return (m_attributes & PropertyAttributes.HasDefault) != 0; }
			set {
				if (value)
					m_attributes |= PropertyAttributes.HasDefault;
				else
					m_attributes &= ~PropertyAttributes.HasDefault;
			}
		}

		#endregion

		public PropertyDefinition (string name, TypeReference propertyType, PropertyAttributes attrs) : base (name, propertyType)
		{
			m_attributes = attrs;
		}

		public static MethodDefinition CreateGetMethod (PropertyDefinition prop)
		{
			MethodDefinition get = new MethodDefinition (
				string.Concat ("get_", prop.Name), (MethodAttributes) 0, prop.PropertyType);
			prop.GetMethod = get;
			return get;
		}

		public static MethodDefinition CreateSetMethod (PropertyDefinition prop)
		{
			MethodDefinition set = new MethodDefinition (
				string.Concat ("set_", prop.Name), (MethodAttributes) 0, prop.PropertyType);
			prop.SetMethod = set;
			return set;
		}

		public PropertyDefinition Clone ()
		{
			return Clone (this, new ImportContext (NullReferenceImporter.Instance, this.DeclaringType));
		}

		internal static PropertyDefinition Clone (PropertyDefinition prop, ImportContext context)
		{
			PropertyDefinition np = new PropertyDefinition (
				prop.Name,
				context.Import (prop.PropertyType),
				prop.Attributes);

			if (prop.HasConstant)
				np.Constant = prop.Constant;

			if (context != null && context.GenericContext.Type is TypeDefinition) {
				TypeDefinition type = context.GenericContext.Type as TypeDefinition;
				if (prop.SetMethod != null)
					np.SetMethod = type.Methods.GetMethod (prop.SetMethod.Name, prop.SetMethod.Parameters);
				if (prop.GetMethod != null)
					np.GetMethod = type.Methods.GetMethod (prop.GetMethod.Name, prop.GetMethod.Parameters);
			}

			foreach (CustomAttribute ca in prop.CustomAttributes)
				np.CustomAttributes.Add (CustomAttribute.Clone (ca, context));

			return np;
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append (PropertyType.ToString ());
			sb.Append (' ');

			if (this.DeclaringType != null) {
				sb.Append (this.DeclaringType.ToString ());
				sb.Append ("::");
			}

			sb.Append (this.Name);
			sb.Append ('(');
			ParameterDefinitionCollection parameters = this.Parameters;
			for (int i = 0; i < parameters.Count; i++) {
				if (i > 0)
					sb.Append (',');
				sb.Append (parameters [i].ParameterType.ToString ());
			}
			sb.Append (')');
			return sb.ToString ();
		}

		public override void Accept (IReflectionVisitor visitor)
		{
			visitor.VisitPropertyDefinition (this);

			this.CustomAttributes.Accept (visitor);
		}
	}
}
