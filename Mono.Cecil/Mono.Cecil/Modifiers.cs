//
// Modifiers.cs
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

using MD = Mono.Cecil.Metadata;

namespace Mono.Cecil {

	public interface IModifierType {
		TypeReference ModifierType { get; }
		TypeReference ElementType { get; }
	}

	public sealed class OptionalModifierType : TypeSpecification, IModifierType {

		TypeReference modifier_type;

		public TypeReference ModifierType {
			get { return modifier_type; }
			set { modifier_type = value; }
		}

		public override string Name {
			get { return base.Name + Suffix; }
		}

		public override string FullName {
			get { return base.FullName + Suffix; }
		}

		string Suffix {
			get { return " modopt(" + modifier_type + ")"; }
		}

		public override bool IsValueType {
			get { return false; }
			set { throw new InvalidOperationException (); }
		}

		public override bool IsOptionalModifier {
			get { return true; }
		}

		internal override bool ContainsGenericParameter {
			get { return modifier_type.ContainsGenericParameter || base.ContainsGenericParameter; }
		}

		public OptionalModifierType (TypeReference modifierType, TypeReference type)
			: base (type)
		{
			Mixin.CheckModifier (modifierType, type);
			this.modifier_type = modifierType;
			this.etype = MD.ElementType.CModOpt;
		}
	}

	public sealed class RequiredModifierType : TypeSpecification, IModifierType {

		TypeReference modifier_type;

		public TypeReference ModifierType {
			get { return modifier_type; }
			set { modifier_type = value; }
		}

		public override string Name {
			get { return base.Name + Suffix; }
		}

		public override string FullName {
			get { return base.FullName + Suffix; }
		}

		string Suffix {
			get { return " modreq(" + modifier_type + ")"; }
		}

		public override bool IsValueType {
			get { return false; }
			set { throw new InvalidOperationException (); }
		}

		public override bool IsRequiredModifier {
			get { return true; }
		}

		internal override bool ContainsGenericParameter {
			get { return modifier_type.ContainsGenericParameter || base.ContainsGenericParameter; }
		}

		public RequiredModifierType (TypeReference modifierType, TypeReference type)
			: base (type)
		{
			Mixin.CheckModifier (modifierType, type);
			this.modifier_type = modifierType;
			this.etype = MD.ElementType.CModReqD;
		}

	}

	static partial class Mixin {

		public static void CheckModifier (TypeReference modifierType, TypeReference type)
		{
			if (modifierType == null)
				throw new ArgumentNullException ("modifierType");
			if (type == null)
				throw new ArgumentNullException ("type");
		}
	}
}
