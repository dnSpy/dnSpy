//
// PropertyDefinition.cs
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

using System.Text;

using Mono.Collections.Generic;

namespace Mono.Cecil {

	public sealed class PropertyDefinition : PropertyReference, IMemberDefinition, IConstantProvider {

		bool? has_this;
		ushort attributes;

		Collection<CustomAttribute> custom_attributes;

		internal MethodDefinition get_method;
		internal MethodDefinition set_method;
		internal Collection<MethodDefinition> other_methods;

		object constant = Mixin.NotResolved;

		public PropertyAttributes Attributes {
			get { return (PropertyAttributes) attributes; }
			set { attributes = (ushort) value; }
		}

		public bool HasThis {
			get {
				if (has_this.HasValue)
					return has_this.Value;

				if (GetMethod != null)
					return get_method.HasThis;

				if (SetMethod != null)
					return set_method.HasThis;

				return false;
			}
			set { has_this = value; }
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

		public MethodDefinition GetMethod {
			get {
				if (get_method != null)
					return get_method;

				InitializeMethods ();
				return get_method;
			}
			set { get_method = value; }
		}

		public MethodDefinition SetMethod {
			get {
				if (set_method != null)
					return set_method;

				InitializeMethods ();
				return set_method;
			}
			set { set_method = value; }
		}

		public bool HasOtherMethods {
			get {
				if (other_methods != null)
					return other_methods.Count > 0;

				InitializeMethods ();
				return !other_methods.IsNullOrEmpty ();
			}
		}

		public Collection<MethodDefinition> OtherMethods {
			get {
				if (other_methods != null)
					return other_methods;

				InitializeMethods ();

				if (other_methods != null)
					return other_methods;

				return other_methods = new Collection<MethodDefinition> ();
			}
		}

		public bool HasParameters {
			get {
				InitializeMethods ();

				if (get_method != null)
					return get_method.HasParameters;

				if (set_method != null)
					return set_method.HasParameters && set_method.Parameters.Count > 1;

				return false;
			}
		}

		public override Collection<ParameterDefinition> Parameters {
			get {
				InitializeMethods ();

				if (get_method != null)
					return MirrorParameters (get_method, 0);

				if (set_method != null)
					return MirrorParameters (set_method, 1);

				return new Collection<ParameterDefinition> ();
			}
		}

		static Collection<ParameterDefinition> MirrorParameters (MethodDefinition method, int bound)
		{
			var parameters = new Collection<ParameterDefinition> ();
			if (!method.HasParameters)
				return parameters;

			var original_parameters = method.Parameters;
			var end = original_parameters.Count - bound;

			for (int i = 0; i < end; i++)
				parameters.Add (original_parameters [i]);

			return parameters;
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

		#region PropertyAttributes

		public bool IsSpecialName {
			get { return attributes.GetAttributes ((ushort) PropertyAttributes.SpecialName); }
			set { attributes = attributes.SetAttributes ((ushort) PropertyAttributes.SpecialName, value); }
		}

		public bool IsRuntimeSpecialName {
			get { return attributes.GetAttributes ((ushort) PropertyAttributes.RTSpecialName); }
			set { attributes = attributes.SetAttributes ((ushort) PropertyAttributes.RTSpecialName, value); }
		}

		public bool HasDefault {
			get { return attributes.GetAttributes ((ushort) PropertyAttributes.HasDefault); }
			set { attributes = attributes.SetAttributes ((ushort) PropertyAttributes.HasDefault, value); }
		}

		#endregion

		public new TypeDefinition DeclaringType {
			get { return (TypeDefinition) base.DeclaringType; }
			set { base.DeclaringType = value; }
		}

		public override bool IsDefinition {
			get { return true; }
		}

		public override string FullName {
			get {
				var builder = new StringBuilder ();
				builder.Append (PropertyType.ToString ());
				builder.Append (' ');
				builder.Append (MemberFullName ());
				builder.Append ('(');
				if (HasParameters) {
					var parameters = Parameters;
					for (int i = 0; i < parameters.Count; i++) {
						if (i > 0)
							builder.Append (',');
						builder.Append (parameters [i].ParameterType.FullName);
					}
				}
				builder.Append (')');
				return builder.ToString ();
			}
		}

		public PropertyDefinition (string name, PropertyAttributes attributes, TypeReference propertyType)
			: base (name, propertyType)
		{
			this.attributes = (ushort) attributes;
			this.token = new MetadataToken (TokenType.Property);
		}

		void InitializeMethods ()
		{
			var module = this.Module;
			lock (module.SyncRoot) {
				if (get_method != null || set_method != null)
					return;

				if (!module.HasImage ())
					return;

				module.Read (this, (property, reader) => reader.ReadMethods (property));
			}
		}

		public override PropertyDefinition Resolve ()
		{
			return this;
		}
	}
}
