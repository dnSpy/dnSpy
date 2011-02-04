//
// CustomAttribute.cs
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

using System;

using Mono.Collections.Generic;

namespace Mono.Cecil {

	public struct CustomAttributeArgument {

		readonly TypeReference type;
		readonly object value;

		public TypeReference Type {
			get { return type; }
		}

		public object Value {
			get { return value; }
		}

		public CustomAttributeArgument (TypeReference type, object value)
		{
			Mixin.CheckType (type);
			this.type = type;
			this.value = value;
		}
	}

	public struct CustomAttributeNamedArgument {

		readonly string name;
		readonly CustomAttributeArgument argument;

		public string Name {
			get { return name; }
		}

		public CustomAttributeArgument Argument {
			get { return argument; }
		}

		public CustomAttributeNamedArgument (string name, CustomAttributeArgument argument)
		{
			Mixin.CheckName (name);
			this.name = name;
			this.argument = argument;
		}
	}

	public interface ICustomAttribute {

		TypeReference AttributeType { get; }

		bool HasFields { get; }
		bool HasProperties { get; }
		Collection<CustomAttributeNamedArgument> Fields { get; }
		Collection<CustomAttributeNamedArgument> Properties { get; }
	}

	public sealed class CustomAttribute : ICustomAttribute {

		readonly internal uint signature;
		internal bool resolved;
		MethodReference constructor;
		byte [] blob;
		internal Collection<CustomAttributeArgument> arguments;
		internal Collection<CustomAttributeNamedArgument> fields;
		internal Collection<CustomAttributeNamedArgument> properties;

		public MethodReference Constructor {
			get { return constructor; }
			set { constructor = value; }
		}

		public TypeReference AttributeType {
			get { return constructor.DeclaringType; }
		}

		public bool IsResolved {
			get { return resolved; }
		}

		public bool HasConstructorArguments {
			get {
				Resolve ();

				return !arguments.IsNullOrEmpty ();
			}
		}

		public Collection<CustomAttributeArgument> ConstructorArguments {
			get {
				Resolve ();

				return arguments ?? (arguments = new Collection<CustomAttributeArgument> ());
			}
		}

		public bool HasFields {
			get {
				Resolve ();

				return !fields.IsNullOrEmpty ();
			}
		}

		public Collection<CustomAttributeNamedArgument> Fields {
			get {
				Resolve ();

				return fields ?? (fields = new Collection<CustomAttributeNamedArgument> ());
			}
		}

		public bool HasProperties {
			get {
				Resolve ();

				return !properties.IsNullOrEmpty ();
			}
		}

		public Collection<CustomAttributeNamedArgument> Properties {
			get {
				Resolve ();

				return properties ?? (properties = new Collection<CustomAttributeNamedArgument> ());
			}
		}

		internal bool HasImage {
			get { return constructor != null && constructor.HasImage; }
		}

		internal ModuleDefinition Module {
			get { return constructor.Module; }
		}

		internal CustomAttribute (uint signature, MethodReference constructor)
		{
			this.signature = signature;
			this.constructor = constructor;
			this.resolved = false;
		}

		public CustomAttribute (MethodReference constructor)
		{
			this.constructor = constructor;
			this.resolved = true;
		}

		public CustomAttribute (MethodReference constructor, byte [] blob)
		{
			this.constructor = constructor;
			this.resolved = false;
			this.blob = blob;
		}

		public byte [] GetBlob ()
		{
			if (blob != null)
				return blob;

			if (!HasImage || signature == 0)
				throw new NotSupportedException ();

			return blob = Module.Read (this, (attribute, reader) => reader.ReadCustomAttributeBlob (attribute.signature));
		}

		void Resolve ()
		{
			if (resolved || !HasImage)
				return;

			try {
				Module.Read (this, (attribute, reader) => {
					reader.ReadCustomAttributeSignature (attribute);
					return this;
				});

				resolved = true;
			} catch (ResolutionException) {
				if (arguments != null)
					arguments.Clear ();
				if (fields != null)
					fields.Clear ();
				if (properties != null)
					properties.Clear ();

				resolved = false;
			}
		}
	}

	static partial class Mixin {

		public static void CheckName (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			if (name.Length == 0)
				throw new ArgumentException ("Empty name");
		}
	}
}
