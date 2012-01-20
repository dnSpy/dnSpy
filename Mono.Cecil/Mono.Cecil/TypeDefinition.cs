//
// TypeDefinition.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
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

using System;

using Mono.Cecil.Metadata;
using Mono.Collections.Generic;

namespace Mono.Cecil {

	public sealed class TypeDefinition : TypeReference, IMemberDefinition, ISecurityDeclarationProvider {

		uint attributes;
		TypeReference base_type;
		internal Range fields_range;
		internal Range methods_range;

		short packing_size = Mixin.NotResolvedMarker;
		int class_size = Mixin.NotResolvedMarker;

		Collection<TypeReference> interfaces;
		Collection<TypeDefinition> nested_types;
		Collection<MethodDefinition> methods;
		Collection<FieldDefinition> fields;
		Collection<EventDefinition> events;
		Collection<PropertyDefinition> properties;
		Collection<CustomAttribute> custom_attributes;
		Collection<SecurityDeclaration> security_declarations;

		public TypeAttributes Attributes {
			get { return (TypeAttributes) attributes; }
			set { attributes = (uint) value; }
		}

		public TypeReference BaseType {
			get { return base_type; }
			set { base_type = value; }
		}

		void ResolveLayout ()
		{
			if (packing_size != Mixin.NotResolvedMarker || class_size != Mixin.NotResolvedMarker)
				return;

			if (!HasImage) {
				packing_size = Mixin.NoDataMarker;
				class_size = Mixin.NoDataMarker;
				return;
			}

			var row = Module.Read (this, (type, reader) => reader.ReadTypeLayout (type));

			packing_size = row.Col1;
			class_size = row.Col2;
		}

		public bool HasLayoutInfo {
			get {
				if (packing_size >= 0 || class_size >= 0)
					return true;

				ResolveLayout ();

				return packing_size >= 0 || class_size >= 0;
			}
		}

		public short PackingSize {
			get {
				if (packing_size >= 0)
					return packing_size;

				ResolveLayout ();

				return packing_size >= 0 ? packing_size : (short) -1;
			}
			set { packing_size = value; }
		}

		public int ClassSize {
			get {
				if (class_size >= 0)
					return class_size;

				ResolveLayout ();

				return class_size >= 0 ? class_size : -1;
			}
			set { class_size = value; }
		}

		public bool HasInterfaces {
			get {
				if (interfaces != null)
					return interfaces.Count > 0;

				if (HasImage)
					return Module.Read (this, (type, reader) => reader.HasInterfaces (type));

				return false;
			}
		}

		public Collection<TypeReference> Interfaces {
			get {
				if (interfaces != null)
					return interfaces;

				if (HasImage)
					return Module.Read (ref interfaces, this, (type, reader) => reader.ReadInterfaces (type));

				return interfaces = new Collection<TypeReference> ();
			}
		}

		public bool HasNestedTypes {
			get {
				if (nested_types != null)
					return nested_types.Count > 0;

				if (HasImage)
					return Module.Read (this, (type, reader) => reader.HasNestedTypes (type));

				return false;
			}
		}

		public Collection<TypeDefinition> NestedTypes {
			get {
				if (nested_types != null)
					return nested_types;

				if (HasImage)
					return Module.Read (ref nested_types, this, (type, reader) => reader.ReadNestedTypes (type));

				return nested_types = new MemberDefinitionCollection<TypeDefinition> (this);
			}
		}

		public bool HasMethods {
			get {
				if (methods != null)
					return methods.Count > 0;

				if (HasImage)
					return methods_range.Length > 0;

				return false;
			}
		}

		public Collection<MethodDefinition> Methods {
			get {
				if (methods != null)
					return methods;

				if (HasImage)
					return Module.Read (ref methods, this, (type, reader) => reader.ReadMethods (type));

				return methods = new MemberDefinitionCollection<MethodDefinition> (this);
			}
		}

		public bool HasFields {
			get {
				if (fields != null)
					return fields.Count > 0;

				if (HasImage)
					return fields_range.Length > 0;

				return false;
			}
		}

		public Collection<FieldDefinition> Fields {
			get {
				if (fields != null)
					return fields;

				if (HasImage)
					return Module.Read (ref fields, this, (type, reader) => reader.ReadFields (type));

				return fields = new MemberDefinitionCollection<FieldDefinition> (this);
			}
		}

