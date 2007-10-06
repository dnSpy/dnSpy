//
// ParameterDefinition.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 - 2007 Jb Evain
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

	public sealed class ParameterDefinition : ParameterReference, IHasMarshalSpec,
		IMetadataTokenProvider, ICustomAttributeProvider, IHasConstant {

		ParameterAttributes m_attributes;

		bool m_hasConstant;
		object m_const;

		MethodReference m_method;
		CustomAttributeCollection m_customAttrs;

		MarshalSpec m_marshalDesc;

		public ParameterAttributes Attributes {
			get { return m_attributes; }
			set { m_attributes = value; }
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

		public MethodReference Method {
			get { return m_method; }
			set { m_method = value; }
		}

		public CustomAttributeCollection CustomAttributes {
			get {
				if (m_customAttrs == null)
					m_customAttrs = new CustomAttributeCollection (this);

				return m_customAttrs;
			}
		}

		public MarshalSpec MarshalSpec {
			get { return m_marshalDesc; }
			set { m_marshalDesc = value; }
		}

		#region ParameterAttributes

		public bool IsIn {
			get { return (m_attributes & ParameterAttributes.In) != 0; }
			set {
				if (value)
					m_attributes |= ParameterAttributes.In;
				else
					m_attributes &= ~ParameterAttributes.In;
			}
		}

		public bool IsOut {
			get { return (m_attributes & ParameterAttributes.Out) != 0; }
			set {
				if (value)
					m_attributes |= ParameterAttributes.Out;
				else
					m_attributes &= ~ParameterAttributes.Out;
			}
		}

		public bool IsOptional {
			get { return (m_attributes & ParameterAttributes.Optional) != 0; }
			set {
				if (value)
					m_attributes |= ParameterAttributes.Optional;
				else
					m_attributes &= ~ParameterAttributes.Optional;
			}
		}

		public bool HasDefault {
			get { return (m_attributes & ParameterAttributes.HasDefault) != 0; }
			set {
				if (value)
					m_attributes |= ParameterAttributes.HasDefault;
				else
					m_attributes &= ~ParameterAttributes.HasDefault;
			}
		}

		#endregion

		public ParameterDefinition (TypeReference paramType) :
			this (string.Empty, -1, (ParameterAttributes) 0, paramType)
		{
		}

		public ParameterDefinition (string name, int seq, ParameterAttributes attrs, TypeReference paramType) : base (name, seq, paramType)
		{
			m_attributes = attrs;
		}

		public ParameterDefinition Clone ()
		{
			return Clone (this, new ImportContext (NullReferenceImporter.Instance, m_method));
		}

		internal static ParameterDefinition Clone (ParameterDefinition param, ImportContext context)
		{
			ParameterDefinition np = new ParameterDefinition (
				param.Name,
				param.Sequence,
				param.Attributes,
				context.Import (param.ParameterType));

			if (param.HasConstant)
				np.Constant = param.Constant;

			if (param.MarshalSpec != null)
				np.MarshalSpec = param.MarshalSpec;

			foreach (CustomAttribute ca in param.CustomAttributes)
				np.CustomAttributes.Add (CustomAttribute.Clone (ca, context));

			return np;
		}

		public override void Accept (IReflectionVisitor visitor)
		{
			visitor.VisitParameterDefinition (this);

			if (this.MarshalSpec != null)
				this.MarshalSpec.Accept (visitor);

			this.CustomAttributes.Accept (visitor);
		}
	}
}
