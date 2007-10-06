//
// AggressiveRefletionReader.cs
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

	using Mono.Cecil.Metadata;
	using Mono.Cecil.Signatures;

	internal sealed class AggressiveReflectionReader : ReflectionReader {

		public AggressiveReflectionReader (ModuleDefinition module) : base (module)
		{
		}

		public override void VisitTypeDefinitionCollection (TypeDefinitionCollection types)
		{
			base.VisitTypeDefinitionCollection (types);

			ReadGenericParameterConstraints ();
			ReadClassLayoutInfos ();
			ReadFieldLayoutInfos ();
			ReadPInvokeInfos ();
			ReadProperties ();
			ReadEvents ();
			ReadSemantics ();
			ReadInterfaces ();
			ReadOverrides ();
			ReadSecurityDeclarations ();
			ReadCustomAttributes ();
			ReadConstants ();
			ReadExternTypes ();
			ReadMarshalSpecs ();
			ReadInitialValues ();

			m_events = null;
			m_properties = null;
			m_parameters = null;
		}

		void ReadGenericParameterConstraints ()
		{
			if (!m_tHeap.HasTable (GenericParamConstraintTable.RId))
				return;

			GenericParamConstraintTable gpcTable = m_tableReader.GetGenericParamConstraintTable ();
			for (int i = 0; i < gpcTable.Rows.Count; i++) {
				GenericParamConstraintRow gpcRow = gpcTable [i];
				GenericParameter gp = GetGenericParameterAt (gpcRow.Owner);

				gp.Constraints.Add (GetTypeDefOrRef (gpcRow.Constraint, new GenericContext (gp.Owner)));
			}
		}

		void ReadClassLayoutInfos ()
		{
			if (!m_tHeap.HasTable (ClassLayoutTable.RId))
				return;

			ClassLayoutTable clTable = m_tableReader.GetClassLayoutTable ();
			for (int i = 0; i < clTable.Rows.Count; i++) {
				ClassLayoutRow clRow = clTable [i];
				TypeDefinition type = GetTypeDefAt (clRow.Parent);
				type.PackingSize = clRow.PackingSize;
				type.ClassSize = clRow.ClassSize;
			}
		}

		void ReadFieldLayoutInfos ()
		{
			if (!m_tHeap.HasTable (FieldLayoutTable.RId))
				return;

			FieldLayoutTable flTable = m_tableReader.GetFieldLayoutTable ();
			for (int i = 0; i < flTable.Rows.Count; i++) {
				FieldLayoutRow flRow = flTable [i];
				FieldDefinition field = GetFieldDefAt (flRow.Field);
				field.Offset = flRow.Offset;
			}
		}

		void ReadPInvokeInfos ()
		{
			if (!m_tHeap.HasTable (ImplMapTable.RId))
				return;

			ImplMapTable imTable = m_tableReader.GetImplMapTable ();
			for (int i = 0; i < imTable.Rows.Count; i++) {
				ImplMapRow imRow = imTable [i];
				if (imRow.MemberForwarded.TokenType == TokenType.Method) { // should always be true
					MethodDefinition meth = GetMethodDefAt (imRow.MemberForwarded.RID);
					meth.PInvokeInfo = new PInvokeInfo (
						meth, imRow.MappingFlags, MetadataRoot.Streams.StringsHeap [imRow.ImportName],
						Module.ModuleReferences [(int) imRow.ImportScope - 1]);
				}
			}
		}

		void ReadProperties ()
		{
			if (!m_tHeap.HasTable (PropertyTable.RId))
				return;

			PropertyTable propsTable = m_tableReader.GetPropertyTable ();
			PropertyMapTable pmapTable = m_tableReader.GetPropertyMapTable ();
			m_properties = new PropertyDefinition [propsTable.Rows.Count];
			for (int i = 0; i < pmapTable.Rows.Count; i++) {
				PropertyMapRow pmapRow = pmapTable [i];
				if (pmapRow.Parent == 0)
					continue;

				TypeDefinition owner = GetTypeDefAt (pmapRow.Parent);

				GenericContext context = new GenericContext (owner);

				int start = (int) pmapRow.PropertyList, last = propsTable.Rows.Count + 1, end;
				if (i < pmapTable.Rows.Count - 1)
					end = (int) pmapTable [i + 1].PropertyList;
				else
					end = last;

				if (end > last)
					end = last;

				for (int j = start; j < end; j++) {
					PropertyRow prow = propsTable [j - 1];
					PropertySig psig = m_sigReader.GetPropSig (prow.Type);
					PropertyDefinition pdef = new PropertyDefinition (
						m_root.Streams.StringsHeap [prow.Name],
						GetTypeRefFromSig (psig.Type, context),
					prow.Flags);
					pdef.MetadataToken = MetadataToken.FromMetadataRow (TokenType.Property, j - 1);

					pdef.PropertyType = GetModifierType (psig.CustomMods, pdef.PropertyType);

					if (!IsDeleted (pdef))
						owner.Properties.Add (pdef);

					m_properties [j - 1] = pdef;
				}
			}
		}

		void ReadEvents ()
		{
			if (!m_tHeap.HasTable (EventTable.RId))
				return;

			EventTable evtTable = m_tableReader.GetEventTable ();
			EventMapTable emapTable = m_tableReader.GetEventMapTable ();
			m_events = new EventDefinition [evtTable.Rows.Count];
			for (int i = 0; i < emapTable.Rows.Count; i++) {
				EventMapRow emapRow = emapTable [i];
				if (emapRow.Parent == 0)
					continue;

				TypeDefinition owner = GetTypeDefAt (emapRow.Parent);
				GenericContext context = new GenericContext (owner);

				int start = (int) emapRow.EventList, last = evtTable.Rows.Count + 1, end;
				if (i < (emapTable.Rows.Count - 1))
					end = (int) emapTable [i + 1].EventList;
				else
					end = last;

				if (end > last)
					end = last;

				for (int j = start; j < end; j++) {
					EventRow erow = evtTable [j - 1];
					EventDefinition edef = new EventDefinition (
						m_root.Streams.StringsHeap [erow.Name],
						GetTypeDefOrRef (erow.EventType, context), erow.EventFlags);
					edef.MetadataToken = MetadataToken.FromMetadataRow (TokenType.Event, j - 1);

					if (!IsDeleted (edef))
						owner.Events.Add (edef);

					m_events [j - 1] = edef;
				}
			}
		}

		void ReadSemantics ()
		{
			if (!m_tHeap.HasTable (MethodSemanticsTable.RId))
				return;

			MethodSemanticsTable semTable = m_tableReader.GetMethodSemanticsTable ();
			for (int i = 0; i < semTable.Rows.Count; i++) {
				MethodSemanticsRow semRow = semTable [i];
				MethodDefinition semMeth = GetMethodDefAt (semRow.Method);
				semMeth.SemanticsAttributes = semRow.Semantics;
				switch (semRow.Association.TokenType) {
				case TokenType.Event :
					EventDefinition evt = GetEventDefAt (semRow.Association.RID);
					if ((semRow.Semantics & MethodSemanticsAttributes.AddOn) != 0)
						evt.AddMethod = semMeth;
					else if ((semRow.Semantics & MethodSemanticsAttributes.Fire) != 0)
						evt.InvokeMethod = semMeth;
					else if ((semRow.Semantics & MethodSemanticsAttributes.RemoveOn) != 0)
						evt.RemoveMethod = semMeth;
					break;
				case TokenType.Property :
					PropertyDefinition prop = GetPropertyDefAt (semRow.Association.RID);
					if ((semRow.Semantics & MethodSemanticsAttributes.Getter) != 0)
						prop.GetMethod = semMeth;
					else if ((semRow.Semantics & MethodSemanticsAttributes.Setter) != 0)
						prop.SetMethod = semMeth;
					break;
				}
			}
		}

		void ReadInterfaces ()
		{
			if (!m_tHeap.HasTable (InterfaceImplTable.RId))
				return;

			InterfaceImplTable intfsTable = m_tableReader.GetInterfaceImplTable ();
			for (int i = 0; i < intfsTable.Rows.Count; i++) {
				InterfaceImplRow intfsRow = intfsTable [i];
				TypeDefinition owner = GetTypeDefAt (intfsRow.Class);
				owner.Interfaces.Add (GetTypeDefOrRef (intfsRow.Interface, new GenericContext (owner)));
			}
		}

		void ReadOverrides ()
		{
			if (!m_tHeap.HasTable (MethodImplTable.RId))
				return;

			MethodImplTable implTable = m_tableReader.GetMethodImplTable ();
			for (int i = 0; i < implTable.Rows.Count; i++) {
				MethodImplRow implRow = implTable [i];
				if (implRow.MethodBody.TokenType == TokenType.Method) {
					MethodDefinition owner = GetMethodDefAt (implRow.MethodBody.RID);
					switch (implRow.MethodDeclaration.TokenType) {
					case TokenType.Method :
						owner.Overrides.Add (
							GetMethodDefAt (implRow.MethodDeclaration.RID));
						break;
					case TokenType.MemberRef :
						owner.Overrides.Add (
							(MethodReference) GetMemberRefAt (
								implRow.MethodDeclaration.RID, new GenericContext (owner)));
						break;
					}
				}
			}
		}

		void ReadSecurityDeclarations ()
		{
			if (!m_tHeap.HasTable (DeclSecurityTable.RId))
				return;

			DeclSecurityTable dsTable = m_tableReader.GetDeclSecurityTable ();
			for (int i = 0; i < dsTable.Rows.Count; i++) {
				DeclSecurityRow dsRow = dsTable [i];
				SecurityDeclaration dec = BuildSecurityDeclaration (dsRow);

				if (dsRow.Parent.RID == 0)
					continue;

				IHasSecurity owner = null;
				switch (dsRow.Parent.TokenType) {
				case TokenType.Assembly :
					owner = this.Module.Assembly;
					break;
				case TokenType.TypeDef :
					owner = GetTypeDefAt (dsRow.Parent.RID);
					break;
				case TokenType.Method :
					owner = GetMethodDefAt (dsRow.Parent.RID);
					break;
				}

				owner.SecurityDeclarations.Add (dec);
			}
		}

		void ReadCustomAttributes ()
		{
			if (!m_tHeap.HasTable (CustomAttributeTable.RId))
				return;

			CustomAttributeTable caTable = m_tableReader.GetCustomAttributeTable ();
			for (int i = 0; i < caTable.Rows.Count; i++) {
				CustomAttributeRow caRow = caTable [i];
				MethodReference ctor;

				if (caRow.Type.RID == 0)
					continue;

				if (caRow.Type.TokenType == TokenType.Method)
					ctor = GetMethodDefAt (caRow.Type.RID);
				else
					ctor = GetMemberRefAt (caRow.Type.RID, new GenericContext ()) as MethodReference;

				CustomAttrib ca = m_sigReader.GetCustomAttrib (caRow.Value, ctor);
				CustomAttribute cattr;
				if (!ca.Read) {
					cattr = new CustomAttribute (ctor);
					cattr.Resolved = false;
					cattr.Blob = m_root.Streams.BlobHeap.Read (caRow.Value);
				} else
					cattr = BuildCustomAttribute (ctor, ca);

				if (caRow.Parent.RID == 0)
					continue;

				ICustomAttributeProvider owner = null;
				switch (caRow.Parent.TokenType) {
				case TokenType.Assembly :
					owner = this.Module.Assembly;
					break;
				case TokenType.Module :
					owner = this.Module;
					break;
				case TokenType.TypeDef :
					owner = GetTypeDefAt (caRow.Parent.RID);
					break;
				case TokenType.TypeRef :
					owner = GetTypeRefAt (caRow.Parent.RID);
					break;
				case TokenType.Field :
					owner = GetFieldDefAt (caRow.Parent.RID);
					break;
				case TokenType.Method :
					owner = GetMethodDefAt (caRow.Parent.RID);
					break;
				case TokenType.Property :
					owner = GetPropertyDefAt (caRow.Parent.RID);
					break;
				case TokenType.Event :
					owner = GetEventDefAt (caRow.Parent.RID);
					break;
				case TokenType.Param :
					owner = GetParamDefAt (caRow.Parent.RID);
					break;
				case TokenType.GenericParam :
					owner = GetGenericParameterAt (caRow.Parent.RID);
					break;
				default :
					//TODO: support other ?
					break;
				}

				if (owner != null)
					owner.CustomAttributes.Add (cattr);
			}
		}

		void ReadConstants ()
		{
			if (!m_tHeap.HasTable (ConstantTable.RId))
				return;

			ConstantTable csTable = m_tableReader.GetConstantTable ();
			for (int i = 0; i < csTable.Rows.Count; i++) {
				ConstantRow csRow = csTable [i];

				object constant = GetConstant (csRow.Value, csRow.Type);

				IHasConstant owner = null;
				switch (csRow.Parent.TokenType) {
				case TokenType.Field :
					owner = GetFieldDefAt (csRow.Parent.RID);
					break;
				case TokenType.Property :
					owner = GetPropertyDefAt (csRow.Parent.RID);
					break;
				case TokenType.Param :
					owner = GetParamDefAt (csRow.Parent.RID);
					break;
				}

				owner.Constant = constant;
			}
		}

		void ReadExternTypes ()
		{
			base.VisitExternTypeCollection (Module.ExternTypes);
		}

		void ReadMarshalSpecs ()
		{
			if (!m_tHeap.HasTable (FieldMarshalTable.RId))
				return;

			FieldMarshalTable fmTable = m_tableReader.GetFieldMarshalTable ();
			for (int i = 0; i < fmTable.Rows.Count; i++) {
				FieldMarshalRow fmRow = fmTable [i];

				IHasMarshalSpec owner = null;
				switch (fmRow.Parent.TokenType) {
				case TokenType.Field:
					owner = GetFieldDefAt (fmRow.Parent.RID);
					break;
				case TokenType.Param:
					owner = GetParamDefAt (fmRow.Parent.RID);
					break;
				}

				owner.MarshalSpec = BuildMarshalDesc (
					m_sigReader.GetMarshalSig (fmRow.NativeType), owner);
			}
		}

		void ReadInitialValues ()
		{
			if (!m_tHeap.HasTable (FieldRVATable.RId))
				return;

			FieldRVATable frTable = m_tableReader.GetFieldRVATable ();
			for (int i = 0; i < frTable.Rows.Count; i++) {
				FieldRVARow frRow = frTable [i];
				FieldDefinition field = GetFieldDefAt (frRow.Field);
				field.RVA = frRow.RVA;
				SetInitialValue (field);
			}
		}
	}
}
