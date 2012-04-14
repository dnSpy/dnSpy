//
// import.cs: System.Reflection conversions
//
// Authors: Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2009-2011 Novell, Inc
// Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
//

using System;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections.Generic;

#if STATIC
using MetaType = IKVM.Reflection.Type;
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using MetaType = System.Type;
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Mono.CSharp
{
	public abstract class MetadataImporter
	{
		//
		// Dynamic types reader with additional logic to reconstruct a dynamic
		// type using DynamicAttribute values
		//
		struct DynamicTypeReader
		{
			static readonly bool[] single_attribute = { true };

			public int Position;
			bool[] flags;

			// There is no common type for CustomAttributeData and we cannot
			// use ICustomAttributeProvider
			object provider;

			//
			// A member provider which can be used to get CustomAttributeData
			//
			public DynamicTypeReader (object provider)
			{
				Position = 0;
				flags = null;
				this.provider = provider;
			}

			//
			// Returns true when object at local position has dynamic attribute flag
			//
			public bool IsDynamicObject (MetadataImporter importer)
			{
				if (provider != null)
					ReadAttribute (importer);

				return flags != null && Position < flags.Length && flags[Position];
			}

			//
			// Returns true when DynamicAttribute exists
			//
			public bool HasDynamicAttribute (MetadataImporter importer)
			{
				if (provider != null)
					ReadAttribute (importer);

				return flags != null;
			}

			void ReadAttribute (MetadataImporter importer)
			{
				IList<CustomAttributeData> cad;
				if (provider is MemberInfo) {
					cad = CustomAttributeData.GetCustomAttributes ((MemberInfo) provider);
				} else if (provider is ParameterInfo) {
					cad = CustomAttributeData.GetCustomAttributes ((ParameterInfo) provider);
				} else {
					provider = null;
					return;
				}

				if (cad.Count > 0) {
					foreach (var ca in cad) {
						var dt = ca.Constructor.DeclaringType;
						if (dt.Name != "DynamicAttribute" && dt.Namespace != CompilerServicesNamespace)
							continue;

						if (ca.ConstructorArguments.Count == 0) {
							flags = single_attribute;
							break;
						}

						var arg_type = ca.ConstructorArguments[0].ArgumentType;

						if (arg_type.IsArray && MetaType.GetTypeCode (arg_type.GetElementType ()) == TypeCode.Boolean) {
							var carg = (IList<CustomAttributeTypedArgument>) ca.ConstructorArguments[0].Value;
							flags = new bool[carg.Count];
							for (int i = 0; i < flags.Length; ++i) {
								if (MetaType.GetTypeCode (carg[i].ArgumentType) == TypeCode.Boolean)
									flags[i] = (bool) carg[i].Value;
							}

							break;
						}
					}
				}

				provider = null;
			}
		}

		protected readonly Dictionary<MetaType, TypeSpec> import_cache;
		protected readonly Dictionary<MetaType, TypeSpec> compiled_types;
		protected readonly Dictionary<Assembly, IAssemblyDefinition> assembly_2_definition;
		readonly ModuleContainer module;

		public static readonly string CompilerServicesNamespace = "System.Runtime.CompilerServices";

		protected MetadataImporter (ModuleContainer module)
		{
			this.module = module;

			import_cache = new Dictionary<MetaType, TypeSpec> (1024, ReferenceEquality<MetaType>.Default);
			compiled_types = new Dictionary<MetaType, TypeSpec> (40, ReferenceEquality<MetaType>.Default);
			assembly_2_definition = new Dictionary<Assembly, IAssemblyDefinition> (ReferenceEquality<Assembly>.Default);
			IgnorePrivateMembers = true;
		}

		#region Properties

		public ICollection<IAssemblyDefinition> Assemblies {
			get {
				return assembly_2_definition.Values;
			}
		}

		public bool IgnorePrivateMembers { get; set; }

		#endregion

		public abstract void AddCompiledType (TypeBuilder builder, TypeSpec spec);
		protected abstract MemberKind DetermineKindFromBaseType (MetaType baseType);
		protected abstract bool HasVolatileModifier (MetaType[] modifiers);

		public FieldSpec CreateField (FieldInfo fi, TypeSpec declaringType)
		{
			Modifiers mod = 0;
			var fa = fi.Attributes;
			switch (fa & FieldAttributes.FieldAccessMask) {
				case FieldAttributes.Public:
					mod = Modifiers.PUBLIC;
					break;
				case FieldAttributes.Assembly:
					mod = Modifiers.INTERNAL;
					break;
				case FieldAttributes.Family:
					mod = Modifiers.PROTECTED;
					break;
				case FieldAttributes.FamORAssem:
					mod = Modifiers.PROTECTED | Modifiers.INTERNAL;
					break;
				default:
					// Ignore private fields (even for error reporting) to not require extra dependencies
					if ((IgnorePrivateMembers && !declaringType.IsStruct) ||
						HasAttribute (CustomAttributeData.GetCustomAttributes (fi), "CompilerGeneratedAttribute", CompilerServicesNamespace))
						return null;

					mod = Modifiers.PRIVATE;
					break;
			}

			TypeSpec field_type;

			try {
				field_type = ImportType (fi.FieldType, new DynamicTypeReader (fi));
			} catch (Exception e) {
				// TODO: I should construct fake TypeSpec based on TypeRef signature
				// but there is no way to do it with System.Reflection
				throw new InternalErrorException (e, "Cannot import field `{0}.{1}' referenced in assembly `{2}'",
					declaringType.GetSignatureForError (), fi.Name, declaringType.MemberDefinition.DeclaringAssembly);
			}

			var definition = new ImportedMemberDefinition (fi, field_type, this);

			if ((fa & FieldAttributes.Literal) != 0) {
				var c = Constant.CreateConstantFromValue (field_type, fi.GetRawConstantValue (), Location.Null);
				return new ConstSpec (declaringType, definition, field_type, fi, mod, c);
			}

			if ((fa & FieldAttributes.InitOnly) != 0) {
				if (field_type.BuiltinType == BuiltinTypeSpec.Type.Decimal) {
					var dc = ReadDecimalConstant (CustomAttributeData.GetCustomAttributes (fi));
					if (dc != null)
						return new ConstSpec (declaringType, definition, field_type, fi, mod, dc);
				}

				mod |= Modifiers.READONLY;
			} else {
				var req_mod = fi.GetRequiredCustomModifiers ();
				if (req_mod.Length > 0 && HasVolatileModifier (req_mod))
					mod |= Modifiers.VOLATILE;
			}

			if ((fa & FieldAttributes.Static) != 0) {
				mod |= Modifiers.STATIC;
			} else {
				// Fixed buffers cannot be static
				if (declaringType.IsStruct && field_type.IsStruct && field_type.IsNested &&
					HasAttribute (CustomAttributeData.GetCustomAttributes (fi), "FixedBufferAttribute", CompilerServicesNamespace)) {

					// TODO: Sanity check on field_type (only few types are allowed)
					var element_field = CreateField (fi.FieldType.GetField (FixedField.FixedElementName), declaringType);
					return new FixedFieldSpec (declaringType, definition, fi, element_field, mod);
				}
			}

			return new FieldSpec (declaringType, definition, field_type, fi, mod);
		}

		public EventSpec CreateEvent (EventInfo ei, TypeSpec declaringType, MethodSpec add, MethodSpec remove)
		{
			add.IsAccessor = true;
			remove.IsAccessor = true;

			if (add.Modifiers != remove.Modifiers)
				throw new NotImplementedException ("Different accessor modifiers " + ei.Name);

			var event_type = ImportType (ei.EventHandlerType, new DynamicTypeReader (ei));
			var definition = new ImportedMemberDefinition (ei, event_type,  this);
			return new EventSpec (declaringType, definition, event_type, add.Modifiers, add, remove);
		}

		TypeParameterSpec[] CreateGenericParameters (MetaType type, TypeSpec declaringType)
		{
			var tparams = type.GetGenericArguments ();

			int parent_owned_count;
			if (type.IsNested) {
				parent_owned_count = type.DeclaringType.GetGenericArguments ().Length;

				//
				// System.Reflection duplicates parent type parameters for each
				// nested type with slightly modified properties (eg. different owner)
				// This just makes things more complicated (think of cloned constraints)
				// therefore we remap any nested type owned by parent using `type_cache'
				// to the single TypeParameterSpec
				//
				if (declaringType != null && parent_owned_count > 0) {
					int read_count = 0;
					while (read_count != parent_owned_count) {
						var tparams_count = declaringType.Arity;
						if (tparams_count != 0) {
							var parent_tp = declaringType.MemberDefinition.TypeParameters;
							read_count += tparams_count;
							for (int i = 0; i < tparams_count; i++) {
								import_cache.Add (tparams[parent_owned_count - read_count + i], parent_tp[i]);
							}
						}

						declaringType = declaringType.DeclaringType;
					}
				}			
			} else {
				parent_owned_count = 0;
			}

			if (tparams.Length - parent_owned_count == 0)
				return null;

			return CreateGenericParameters (parent_owned_count, tparams);
		}

		TypeParameterSpec[] CreateGenericParameters (int first, MetaType[] tparams)
		{
			var tspec = new TypeParameterSpec[tparams.Length - first];
			for (int pos = first; pos < tparams.Length; ++pos) {
				var type = tparams[pos];
				int index = pos - first;

				tspec[index] = (TypeParameterSpec) CreateType (type, new DynamicTypeReader (), false);
			}

			return tspec;
		}

		TypeSpec[] CreateGenericArguments (int first, MetaType[] tparams, DynamicTypeReader dtype)
		{
			++dtype.Position;

			var tspec = new TypeSpec [tparams.Length - first];
			for (int pos = first; pos < tparams.Length; ++pos) {
				var type = tparams[pos];
				int index = pos - first;

				TypeSpec spec;
				if (type.HasElementType) {
					var element = type.GetElementType ();
					++dtype.Position;
					spec = ImportType (element, dtype);

					if (!type.IsArray) {
						throw new NotImplementedException ("Unknown element type " + type.ToString ());
					}

					spec = ArrayContainer.MakeType (module, spec, type.GetArrayRank ());
				} else {
					spec = CreateType (type, dtype, true);

					//
					// We treat nested generic types as inflated internally where
					// reflection uses type definition
					//
					// class A<T> {
					//    IFoo<A<T>> foo;	// A<T> is definition in this case
					// }
					//
					// TODO: Is full logic from CreateType needed here as well?
					//
					if (!IsMissingType (type) && type.IsGenericTypeDefinition) {
						var targs = CreateGenericArguments (0, type.GetGenericArguments (), dtype);
						spec = spec.MakeGenericType (module, targs);
					}
				}

				++dtype.Position;
				tspec[index] = spec;
			}

			return tspec;
		}

		public MethodSpec CreateMethod (MethodBase mb, TypeSpec declaringType)
		{
			Modifiers mod = ReadMethodModifiers (mb, declaringType);
			TypeParameterSpec[] tparams;

			var parameters = CreateParameters (declaringType, mb.GetParameters (), mb);

			if (mb.IsGenericMethod) {
				if (!mb.IsGenericMethodDefinition)
					throw new NotSupportedException ("assert");

				tparams = CreateGenericParameters (0, mb.GetGenericArguments ());
			} else {
				tparams = null;
			}

			MemberKind kind;
			TypeSpec returnType;
			if (mb.MemberType == MemberTypes.Constructor) {
				kind = MemberKind.Constructor;
				returnType = module.Compiler.BuiltinTypes.Void;
			} else {
				//
				// Detect operators and destructors
				//
				string name = mb.Name;
				kind = MemberKind.Method;
				if (tparams == null && !mb.DeclaringType.IsInterface && name.Length > 6) {
					if ((mod & (Modifiers.STATIC | Modifiers.PUBLIC)) == (Modifiers.STATIC | Modifiers.PUBLIC)) {
						if (name[2] == '_' && name[1] == 'p' && name[0] == 'o') {
							var op_type = Operator.GetType (name);
							if (op_type.HasValue && parameters.Count > 0 && parameters.Count < 3) {
								kind = MemberKind.Operator;
							}
						}
					} else if (parameters.IsEmpty && name == Destructor.MetadataName) {
						kind = MemberKind.Destructor;
						if (declaringType.BuiltinType == BuiltinTypeSpec.Type.Object) {
							mod &= ~Modifiers.OVERRIDE;
							mod |= Modifiers.VIRTUAL;
						}
					}
				}

				var mi = (MethodInfo) mb;
				returnType = ImportType (mi.ReturnType, new DynamicTypeReader (mi.ReturnParameter));

				// Cannot set to OVERRIDE without full hierarchy checks
				// this flag indicates that the method could be override
				// but further validation is needed
				if ((mod & Modifiers.OVERRIDE) != 0) {
					bool is_real_override = false;
					if (kind == MemberKind.Method && declaringType.BaseType != null) {
						var filter = MemberFilter.Method (name, tparams != null ? tparams.Length : 0, parameters, null);
						var candidate = MemberCache.FindMember (declaringType.BaseType, filter, BindingRestriction.None);

						//
						// For imported class method do additional validation to be sure that metadata
						// override flag was correct
						// 
						// Difference between protected internal and protected is ok
						//
						const Modifiers conflict_mask = Modifiers.AccessibilityMask & ~Modifiers.INTERNAL;
						if (candidate != null && (candidate.Modifiers & conflict_mask) == (mod & conflict_mask) && !candidate.IsStatic) {
							is_real_override = true;
						}
					}

					if (!is_real_override) {
						mod &= ~Modifiers.OVERRIDE;
						if ((mod & Modifiers.SEALED) != 0)
							mod &= ~Modifiers.SEALED;
						else
							mod |= Modifiers.VIRTUAL;
					}
				}
			}

			IMemberDefinition definition;
			if (tparams != null) {
				var gmd = new ImportedGenericMethodDefinition ((MethodInfo) mb, returnType, parameters, tparams, this);
				foreach (var tp in gmd.TypeParameters) {
					ImportTypeParameterTypeConstraints (tp, tp.GetMetaInfo ());
				}

				definition = gmd;
			} else {
				definition = new ImportedParameterMemberDefinition (mb, returnType, parameters, this);
			}

			MethodSpec ms = new MethodSpec (kind, declaringType, definition, returnType, mb, parameters, mod);
			if (tparams != null)
				ms.IsGeneric = true;

			return ms;
		}

		//
		// Imports System.Reflection parameters
		//
		AParametersCollection CreateParameters (TypeSpec parent, ParameterInfo[] pi, MethodBase method)
		{
			int varargs = method != null && (method.CallingConvention & CallingConventions.VarArgs) != 0 ? 1 : 0;

			if (pi.Length == 0 && varargs == 0)
				return ParametersCompiled.EmptyReadOnlyParameters;

			TypeSpec[] types = new TypeSpec[pi.Length + varargs];
			IParameterData[] par = new IParameterData[pi.Length + varargs];
			bool is_params = false;
			for (int i = 0; i < pi.Length; i++) {
				ParameterInfo p = pi[i];
				Parameter.Modifier mod = 0;
				Expression default_value = null;
				if (p.ParameterType.IsByRef) {
					if ((p.Attributes & (ParameterAttributes.Out | ParameterAttributes.In)) == ParameterAttributes.Out)
						mod = Parameter.Modifier.OUT;
					else
						mod = Parameter.Modifier.REF;

					//
					// Strip reference wrapping
					//
					var el = p.ParameterType.GetElementType ();
					types[i] = ImportType (el, new DynamicTypeReader (p));	// TODO: 1-based positio to be csc compatible
				} else if (i == 0 && method.IsStatic && parent.IsStatic && parent.MemberDefinition.DeclaringAssembly.HasExtensionMethod &&
					HasAttribute (CustomAttributeData.GetCustomAttributes (method), "ExtensionAttribute", CompilerServicesNamespace)) {
					mod = Parameter.Modifier.This;
					types[i] = ImportType (p.ParameterType);
				} else {
					types[i] = ImportType (p.ParameterType, new DynamicTypeReader (p));

					if (i >= pi.Length - 2 && types[i] is ArrayContainer) {
						if (HasAttribute (CustomAttributeData.GetCustomAttributes (p), "ParamArrayAttribute", "System")) {
							mod = Parameter.Modifier.PARAMS;
							is_params = true;
						}
					}

					if (!is_params && p.IsOptional) {
						object value = p.RawDefaultValue;
						var ptype = types[i];
						if ((p.Attributes & ParameterAttributes.HasDefault) != 0 && ptype.Kind != MemberKind.TypeParameter && (value != null || TypeSpec.IsReferenceType (ptype))) {
							if (value == null) {
								default_value = Constant.CreateConstant (ptype, null, Location.Null);
							} else {
								default_value = ImportParameterConstant (value);

								if (ptype.IsEnum) {
									default_value = new EnumConstant ((Constant) default_value, ptype);
								}
							}
						} else if (value == Missing.Value) {
							default_value = EmptyExpression.MissingValue;
						} else if (value == null) {
							default_value = new DefaultValueExpression (new TypeExpression (ptype, Location.Null), Location.Null);
						} else if (ptype.BuiltinType == BuiltinTypeSpec.Type.Decimal) {
							default_value = ImportParameterConstant (value);
						}
					}
				}

				par[i] = new ParameterData (p.Name, mod, default_value);
			}

			if (varargs != 0) {
				par[par.Length - 1] = new ArglistParameter (Location.Null);
				types[types.Length - 1] = InternalType.Arglist;
			}

			return method != null ?
				new ParametersImported (par, types, varargs != 0, is_params) :
				new ParametersImported (par, types, is_params);
		}

		//
		// Returns null when the property is not valid C# property
		//
		public PropertySpec CreateProperty (PropertyInfo pi, TypeSpec declaringType, MethodSpec get, MethodSpec set)
		{
			Modifiers mod = 0;
			AParametersCollection param = null;
			TypeSpec type = null;
			if (get != null) {
				mod = get.Modifiers;
				param = get.Parameters;
				type = get.ReturnType;
			}

			bool is_valid_property = true;
			if (set != null) {
				if (set.ReturnType.Kind != MemberKind.Void)
					is_valid_property = false;

				var set_param_count = set.Parameters.Count - 1;

				if (set_param_count < 0) {
					set_param_count = 0;
					is_valid_property = false;
				}

				var set_type = set.Parameters.Types[set_param_count];

				if (mod == 0) {
					AParametersCollection set_based_param;

					if (set_param_count == 0) {
						set_based_param = ParametersCompiled.EmptyReadOnlyParameters;
					} else {
						//
						// Create indexer parameters based on setter method parameters (the last parameter has to be removed)
						//
						var data = new IParameterData[set_param_count];
						var types = new TypeSpec[set_param_count];
						Array.Copy (set.Parameters.FixedParameters, data, set_param_count);
						Array.Copy (set.Parameters.Types, types, set_param_count);
						set_based_param = new ParametersImported (data, types, set.Parameters.HasParams);
					}

					mod = set.Modifiers;
					param = set_based_param;
					type = set_type;
				} else {
					if (set_param_count != get.Parameters.Count)
						is_valid_property = false;

					if (get.ReturnType != set_type)
						is_valid_property = false;

					// Possible custom accessor modifiers
					if ((mod & Modifiers.AccessibilityMask) != (set.Modifiers & Modifiers.AccessibilityMask)) {
						var get_acc = mod & Modifiers.AccessibilityMask;
						if (get_acc != Modifiers.PUBLIC) {
							var set_acc = set.Modifiers & Modifiers.AccessibilityMask;
							// If the accessor modifiers are not same, do extra restriction checks
							if (get_acc != set_acc) {
								var get_restr = ModifiersExtensions.IsRestrictedModifier (get_acc, set_acc);
								var set_restr = ModifiersExtensions.IsRestrictedModifier (set_acc, get_acc);
								if (get_restr && set_restr) {
									is_valid_property = false; // Neither is more restrictive
								}

								if (get_restr) {
									mod &= ~Modifiers.AccessibilityMask;
									mod |= set_acc;
								}
							}
						}
					}
				}
			}

			PropertySpec spec = null;
			if (!param.IsEmpty) {
				var index_name = declaringType.MemberDefinition.GetAttributeDefaultMember ();
				if (index_name == null) {
					is_valid_property = false;
				} else {
					if (get != null) {
						if (get.IsStatic)
							is_valid_property = false;
						if (get.Name.IndexOf (index_name, StringComparison.Ordinal) != 4)
							is_valid_property = false;
					}
					if (set != null) {
						if (set.IsStatic)
							is_valid_property = false;
						if (set.Name.IndexOf (index_name, StringComparison.Ordinal) != 4)
							is_valid_property = false;
					}
				}

				if (is_valid_property)
					spec = new IndexerSpec (declaringType, new ImportedParameterMemberDefinition (pi, type, param, this), type, param, pi, mod);
			}

			if (spec == null)
				spec = new PropertySpec (MemberKind.Property, declaringType, new ImportedMemberDefinition (pi, type, this), type, pi, mod);

			if (!is_valid_property) {
				spec.IsNotCSharpCompatible = true;
				return spec;
			}

			if (set != null)
				spec.Set = set;
			if (get != null)
				spec.Get = get;

			return spec;
		}

		public TypeSpec CreateType (MetaType type)
		{
			return CreateType (type, new DynamicTypeReader (), true);
		}

		public TypeSpec CreateNestedType (MetaType type, TypeSpec declaringType)
		{
			return CreateType (type, declaringType, new DynamicTypeReader (type), false);
		}

		TypeSpec CreateType (MetaType type, DynamicTypeReader dtype, bool canImportBaseType)
		{
			TypeSpec declaring_type;
			if (type.IsNested && !type.IsGenericParameter)
				declaring_type = CreateType (type.DeclaringType, new DynamicTypeReader (type.DeclaringType), true);
			else
				declaring_type = null;

			return CreateType (type, declaring_type, dtype, canImportBaseType);
		}

		TypeSpec CreateType (MetaType type, TypeSpec declaringType, DynamicTypeReader dtype, bool canImportBaseType)
		{
			TypeSpec spec;
			if (import_cache.TryGetValue (type, out spec)) {
				if (spec.BuiltinType == BuiltinTypeSpec.Type.Object) {
					if (dtype.IsDynamicObject (this))
						return module.Compiler.BuiltinTypes.Dynamic;

					return spec;
				}

				if (!spec.IsGeneric || type.IsGenericTypeDefinition)
					return spec;

				if (!dtype.HasDynamicAttribute (this))
					return spec;

				// We've found same object in the cache but this one has a dynamic custom attribute
				// and it's most likely dynamic version of same type IFoo<object> agains IFoo<dynamic>
				// Do type resolve process again in that case

				// TODO: Handle cases where they still unify
			}

			if (IsMissingType (type)) {
				spec = new TypeSpec (MemberKind.MissingType, declaringType, new ImportedTypeDefinition (type, this), type, Modifiers.PUBLIC);
				spec.MemberCache = MemberCache.Empty;
				import_cache.Add (type, spec);
				return spec;
			}

			if (type.IsGenericType && !type.IsGenericTypeDefinition) {
				var type_def = type.GetGenericTypeDefinition ();

				// Generic type definition can also be forwarded
				if (compiled_types.TryGetValue (type_def, out spec))
					return spec;

				var targs = CreateGenericArguments (0, type.GetGenericArguments (), dtype);
				if (declaringType == null) {
					// Simple case, no nesting
					spec = CreateType (type_def, null, new DynamicTypeReader (), canImportBaseType);
					spec = spec.MakeGenericType (module, targs);
				} else {
					//
					// Nested type case, converting .NET types like
					// A`1.B`1.C`1<int, long, string> to typespec like
					// A<int>.B<long>.C<string>
					//
					var nested_hierarchy = new List<TypeSpec> ();
					while (declaringType.IsNested) {
						nested_hierarchy.Add (declaringType);
						declaringType = declaringType.DeclaringType;
					}

					int targs_pos = 0;
					if (declaringType.Arity > 0) {
						spec = declaringType.MakeGenericType (module, targs.Skip (targs_pos).Take (declaringType.Arity).ToArray ());
						targs_pos = spec.Arity;
					} else {
						spec = declaringType;
					}

					for (int i = nested_hierarchy.Count; i != 0; --i) {
						var t = nested_hierarchy [i - 1];
						spec = MemberCache.FindNestedType (spec, t.Name, t.Arity);
						if (t.Arity > 0) {
							spec = spec.MakeGenericType (module, targs.Skip (targs_pos).Take (spec.Arity).ToArray ());
							targs_pos += t.Arity;
						}
					}

					string name = type.Name;
					int index = name.IndexOf ('`');
					if (index > 0)
						name = name.Substring (0, index);

					spec = MemberCache.FindNestedType (spec, name, targs.Length - targs_pos);
					if (spec == null)
						return null;

					if (spec.Arity > 0) {
						spec = spec.MakeGenericType (module, targs.Skip (targs_pos).ToArray ());
					}
				}

				// Don't add generic type with dynamic arguments, they can interfere with same type
				// using object type arguments
				if (!spec.HasDynamicElement) {

					// Add to reading cache to speed up reading
					if (!import_cache.ContainsKey (type))
						import_cache.Add (type, spec);
				}

				return spec;
			}

			Modifiers mod;
			MemberKind kind;

			var ma = type.Attributes;
			switch (ma & TypeAttributes.VisibilityMask) {
			case TypeAttributes.Public:
			case TypeAttributes.NestedPublic:
				mod = Modifiers.PUBLIC;
				break;
			case TypeAttributes.NestedPrivate:
				mod = Modifiers.PRIVATE;
				break;
			case TypeAttributes.NestedFamily:
				mod = Modifiers.PROTECTED;
				break;
			case TypeAttributes.NestedFamORAssem:
				mod = Modifiers.PROTECTED | Modifiers.INTERNAL;
				break;
			default:
				mod = Modifiers.INTERNAL;
				break;
			}

			if ((ma & TypeAttributes.Interface) != 0) {
				kind = MemberKind.Interface;
			} else if (type.IsGenericParameter) {
				kind = MemberKind.TypeParameter;
			} else {
				var base_type = type.BaseType;
				if (base_type == null || (ma & TypeAttributes.Abstract) != 0) {
					kind = MemberKind.Class;
				} else {
					kind = DetermineKindFromBaseType (base_type);
					if (kind == MemberKind.Struct || kind == MemberKind.Delegate) {
						mod |= Modifiers.SEALED;
					}
				}

				if (kind == MemberKind.Class) {
					if ((ma & TypeAttributes.Sealed) != 0) {
						mod |= Modifiers.SEALED;
						if ((ma & TypeAttributes.Abstract) != 0)
							mod |= Modifiers.STATIC;
					} else if ((ma & TypeAttributes.Abstract) != 0) {
						mod |= Modifiers.ABSTRACT;
					}
				}
			}

			var definition = new ImportedTypeDefinition (type, this);
			TypeSpec pt;

			if (kind == MemberKind.Enum) {
				const BindingFlags underlying_member = BindingFlags.DeclaredOnly |
					BindingFlags.Instance |
					BindingFlags.Public | BindingFlags.NonPublic;

				var type_members = type.GetFields (underlying_member);
				foreach (var type_member in type_members) {
					spec = new EnumSpec (declaringType, definition, CreateType (type_member.FieldType), type, mod);
					break;
				}

				if (spec == null)
					kind = MemberKind.Class;

			} else if (kind == MemberKind.TypeParameter) {
				spec = CreateTypeParameter (type, declaringType);
			} else if (type.IsGenericTypeDefinition) {
				definition.TypeParameters = CreateGenericParameters (type, declaringType);
			} else if (compiled_types.TryGetValue (type, out pt)) {
				//
				// Same type was found in inside compiled types. It's
				// either build-in type or forward referenced typed
				// which point into just compiled assembly.
				//
				spec = pt;
				BuiltinTypeSpec bts = pt as BuiltinTypeSpec;
				if (bts != null)
					bts.SetDefinition (definition, type, mod);
			}

			if (spec == null)
				spec = new TypeSpec (kind, declaringType, definition, type, mod);

			import_cache.Add (type, spec);

			//
			// Two stage setup as the base type can be inflated declaring type or
			// another nested type inside same declaring type which has not been
			// loaded, therefore we can import a base type of nested types once
			// the types have been imported
			//
			if (canImportBaseType)
				ImportTypeBase (spec, type);

			return spec;
		}

		public IAssemblyDefinition GetAssemblyDefinition (Assembly assembly)
		{
			IAssemblyDefinition found;
			if (!assembly_2_definition.TryGetValue (assembly, out found)) {

				// This can happen in dynamic context only
				var def = new ImportedAssemblyDefinition (assembly);
				assembly_2_definition.Add (assembly, def);
				def.ReadAttributes ();
				found = def;
			}

			return found;
		}

		public void ImportTypeBase (MetaType type)
		{
			TypeSpec spec = import_cache[type];
			if (spec != null)
				ImportTypeBase (spec, type);
		}

		TypeParameterSpec CreateTypeParameter (MetaType type, TypeSpec declaringType)
		{
			Variance variance;
			switch (type.GenericParameterAttributes & GenericParameterAttributes.VarianceMask) {
			case GenericParameterAttributes.Covariant:
				variance = Variance.Covariant;
				break;
			case GenericParameterAttributes.Contravariant:
				variance = Variance.Contravariant;
				break;
			default:
				variance = Variance.None;
				break;
			}

			SpecialConstraint special = SpecialConstraint.None;
			var import_special = type.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;

			if ((import_special & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0) {
				special |= SpecialConstraint.Struct;
			} else if ((import_special & GenericParameterAttributes.DefaultConstructorConstraint) != 0) {
				special = SpecialConstraint.Constructor;
			}

			if ((import_special & GenericParameterAttributes.ReferenceTypeConstraint) != 0) {
				special |= SpecialConstraint.Class;
			}

			TypeParameterSpec spec;
			var def = new ImportedTypeParameterDefinition (type, this);
			if (type.DeclaringMethod != null) {
				spec = new TypeParameterSpec (type.GenericParameterPosition, def, special, variance, type);
			} else {
				spec = new TypeParameterSpec (declaringType, type.GenericParameterPosition, def, special, variance, type);
			}

			return spec;
		}

		//
		// Test for a custom attribute type match. Custom attributes are not really predefined globaly 
		// they can be assembly specific therefore we do check based on names only
		//
		public static bool HasAttribute (IList<CustomAttributeData> attributesData, string attrName, string attrNamespace)
		{
			if (attributesData.Count == 0)
				return false;

			foreach (var attr in attributesData) {
				var dt = attr.Constructor.DeclaringType;
				if (dt.Name == attrName && dt.Namespace == attrNamespace)
					return true;
			}

			return false;
		}

		void ImportTypeBase (TypeSpec spec, MetaType type)
		{
			if (spec.Kind == MemberKind.Interface)
				spec.BaseType = module.Compiler.BuiltinTypes.Object;
			else if (type.BaseType != null) {
				TypeSpec base_type;
				if (!IsMissingType (type.BaseType) && type.BaseType.IsGenericType)
					base_type = CreateType (type.BaseType, new DynamicTypeReader (type), true);
				else
					base_type = CreateType (type.BaseType);

				spec.BaseType = base_type;
			}

			MetaType[] ifaces;
#if STATIC
			ifaces = type.__GetDeclaredInterfaces ();
			if (ifaces.Length != 0) {
				foreach (var iface in ifaces) {
					var it = CreateType (iface);
					if (it == null)
						continue;

					spec.AddInterface (it);

					// Unfortunately not all languages expand inherited interfaces
					var bifaces = it.Interfaces;
					if (bifaces != null) {
						foreach (var biface in bifaces) {
							spec.AddInterface (biface);
						}
					}
				}
			}

			if (spec.BaseType != null) {
				var bifaces = spec.BaseType.Interfaces;
				if (bifaces != null) {
					//
					// Before adding base class interfaces close defined interfaces
					// on type parameter
					//
					var tp = spec as TypeParameterSpec;
					if (tp != null && tp.InterfacesDefined == null) {
						tp.InterfacesDefined = TypeSpec.EmptyTypes;
					}

					foreach (var iface in bifaces)
						spec.AddInterface (iface);
				}
			}
#else
			ifaces = type.GetInterfaces ();

			if (ifaces.Length > 0) {
				foreach (var iface in ifaces) {
					spec.AddInterface (CreateType (iface));
				}
			}
#endif

			if (spec.MemberDefinition.TypeParametersCount > 0) {
				foreach (var tp in spec.MemberDefinition.TypeParameters) {
					ImportTypeParameterTypeConstraints (tp, tp.GetMetaInfo ());
				}
			}

		}

		protected void ImportTypes (MetaType[] types, Namespace targetNamespace, bool hasExtensionTypes)
		{
			Namespace ns = targetNamespace;
			string prev_namespace = null;
			foreach (var t in types) {
				if (t == null)
					continue;

				// Be careful not to trigger full parent type loading
				if (t.MemberType == MemberTypes.NestedType)
					continue;

				if (t.Name[0] == '<')
					continue;

				var it = CreateType (t, null, new DynamicTypeReader (t), true);
				if (it == null)
					continue;

				if (prev_namespace != t.Namespace) {
					ns = t.Namespace == null ? targetNamespace : targetNamespace.GetNamespace (t.Namespace, true);
					prev_namespace = t.Namespace;
				}

				ns.AddType (module, it);

				if (it.IsStatic && hasExtensionTypes &&
					HasAttribute (CustomAttributeData.GetCustomAttributes (t), "ExtensionAttribute", CompilerServicesNamespace)) {
					it.SetExtensionMethodContainer ();
				}
			}
		}

		void ImportTypeParameterTypeConstraints (TypeParameterSpec spec, MetaType type)
		{
			var constraints = type.GetGenericParameterConstraints ();
			List<TypeSpec> tparams = null;
			foreach (var ct in constraints) {
				if (ct.IsGenericParameter) {
					if (tparams == null)
						tparams = new List<TypeSpec> ();

					tparams.Add (CreateType (ct));
					continue;
				}

				if (!IsMissingType (ct) && ct.IsClass) {
					spec.BaseType = CreateType (ct);
					continue;
				}

				spec.AddInterface (CreateType (ct));
			}

			if (spec.BaseType == null)
				spec.BaseType = module.Compiler.BuiltinTypes.Object;

			if (tparams != null)
				spec.TypeArguments = tparams.ToArray ();
		}

		Constant ImportParameterConstant (object value)
		{
			//
			// Get type of underlying value as int constant can be used for object
			// parameter type. This is not allowed in C# but other languages can do that
			//
			var types = module.Compiler.BuiltinTypes;
			switch (System.Type.GetTypeCode (value.GetType ())) {
			case TypeCode.Boolean:
				return new BoolConstant (types, (bool) value, Location.Null);
			case TypeCode.Byte:
				return new ByteConstant (types, (byte) value, Location.Null);
			case TypeCode.Char:
				return new CharConstant (types, (char) value, Location.Null);
			case TypeCode.Decimal:
				return new DecimalConstant (types, (decimal) value, Location.Null);
			case TypeCode.Double:
				return new DoubleConstant (types, (double) value, Location.Null);
			case TypeCode.Int16:
				return new ShortConstant (types, (short) value, Location.Null);
			case TypeCode.Int32:
				return new IntConstant (types, (int) value, Location.Null);
			case TypeCode.Int64:
				return new LongConstant (types, (long) value, Location.Null);
			case TypeCode.SByte:
				return new SByteConstant (types, (sbyte) value, Location.Null);
			case TypeCode.Single:
				return new FloatConstant (types, (float) value, Location.Null);
			case TypeCode.String:
				return new StringConstant (types, (string) value, Location.Null);
			case TypeCode.UInt16:
				return new UShortConstant (types, (ushort) value, Location.Null);
			case TypeCode.UInt32:
				return new UIntConstant (types, (uint) value, Location.Null);
			case TypeCode.UInt64:
				return new ULongConstant (types, (ulong) value, Location.Null);
			}

			throw new NotImplementedException (value.GetType ().ToString ());
		}

		public TypeSpec ImportType (MetaType type)
		{
			return ImportType (type, new DynamicTypeReader (type));
		}

		TypeSpec ImportType (MetaType type, DynamicTypeReader dtype)
		{
			if (type.HasElementType) {
				var element = type.GetElementType ();
				++dtype.Position;
				var spec = ImportType (element, dtype);

				if (type.IsArray)
					return ArrayContainer.MakeType (module, spec, type.GetArrayRank ());
				if (type.IsByRef)
					return ReferenceContainer.MakeType (module, spec);
				if (type.IsPointer)
					return PointerContainer.MakeType (module, spec);

				throw new NotImplementedException ("Unknown element type " + type.ToString ());
			}

			return CreateType (type, dtype, true);
		}

		static bool IsMissingType (MetaType type)
		{
#if STATIC
			return type.__IsMissing;
#else
			return false;
#endif
		}

		//
		// Decimal constants cannot be encoded in the constant blob, and thus are marked
		// as IsInitOnly ('readonly' in C# parlance).  We get its value from the 
		// DecimalConstantAttribute metadata.
		//
		Constant ReadDecimalConstant (IList<CustomAttributeData> attrs)
		{
			if (attrs.Count == 0)
				return null;

			foreach (var ca in attrs) {
				var dt = ca.Constructor.DeclaringType;
				if (dt.Name != "DecimalConstantAttribute" || dt.Namespace != CompilerServicesNamespace)
					continue;

				var value = new decimal (
					(int) (uint) ca.ConstructorArguments[4].Value,
					(int) (uint) ca.ConstructorArguments[3].Value,
					(int) (uint) ca.ConstructorArguments[2].Value,
					(byte) ca.ConstructorArguments[1].Value != 0,
					(byte) ca.ConstructorArguments[0].Value);

				return new DecimalConstant (module.Compiler.BuiltinTypes, value, Location.Null);
			}

			return null;
		}

		static Modifiers ReadMethodModifiers (MethodBase mb, TypeSpec declaringType)
		{
			Modifiers mod;
			var ma = mb.Attributes;
			switch (ma & MethodAttributes.MemberAccessMask) {
			case MethodAttributes.Public:
				mod = Modifiers.PUBLIC;
				break;
			case MethodAttributes.Assembly:
				mod = Modifiers.INTERNAL;
				break;
			case MethodAttributes.Family:
				mod = Modifiers.PROTECTED;
				break;
			case MethodAttributes.FamORAssem:
				mod = Modifiers.PROTECTED | Modifiers.INTERNAL;
				break;
			default:
				mod = Modifiers.PRIVATE;
				break;
			}

			if ((ma & MethodAttributes.Static) != 0) {
				mod |= Modifiers.STATIC;
				return mod;
			}
			if ((ma & MethodAttributes.Abstract) != 0 && declaringType.IsClass) {
				mod |= Modifiers.ABSTRACT;
				return mod;
			}

			// It can be sealed and override
			if ((ma & MethodAttributes.Final) != 0)
				mod |= Modifiers.SEALED;

			if ((ma & MethodAttributes.Virtual) != 0) {
				// Not every member can be detected based on MethodAttribute, we
				// set virtual or non-virtual only when we are certain. Further checks
				// to really find out what `virtual' means for this member are done
				// later
				if ((ma & MethodAttributes.NewSlot) != 0) {
					if ((mod & Modifiers.SEALED) != 0) {
						mod &= ~Modifiers.SEALED;
					} else {
						mod |= Modifiers.VIRTUAL;
					}
				} else {
					mod |= Modifiers.OVERRIDE;
				}
			}

			return mod;
		}
	}

	abstract class ImportedDefinition : IMemberDefinition
	{
		protected class AttributesBag
		{
			public static readonly AttributesBag Default = new AttributesBag ();

			public AttributeUsageAttribute AttributeUsage;
			public ObsoleteAttribute Obsolete;
			public string[] Conditionals;
			public string DefaultIndexerName;
			public bool? CLSAttributeValue;
			public TypeSpec CoClass;
			
			public static AttributesBag Read (MemberInfo mi, MetadataImporter importer)
			{
				AttributesBag bag = null;
				List<string> conditionals = null;

				// It should not throw any loading exception
				IList<CustomAttributeData> attrs = CustomAttributeData.GetCustomAttributes (mi);

				foreach (var a in attrs) {
					var dt = a.Constructor.DeclaringType;
					string name = dt.Name;
					if (name == "ObsoleteAttribute") {
						if (dt.Namespace != "System")
							continue;

						if (bag == null)
							bag = new AttributesBag ();

						var args = a.ConstructorArguments;

						if (args.Count == 1) {
							bag.Obsolete = new ObsoleteAttribute ((string) args[0].Value);
						} else if (args.Count == 2) {
							bag.Obsolete = new ObsoleteAttribute ((string) args[0].Value, (bool) args[1].Value);
						} else {
							bag.Obsolete = new ObsoleteAttribute ();
						}

						continue;
					}

					if (name == "ConditionalAttribute") {
						if (dt.Namespace != "System.Diagnostics")
							continue;

						if (bag == null)
							bag = new AttributesBag ();

						if (conditionals == null)
							conditionals = new List<string> (2);

						conditionals.Add ((string) a.ConstructorArguments[0].Value);
						continue;
					}

					if (name == "CLSCompliantAttribute") {
						if (dt.Namespace != "System")
							continue;

						if (bag == null)
							bag = new AttributesBag ();

						bag.CLSAttributeValue = (bool) a.ConstructorArguments[0].Value;
						continue;
					}

					// Type only attributes
					if (mi.MemberType == MemberTypes.TypeInfo || mi.MemberType == MemberTypes.NestedType) {
						if (name == "DefaultMemberAttribute") {
							if (dt.Namespace != "System.Reflection")
								continue;

							if (bag == null)
								bag = new AttributesBag ();

							bag.DefaultIndexerName = (string) a.ConstructorArguments[0].Value;
							continue;
						}

						if (name == "AttributeUsageAttribute") {
							if (dt.Namespace != "System")
								continue;

							if (bag == null)
								bag = new AttributesBag ();

							bag.AttributeUsage = new AttributeUsageAttribute ((AttributeTargets) a.ConstructorArguments[0].Value);
							foreach (var named in a.NamedArguments) {
								if (named.MemberInfo.Name == "AllowMultiple")
									bag.AttributeUsage.AllowMultiple = (bool) named.TypedValue.Value;
								else if (named.MemberInfo.Name == "Inherited")
									bag.AttributeUsage.Inherited = (bool) named.TypedValue.Value;
							}
							continue;
						}

						// Interface only attribute
						if (name == "CoClassAttribute") {
							if (dt.Namespace != "System.Runtime.InteropServices")
								continue;

							if (bag == null)
								bag = new AttributesBag ();

							bag.CoClass = importer.ImportType ((MetaType) a.ConstructorArguments[0].Value);
							continue;
						}
					}
				}

				if (bag == null)
					return Default;

				if (conditionals != null)
					bag.Conditionals = conditionals.ToArray ();
				
				return bag;
			}
		}

		protected readonly MemberInfo provider;
		protected AttributesBag cattrs;
		protected readonly MetadataImporter importer;

		public ImportedDefinition (MemberInfo provider, MetadataImporter importer)
		{
			this.provider = provider;
			this.importer = importer;
		}

		#region Properties

		public bool IsImported {
			get {
				return true;
			}
		}

		public virtual string Name {
			get {
				return provider.Name;
			}
		}

		#endregion

		public string[] ConditionalConditions ()
		{
			if (cattrs == null)
				ReadAttributes ();

			return cattrs.Conditionals;
		}

		public ObsoleteAttribute GetAttributeObsolete ()
		{
			if (cattrs == null)
				ReadAttributes ();

			return cattrs.Obsolete;
		}

		public bool? CLSAttributeValue {
			get {
				if (cattrs == null)
					ReadAttributes ();

				return cattrs.CLSAttributeValue;
			}
		}

		protected void ReadAttributes ()
		{
			cattrs = AttributesBag.Read (provider, importer);
		}

		public void SetIsAssigned ()
		{
			// Unused for imported members
		}

		public void SetIsUsed ()
		{
			// Unused for imported members
		}
	}

	public class ImportedModuleDefinition
	{
		readonly Module module;
		bool cls_compliant;
		
		public ImportedModuleDefinition (Module module)
		{
			this.module = module;
		}

		#region Properties

		public bool IsCLSCompliant {
			get {
				return cls_compliant;
			}
		}

		public string Name {
			get {
				return module.Name;
			}
		}

		#endregion

		public void ReadAttributes ()
		{
			IList<CustomAttributeData> attrs = CustomAttributeData.GetCustomAttributes (module);

			foreach (var a in attrs) {
				var dt = a.Constructor.DeclaringType;
				if (dt.Name == "CLSCompliantAttribute") {
					if (dt.Namespace != "System")
						continue;

					cls_compliant = (bool) a.ConstructorArguments[0].Value;
					continue;
				}
			}
		}

		//
		// Reads assembly attributes which where attached to a special type because
		// module does have assembly manifest
		//
		public List<Attribute> ReadAssemblyAttributes ()
		{
			var t = module.GetType (AssemblyAttributesPlaceholder.GetGeneratedName (Name));
			if (t == null)
				return null;

			var field = t.GetField (AssemblyAttributesPlaceholder.AssemblyFieldName, BindingFlags.NonPublic | BindingFlags.Static);
			if (field == null)
				return null;

			// TODO: implement, the idea is to fabricate specil Attribute class and
			// add it to OptAttributes before resolving the source code attributes
			// Need to build module location as well for correct error reporting

			//var assembly_attributes = CustomAttributeData.GetCustomAttributes (field);
			//var attrs = new List<Attribute> (assembly_attributes.Count);
			//foreach (var a in assembly_attributes)
			//{
			//    var type = metaImporter.ImportType (a.Constructor.DeclaringType);
			//    var ctor = metaImporter.CreateMethod (a.Constructor, type);

			//    foreach (var carg in a.ConstructorArguments) {
			//        carg.Value
			//    }

			//    attrs.Add (new Attribute ("assembly", ctor, null, Location.Null, true));
			//}

			return null;
		}
	}

	public class ImportedAssemblyDefinition : IAssemblyDefinition
	{
		readonly Assembly assembly;
		readonly AssemblyName aname;
		bool cls_compliant;
		bool contains_extension_methods;

		List<AssemblyName> internals_visible_to;
		Dictionary<IAssemblyDefinition, AssemblyName> internals_visible_to_cache;

		public ImportedAssemblyDefinition (Assembly assembly)
		{
			this.assembly = assembly;
			this.aname = assembly.GetName ();
		}

		#region Properties

		public Assembly Assembly {
			get {
				return assembly;
			}
		}

		public string FullName {
			get {
				return aname.FullName;
			}
		}

		public bool HasExtensionMethod {
			get {
				return contains_extension_methods;
			}
		}

		public bool HasStrongName {
			get {
				return aname.GetPublicKey ().Length != 0;
			}
		}

		public bool IsMissing {
			get {
#if STATIC
				return assembly.__IsMissing;
#else
				return false;
#endif
			}
		}

		public bool IsCLSCompliant {
			get {
				return cls_compliant;
			}
		}

		public string Location {
			get {
				return assembly.Location;
			}
		}

		public string Name {
			get {
				return aname.Name;
			}
		}

		#endregion

		public byte[] GetPublicKeyToken ()
		{
			return aname.GetPublicKeyToken ();
		}

		public AssemblyName GetAssemblyVisibleToName (IAssemblyDefinition assembly)
		{
			return internals_visible_to_cache [assembly];
		}

		public bool IsFriendAssemblyTo (IAssemblyDefinition assembly)
		{
			if (internals_visible_to == null)
				return false;

			AssemblyName is_visible = null;
			if (internals_visible_to_cache == null) {
				internals_visible_to_cache = new Dictionary<IAssemblyDefinition, AssemblyName> ();
			} else {
				if (internals_visible_to_cache.TryGetValue (assembly, out is_visible))
					return is_visible != null;
			}

			var token = assembly.GetPublicKeyToken ();
			if (token != null && token.Length == 0)
				token = null;

			foreach (var internals in internals_visible_to) {
				if (internals.Name != assembly.Name)
					continue;

				if (token == null && assembly is AssemblyDefinition) {
					is_visible = internals;
					break;
				}

				if (!ArrayComparer.IsEqual (token, internals.GetPublicKeyToken ()))
					continue;

				is_visible = internals;
				break;
			}

			internals_visible_to_cache.Add (assembly, is_visible);
			return is_visible != null;
		}

		public void ReadAttributes ()
		{
#if STATIC
			if (assembly.__IsMissing)
				return;
#endif

			IList<CustomAttributeData> attrs = CustomAttributeData.GetCustomAttributes (assembly);

			foreach (var a in attrs) {
				var dt = a.Constructor.DeclaringType;
				var name = dt.Name;
				if (name == "CLSCompliantAttribute") {
					if (dt.Namespace == "System") {
						cls_compliant = (bool) a.ConstructorArguments[0].Value;
					}
					continue;
				}

				if (name == "InternalsVisibleToAttribute") {
					if (dt.Namespace != MetadataImporter.CompilerServicesNamespace)
						continue;

					string s = a.ConstructorArguments[0].Value as string;
					if (s == null)
						continue;

					var an = new AssemblyName (s);
					if (internals_visible_to == null)
						internals_visible_to = new List<AssemblyName> ();

					internals_visible_to.Add (an);
					continue;
				}

				if (name == "ExtensionAttribute") {
					if (dt.Namespace == MetadataImporter.CompilerServicesNamespace)
						contains_extension_methods = true;

					continue;
				}
			}
		}

		public override string ToString ()
		{
			return FullName;
		}
	}

	class ImportedMemberDefinition : ImportedDefinition
	{
		readonly TypeSpec type;

		public ImportedMemberDefinition (MemberInfo member, TypeSpec type, MetadataImporter importer)
			: base (member, importer)
		{
			this.type = type;
		}

		#region Properties

		public TypeSpec MemberType {
			get {
				return type;
			}
		}

		#endregion
	}

	class ImportedParameterMemberDefinition : ImportedMemberDefinition, IParametersMember
	{
		readonly AParametersCollection parameters;

		public ImportedParameterMemberDefinition (MethodBase provider, TypeSpec type, AParametersCollection parameters, MetadataImporter importer)
			: base (provider, type, importer)
		{
			this.parameters = parameters;
		}

		public ImportedParameterMemberDefinition (PropertyInfo provider, TypeSpec type, AParametersCollection parameters, MetadataImporter importer)
			: base (provider, type, importer)
		{
			this.parameters = parameters;
		}

		#region Properties

		public AParametersCollection Parameters {
			get {
				return parameters;
			}
		}

		#endregion
	}

	class ImportedGenericMethodDefinition : ImportedParameterMemberDefinition, IGenericMethodDefinition
	{
		readonly TypeParameterSpec[] tparams;

		public ImportedGenericMethodDefinition (MethodInfo provider, TypeSpec type, AParametersCollection parameters, TypeParameterSpec[] tparams, MetadataImporter importer)
			: base (provider, type, parameters, importer)
		{
			this.tparams = tparams;
		}

		#region Properties

		public TypeParameterSpec[] TypeParameters {
			get {
				return tparams;
			}
		}

		public int TypeParametersCount {
			get {
				return tparams.Length;
			}
		}

		#endregion
	}

	class ImportedTypeDefinition : ImportedDefinition, ITypeDefinition
	{
		TypeParameterSpec[] tparams;
		string name;

		public ImportedTypeDefinition (MetaType type, MetadataImporter importer)
			: base (type, importer)
		{
		}

		#region Properties

		public IAssemblyDefinition DeclaringAssembly {
			get {
				return importer.GetAssemblyDefinition (provider.Module.Assembly);
			}
		}

		bool ITypeDefinition.IsPartial {
			get {
				return false;
			}
		}

		public override string Name {
			get {
				if (name == null) {
					name = base.Name;
					if (tparams != null) {
						int arity_start = name.IndexOf ('`');
						if (arity_start > 0)
							name = name.Substring (0, arity_start);
					}
				}

				return name;
			}
		}

		public string Namespace {
			get {
				return ((MetaType) provider).Namespace;
			}
		}

		public int TypeParametersCount {
			get {
				return tparams == null ? 0 : tparams.Length;
			}
		}

		public TypeParameterSpec[] TypeParameters {
			get {
				return tparams;
			}
			set {
				tparams = value;
			}
		}

		#endregion

		public static void Error_MissingDependency (IMemberContext ctx, List<TypeSpec> types, Location loc)
		{
			// 
			// Report details about missing type and most likely cause of the problem.
			// csc reports 1683, 1684 as warnings but we report them only when used
			// or referenced from the user core in which case compilation error has to
			// be reported because compiler cannot continue anyway
			//
			foreach (var t in types) {
				string name = t.GetSignatureForError ();

				if (t.MemberDefinition.DeclaringAssembly == ctx.Module.DeclaringAssembly) {
					ctx.Module.Compiler.Report.Error (1683, loc,
						"Reference to type `{0}' claims it is defined in this assembly, but it is not defined in source or any added modules",
						name);
				} else if (t.MemberDefinition.DeclaringAssembly.IsMissing) {
					ctx.Module.Compiler.Report.Error (12, loc,
						"The type `{0}' is defined in an assembly that is not referenced. Consider adding a reference to assembly `{1}'",
						name, t.MemberDefinition.DeclaringAssembly.FullName);
				} else {
					ctx.Module.Compiler.Report.Error (1684, loc,
						"Reference to type `{0}' claims it is defined assembly `{1}', but it could not be found",
						name, t.MemberDefinition.DeclaringAssembly.FullName);
				}
			}
		}

		public TypeSpec GetAttributeCoClass ()
		{
			if (cattrs == null)
				ReadAttributes ();

			return cattrs.CoClass;
		}

		public string GetAttributeDefaultMember ()
		{
			if (cattrs == null)
				ReadAttributes ();

			return cattrs.DefaultIndexerName;
		}

		public AttributeUsageAttribute GetAttributeUsage (PredefinedAttribute pa)
		{
			if (cattrs == null)
				ReadAttributes ();

			return cattrs.AttributeUsage;
		}

		bool ITypeDefinition.IsInternalAsPublic (IAssemblyDefinition assembly)
		{
			var a = importer.GetAssemblyDefinition (provider.Module.Assembly);
			return a == assembly || a.IsFriendAssemblyTo (assembly);
		}

		public void LoadMembers (TypeSpec declaringType, bool onlyTypes, ref MemberCache cache)
		{
			//
			// Not interested in members of nested private types unless the importer needs them
			//
			if (declaringType.IsPrivate && importer.IgnorePrivateMembers) {
				cache = MemberCache.Empty;
				return;
			}

			var loading_type = (MetaType) provider;
			const BindingFlags all_members = BindingFlags.DeclaredOnly |
				BindingFlags.Static | BindingFlags.Instance |
				BindingFlags.Public | BindingFlags.NonPublic;

			const MethodAttributes explicit_impl = MethodAttributes.NewSlot |
					MethodAttributes.Virtual | MethodAttributes.HideBySig |
					MethodAttributes.Final;

			Dictionary<MethodBase, MethodSpec> possible_accessors = null;
			List<EventSpec> imported_events = null;
			EventSpec event_spec;
			MemberSpec imported;
			MethodInfo m;
			MemberInfo[] all;
			try {
				all = loading_type.GetMembers (all_members);
			} catch (Exception e) {
				throw new InternalErrorException (e, "Could not import type `{0}' from `{1}'",
					declaringType.GetSignatureForError (), declaringType.MemberDefinition.DeclaringAssembly.FullName);
			}

			if (cache == null) {
				cache = new MemberCache (all.Length);

				//
				// Do the types first as they can be referenced by the members before
				// they are found or inflated
				//
				foreach (var member in all) {
					if (member.MemberType != MemberTypes.NestedType)
						continue;

					var t = (MetaType) member;

					// Ignore compiler generated types, mostly lambda containers
					if ((t.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate && importer.IgnorePrivateMembers)
						continue;

					try {
						imported = importer.CreateNestedType (t, declaringType);
					} catch (Exception e) {
						throw new InternalErrorException (e, "Could not import nested type `{0}' from `{1}'",
							t.FullName, declaringType.MemberDefinition.DeclaringAssembly.FullName);
					}

					cache.AddMemberImported (imported);
				}

				foreach (var member in all) {
					if (member.MemberType != MemberTypes.NestedType)
						continue;

					var t = (MetaType) member;

					if ((t.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate && importer.IgnorePrivateMembers)
						continue;

					importer.ImportTypeBase (t);
				}
			}

			//
			// Load base interfaces first to minic behaviour of compiled members
			//
			if (declaringType.IsInterface && declaringType.Interfaces != null) {
				foreach (var iface in declaringType.Interfaces) {
					cache.AddInterface (iface);
				}
			}

			if (!onlyTypes) {
				//
				// The logic here requires methods to be returned first which seems to work for both Mono and .NET
				//
				foreach (var member in all) {
					switch (member.MemberType) {
					case MemberTypes.Constructor:
					case MemberTypes.Method:
						MethodBase mb = (MethodBase) member;
						var attrs = mb.Attributes;

						if ((attrs & MethodAttributes.MemberAccessMask) == MethodAttributes.Private) {
							if (importer.IgnorePrivateMembers)
								continue;

							// Ignore explicitly implemented members
							if ((attrs & explicit_impl) == explicit_impl)
								continue;

							// Ignore compiler generated methods
							if (MetadataImporter.HasAttribute (CustomAttributeData.GetCustomAttributes (mb), "CompilerGeneratedAttribute", MetadataImporter.CompilerServicesNamespace))
								continue;
						}

						imported = importer.CreateMethod (mb, declaringType);
						if (imported.Kind == MemberKind.Method && !imported.IsGeneric) {
							if (possible_accessors == null)
								possible_accessors = new Dictionary<MethodBase, MethodSpec> (ReferenceEquality<MethodBase>.Default);

							// There are no metadata rules for accessors, we have to consider any method as possible candidate
							possible_accessors.Add (mb, (MethodSpec) imported);
						}

						break;
					case MemberTypes.Property:
						if (possible_accessors == null)
							continue;

						var p = (PropertyInfo) member;
						//
						// Links possible accessors with property
						//
						MethodSpec get, set;
						m = p.GetGetMethod (true);
						if (m == null || !possible_accessors.TryGetValue (m, out get))
							get = null;

						m = p.GetSetMethod (true);
						if (m == null || !possible_accessors.TryGetValue (m, out set))
							set = null;

						// No accessors registered (e.g. explicit implementation)
						if (get == null && set == null)
							continue;

						imported = importer.CreateProperty (p, declaringType, get, set);
						if (imported == null)
							continue;

						break;
					case MemberTypes.Event:
						if (possible_accessors == null)
							continue;

						var e = (EventInfo) member;
						//
						// Links accessors with event
						//
						MethodSpec add, remove;
						m = e.GetAddMethod (true);
						if (m == null || !possible_accessors.TryGetValue (m, out add))
							add = null;

						m = e.GetRemoveMethod (true);
						if (m == null || !possible_accessors.TryGetValue (m, out remove))
							remove = null;

						// Both accessors are required
						if (add == null || remove == null)
							continue;

						event_spec = importer.CreateEvent (e, declaringType, add, remove);
						if (!importer.IgnorePrivateMembers) {
							if (imported_events == null)
								imported_events = new List<EventSpec> ();

							imported_events.Add (event_spec);
						}

						imported = event_spec;
						break;
					case MemberTypes.Field:
						var fi = (FieldInfo) member;

						imported = importer.CreateField (fi, declaringType);
						if (imported == null)
							continue;

						//
						// For dynamic binder event has to be fully restored to allow operations
						// within the type container to work correctly
						//
						if (imported_events != null) {
							// The backing event field should be private but it may not
							int i;
							for (i = 0; i < imported_events.Count; ++i) {
								var ev = imported_events[i];
								if (ev.Name == fi.Name) {
									ev.BackingField = (FieldSpec) imported;
									imported_events.RemoveAt (i);
									i = -1;
									break;
								}
							}

							if (i < 0)
								continue;
						}

						break;
					case MemberTypes.NestedType:
						// Already in the cache from the first pass
						continue;
					default:
						throw new NotImplementedException (member.ToString ());
					}

					cache.AddMemberImported (imported);
				}
			}
		}
	}

	class ImportedTypeParameterDefinition : ImportedDefinition, ITypeDefinition
	{
		public ImportedTypeParameterDefinition (MetaType type, MetadataImporter importer)
			: base (type, importer)
		{
		}

		#region Properties

		public IAssemblyDefinition DeclaringAssembly {
			get {
				throw new NotImplementedException ();
			}
		}

		bool ITypeDefinition.IsPartial {
			get {
				return false;
			}
		}

		public string Namespace {
			get {
				return null;
			}
		}

		public int TypeParametersCount {
			get {
				return 0;
			}
		}

		public TypeParameterSpec[] TypeParameters {
			get {
				return null;
			}
		}

		#endregion

		public TypeSpec GetAttributeCoClass ()
		{
			return null;
		}

		public string GetAttributeDefaultMember ()
		{
			throw new NotSupportedException ();
		}

		public AttributeUsageAttribute GetAttributeUsage (PredefinedAttribute pa)
		{
			throw new NotSupportedException ();
		}

		bool ITypeDefinition.IsInternalAsPublic (IAssemblyDefinition assembly)
		{
			throw new NotImplementedException ();
		}

		public void LoadMembers (TypeSpec declaringType, bool onlyTypes, ref MemberCache cache)
		{
			throw new NotImplementedException ();
		}
	}
}
