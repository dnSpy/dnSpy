//
// TypeParser.cs
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
using System.Text;

using Mono.Cecil.Metadata;

namespace Mono.Cecil {

	class TypeParser {

		class Type {
			public const int Ptr = -1;
			public const int ByRef = -2;
			public const int SzArray = -3;

			public string type_fullname;
			public string [] nested_names;
			public int arity;
			public int [] specs;
			public Type [] generic_arguments;
			public string assembly;
		}

		readonly string fullname;
		readonly int length;

		int position;

		TypeParser (string fullname)
		{
			this.fullname = fullname;
			this.length = fullname.Length;
		}

		Type ParseType (bool fq_name)
		{
			var type = new Type ();
			type.type_fullname = ParsePart ();

			type.nested_names = ParseNestedNames ();

			if (TryGetArity (type))
				type.generic_arguments = ParseGenericArguments (type.arity);

			type.specs = ParseSpecs ();

			if (fq_name)
				type.assembly = ParseAssemblyName ();

			return type;
		}

		static bool TryGetArity (Type type)
		{
			int arity = 0;

			TryAddArity (type.type_fullname, ref arity);

			var nested_names = type.nested_names;
			if (!nested_names.IsNullOrEmpty ()) {
				for (int i = 0; i < nested_names.Length; i++)
					TryAddArity (nested_names [i], ref arity);
			}

			type.arity = arity;
			return arity > 0;
		}

		static bool TryGetArity (string name, out int arity)
		{
			arity = 0;
			var index = name.LastIndexOf ('`');
			if (index == -1)
				return false;

			return ParseInt32 (name.Substring (index + 1), out arity);
		}

		static bool ParseInt32 (string value, out int result)
		{
#if CF
			try {
				result = int.Parse (value);
				return true;
			} catch {
				result = 0;
				return false;
			}
#else
			return int.TryParse (value, out result);
#endif
		}

		static void TryAddArity (string name, ref int arity)
		{
			int type_arity;
			if (!TryGetArity (name, out type_arity))
				return;

			arity += type_arity;
		}

		string ParsePart ()
		{
			int start = position;
			while (position < length && !IsDelimiter (fullname [position]))
				position++;

			return fullname.Substring (start, position - start);
		}

		static bool IsDelimiter (char chr)
		{
			return "+,[]*&".IndexOf (chr) != -1;
		}

		void TryParseWhiteSpace ()
		{
			while (position < length && Char.IsWhiteSpace (fullname [position]))
				position++;
		}

		string [] ParseNestedNames ()
		{
			string [] nested_names = null;
			while (TryParse ('+'))
				Add (ref nested_names, ParsePart ());

			return nested_names;
		}

		bool TryParse (char chr)
		{
			if (position < length && fullname [position] == chr) {
				position++;
				return true;
			}

			return false;
		}

		static void Add<T> (ref T [] array, T item)
		{
			if (array == null) {
				array = new [] { item };
				return;
			}

#if !CF
			Array.Resize (ref array, array.Length + 1);
#else
			var copy = new T [array.Length + 1];
			Array.Copy (array, copy, array.Length);
			array = copy;
#endif
			array [array.Length - 1] = item;
		}

		int [] ParseSpecs ()
		{
			int [] specs = null;

			while (position < length) {
				switch (fullname [position]) {
				case '*':
					position++;
					Add (ref specs, Type.Ptr);
					break;
				case '&':
					position++;
					Add (ref specs, Type.ByRef);
					break;
				case '[':
					position++;
					switch (fullname [position]) {
					case ']':
						position++;
						Add (ref specs, Type.SzArray);
						break;
					case '*':
						position++;
						Add (ref specs, 1);
						break;
					default:
						var rank = 1;
						while (TryParse (','))
							rank++;

						Add (ref specs, rank);

						TryParse (']');
						break;
					}
					break;
				default:
					return specs;
				}
			}

			return specs;
		}