		public bool HasEvents {
			get {
				if (events != null)
					return events.Count > 0;

				if (HasImage)
					return Module.Read (this, (type, reader) => reader.HasEvents (type));

				return false;
			}
		}

		public Collection<EventDefinition> Events {
			get {
				if (events != null)
					return events;

				if (HasImage)
					return Module.Read (ref events, this, (type, reader) => reader.ReadEvents (type));

				return events = new MemberDefinitionCollection<EventDefinition> (this);
			}
		}

		public bool HasProperties {
			get {
				if (properties != null)
					return properties.Count > 0;

				if (HasImage)
					return Module.Read (this, (type, reader) => reader.HasProperties (type));

				return false;
			}
		}

		public Collection<PropertyDefinition> Properties {
			get {
				if (properties != null)
					return properties;

				if (HasImage)
					return Module.Read (ref properties, this, (type, reader) => reader.ReadProperties (type));

				return properties = new MemberDefinitionCollection<PropertyDefinition> (this);
			}
		}

		public bool HasSecurityDeclarations {
			get {
				if (security_declarations != null)
					return security_declarations.Count > 0;

				return this.GetHasSecurityDeclarations (Module);
			}
		}

		public Collection<SecurityDeclaration> SecurityDeclarations {
			get { return security_declarations ?? (this.GetSecurityDeclarations (ref security_declarations, Module)); }
		}

		public bool HasCustomAttributes {
			get {
				if (custom_attributes != null)
					return custom_attributes.Count > 0;

				return this.GetHasCustomAttributes (Module);
			}
		}

		public Collection<CustomAttribute> CustomAttributes {
			get { return custom_attributes ?? (this.GetCustomAttributes (ref custom_attributes, Module)); }
		}

		public override bool HasGenericParameters {
			get {
				if (generic_parameters != null)
					return generic_parameters.Count > 0;

				return this.GetHasGenericParameters (Module);
			}
		}

		public override Collection<GenericParameter> GenericParameters {
			get { return generic_parameters ?? (this.GetGenericParameters (ref generic_parameters, Module)); }
		}

		#region TypeAttributes

