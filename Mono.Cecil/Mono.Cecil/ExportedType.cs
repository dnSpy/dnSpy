//
// ExportedType.cs
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

namespace Mono.Cecil {

	public class ExportedType : IMetadataTokenProvider {

		string @namespace;
		string name;
		uint attributes;
		IMetadataScope scope;
		ModuleDefinition module;
		int identifier;
		ExportedType declaring_type;
		internal MetadataToken token;

		public string Namespace {
			get { return @namespace; }
			set { @namespace = value; }
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}

		public TypeAttributes Attributes {
			get { return (TypeAttributes) attributes; }
			set { attributes = (uint) value; }
		}

		public IMetadataScope Scope {
			get {
				if (declaring_type != null)
					return declaring_type.Scope;

				return scope;
			}
		}

		public ExportedType DeclaringType {
			get { return declaring_type; }
			set { declaring_type = value; }
		}

		public MetadataToken MetadataToken {
			get { return token; }
			set { token = value; }
		}

		public int Identifier {
			get { return identifier; }
			set { identifier = value; }
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

		public bool IsForwarder {
			get { return attributes.GetAttributes ((uint) TypeAttributes.Forwarder); }
			set { attributes = attributes.SetAttributes ((uint) TypeAttributes.Forwarder, value); }
		}

		public string FullName {
			get {
				if (declaring_type != null)
					return declaring_type.FullName + "/" + name;

				if (string.IsNullOrEmpty (@namespace))
					return name;

				return @namespace + "." + name;
			}
		}

		public ExportedType (string @namespace, string name, ModuleDefinition module, IMetadataScope scope)
		{
			this.@namespace = @namespace;
			this.name = name;
			this.scope = scope;
			this.module = module;
		}

		public override string ToString ()
		{
			return FullName;
		}

		public TypeDefinition Resolve ()
		{
			return module.Resolve (CreateReference ());
		}

		internal TypeReference CreateReference ()
		{
			return new TypeReference (@namespace, name, module, scope) {
				DeclaringType = declaring_type != null ? declaring_type.CreateReference () : null,
			};
		}
	}
}