		Type [] ParseGenericArguments (int arity)
		{
			Type [] generic_arguments = null;

			if (position == length || fullname [position] != '[')
				return generic_arguments;

			TryParse ('[');

			for (int i = 0; i < arity; i++) {
				var fq_argument = TryParse ('[');
				Add (ref generic_arguments, ParseType (fq_argument));
				if (fq_argument)
					TryParse (']');

				TryParse (',');
				TryParseWhiteSpace ();
			}

			TryParse (']');

			return generic_arguments;
		}

		string ParseAssemblyName ()
		{
			if (!TryParse (','))
				return string.Empty;

			TryParseWhiteSpace ();

			var start = position;
			while (position < length) {
				var chr = fullname [position];
				if (chr == '[' || chr == ']')
					break;

				position++;
			}

			return fullname.Substring (start, position - start);
		}

		public static TypeReference ParseType (ModuleDefinition module, string fullname)
		{
			if (fullname == null)
				return null;

			var parser = new TypeParser (fullname);
			return GetTypeReference (module, parser.ParseType (true));
		}

		static TypeReference GetTypeReference (ModuleDefinition module, Type type_info)
		{
			TypeReference type;
			if (!TryGetDefinition (module, type_info, out type))
				type = CreateReference (type_info, module, GetMetadataScope (module, type_info));

			return CreateSpecs (type, type_info);
		}

		static TypeReference CreateSpecs (TypeReference type, Type type_info)
		{
			type = TryCreateGenericInstanceType (type, type_info);

			var specs = type_info.specs;
			if (specs.IsNullOrEmpty ())
				return type;

			for (int i = 0; i < specs.Length; i++) {
				switch (specs [i]) {
				case Type.Ptr:
					type = new PointerType (type);
					break;
				case Type.ByRef:
					type = new ByReferenceType (type);
					break;
				case Type.SzArray:
					type = new ArrayType (type);
					break;
				default:
					var array = new ArrayType (type);
					array.Dimensions.Clear ();

					for (int j = 0; j < specs [i]; j++)
						array.Dimensions.Add (new ArrayDimension ());

					type = array;
					break;
				}
			}

			return type;
		}

		static TypeReference TryCreateGenericInstanceType (TypeReference type, Type type_info)
		{
			var generic_arguments = type_info.generic_arguments;
			if (generic_arguments.IsNullOrEmpty ())
				return type;

			var instance = new GenericInstanceType (type);
			var instance_arguments = instance.GenericArguments;

			for (int i = 0; i < generic_arguments.Length; i++)
				instance_arguments.Add (GetTypeReference (type.Module, generic_arguments [i]));

			return instance;
		}

		public static void SplitFullName (string fullname, out string @namespace, out string name)
		{
			var last_dot = fullname.LastIndexOf ('.');

			if (last_dot == -1) {
				@namespace = string.Empty;
				name = fullname;
			} else {
				@namespace = fullname.Substring (0, last_dot);
				name = fullname.Substring (last_dot + 1);
			}
		}

		static TypeReference CreateReference (Type type_info, ModuleDefinition module, IMetadataScope scope)
		{
			string @namespace, name;
			SplitFullName (type_info.type_fullname, out @namespace, out name);

			var type = new TypeReference (@namespace, name, module, scope);
			MetadataSystem.TryProcessPrimitiveType (type);

			AdjustGenericParameters (type);

			var nested_names = type_info.nested_names;
			if (nested_names.IsNullOrEmpty ())
				return type;

			for (int i = 0; i < nested_names.Length; i++) {
				type = new TypeReference (string.Empty, nested_names [i], module, null) {
					DeclaringType = type,
				};

				AdjustGenericParameters (type);
			}

			return type;
		}

		static void AdjustGenericParameters (TypeReference type)
		{
			int arity;
			if (!TryGetArity (type.Name, out arity))
				return;

			for (int i = 0; i < arity; i++)
				type.GenericParameters.Add (new GenericParameter (type));
		}

		static IMetadataScope GetMetadataScope (ModuleDefinition module, Type type_info)
		{
			if (string.IsNullOrEmpty (type_info.assembly))
				return module.TypeSystem.Corlib;

			return MatchReference (module, AssemblyNameReference.Parse (type_info.assembly));
		}