		public bool IsNotPublic {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NotPublic); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NotPublic, value); }
		}

		public bool IsPublic {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.Public); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.Public, value); }
		}

		public bool IsNestedPublic {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedPublic); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedPublic, value); }
		}

		public bool IsNestedPrivate {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedPrivate); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedPrivate, value); }
		}

		public bool IsNestedFamily {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedFamily); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedFamily, value); }
		}

		public bool IsNestedAssembly {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedAssembly); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedAssembly, value); }
		}

		public bool IsNestedFamilyAndAssembly {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedFamANDAssem); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedFamANDAssem, value); }
		}

		public bool IsNestedFamilyOrAssembly {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedFamORAssem); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedFamORAssem, value); }
		}

		public bool IsAutoLayout {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.LayoutMask, (uint) TypeAttributes.AutoLayout); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.LayoutMask, (uint) TypeAttributes.AutoLayout, value); }
		}

		public bool IsSequentialLayout {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.LayoutMask, (uint) TypeAttributes.SequentialLayout); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.LayoutMask, (uint) TypeAttributes.SequentialLayout, value); }
		}

		public bool IsExplicitLayout {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.LayoutMask, (uint) TypeAttributes.ExplicitLayout); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.LayoutMask, (uint) TypeAttributes.ExplicitLayout, value); }
		}

		public bool IsClass {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.ClassSemanticMask, (uint) TypeAttributes.Class); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.ClassSemanticMask, (uint) TypeAttributes.Class, value); }
		}

		public bool IsInterface {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.ClassSemanticMask, (uint) TypeAttributes.Interface); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.ClassSemanticMask, (uint) TypeAttributes.Interface, value); }
		}

		public bool IsAbstract {
			get { return attributes.GetAttributes ((uint) TypeAttributes.Abstract); }
			set { attributes = attributes.SetAttributes ((uint) TypeAttributes.Abstract, value); }
		}

		public bool IsSealed {
			get { return attributes.GetAttributes ((uint) TypeAttributes.Sealed); }
			set { attributes = attributes.SetAttributes ((uint) TypeAttributes.Sealed, value); }
		}

		public bool IsSpecialName {
			get { return attributes.GetAttributes ((uint) TypeAttributes.SpecialName); }
			set { attributes = attributes.SetAttributes ((uint) TypeAttributes.SpecialName, value); }
		}

		public bool IsImport {
			get { return attributes.GetAttributes ((uint) TypeAttributes.Import); }
			set { attributes = attributes.SetAttributes ((uint) TypeAttributes.Import, value); }
		}

		public bool IsSerializable {
			get { return attributes.GetAttributes ((uint) TypeAttributes.Serializable); }
			set { attributes = attributes.SetAttributes ((uint) TypeAttributes.Serializable, value); }
		}

		public bool IsAnsiClass {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.StringFormatMask, (uint) TypeAttributes.AnsiClass); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.StringFormatMask, (uint) TypeAttributes.AnsiClass, value); }
		}

		public bool IsUnicodeClass {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.StringFormatMask, (uint) TypeAttributes.UnicodeClass); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.StringFormatMask, (uint) TypeAttributes.UnicodeClass, value); }
		}

		public bool IsAutoClass {
			get { return attributes.GetMaskedAttributes ((uint) TypeAttributes.StringFormatMask, (uint) TypeAttributes.AutoClass); }
			set { attributes = attributes.SetMaskedAttributes ((uint) TypeAttributes.StringFormatMask, (uint) TypeAttributes.AutoClass, value); }
		}

		public bool IsBeforeFieldInit {
			get { return attributes.GetAttributes ((uint) TypeAttributes.BeforeFieldInit); }
			set { attributes = attributes.SetAttributes ((uint) TypeAttributes.BeforeFieldInit, value); }
		}

		public bool IsRuntimeSpecialName {
			get { return attributes.GetAttributes ((uint) TypeAttributes.RTSpecialName); }
			set { attributes = attributes.SetAttributes ((uint) TypeAttributes.RTSpecialName, value); }
		}

		public bool HasSecurity {
			get { return attributes.GetAttributes ((uint) TypeAttributes.HasSecurity); }
			set { attributes = attributes.SetAttributes ((uint) TypeAttributes.HasSecurity, value); }
		}

		#endregion

		public bool IsEnum {
			get { return base_type != null && base_type.IsTypeOf ("System", "Enum"); }
		}

		public override bool IsValueType {
			get {
				if (base_type == null)
					return false;

				return base_type.IsTypeOf ("System", "Enum") || (base_type.IsTypeOf ("System", "ValueType") && !this.IsTypeOf ("System", "Enum"));
			}
		}

		public override bool IsPrimitive {
			get {
				ElementType primitive_etype;
				return MetadataSystem.TryGetPrimitiveElementType (this, out primitive_etype);
			}
		}

		public override MetadataType MetadataType {
			get {
				ElementType primitive_etype;
				if (MetadataSystem.TryGetPrimitiveElementType (this, out primitive_etype))
					return (MetadataType) primitive_etype;

				return base.MetadataType;
			}
		}

		public override bool IsDefinition {
			get { return true; }
		}

		public new TypeDefinition DeclaringType {
			get { return (TypeDefinition) base.DeclaringType; }
			set { base.DeclaringType = value; }
		}

		public TypeDefinition (string @namespace, string name, TypeAttributes attributes)
			: base (@namespace, name)
		{
			this.attributes = (uint) attributes;
			this.token = new MetadataToken (TokenType.TypeDef);
		}

		public TypeDefinition (string @namespace, string name, TypeAttributes attributes, TypeReference baseType) :
			this (@namespace, name, attributes)
		{
			this.BaseType = baseType;
		}

		public override TypeDefinition Resolve ()
		{
			return this;
		}
	}

	static partial class Mixin {

		public static TypeReference GetEnumUnderlyingType (this TypeDefinition self)
		{
			var fields = self.Fields;

			for (int i = 0; i < fields.Count; i++) {
				var field = fields [i];
				if (!field.IsStatic)
					return field.FieldType;
			}

			throw new ArgumentException ();
		}

		public static TypeDefinition GetNestedType (this TypeDefinition self, string name)
		{
			if (!self.HasNestedTypes)
				return null;

			var nested_types = self.NestedTypes;

			for (int i = 0; i < nested_types.Count; i++) {
				var nested_type = nested_types [i];
				if (nested_type.Name == name)
					return nested_type;
			}

			return null;
		}
	}
}
