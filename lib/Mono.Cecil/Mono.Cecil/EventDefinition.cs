//
// EventDefinition.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2010 Jb Evain
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

	public sealed class EventDefinition : EventReference, IMemberDefinition {

		ushort attributes;

		Collection<CustomAttribute> custom_attributes;

		internal MethodDefinition add_method;
		internal MethodDefinition invoke_method;
		internal MethodDefinition remove_method;
		internal Collection<MethodDefinition> other_methods;

		public EventAttributes Attributes {
			get { return (EventAttributes) attributes; }
			set { attributes = (ushort) value; }
		}

		public MethodDefinition AddMethod {
			get {
				if (add_method != null)
					return add_method;

				InitializeMethods ();
				return add_method;
			}
			set { add_method = value; }
		}

		public MethodDefinition InvokeMethod {
			get {
				if (invoke_method != null)
					return invoke_method;

				InitializeMethods ();
				return invoke_method;
			}
			set { invoke_method = value; }
		}

		public MethodDefinition RemoveMethod {
			get {
				if (remove_method != null)
					return remove_method;

				InitializeMethods ();
				return remove_method;
			}
			set { remove_method = value; }
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

		public bool HasCustomAttributes {
			get {
				if (custom_attributes != null)
					return custom_attributes.Count > 0;

				return this.GetHasCustomAttributes (Module);
			}
		}

		public Collection<CustomAttribute> CustomAttributes {
			get { return custom_attributes ?? (custom_attributes = this.GetCustomAttributes (Module)); }
		}

		#region EventAttributes

		public bool IsSpecialName {
			get { return attributes.GetAttributes ((ushort) EventAttributes.SpecialName); }
			set { attributes = attributes.SetAttributes ((ushort) EventAttributes.SpecialName, value); }
		}

		public bool IsRuntimeSpecialName {
			get { return attributes.GetAttributes ((ushort) FieldAttributes.RTSpecialName); }
			set { attributes = attributes.SetAttributes ((ushort) FieldAttributes.RTSpecialName, value); }
		}

		#endregion

		public new TypeDefinition DeclaringType {
			get { return (TypeDefinition) base.DeclaringType; }
			set { base.DeclaringType = value; }
		}

		public override bool IsDefinition {
			get { return true; }
		}

		public EventDefinition (string name, EventAttributes attributes, TypeReference eventType)
			: base (name, eventType)
		{
			this.attributes = (ushort) attributes;
			this.token = new MetadataToken (TokenType.Event);
		}

		void InitializeMethods ()
		{
			if (add_method != null
				|| invoke_method != null
				|| remove_method != null)
				return;

			var module = this.Module;
			if (!module.HasImage ())
				return;

			module.Read (this, (@event, reader) => reader.ReadMethods (@event));
		}
	}
}