		static AssemblyNameReference MatchReference (ModuleDefinition module, AssemblyNameReference pattern)
		{
			var references = module.AssemblyReferences;

			for (int i = 0; i < references.Count; i++) {
				var reference = references [i];
				if (reference.FullName == pattern.FullName)
					return reference;
			}

			return pattern;
		}

		static bool TryGetDefinition (ModuleDefinition module, Type type_info, out TypeReference type)
		{
			type = null;
			if (!TryCurrentModule (module, type_info))
				return false;

			var typedef = module.GetType (type_info.type_fullname);
			if (typedef == null)
				return false;

			var nested_names = type_info.nested_names;
			if (!nested_names.IsNullOrEmpty ()) {
				for (int i = 0; i < nested_names.Length; i++)
					typedef = typedef.GetNestedType (nested_names [i]);
			}

			type = typedef;
			return true;
		}

		static bool TryCurrentModule (ModuleDefinition module, Type type_info)
		{
			if (string.IsNullOrEmpty (type_info.assembly))
				return true;

			if (module.assembly != null && module.assembly.Name.FullName == type_info.assembly)
				return true;

			return false;
		}

		public static string ToParseable (TypeReference type)
		{
			if (type == null)
				return null;

			var name = new StringBuilder ();
			AppendType (type, name, true, true);
			return name.ToString ();
		}

		static void AppendType (TypeReference type, StringBuilder name, bool fq_name, bool top_level)
		{
			var declaring_type = type.DeclaringType;
			if (declaring_type != null) {
				AppendType (declaring_type, name, false, top_level);
				name.Append ('+');
			}

			var @namespace = type.Namespace;
			if (!string.IsNullOrEmpty (@namespace)) {
				name.Append (@namespace);
				name.Append ('.');
			}

			name.Append (type.GetElementType ().Name);

			if (!fq_name)
				return;

			if (type.IsTypeSpecification ())
				AppendTypeSpecification ((TypeSpecification) type, name);

			if (RequiresFullyQualifiedName (type, top_level)) {
				name.Append (", ");
				name.Append (GetScopeFullName (type));
			}
		}

		static string GetScopeFullName (TypeReference type)
		{
			var scope = type.Scope;
			switch (scope.MetadataScopeType) {
			case MetadataScopeType.AssemblyNameReference:
				return ((AssemblyNameReference) scope).FullName;
			case MetadataScopeType.ModuleDefinition:
				return ((ModuleDefinition) scope).Assembly.Name.FullName;
			}

			throw new ArgumentException ();
		}

		static void AppendTypeSpecification (TypeSpecification type, StringBuilder name)
		{
			if (type.ElementType.IsTypeSpecification ())
				AppendTypeSpecification ((TypeSpecification) type.ElementType, name);

			switch (type.etype) {
			case ElementType.Ptr:
				name.Append ('*');
				break;
			case ElementType.ByRef:
				name.Append ('&');
				break;
			case ElementType.SzArray:
			case ElementType.Array:
				var array = (ArrayType) type;
				if (array.IsVector) {
					name.Append ("[]");
				} else {
					name.Append ('[');
					for (int i = 1; i < array.Rank; i++)
						name.Append (',');
					name.Append (']');
				}
				break;
			case ElementType.GenericInst:
				var instance = (GenericInstanceType) type;
				var arguments = instance.GenericArguments;

				name.Append ('[');

				for (int i = 0; i < arguments.Count; i++) {
					if (i > 0)
						name.Append (',');

					var argument = arguments [i];
					var requires_fqname = argument.Scope != argument.Module;

					if (requires_fqname)
						name.Append ('[');

					AppendType (argument, name, true, false);

					if (requires_fqname)
						name.Append (']');
				}

				name.Append (']');
				break;
			default:
				return;
			}
		}

		static bool RequiresFullyQualifiedName (TypeReference type, bool top_level)
		{
			if (type.Scope == type.Module)
				return false;

			if (type.Scope.Name == "mscorlib" && top_level)
				return false;

			return true;
		}
	}
}
