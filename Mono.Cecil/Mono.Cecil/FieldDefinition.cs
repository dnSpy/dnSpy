//
// FieldDefinition.cs
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

using Mono.Collections.Generic;

namespace Mono.Cecil {

	public sealed class FieldDefinition : FieldReference, IMemberDefinition, IConstantProvider, IMarshalInfoProvider {

		ushort attributes;
		Collection<CustomAttribute> custom_attributes;

		int offset = Mixin.NotResolvedMarker;

		internal int rva = Mixin.NotResolvedMarker;
		byte [] initial_value;

		object constant = Mixin.NotResolved;

		MarshalInfo marshal_info;

		void ResolveLayout ()
		{
			if (offset != Mixin.NotResolvedMarker)
				return;

			if (!HasImage) {
				offset = Mixin.NoDataMarker;
				return;
			}

			offset = Module.Read (this, (field, reader) => reader.ReadFieldLayout (field));
		}

		public bool HasLayoutInfo {
			get {
				if (offset >= 0)
					return true;

				ResolveLayout ();

				return offset >= 0;
			}
		}

		public int Offset {
			get {
				if (offset >= 0)
					return offset;

				ResolveLayout ();

				return offset >= 0 ? offset : -1;
			}
			set { offset = value; }
		}

		void ResolveRVA ()
		{
			if (rva != Mixin.NotResolvedMarker)
				return;

			if (!HasImage)
				return;

			rva = Module.Read (this, (field, reader) => reader.ReadFieldRVA (field));
		}

		public int RVA {
			get {
				if (rva > 0)
					return rva;

				ResolveRVA ();

				return rva > 0 ? rva : 0;
			}
		}

		public byte [] InitialValue {
			get {
				if (initial_value != null)
					return initial_value;

				ResolveRVA ();

				if (initial_value == null)
					initial_value = Empty<byte>.Array;

				return initial_value;
			}
			set { initial_value = value; }
		}

		public FieldAttributes Attributes {
			get { return (FieldAttributes) attributes; }
			set { attributes = (ushort) value; }
		}

		public bool HasConstant {
			get {
				this.ResolveConstant (ref constant, Module);

				return constant != Mixin.NoValue;
			}
			set { if (!value) constant = Mixin.NoValue; }
		}

		public object Constant {
			get { return HasConstant ? constant : null;	}
			set { constant = value; }
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

		public bool HasMarshalInfo {
			get {
				if (marshal_info != null)
					return true;

				return this.GetHasMarshalInfo (Module);
			}
		}

		public MarshalInfo MarshalInfo {
			get { return marshal_info ?? (this.GetMarshalInfo (ref marshal_info, Module)); }
			set { marshal_info = value; }
		}

		#region FieldAttributes

		public bool IsCompilerControlled {
			get { return attributes.GetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.CompilerControlled); }
			set { attributes = attributes.SetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.CompilerControlled, value); }
		}

		public bool IsPrivate {
			get { return attributes.GetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Private); }
			set { attributes = attributes.SetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Private, value); }
		}

		public bool IsFamilyAndAssembly {
			get { return attributes.GetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.FamANDAssem); }
			set { attributes = attributes.SetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.FamANDAssem, value); }
		}

		public bool IsAssembly {
			get { return attributes.GetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Assembly); }
			set { attributes = attributes.SetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Assembly, value); }
		}

		public bool IsFamily {
			get { return attributes.GetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Family); }
			set { attributes = attributes.SetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Family, value); }
		}

		public bool IsFamilyOrAssembly {
			get { return attributes.GetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.FamORAssem); }
			set { attributes = attributes.SetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.FamORAssem, value); }
		}

		public bool IsPublic {
			get { return attributes.GetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Public); }
			set { attributes = attributes.SetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Public, value); }
		}

		public bool IsStatic {
			get { return attributes.GetAttributes ((ushort) FieldAttributes.Static); }
			set { attributes = attributes.SetAttributes ((ushort) FieldAttributes.Static, value); }
		}

		public bool IsInitOnly {
			get { return attributes.GetAttributes ((ushort) FieldAttributes.InitOnly); }
			set { attributes = attributes.SetAttributes ((ushort) FieldAttributes.InitOnly, value); }
		}

		public bool IsLiteral {
			get { return attributes.GetAttributes ((ushort) FieldAttributes.Literal); }
			set { attributes = attributes.SetAttributes ((ushort) FieldAttributes.Literal, value); }
		}

		public bool IsNotSerialized {
			get { return attributes.GetAttributes ((ushort) FieldAttributes.NotSerialized); }
			set { attributes = attributes.SetAttributes ((ushort) FieldAttributes.NotSerialized, value); }
		}

		public bool IsSpecialName {
			get { return attributes.GetAttributes ((ushort) FieldAttributes.SpecialName); }
			set { attributes = attributes.SetAttributes ((ushort) FieldAttributes.SpecialName, value); }
		}

		public bool IsPInvokeImpl {
			get { return attributes.GetAttributes ((ushort) FieldAttributes.PInvokeImpl); }
			set { attributes = attributes.SetAttributes ((ushort) FieldAttributes.PInvokeImpl, value); }
		}

		public bool IsRuntimeSpecialName {
			get { return attributes.GetAttributes ((ushort) FieldAttributes.RTSpecialName); }
			set { attributes = attributes.SetAttributes ((ushort) FieldAttributes.RTSpecialName, value); }
		}

		public bool HasDefault {
			get { return attributes.GetAttributes ((ushort) FieldAttributes.HasDefault); }
			set { attributes = attributes.SetAttributes ((ushort) FieldAttributes.HasDefault, value); }
		}

		#endregion

		public override bool IsDefinition {
			get { return true; }
		}

		public new TypeDefinition DeclaringType {
			get { return (TypeDefinition) base.DeclaringType; }
			set { base.DeclaringType = value; }
		}

		public FieldDefinition (string name, FieldAttributes attributes, TypeReference fieldType)
			: base (name, fieldType)
		{
			this.attributes = (ushort) attributes;
		}

		public override FieldDefinition Resolve ()
		{
			return this;
		}
	}

	static partial class Mixin {

		public const int NotResolvedMarker = -2;
		public const int NoDataMarker = -1;
	}
}
