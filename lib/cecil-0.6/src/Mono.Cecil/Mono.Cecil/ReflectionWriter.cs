//
// ReflectionWriter.cs
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

	using System;
	using System.Collections;
	using System.Globalization;
	using System.Text;

	using Mono.Cecil.Binary;
	using Mono.Cecil.Cil;
	using Mono.Cecil.Metadata;
	using Mono.Cecil.Signatures;

	internal sealed class ReflectionWriter : BaseReflectionVisitor {

		StructureWriter m_structureWriter;
		ModuleDefinition m_mod;
		SignatureWriter m_sigWriter;
		CodeWriter m_codeWriter;
		MetadataWriter m_mdWriter;
		MetadataTableWriter m_tableWriter;
		MetadataRowWriter m_rowWriter;

		bool m_saveSymbols;
		string m_asmOutput;
		ISymbolWriter m_symbolWriter;

		ArrayList m_typeDefStack;
		ArrayList m_methodStack;
		ArrayList m_fieldStack;
		ArrayList m_genericParamStack;
		IDictionary m_typeSpecTokenCache;

		uint m_methodIndex;
		uint m_fieldIndex;
		uint m_paramIndex;
		uint m_eventIndex;
		uint m_propertyIndex;

		MemoryBinaryWriter m_constWriter;

		public StructureWriter StructureWriter {
			get { return m_structureWriter; }
			set {
				 m_structureWriter = value;

				Initialize ();
			}
		}

		public CodeWriter CodeWriter {
			get { return m_codeWriter; }
		}

		public bool SaveSymbols {
			get { return m_saveSymbols; }
			set { m_saveSymbols = value; }
		}

		public string OutputFile
		{
			get { return m_asmOutput; }
			set { m_asmOutput = value; }
		}

		public ISymbolWriter SymbolWriter {
			get { return m_symbolWriter; }
			set { m_symbolWriter = value; }
		}

		public SignatureWriter SignatureWriter {
			get { return m_sigWriter; }
		}

		public MetadataWriter MetadataWriter {
			get { return m_mdWriter; }
		}

		public MetadataTableWriter MetadataTableWriter {
			get { return m_tableWriter; }
		}

		public MetadataRowWriter MetadataRowWriter {
			get { return m_rowWriter; }
		}

		public ReflectionWriter (ModuleDefinition mod)
		{
			m_mod = mod;
		}

		void Initialize ()
		{
			m_mdWriter = new MetadataWriter (
				m_mod.Assembly,
				m_mod.Image.MetadataRoot,
				m_structureWriter.Assembly.Kind,
				m_mod.Assembly.Runtime,
				m_structureWriter.GetWriter ());
			m_tableWriter = m_mdWriter.GetTableVisitor ();
			m_rowWriter = m_tableWriter.GetRowVisitor () as MetadataRowWriter;
			m_sigWriter = new SignatureWriter (m_mdWriter);
			m_codeWriter = new CodeWriter (this, m_mdWriter.CilWriter);

			m_typeDefStack = new ArrayList ();
			m_methodStack = new ArrayList ();
			m_fieldStack = new ArrayList ();
			m_genericParamStack = new ArrayList ();
			m_typeSpecTokenCache = new Hashtable ();

			m_methodIndex = 1;
			m_fieldIndex = 1;
			m_paramIndex = 1;
			m_eventIndex = 1;
			m_propertyIndex = 1;

			m_constWriter = new MemoryBinaryWriter ();
		}

		public TypeReference GetCoreType (string name)
		{
			return m_mod.Controller.Reader.SearchCoreType (name);
		}

		public static uint GetRidFor (IMetadataTokenProvider tp)
		{
			return tp.MetadataToken.RID;
		}

		public uint GetRidFor (AssemblyNameReference asmName)
		{
			return (uint) m_mod.AssemblyReferences.IndexOf (asmName) + 1;
		}

		public uint GetRidFor (ModuleDefinition mod)
		{
			return (uint) m_mod.Assembly.Modules.IndexOf (mod) + 1;
		}

		public uint GetRidFor (ModuleReference modRef)
		{
			return (uint) m_mod.ModuleReferences.IndexOf (modRef) + 1;
		}

		static bool IsTypeSpec (TypeReference type)
		{
			return type is TypeSpecification || type is GenericParameter;
		}

		public MetadataToken GetTypeDefOrRefToken (TypeReference type)
		{
			if (IsTypeSpec (type)) {
				uint sig = m_sigWriter.AddTypeSpec (GetTypeSpecSig (type));
				if (m_typeSpecTokenCache.Contains (sig))
					return (MetadataToken) m_typeSpecTokenCache [sig];

				TypeSpecTable tsTable = m_tableWriter.GetTypeSpecTable ();
				TypeSpecRow tsRow = m_rowWriter.CreateTypeSpecRow (sig);
				tsTable.Rows.Add (tsRow);

				MetadataToken token = new MetadataToken (TokenType.TypeSpec, (uint) tsTable.Rows.Count);
				if (! (type is GenericParameter))
					type.MetadataToken = token;

				m_typeSpecTokenCache [sig] = token;
				return token;
			} else if (type != null)
				return type.MetadataToken;
			else // <Module> and interfaces
				return new MetadataToken (TokenType.TypeRef, 0);
		}

		public MetadataToken GetMemberRefToken (MemberReference member)
		{
			return member.MetadataToken;
		}

		public MetadataToken GetMethodSpecToken (GenericInstanceMethod gim)
		{
			uint sig = m_sigWriter.AddMethodSpec (GetMethodSpecSig (gim));
			MethodSpecTable msTable = m_tableWriter.GetMethodSpecTable ();

			MetadataToken meth = GetMemberRefToken (gim.ElementMethod);

			for (int i = 0; i < msTable.Rows.Count; i++) {
				MethodSpecRow row = msTable [i];
				if (row.Method == meth && row.Instantiation == sig)
					return MetadataToken.FromMetadataRow (TokenType.MethodSpec, i);
			}

			MethodSpecRow msRow = m_rowWriter.CreateMethodSpecRow (
				meth,
				sig);
			msTable.Rows.Add (msRow);
			gim.MetadataToken = new MetadataToken (TokenType.MethodSpec, (uint) msTable.Rows.Count);
			return gim.MetadataToken;
		}

		public override void VisitModuleDefinition (ModuleDefinition mod)
		{
			mod.FullLoad ();
		}

		public override void VisitTypeDefinitionCollection (TypeDefinitionCollection types)
		{
			TypeDefTable tdTable = m_tableWriter.GetTypeDefTable ();

			if (types [Constants.ModuleType] == null)
				types.Add (new TypeDefinition (
						Constants.ModuleType, string.Empty, TypeAttributes.NotPublic));

			foreach (TypeDefinition t in types)
				m_typeDefStack.Add (t);

			m_typeDefStack.Sort (TableComparers.TypeDef.Instance);

			for (int i = 0; i < m_typeDefStack.Count; i++) {
				TypeDefinition t = (TypeDefinition) m_typeDefStack [i];
				if (t.Module.Assembly != m_mod.Assembly)
					throw new ReflectionException ("A type as not been correctly imported");

				t.MetadataToken = new MetadataToken (TokenType.TypeDef, (uint) (i + 1));
			}

			foreach (TypeDefinition t in m_typeDefStack) {
				TypeDefRow tdRow = m_rowWriter.CreateTypeDefRow (
					t.Attributes,
					m_mdWriter.AddString (t.Name),
					m_mdWriter.AddString (t.Namespace),
					GetTypeDefOrRefToken (t.BaseType),
					0,
					0);

				tdTable.Rows.Add (tdRow);
			}
		}

		public void CompleteTypeDefinitions ()
		{
			TypeDefTable tdTable = m_tableWriter.GetTypeDefTable ();

			for (int i = 0; i < m_typeDefStack.Count; i++) {
				TypeDefRow tdRow = tdTable [i];
				TypeDefinition t = (TypeDefinition) m_typeDefStack [i];
				tdRow.FieldList = m_fieldIndex;
				tdRow.MethodList = m_methodIndex;
				foreach (FieldDefinition field in t.Fields)
					VisitFieldDefinition (field);
				foreach (MethodDefinition ctor in t.Constructors)
					VisitMethodDefinition (ctor);
				foreach (MethodDefinition meth in t.Methods)
					VisitMethodDefinition (meth);

				if (t.HasLayoutInfo)
					WriteLayout (t);
			}

			foreach (FieldDefinition field in m_fieldStack) {
				VisitCustomAttributeCollection (field.CustomAttributes);
				if (field.MarshalSpec != null)
					VisitMarshalSpec (field.MarshalSpec);
			}

			foreach (MethodDefinition meth in m_methodStack) {
				VisitCustomAttributeCollection (meth.ReturnType.CustomAttributes);
				foreach (ParameterDefinition param in meth.Parameters)
					VisitCustomAttributeCollection (param.CustomAttributes);
				VisitGenericParameterCollection (meth.GenericParameters);
				VisitOverrideCollection (meth.Overrides);
				VisitCustomAttributeCollection (meth.CustomAttributes);
				VisitSecurityDeclarationCollection (meth.SecurityDeclarations);
				if (meth.PInvokeInfo != null) {
					meth.Attributes |= MethodAttributes.PInvokeImpl;
					VisitPInvokeInfo (meth.PInvokeInfo);
				}
			}

			foreach (TypeDefinition t in m_typeDefStack)
				t.Accept (this);
		}

		public override void VisitTypeReferenceCollection (TypeReferenceCollection refs)
		{
			ArrayList orderedTypeRefs = new ArrayList (refs.Count);
			foreach (TypeReference tr in refs)
				orderedTypeRefs.Add (tr);

			orderedTypeRefs.Sort (TableComparers.TypeRef.Instance);

			TypeRefTable trTable = m_tableWriter.GetTypeRefTable ();
			foreach (TypeReference t in orderedTypeRefs) {
				MetadataToken scope;

				if (t.Module.Assembly != m_mod.Assembly)
					throw new ReflectionException ("A type as not been correctly imported");

				if (t.Scope == null)
					continue;

				if (t.DeclaringType != null)
					scope = new MetadataToken (TokenType.TypeRef, GetRidFor (t.DeclaringType));
				else if (t.Scope is AssemblyNameReference)
					scope = new MetadataToken (TokenType.AssemblyRef,
						GetRidFor ((AssemblyNameReference) t.Scope));
				else if (t.Scope is ModuleDefinition)
					scope = new MetadataToken (TokenType.Module,
						GetRidFor ((ModuleDefinition) t.Scope));
				else if (t.Scope is ModuleReference)
					scope = new MetadataToken (TokenType.ModuleRef,
						GetRidFor ((ModuleReference) t.Scope));
				else
					scope = new MetadataToken (TokenType.ExportedType, 0);

				TypeRefRow trRow = m_rowWriter.CreateTypeRefRow (
					scope,
					m_mdWriter.AddString (t.Name),
					m_mdWriter.AddString (t.Namespace));

				trTable.Rows.Add (trRow);
				t.MetadataToken = new MetadataToken (TokenType.TypeRef, (uint) trTable.Rows.Count);
			}
		}

		public override void VisitMemberReferenceCollection (MemberReferenceCollection members)
		{
			if (members.Count == 0)
				return;

			MemberRefTable mrTable = m_tableWriter.GetMemberRefTable ();
			foreach (MemberReference member in members) {
				uint sig = 0;
				if (member is FieldReference)
					sig = m_sigWriter.AddFieldSig (GetFieldSig (member as FieldReference));
				else if (member is MethodReference)
					sig = m_sigWriter.AddMethodRefSig (GetMethodRefSig ((MethodReference) member));

				MemberRefRow mrRow = m_rowWriter.CreateMemberRefRow (
					GetTypeDefOrRefToken (member.DeclaringType),
					m_mdWriter.AddString (member.Name),
					sig);

				mrTable.Rows.Add (mrRow);
				member.MetadataToken = new MetadataToken (
					TokenType.MemberRef, (uint) mrTable.Rows.Count);
			}
		}

		public override void VisitGenericParameterCollection (GenericParameterCollection parameters)
		{
			if (parameters.Count == 0)
				return;

			foreach (GenericParameter gp in parameters)
				m_genericParamStack.Add (gp);
		}

		public override void VisitInterfaceCollection (InterfaceCollection interfaces)
		{
			if (interfaces.Count == 0)
				return;

			InterfaceImplTable iiTable = m_tableWriter.GetInterfaceImplTable ();
			foreach (TypeReference interf in interfaces) {
				InterfaceImplRow iiRow = m_rowWriter.CreateInterfaceImplRow (
					GetRidFor (interfaces.Container),
					GetTypeDefOrRefToken (interf));

				iiTable.Rows.Add (iiRow);
			}
		}

		public override void VisitExternTypeCollection (ExternTypeCollection externs)
		{
			VisitCollection (externs);
		}

		public override void VisitExternType (TypeReference externType)
		{
			// TODO
		}

		public override void VisitOverrideCollection (OverrideCollection meths)
		{
			if (meths.Count == 0)
				return;

			MethodImplTable miTable = m_tableWriter.GetMethodImplTable ();
			foreach (MethodReference ov in meths) {
				MethodImplRow miRow = m_rowWriter.CreateMethodImplRow (
					GetRidFor (meths.Container.DeclaringType as TypeDefinition),
					new MetadataToken (TokenType.Method, GetRidFor (meths.Container)),
					GetMemberRefToken (ov));

				miTable.Rows.Add (miRow);
			}
		}

		public override void VisitNestedTypeCollection (NestedTypeCollection nestedTypes)
		{
			if (nestedTypes.Count == 0)
				return;

			NestedClassTable ncTable = m_tableWriter.GetNestedClassTable ();
			foreach (TypeDefinition nested in nestedTypes) {
				NestedClassRow ncRow = m_rowWriter.CreateNestedClassRow (
					nested.MetadataToken.RID,
					GetRidFor (nestedTypes.Container));

				ncTable.Rows.Add (ncRow);
			}
		}

		public override void VisitParameterDefinitionCollection (ParameterDefinitionCollection parameters)
		{
			if (parameters.Count == 0)
				return;

			ushort seq = 1;
			ParamTable pTable = m_tableWriter.GetParamTable ();
			foreach (ParameterDefinition param in parameters)
				InsertParameter (pTable, param, seq++);
		}

		void InsertParameter (ParamTable pTable, ParameterDefinition param, ushort seq)
		{
			ParamRow pRow = m_rowWriter.CreateParamRow (
				param.Attributes,
				seq,
				m_mdWriter.AddString (param.Name));

			pTable.Rows.Add (pRow);
			param.MetadataToken = new MetadataToken (TokenType.Param, (uint) pTable.Rows.Count);

			if (param.MarshalSpec != null)
				param.MarshalSpec.Accept (this);

			if (param.HasConstant)
				WriteConstant (param, param.ParameterType);

			m_paramIndex++;
		}

		static bool RequiresParameterRow (MethodReturnType mrt)
		{
			return mrt.HasConstant || mrt.MarshalSpec != null ||
				mrt.CustomAttributes.Count > 0 || mrt.Parameter.Attributes != (ParameterAttributes) 0;
		}

		public override void VisitMethodDefinition (MethodDefinition method)
		{
			MethodTable mTable = m_tableWriter.GetMethodTable ();
			MethodRow mRow = m_rowWriter.CreateMethodRow (
				RVA.Zero,
				method.ImplAttributes,
				method.Attributes,
				m_mdWriter.AddString (method.Name),
				m_sigWriter.AddMethodDefSig (GetMethodDefSig (method)),
				m_paramIndex);

			mTable.Rows.Add (mRow);
			m_methodStack.Add (method);
			method.MetadataToken = new MetadataToken (TokenType.Method, (uint) mTable.Rows.Count);
			m_methodIndex++;

			if (RequiresParameterRow (method.ReturnType))
				InsertParameter (m_tableWriter.GetParamTable (), method.ReturnType.Parameter, 0);

			VisitParameterDefinitionCollection (method.Parameters);
		}

		public override void VisitPInvokeInfo (PInvokeInfo pinvk)
		{
			ImplMapTable imTable = m_tableWriter.GetImplMapTable ();
			ImplMapRow imRow = m_rowWriter.CreateImplMapRow (
				pinvk.Attributes,
				new MetadataToken (TokenType.Method, GetRidFor (pinvk.Method)),
				m_mdWriter.AddString (pinvk.EntryPoint),
				GetRidFor (pinvk.Module));

			imTable.Rows.Add (imRow);
		}

		public override void VisitEventDefinitionCollection (EventDefinitionCollection events)
		{
			if (events.Count == 0)
				return;

			EventMapTable emTable = m_tableWriter.GetEventMapTable ();
			EventMapRow emRow = m_rowWriter.CreateEventMapRow (
				GetRidFor (events.Container),
				m_eventIndex);

			emTable.Rows.Add (emRow);
			VisitCollection (events);
		}

		public override void VisitEventDefinition (EventDefinition evt)
		{
			EventTable eTable = m_tableWriter.GetEventTable ();
			EventRow eRow = m_rowWriter.CreateEventRow (
				evt.Attributes,
				m_mdWriter.AddString (evt.Name),
				GetTypeDefOrRefToken (evt.EventType));

			eTable.Rows.Add (eRow);
			evt.MetadataToken = new MetadataToken (TokenType.Event, (uint) eTable.Rows.Count);

			if (evt.AddMethod != null)
				WriteSemantic (MethodSemanticsAttributes.AddOn, evt, evt.AddMethod);

			if (evt.InvokeMethod != null)
				WriteSemantic (MethodSemanticsAttributes.Fire, evt, evt.InvokeMethod);

			if (evt.RemoveMethod != null)
				WriteSemantic (MethodSemanticsAttributes.RemoveOn, evt, evt.RemoveMethod);

			m_eventIndex++;
		}

		public override void VisitFieldDefinition (FieldDefinition field)
		{
			FieldTable fTable = m_tableWriter.GetFieldTable ();
			FieldRow fRow = m_rowWriter.CreateFieldRow (
				field.Attributes,
				m_mdWriter.AddString (field.Name),
				m_sigWriter.AddFieldSig (GetFieldSig (field)));

			fTable.Rows.Add (fRow);
			field.MetadataToken = new MetadataToken (TokenType.Field, (uint) fTable.Rows.Count);
			m_fieldIndex++;

			if (field.HasConstant)
				WriteConstant (field, field.FieldType);

			if (field.HasLayoutInfo)
				WriteLayout (field);

			m_fieldStack.Add (field);
		}

		public override void VisitPropertyDefinitionCollection (PropertyDefinitionCollection properties)
		{
			if (properties.Count == 0)
				return;

			PropertyMapTable pmTable = m_tableWriter.GetPropertyMapTable ();
			PropertyMapRow pmRow = m_rowWriter.CreatePropertyMapRow (
				GetRidFor (properties.Container),
				m_propertyIndex);

			pmTable.Rows.Add (pmRow);
			VisitCollection (properties);
		}

		public override void VisitPropertyDefinition (PropertyDefinition property)
		{
			PropertyTable pTable = m_tableWriter.GetPropertyTable ();
			PropertyRow pRow = m_rowWriter.CreatePropertyRow (
				property.Attributes,
				m_mdWriter.AddString (property.Name),
				m_sigWriter.AddPropertySig (GetPropertySig (property)));

			pTable.Rows.Add (pRow);
			property.MetadataToken = new MetadataToken (TokenType.Property, (uint) pTable.Rows.Count);

			if (property.GetMethod != null)
				WriteSemantic (MethodSemanticsAttributes.Getter, property, property.GetMethod);

			if (property.SetMethod != null)
				WriteSemantic (MethodSemanticsAttributes.Setter, property, property.SetMethod);

			if (property.HasConstant)
				WriteConstant (property, property.PropertyType);

			m_propertyIndex++;
		}

		public override void VisitSecurityDeclarationCollection (SecurityDeclarationCollection secDecls)
		{
			if (secDecls.Count == 0)
				return;

			DeclSecurityTable dsTable = m_tableWriter.GetDeclSecurityTable ();
			foreach (SecurityDeclaration secDec in secDecls) {
				DeclSecurityRow dsRow = m_rowWriter.CreateDeclSecurityRow (
					secDec.Action,
					secDecls.Container.MetadataToken,
					m_mdWriter.AddBlob (secDec.Resolved ?
						m_mod.GetAsByteArray (secDec) : secDec.Blob));

				dsTable.Rows.Add (dsRow);
			}
		}

		public override void VisitCustomAttributeCollection (CustomAttributeCollection customAttrs)
		{
			if (customAttrs.Count == 0)
				return;

			CustomAttributeTable caTable = m_tableWriter.GetCustomAttributeTable ();
			foreach (CustomAttribute ca in customAttrs) {
				MetadataToken parent;
				if (customAttrs.Container is AssemblyDefinition)
					parent = new MetadataToken (TokenType.Assembly, 1);
				else if (customAttrs.Container is ModuleDefinition)
					parent = new MetadataToken (TokenType.Module, 1);
				else if (customAttrs.Container is IMetadataTokenProvider)
					parent = ((IMetadataTokenProvider) customAttrs.Container).MetadataToken;
				else
					throw new ReflectionException ("Unknown Custom Attribute parent");

				uint value = ca.Resolved ?
					m_sigWriter.AddCustomAttribute (GetCustomAttributeSig (ca), ca.Constructor) :
					m_mdWriter.AddBlob (m_mod.GetAsByteArray (ca));
				CustomAttributeRow caRow = m_rowWriter.CreateCustomAttributeRow (
					parent,
					GetMemberRefToken (ca.Constructor),
					value);

				caTable.Rows.Add (caRow);
			}
		}

		public override void VisitMarshalSpec (MarshalSpec marshalSpec)
		{
			FieldMarshalTable fmTable = m_tableWriter.GetFieldMarshalTable ();
			FieldMarshalRow fmRow = m_rowWriter.CreateFieldMarshalRow (
				marshalSpec.Container.MetadataToken,
				m_sigWriter.AddMarshalSig (GetMarshalSig (marshalSpec)));

			fmTable.Rows.Add (fmRow);
		}

		void WriteConstant (IHasConstant hc, TypeReference type)
		{
			ConstantTable cTable = m_tableWriter.GetConstantTable ();
			ElementType et;
			if (type is TypeDefinition && (type as TypeDefinition).IsEnum) {
				Type t = hc.Constant.GetType ();
				if (t.IsEnum)
					t = Enum.GetUnderlyingType (t);

				et = GetCorrespondingType (string.Concat (t.Namespace, '.', t.Name));
			} else
				et = GetCorrespondingType (type.FullName);

			if (et == ElementType.Object)
				et = hc.Constant == null ?
					ElementType.Class :
					GetCorrespondingType (hc.Constant.GetType ().FullName);

			ConstantRow cRow = m_rowWriter.CreateConstantRow (
				et,
				hc.MetadataToken,
				m_mdWriter.AddBlob (EncodeConstant (et, hc.Constant)));

			cTable.Rows.Add (cRow);
		}

		void WriteLayout (FieldDefinition field)
		{
			FieldLayoutTable flTable = m_tableWriter.GetFieldLayoutTable ();
			FieldLayoutRow flRow = m_rowWriter.CreateFieldLayoutRow (
				field.Offset,
				GetRidFor (field));

			flTable.Rows.Add (flRow);
		}

		void WriteLayout (TypeDefinition type)
		{
			ClassLayoutTable clTable = m_tableWriter.GetClassLayoutTable ();
			ClassLayoutRow clRow = m_rowWriter.CreateClassLayoutRow (
				type.PackingSize,
				type.ClassSize,
				GetRidFor (type));

			clTable.Rows.Add (clRow);
		}

		void WriteSemantic (MethodSemanticsAttributes attrs,
			IMetadataTokenProvider member, MethodDefinition meth)
		{
			MethodSemanticsTable msTable = m_tableWriter.GetMethodSemanticsTable ();
			MethodSemanticsRow msRow = m_rowWriter.CreateMethodSemanticsRow (
				attrs,
				GetRidFor (meth),
				member.MetadataToken);

			msTable.Rows.Add (msRow);
		}

		void SortTables ()
		{
			TablesHeap th = m_mdWriter.GetMetadataRoot ().Streams.TablesHeap;
			th.Sorted = 0;

			if (th.HasTable (NestedClassTable.RId))
				m_tableWriter.GetNestedClassTable ().Rows.Sort (
					TableComparers.NestedClass.Instance);
			th.Sorted |= ((long) 1 << NestedClassTable.RId);

			if (th.HasTable (InterfaceImplTable.RId))
				m_tableWriter.GetInterfaceImplTable ().Rows.Sort (
					TableComparers.InterfaceImpl.Instance);
			th.Sorted |= ((long) 1 << InterfaceImplTable.RId);

			if (th.HasTable (ConstantTable.RId))
				m_tableWriter.GetConstantTable ().Rows.Sort (
					TableComparers.Constant.Instance);
			th.Sorted |= ((long) 1 << ConstantTable.RId);

			if (th.HasTable (MethodSemanticsTable.RId))
				m_tableWriter.GetMethodSemanticsTable ().Rows.Sort (
					TableComparers.MethodSem.Instance);
			th.Sorted |= ((long) 1 << MethodSemanticsTable.RId);

			if (th.HasTable (FieldMarshalTable.RId))
				m_tableWriter.GetFieldMarshalTable ().Rows.Sort (
					TableComparers.FieldMarshal.Instance);
			th.Sorted |= ((long) 1 << FieldMarshalTable.RId);

			if (th.HasTable (ClassLayoutTable.RId))
				m_tableWriter.GetClassLayoutTable ().Rows.Sort (
					TableComparers.TypeLayout.Instance);
			th.Sorted |= ((long) 1 << ClassLayoutTable.RId);

			if (th.HasTable (FieldLayoutTable.RId))
				m_tableWriter.GetFieldLayoutTable ().Rows.Sort (
					TableComparers.FieldLayout.Instance);
			th.Sorted |= ((long) 1 << FieldLayoutTable.RId);

			if (th.HasTable (ImplMapTable.RId))
				m_tableWriter.GetImplMapTable ().Rows.Sort (
					TableComparers.PInvoke.Instance);
			th.Sorted |= ((long) 1 << ImplMapTable.RId);

			if (th.HasTable (FieldRVATable.RId))
				m_tableWriter.GetFieldRVATable ().Rows.Sort (
					TableComparers.FieldRVA.Instance);
			th.Sorted |= ((long) 1 << FieldRVATable.RId);

			if (th.HasTable (MethodImplTable.RId))
				m_tableWriter.GetMethodImplTable ().Rows.Sort (
					TableComparers.Override.Instance);
			th.Sorted |= ((long) 1 << MethodImplTable.RId);

			if (th.HasTable (CustomAttributeTable.RId))
				m_tableWriter.GetCustomAttributeTable ().Rows.Sort (
					TableComparers.CustomAttribute.Instance);
			th.Sorted |= ((long) 1 << CustomAttributeTable.RId);

			if (th.HasTable (DeclSecurityTable.RId))
				m_tableWriter.GetDeclSecurityTable ().Rows.Sort (
					TableComparers.SecurityDeclaration.Instance);
			th.Sorted |= ((long) 1 << DeclSecurityTable.RId);
		}

		void CompleteGenericTables ()
		{
			if (m_genericParamStack.Count == 0)
				return;

			TablesHeap th = m_mdWriter.GetMetadataRoot ().Streams.TablesHeap;
			GenericParamTable gpTable = m_tableWriter.GetGenericParamTable ();
			GenericParamConstraintTable gpcTable = m_tableWriter.GetGenericParamConstraintTable ();

			m_genericParamStack.Sort (TableComparers.GenericParam.Instance);

			foreach (GenericParameter gp in m_genericParamStack) {
				GenericParamRow gpRow = m_rowWriter.CreateGenericParamRow (
					(ushort) gp.Owner.GenericParameters.IndexOf (gp),
					gp.Attributes,
					gp.Owner.MetadataToken,
					m_mdWriter.AddString (gp.Name));

				gpTable.Rows.Add (gpRow);
				gp.MetadataToken = new MetadataToken (TokenType.GenericParam, (uint) gpTable.Rows.Count);

				VisitCustomAttributeCollection (gp.CustomAttributes);

				if (gp.Constraints.Count == 0)
					continue;

				foreach (TypeReference constraint in gp.Constraints) {
					GenericParamConstraintRow gpcRow = m_rowWriter.CreateGenericParamConstraintRow (
						(uint) gpTable.Rows.Count,
						GetTypeDefOrRefToken (constraint));

					gpcTable.Rows.Add (gpcRow);
				}
			}

			th.Sorted |= ((long) 1 << GenericParamTable.RId);
			th.Sorted |= ((long) 1 << GenericParamConstraintTable.RId);
		}

		public override void TerminateModuleDefinition (ModuleDefinition module)
		{
			VisitCustomAttributeCollection (module.Assembly.CustomAttributes);
			VisitSecurityDeclarationCollection (module.Assembly.SecurityDeclarations);
			VisitCustomAttributeCollection (module.CustomAttributes);

			CompleteGenericTables ();
			SortTables ();

			MethodTable mTable = m_tableWriter.GetMethodTable ();
			for (int i = 0; i < m_methodStack.Count; i++) {
				MethodDefinition meth = (MethodDefinition) m_methodStack [i];
				if (meth.HasBody)
					mTable [i].RVA = m_codeWriter.WriteMethodBody (meth);
			}

			if (m_fieldStack.Count > 0) {
				FieldRVATable frTable = null;
				foreach (FieldDefinition field in m_fieldStack) {
					if (field.InitialValue != null && field.InitialValue.Length > 0) {
						if (frTable == null)
							frTable = m_tableWriter.GetFieldRVATable ();

						FieldRVARow frRow = m_rowWriter.CreateFieldRVARow (
							m_mdWriter.GetDataCursor (),
							field.MetadataToken.RID);

						m_mdWriter.AddData (field.InitialValue.Length + 3 & (~3));
						m_mdWriter.AddFieldInitData (field.InitialValue);

						frTable.Rows.Add (frRow);
					}
				}
			}

			if (m_symbolWriter != null)
				m_symbolWriter.Dispose ();

			if (m_mod.Assembly.EntryPoint != null)
				m_mdWriter.EntryPointToken =
					((uint) TokenType.Method) | GetRidFor (m_mod.Assembly.EntryPoint);

			m_mod.Image.MetadataRoot.Accept (m_mdWriter);
		}

		public static ElementType GetCorrespondingType (string fullName)
		{
			switch (fullName) {
			case Constants.Boolean :
				return ElementType.Boolean;
			case Constants.Char :
				return ElementType.Char;
			case Constants.SByte :
				return ElementType.I1;
			case Constants.Int16 :
				return ElementType.I2;
			case Constants.Int32 :
				return ElementType.I4;
			case Constants.Int64 :
				return ElementType.I8;
			case Constants.Byte :
				return ElementType.U1;
			case Constants.UInt16 :
				return ElementType.U2;
			case Constants.UInt32 :
				return ElementType.U4;
			case Constants.UInt64 :
				return ElementType.U8;
			case Constants.Single :
				return ElementType.R4;
			case Constants.Double :
				return ElementType.R8;
			case Constants.String :
				return ElementType.String;
			case Constants.Type :
				return ElementType.Type;
			case Constants.Object :
				return ElementType.Object;
			default:
				return ElementType.Class;
			}
		}

		byte [] EncodeConstant (ElementType et, object value)
		{
			m_constWriter.Empty ();

			if (value == null)
				et = ElementType.Class;

			IConvertible ic = value as IConvertible;
			IFormatProvider fp = CultureInfo.CurrentCulture.NumberFormat;

			switch (et) {
			case ElementType.Boolean :
				m_constWriter.Write ((byte) (ic.ToBoolean (fp) ? 1 : 0));
				break;
			case ElementType.Char :
				m_constWriter.Write ((ushort) ic.ToChar (fp));
				break;
			case ElementType.I1 :
				m_constWriter.Write (ic.ToSByte (fp));
				break;
			case ElementType.I2 :
				m_constWriter.Write (ic.ToInt16 (fp));
				break;
			case ElementType.I4 :
				m_constWriter.Write (ic.ToInt32 (fp));
				break;
			case ElementType.I8 :
				m_constWriter.Write (ic.ToInt64 (fp));
				break;
			case ElementType.U1 :
				m_constWriter.Write (ic.ToByte (fp));
				break;
			case ElementType.U2 :
				m_constWriter.Write (ic.ToUInt16 (fp));
				break;
			case ElementType.U4 :
				m_constWriter.Write (ic.ToUInt32 (fp));
				break;
			case ElementType.U8 :
				m_constWriter.Write (ic.ToUInt64 (fp));
				break;
			case ElementType.R4 :
				m_constWriter.Write (ic.ToSingle (fp));
				break;
			case ElementType.R8 :
				m_constWriter.Write (ic.ToDouble (fp));
				break;
			case ElementType.String :
				m_constWriter.Write (Encoding.Unicode.GetBytes ((string) value));
				break;
			case ElementType.Class :
				m_constWriter.Write (new byte [4]);
				break;
			default :
				throw new ArgumentException ("Non valid element for a constant");
			}

			return m_constWriter.ToArray ();
		}

		public SigType GetSigType (TypeReference type)
		{
			string name = type.FullName;

			switch (name) {
			case Constants.Void :
				return new SigType (ElementType.Void);
			case Constants.Object :
				return new SigType (ElementType.Object);
			case Constants.Boolean :
				return new SigType (ElementType.Boolean);
			case Constants.String :
				return new SigType (ElementType.String);
			case Constants.Char :
				return new SigType (ElementType.Char);
			case Constants.SByte :
				return new SigType (ElementType.I1);
			case Constants.Byte :
				return new SigType (ElementType.U1);
			case Constants.Int16 :
				return new SigType (ElementType.I2);
			case Constants.UInt16 :
				return new SigType (ElementType.U2);
			case Constants.Int32 :
				return new SigType (ElementType.I4);
			case Constants.UInt32 :
				return new SigType (ElementType.U4);
			case Constants.Int64 :
				return new SigType (ElementType.I8);
			case Constants.UInt64 :
				return new SigType (ElementType.U8);
			case Constants.Single :
				return new SigType (ElementType.R4);
			case Constants.Double :
				return new SigType (ElementType.R8);
			case Constants.IntPtr :
				return new SigType (ElementType.I);
			case Constants.UIntPtr :
				return new SigType (ElementType.U);
			case Constants.TypedReference :
				return new SigType (ElementType.TypedByRef);
			}

			if (type is GenericParameter) {
				GenericParameter gp = type as GenericParameter;
				int pos = gp.Owner.GenericParameters.IndexOf (gp);
				if (gp.Owner is TypeReference)
					return new VAR (pos);
				else if (gp.Owner is MethodReference)
					return new MVAR (pos);
				else
					throw new ReflectionException ("Unkown generic parameter type");
			} else if (type is GenericInstanceType) {
				GenericInstanceType git = type as GenericInstanceType;
				GENERICINST gi = new GENERICINST ();
				gi.ValueType = git.IsValueType;
				gi.Type = GetTypeDefOrRefToken (git.ElementType);
				gi.Signature = new GenericInstSignature ();
				gi.Signature.Arity = git.GenericArguments.Count;
				gi.Signature.Types = new GenericArg [gi.Signature.Arity];
				for (int i = 0; i < git.GenericArguments.Count; i++)
					gi.Signature.Types [i] = GetGenericArgSig (git.GenericArguments [i]);

				return gi;
			} else if (type is ArrayType) {
				ArrayType aryType = type as ArrayType;
				if (aryType.IsSizedArray) {
					SZARRAY szary = new SZARRAY ();
					szary.CustomMods = GetCustomMods (aryType.ElementType);
					szary.Type = GetSigType (aryType.ElementType);
					return szary;
				}

				// not optimized
				ArrayShape shape = new ArrayShape ();
				shape.Rank = aryType.Dimensions.Count;
				shape.NumSizes = 0;

				for (int i = 0; i < shape.Rank; i++) {
					ArrayDimension dim = aryType.Dimensions [i];
					if (dim.UpperBound > 0)
						shape.NumSizes++;
				}

				shape.Sizes = new int [shape.NumSizes];
				shape.NumLoBounds = shape.Rank;
				shape.LoBounds = new int [shape.NumLoBounds];

				for (int i = 0; i < shape.Rank; i++) {
					ArrayDimension dim = aryType.Dimensions [i];
					shape.LoBounds [i] = dim.LowerBound;
					if (dim.UpperBound > 0)
						shape.Sizes [i] = dim.UpperBound - dim.LowerBound + 1;
				}

				ARRAY ary = new ARRAY ();
				ary.Shape = shape;
				ary.CustomMods = GetCustomMods (aryType.ElementType);
				ary.Type = GetSigType (aryType.ElementType);
				return ary;
			} else if (type is PointerType) {
				PTR p = new PTR ();
				TypeReference elementType = (type as PointerType).ElementType;
				p.Void = elementType.FullName == Constants.Void;
				if (!p.Void) {
					p.CustomMods = GetCustomMods (elementType);
					p.PtrType = GetSigType (elementType);
				}
				return p;
			} else if (type is FunctionPointerType) {
				FNPTR fp = new FNPTR ();
				FunctionPointerType fptr = type as FunctionPointerType;

				int sentinel = fptr.GetSentinel ();
				if (sentinel < 0)
					fp.Method = GetMethodDefSig (fptr);
				else
					fp.Method = GetMethodRefSig (fptr);

				return fp;
			} else if (type is TypeSpecification) {
				return GetSigType ((type as TypeSpecification).ElementType);
			} else if (type.IsValueType) {
				VALUETYPE vt = new VALUETYPE ();
				vt.Type = GetTypeDefOrRefToken (type);
				return vt;
			} else {
				CLASS c = new CLASS ();
				c.Type = GetTypeDefOrRefToken (type);
				return c;
			}
		}

		public GenericArg GetGenericArgSig (TypeReference type)
		{
			GenericArg arg = new GenericArg (GetSigType (type));
			arg.CustomMods = GetCustomMods (type);
			return arg;
		}

		public CustomMod [] GetCustomMods (TypeReference type)
		{
			ModType modifier = type as ModType;
			if (modifier == null)
				return new CustomMod [0];

			ArrayList cmods = new ArrayList ();
			do {
				CustomMod cmod = new CustomMod ();
				cmod.TypeDefOrRef = GetTypeDefOrRefToken (modifier.ModifierType);

				if (modifier is ModifierOptional)
					cmod.CMOD = CustomMod.CMODType.OPT;
				else if (modifier is ModifierRequired)
					cmod.CMOD = CustomMod.CMODType.REQD;

				cmods.Add (cmod);
				modifier = modifier.ElementType as ModType;
			} while (modifier != null);

			return cmods.ToArray (typeof (CustomMod)) as CustomMod [];
		}

		public Signature GetMemberRefSig (MemberReference member)
		{
			if (member is FieldReference)
				return GetFieldSig (member as FieldReference);
			else
				return GetMemberRefSig (member as MethodReference);
		}

		public FieldSig GetFieldSig (FieldReference field)
		{
			FieldSig sig = new FieldSig ();
			sig.CallingConvention |= 0x6;
			sig.Field = true;
			sig.CustomMods = GetCustomMods (field.FieldType);
			sig.Type = GetSigType (field.FieldType);
			return sig;
		}

		Param [] GetParametersSig (ParameterDefinitionCollection parameters)
		{
			Param [] ret = new Param [parameters.Count];
			for (int i = 0; i < ret.Length; i++) {
				ParameterDefinition pDef = parameters [i];
				Param p = new Param ();
				p.CustomMods = GetCustomMods (pDef.ParameterType);
				if (pDef.ParameterType.FullName == Constants.TypedReference)
					p.TypedByRef = true;
				else if (IsByReferenceType (pDef.ParameterType)) {
					p.ByRef = true;
					p.Type = GetSigType (pDef.ParameterType);
				} else
					p.Type = GetSigType (pDef.ParameterType);
				ret [i] = p;
			}
			return ret;
		}

		void CompleteMethodSig (IMethodSignature meth, MethodSig sig)
		{
			sig.HasThis = meth.HasThis;
			sig.ExplicitThis = meth.ExplicitThis;
			if (sig.HasThis)
				sig.CallingConvention |= 0x20;
			if (sig.ExplicitThis)
				sig.CallingConvention |= 0x40;

			if ((meth.CallingConvention & MethodCallingConvention.VarArg) != 0)
				sig.CallingConvention |= 0x5;

			sig.ParamCount = meth.Parameters.Count;
			sig.Parameters = GetParametersSig (meth.Parameters);

			RetType rtSig = new RetType ();
			rtSig.CustomMods = GetCustomMods (meth.ReturnType.ReturnType);

			if (meth.ReturnType.ReturnType.FullName == Constants.Void)
				rtSig.Void = true;
			else if (meth.ReturnType.ReturnType.FullName == Constants.TypedReference)
				rtSig.TypedByRef = true;
			else if (IsByReferenceType (meth.ReturnType.ReturnType)) {
				rtSig.ByRef = true;
				rtSig.Type = GetSigType (meth.ReturnType.ReturnType);
			} else
				rtSig.Type = GetSigType (meth.ReturnType.ReturnType);

			sig.RetType = rtSig;
		}

		static bool IsByReferenceType (TypeReference type)
		{
			TypeSpecification ts = type as TypeSpecification;
			while (ts != null) {
				if (ts is ReferenceType)
					return true;
				ts = ts.ElementType as TypeSpecification;
			}
			return false;
		}

		public MethodRefSig GetMethodRefSig (IMethodSignature meth)
		{
			MethodReference methodRef = meth as MethodReference;
			if (methodRef != null && methodRef.GenericParameters.Count > 0)
				return GetMethodDefSig (meth);

			MethodRefSig methSig = new MethodRefSig ();

			CompleteMethodSig (meth, methSig);

			int sentinel = meth.GetSentinel ();
			if (sentinel >= 0)
				methSig.Sentinel = sentinel;

			if ((meth.CallingConvention & MethodCallingConvention.C) != 0)
				methSig.CallingConvention |= 0x1;
			else if ((meth.CallingConvention & MethodCallingConvention.StdCall) != 0)
				methSig.CallingConvention |= 0x2;
			else if ((meth.CallingConvention & MethodCallingConvention.ThisCall) != 0)
				methSig.CallingConvention |= 0x3;
			else if ((meth.CallingConvention & MethodCallingConvention.FastCall) != 0)
				methSig.CallingConvention |= 0x4;

			return methSig;
		}

		public MethodDefSig GetMethodDefSig (IMethodSignature meth)
		{
			MethodDefSig sig = new MethodDefSig ();

			CompleteMethodSig (meth, sig);

			MethodReference methodRef = meth as MethodReference;
			if (methodRef != null && methodRef.GenericParameters.Count > 0) {
				sig.CallingConvention |= 0x10;
				sig.GenericParameterCount = methodRef.GenericParameters.Count;
			}

			return sig;
		}

		public PropertySig GetPropertySig (PropertyDefinition prop)
		{
			PropertySig ps = new PropertySig ();
			ps.CallingConvention |= 0x8;

			bool hasThis;
			bool explicitThis;
			MethodCallingConvention mcc;
			ParameterDefinitionCollection parameters = prop.Parameters;

			MethodDefinition meth;
			if (prop.GetMethod != null)
				meth = prop.GetMethod;
			else if (prop.SetMethod != null)
				meth = prop.SetMethod;
			else
				meth = null;

			if (meth != null) {
				hasThis = meth.HasThis;
				explicitThis = meth.ExplicitThis;
				mcc = meth.CallingConvention;
			} else {
				hasThis = explicitThis = false;
				mcc = MethodCallingConvention.Default;
			}

			if (hasThis)
				ps.CallingConvention |= 0x20;
			if (explicitThis)
				ps.CallingConvention |= 0x40;

			if ((mcc & MethodCallingConvention.VarArg) != 0)
				ps.CallingConvention |= 0x5;

			int paramCount = parameters != null ? parameters.Count : 0;

			ps.ParamCount = paramCount;
			ps.Parameters = GetParametersSig (parameters);
			ps.CustomMods = GetCustomMods (prop.PropertyType);
			ps.Type = GetSigType (prop.PropertyType);

			return ps;
		}

		public TypeSpec GetTypeSpecSig (TypeReference type)
		{
			TypeSpec ts = new TypeSpec ();
			ts.CustomMods = GetCustomMods (type);
			ts.Type = GetSigType (type);
			return ts;
		}

		public MethodSpec GetMethodSpecSig (GenericInstanceMethod gim)
		{
			GenericInstSignature gis = new GenericInstSignature ();
			gis.Arity = gim.GenericArguments.Count;
			gis.Types = new GenericArg [gis.Arity];
			for (int i = 0; i < gis.Arity; i++)
				gis.Types [i] = GetGenericArgSig (gim.GenericArguments [i]);

			return new MethodSpec (gis);
		}

		static string GetObjectTypeName (object o)
		{
			Type t = o.GetType ();
			return string.Concat (t.Namespace, ".", t.Name);
		}

		static CustomAttrib.Elem CreateElem (TypeReference type, object value)
		{
			CustomAttrib.Elem elem = new CustomAttrib.Elem ();
			elem.Value = value;
			elem.ElemType = type;
			elem.FieldOrPropType = GetCorrespondingType (type.FullName);

			if (elem.FieldOrPropType == ElementType.Class)
				throw new NotImplementedException ("Writing enums");

			switch (elem.FieldOrPropType) {
			case ElementType.Boolean :
			case ElementType.Char :
			case ElementType.R4 :
			case ElementType.R8 :
			case ElementType.I1 :
			case ElementType.I2 :
			case ElementType.I4 :
			case ElementType.I8 :
			case ElementType.U1 :
			case ElementType.U2 :
			case ElementType.U4 :
			case ElementType.U8 :
				elem.Simple = true;
				break;
			case ElementType.String:
				elem.String = true;
				break;
			case ElementType.Type:
				elem.Type = true;
				break;
			case ElementType.Object:
				elem.BoxedValueType = true;
				if (value == null)
					elem.FieldOrPropType = ElementType.String;
				else
					elem.FieldOrPropType = GetCorrespondingType (
						GetObjectTypeName (value));
				break;
			}

			return elem;
		}

		static CustomAttrib.FixedArg CreateFixedArg (TypeReference type, object value)
		{
			CustomAttrib.FixedArg fa = new CustomAttrib.FixedArg ();
			if (value is object []) {
				fa.SzArray = true;
				object [] values = value as object [];
				TypeReference obj = ((ArrayType) type).ElementType;
				fa.NumElem = (uint) values.Length;
				fa.Elems = new CustomAttrib.Elem [values.Length];
				for (int i = 0; i < values.Length; i++)
					fa.Elems [i] = CreateElem (obj, values [i]);
			} else {
				fa.Elems = new CustomAttrib.Elem [1];
				fa.Elems [0] = CreateElem (type, value);
			}

			return fa;
		}

		static CustomAttrib.NamedArg CreateNamedArg (TypeReference type, string name,
			object value, bool field)
		{
			CustomAttrib.NamedArg na = new CustomAttrib.NamedArg ();
			na.Field = field;
			na.Property = !field;

			na.FieldOrPropName = name;
			na.FieldOrPropType = GetCorrespondingType (type.FullName);
			na.FixedArg = CreateFixedArg (type, value);

			return na;
		}

		public static CustomAttrib GetCustomAttributeSig (CustomAttribute ca)
		{
			CustomAttrib cas = new CustomAttrib (ca.Constructor);
			cas.Prolog = CustomAttrib.StdProlog;

			cas.FixedArgs = new CustomAttrib.FixedArg [ca.Constructor.Parameters.Count];

			for (int i = 0; i < cas.FixedArgs.Length; i++)
				cas.FixedArgs [i] = CreateFixedArg (
					ca.Constructor.Parameters [i].ParameterType, ca.ConstructorParameters [i]);

			int nn = ca.Fields.Count + ca.Properties.Count;
			cas.NumNamed = (ushort) nn;
			cas.NamedArgs = new CustomAttrib.NamedArg [nn];

			if (cas.NamedArgs.Length > 0) {
				int curs = 0;
				foreach (DictionaryEntry entry in ca.Fields) {
					string field = (string) entry.Key;
					cas.NamedArgs [curs++] = CreateNamedArg (
						ca.GetFieldType (field), field, entry.Value, true);
				}

				foreach (DictionaryEntry entry in ca.Properties) {
					string property = (string) entry.Key;
					cas.NamedArgs [curs++] = CreateNamedArg (
						ca.GetPropertyType (property), property, entry.Value, false);
				}
			}

			return cas;
		}

		static MarshalSig GetMarshalSig (MarshalSpec mSpec)
		{
			MarshalSig ms = new MarshalSig (mSpec.NativeIntrinsic);

			if (mSpec is ArrayMarshalSpec) {
				ArrayMarshalSpec amd = mSpec as ArrayMarshalSpec;
				MarshalSig.Array ar = new MarshalSig.Array ();
				ar.ArrayElemType = amd.ElemType;
				ar.NumElem = amd.NumElem;
				ar.ParamNum = amd.ParamNum;
				ar.ElemMult = amd.ElemMult;
				ms.Spec = ar;
			} else if (mSpec is CustomMarshalerSpec) {
				CustomMarshalerSpec cmd = mSpec as CustomMarshalerSpec;
				MarshalSig.CustomMarshaler cm = new MarshalSig.CustomMarshaler ();
				cm.Guid = cmd.Guid.ToString ();
				cm.UnmanagedType = cmd.UnmanagedType;
				cm.ManagedType = cmd.ManagedType;
				cm.Cookie = cmd.Cookie;
				ms.Spec = cm;
			} else if (mSpec is FixedArraySpec) {
				FixedArraySpec fad = mSpec as FixedArraySpec;
				MarshalSig.FixedArray fa = new MarshalSig.FixedArray ();
				fa.ArrayElemType  = fad.ElemType;
				fa.NumElem = fad.NumElem;
				ms.Spec = fa;
			} else if (mSpec is FixedSysStringSpec) {
				MarshalSig.FixedSysString fss = new MarshalSig.FixedSysString ();
				fss.Size = (mSpec as FixedSysStringSpec).Size;
				ms.Spec = fss;
			} else if (mSpec is SafeArraySpec) {
				MarshalSig.SafeArray sa = new MarshalSig.SafeArray ();
				sa.ArrayElemType = (mSpec as SafeArraySpec).ElemType;
				ms.Spec = sa;
			}

			return ms;
		}

		public void WriteSymbols (ModuleDefinition module)
		{
			if (!m_saveSymbols)
				return;

			if (m_asmOutput == null)
				m_asmOutput = module.Assembly.Name.Name + "." + (module.Assembly.Kind == AssemblyKind.Dll ? "dll" : "exe");

			if (m_symbolWriter == null)
				m_symbolWriter = SymbolStoreHelper.GetWriter (module, m_asmOutput);

			foreach (TypeDefinition type in module.Types) {
				foreach (MethodDefinition method in type.Methods)
					WriteSymbols (method);
				foreach (MethodDefinition ctor in type.Constructors)
					WriteSymbols (ctor);
			}

			m_symbolWriter.Dispose ();
		}

		void WriteSymbols (MethodDefinition meth)
		{
			if (!meth.HasBody)
				return;

			m_symbolWriter.Write (meth.Body, GetVariablesSig (meth));
		}

		byte [][] GetVariablesSig (MethodDefinition meth)
		{
			VariableDefinitionCollection variables = meth.Body.Variables;
			byte [][] signatures = new byte [variables.Count][];
			for (int i = 0; i < variables.Count; i++) {
				signatures [i] = GetVariableSig (variables [i]);
			}
			return signatures;
		}

		byte [] GetVariableSig (VariableDefinition var)
		{
			return m_sigWriter.CompressLocalVar (m_codeWriter.GetLocalVariableSig (var));
		}
	}
}
