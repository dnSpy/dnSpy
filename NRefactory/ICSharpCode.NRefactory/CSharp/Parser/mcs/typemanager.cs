//
// typemanager.cs: C# type manager
//
// Author: Miguel de Icaza (miguel@gnu.org)
//         Ravi Pratap     (ravi@ximian.com)
//         Marek Safar     (marek.safar@seznam.cz)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
// Copyright 2003-2008 Novell, Inc.
//

using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;

namespace Mono.CSharp
{
	//
	// All compiler build-in types (they have to exist otherwise the compile will not work)
	//
	public class BuildinTypes
	{
		public readonly BuildinTypeSpec Object;
		public readonly BuildinTypeSpec ValueType;
		public readonly BuildinTypeSpec Attribute;

		public readonly BuildinTypeSpec Int;
		public readonly BuildinTypeSpec UInt;
		public readonly BuildinTypeSpec Long;
		public readonly BuildinTypeSpec ULong;
		public readonly BuildinTypeSpec Float;
		public readonly BuildinTypeSpec Double;
		public readonly BuildinTypeSpec Char;
		public readonly BuildinTypeSpec Short;
		public readonly BuildinTypeSpec Decimal;
		public readonly BuildinTypeSpec Bool;
		public readonly BuildinTypeSpec SByte;
		public readonly BuildinTypeSpec Byte;
		public readonly BuildinTypeSpec UShort;
		public readonly BuildinTypeSpec String;

		public readonly BuildinTypeSpec Enum;
		public readonly BuildinTypeSpec Delegate;
		public readonly BuildinTypeSpec MulticastDelegate;
		public readonly BuildinTypeSpec Void;
		public readonly BuildinTypeSpec Array;
		public readonly BuildinTypeSpec Type;
		public readonly BuildinTypeSpec IEnumerator;
		public readonly BuildinTypeSpec IEnumerable;
		public readonly BuildinTypeSpec IDisposable;
		public readonly BuildinTypeSpec IntPtr;
		public readonly BuildinTypeSpec UIntPtr;
		public readonly BuildinTypeSpec RuntimeFieldHandle;
		public readonly BuildinTypeSpec RuntimeTypeHandle;
		public readonly BuildinTypeSpec Exception;

		//
		// These are internal buil-in types which depend on other
		// build-in type (mostly object)
		//
		public readonly BuildinTypeSpec Dynamic;
		public readonly BuildinTypeSpec Null;

		readonly BuildinTypeSpec[] types;

		public BuildinTypes ()
		{
			Object = new BuildinTypeSpec (MemberKind.Class, "System", "Object", BuildinTypeSpec.Type.Object);
			ValueType = new BuildinTypeSpec (MemberKind.Class, "System", "ValueType", BuildinTypeSpec.Type.ValueType);
			Attribute = new BuildinTypeSpec (MemberKind.Class, "System", "Attribute", BuildinTypeSpec.Type.Attribute);

			Int = new BuildinTypeSpec (MemberKind.Struct, "System", "Int32", BuildinTypeSpec.Type.Int);
			Long = new BuildinTypeSpec (MemberKind.Struct, "System", "Int64", BuildinTypeSpec.Type.Long);
			UInt = new BuildinTypeSpec (MemberKind.Struct, "System", "UInt32", BuildinTypeSpec.Type.UInt);
			ULong = new BuildinTypeSpec (MemberKind.Struct, "System", "UInt64", BuildinTypeSpec.Type.ULong);
			Byte = new BuildinTypeSpec (MemberKind.Struct, "System", "Byte", BuildinTypeSpec.Type.Byte);
			SByte = new BuildinTypeSpec (MemberKind.Struct, "System", "SByte", BuildinTypeSpec.Type.SByte);
			Short = new BuildinTypeSpec (MemberKind.Struct, "System", "Int16", BuildinTypeSpec.Type.Short);
			UShort = new BuildinTypeSpec (MemberKind.Struct, "System", "UInt16", BuildinTypeSpec.Type.UShort);

			IEnumerator = new BuildinTypeSpec (MemberKind.Interface, "System.Collections", "IEnumerator", BuildinTypeSpec.Type.IEnumerator);
			IEnumerable = new BuildinTypeSpec (MemberKind.Interface, "System.Collections", "IEnumerable", BuildinTypeSpec.Type.IEnumerable);
			IDisposable = new BuildinTypeSpec (MemberKind.Interface, "System", "IDisposable", BuildinTypeSpec.Type.IDisposable);

			Char = new BuildinTypeSpec (MemberKind.Struct, "System", "Char", BuildinTypeSpec.Type.Char);
			String = new BuildinTypeSpec (MemberKind.Class, "System", "String", BuildinTypeSpec.Type.String);
			Float = new BuildinTypeSpec (MemberKind.Struct, "System", "Single", BuildinTypeSpec.Type.Float);
			Double = new BuildinTypeSpec (MemberKind.Struct, "System", "Double", BuildinTypeSpec.Type.Double);
			Decimal = new BuildinTypeSpec (MemberKind.Struct, "System", "Decimal", BuildinTypeSpec.Type.Decimal);
			Bool = new BuildinTypeSpec (MemberKind.Struct, "System", "Boolean", BuildinTypeSpec.Type.Bool);
			IntPtr = new BuildinTypeSpec (MemberKind.Struct, "System", "IntPtr", BuildinTypeSpec.Type.IntPtr);
			UIntPtr = new BuildinTypeSpec (MemberKind.Struct, "System", "UIntPtr", BuildinTypeSpec.Type.UIntPtr);

			MulticastDelegate = new BuildinTypeSpec (MemberKind.Class, "System", "MulticastDelegate", BuildinTypeSpec.Type.MulticastDelegate);
			Delegate = new BuildinTypeSpec (MemberKind.Class, "System", "Delegate", BuildinTypeSpec.Type.Delegate);
			Enum = new BuildinTypeSpec (MemberKind.Class, "System", "Enum", BuildinTypeSpec.Type.Enum);
			Array = new BuildinTypeSpec (MemberKind.Class, "System", "Array", BuildinTypeSpec.Type.Array);
			Void = new BuildinTypeSpec (MemberKind.Struct, "System", "Void", BuildinTypeSpec.Type.Void);
			Type = new BuildinTypeSpec (MemberKind.Class, "System", "Type", BuildinTypeSpec.Type.Type);
			Exception = new BuildinTypeSpec (MemberKind.Class, "System", "Exception", BuildinTypeSpec.Type.Exception);
			RuntimeFieldHandle = new BuildinTypeSpec (MemberKind.Struct, "System", "RuntimeFieldHandle", BuildinTypeSpec.Type.RuntimeFieldHandle);
			RuntimeTypeHandle = new BuildinTypeSpec (MemberKind.Struct, "System", "RuntimeTypeHandle", BuildinTypeSpec.Type.RuntimeTypeHandle);

			Dynamic = new BuildinTypeSpec ("dynamic", BuildinTypeSpec.Type.Dynamic);
			Null = new BuildinTypeSpec ("null", BuildinTypeSpec.Type.Null);
			Null.MemberCache = MemberCache.Empty;

			types = new BuildinTypeSpec[] {
				Object, ValueType, Attribute,
				Int, UInt, Long, ULong, Float, Double, Char, Short, Decimal, Bool, SByte, Byte, UShort, String,
				Enum, Delegate, MulticastDelegate, Void, Array, Type, IEnumerator, IEnumerable, IDisposable,
				IntPtr, UIntPtr, RuntimeFieldHandle, RuntimeTypeHandle, Exception };

			// Deal with obsolete static types
			// TODO: remove
			TypeManager.object_type = Object;
			TypeManager.value_type = ValueType;
			TypeManager.string_type = String;
			TypeManager.int32_type = Int;
			TypeManager.uint32_type = UInt;
			TypeManager.int64_type = Long;
			TypeManager.uint64_type = ULong;
			TypeManager.float_type = Float;
			TypeManager.double_type = Double;
			TypeManager.char_type = Char;
			TypeManager.short_type = Short;
			TypeManager.decimal_type = Decimal;
			TypeManager.bool_type = Bool;
			TypeManager.sbyte_type = SByte;
			TypeManager.byte_type = Byte;
			TypeManager.ushort_type = UShort;
			TypeManager.enum_type = Enum;
			TypeManager.delegate_type = Delegate;
			TypeManager.multicast_delegate_type = MulticastDelegate; ;
			TypeManager.void_type = Void;
			TypeManager.array_type = Array; ;
			TypeManager.runtime_handle_type = RuntimeTypeHandle;
			TypeManager.type_type = Type;
			TypeManager.ienumerator_type = IEnumerator;
			TypeManager.ienumerable_type = IEnumerable;
			TypeManager.idisposable_type = IDisposable;
			TypeManager.intptr_type = IntPtr;
			TypeManager.uintptr_type = UIntPtr;
			TypeManager.runtime_field_handle_type = RuntimeFieldHandle;
			TypeManager.attribute_type = Attribute;
			TypeManager.exception_type = Exception;

			InternalType.Dynamic = Dynamic;
			InternalType.Null = Null;
		}

		public BuildinTypeSpec[] AllTypes {
			get {
				return types;
			}
		}

		public bool CheckDefinitions (ModuleContainer module)
		{
			var ctx = module.Compiler;
			foreach (var p in types) {
				var found = PredefinedType.Resolve (module, p.Kind, p.Namespace, p.Name, p.Arity, Location.Null);
				if (found == null || found == p)
					continue;

				var tc = found.MemberDefinition as TypeContainer;
				if (tc != null) {
					var ns = module.GlobalRootNamespace.GetNamespace (p.Namespace, false);
					ns.ReplaceTypeWithPredefined (found, p);

					tc.SetPredefinedSpec (p);
					p.SetDefinition (found);
				}
			}

			if (ctx.Report.Errors != 0)
				return false;

			// Set internal build-in types
			Dynamic.SetDefinition (Object);
			Null.SetDefinition (Object);

			return true;
		}
	}

	//
	// Compiler predefined types. Usually used for compiler generated
	// code or for comparison against well known framework type
	//
	class PredefinedTypes
	{
		// TODO: These two exist only to reject type comparison
		public readonly PredefinedType TypedReference;
		public readonly PredefinedType ArgIterator;

		public readonly PredefinedType MarshalByRefObject;
		public readonly PredefinedType RuntimeHelpers;
		public readonly PredefinedType IAsyncResult;
		public readonly PredefinedType AsyncCallback;
		public readonly PredefinedType RuntimeArgumentHandle;
		public readonly PredefinedType CharSet;
		public readonly PredefinedType IsVolatile;
		public readonly PredefinedType IEnumeratorGeneric;
		public readonly PredefinedType IListGeneric;
		public readonly PredefinedType ICollectionGeneric;
		public readonly PredefinedType IEnumerableGeneric;
		public readonly PredefinedType Nullable;
		public readonly PredefinedType Activator;
		public readonly PredefinedType Interlocked;
		public readonly PredefinedType Monitor;
		public readonly PredefinedType NotSupportedException;
		public readonly PredefinedType RuntimeFieldHandle;
		public readonly PredefinedType RuntimeMethodHandle;
		public readonly PredefinedType SecurityAction;

		//
		// C# 3.0
		//
		public readonly PredefinedType Expression;
		public readonly PredefinedType ExpressionGeneric;
		public readonly PredefinedType ParameterExpression;
		public readonly PredefinedType FieldInfo;
		public readonly PredefinedType MethodBase;
		public readonly PredefinedType MethodInfo;
		public readonly PredefinedType ConstructorInfo;

		//
		// C# 4.0
		//
		public readonly PredefinedType Binder;
		public readonly PredefinedType CallSite;
		public readonly PredefinedType CallSiteGeneric;
		public readonly PredefinedType BinderFlags;

		public PredefinedTypes (ModuleContainer module)
		{
			TypedReference = new PredefinedType (module, MemberKind.Struct, "System", "TypedReference");
			ArgIterator = new PredefinedType (module, MemberKind.Struct, "System", "ArgIterator");
			MarshalByRefObject = new PredefinedType (module, MemberKind.Class, "System", "MarshalByRefObject");
			RuntimeHelpers = new PredefinedType (module, MemberKind.Class, "System.Runtime.CompilerServices", "RuntimeHelpers");
			IAsyncResult = new PredefinedType (module, MemberKind.Interface, "System", "IAsyncResult");
			AsyncCallback = new PredefinedType (module, MemberKind.Delegate, "System", "AsyncCallback");
			RuntimeArgumentHandle = new PredefinedType (module, MemberKind.Struct, "System", "RuntimeArgumentHandle");
			CharSet = new PredefinedType (module, MemberKind.Enum, "System.Runtime.InteropServices", "CharSet");
			IsVolatile = new PredefinedType (module, MemberKind.Class, "System.Runtime.CompilerServices", "IsVolatile");
			IEnumeratorGeneric = new PredefinedType (module, MemberKind.Interface, "System.Collections.Generic", "IEnumerator", 1);
			IListGeneric = new PredefinedType (module, MemberKind.Interface, "System.Collections.Generic", "IList", 1);
			ICollectionGeneric = new PredefinedType (module, MemberKind.Interface, "System.Collections.Generic", "ICollection", 1);
			IEnumerableGeneric = new PredefinedType (module, MemberKind.Interface, "System.Collections.Generic", "IEnumerable", 1);
			Nullable = new PredefinedType (module, MemberKind.Struct, "System", "Nullable", 1);
			Activator = new PredefinedType (module, MemberKind.Class, "System", "Activator");
			Interlocked = new PredefinedType (module, MemberKind.Class, "System.Threading", "Interlocked");
			Monitor = new PredefinedType (module, MemberKind.Class, "System.Threading", "Monitor");
			NotSupportedException = new PredefinedType (module, MemberKind.Class, "System", "NotSupportedException");
			RuntimeFieldHandle = new PredefinedType (module, MemberKind.Struct, "System", "RuntimeFieldHandle");
			RuntimeMethodHandle = new PredefinedType (module, MemberKind.Struct, "System", "RuntimeMethodHandle");
			SecurityAction = new PredefinedType (module, MemberKind.Enum, "System.Security.Permissions", "SecurityAction");

			Expression = new PredefinedType (module, MemberKind.Class, "System.Linq.Expressions", "Expression");
			ExpressionGeneric = new PredefinedType (module, MemberKind.Class, "System.Linq.Expressions", "Expression", 1);
			ParameterExpression = new PredefinedType (module, MemberKind.Class, "System.Linq.Expressions", "ParameterExpression");
			FieldInfo = new PredefinedType (module, MemberKind.Class, "System.Reflection", "FieldInfo");
			MethodBase = new PredefinedType (module, MemberKind.Class, "System.Reflection", "MethodBase");
			MethodInfo = new PredefinedType (module, MemberKind.Class, "System.Reflection", "MethodInfo");
			ConstructorInfo = new PredefinedType (module, MemberKind.Class, "System.Reflection", "ConstructorInfo");

			CallSite = new PredefinedType (module, MemberKind.Class, "System.Runtime.CompilerServices", "CallSite");
			CallSiteGeneric = new PredefinedType (module, MemberKind.Class, "System.Runtime.CompilerServices", "CallSite", 1);
			Binder = new PredefinedType (module, MemberKind.Class, "Microsoft.CSharp.RuntimeBinder", "Binder");
			BinderFlags = new PredefinedType (module, MemberKind.Enum, "Microsoft.CSharp.RuntimeBinder", "CSharpBinderFlags");

			//
			// Define types which are used for comparison. It does not matter
			// if they don't exist as no error report is needed
			//
			TypedReference.Define ();
			ArgIterator.Define ();
			MarshalByRefObject.Define ();
			CharSet.Define ();

			IEnumerableGeneric.Define ();
			IListGeneric.Define ();
			ICollectionGeneric.Define ();
			IEnumerableGeneric.Define ();
			IEnumeratorGeneric.Define ();
			Nullable.Define ();
			ExpressionGeneric.Define ();

			// Deal with obsolete static types
			// TODO: remove
			TypeManager.typed_reference_type = TypedReference.TypeSpec;
			TypeManager.arg_iterator_type = ArgIterator.TypeSpec;
			TypeManager.mbr_type = MarshalByRefObject.TypeSpec;
			TypeManager.generic_ilist_type = IListGeneric.TypeSpec;
			TypeManager.generic_icollection_type = ICollectionGeneric.TypeSpec;
			TypeManager.generic_ienumerator_type = IEnumeratorGeneric.TypeSpec;
			TypeManager.generic_ienumerable_type = IEnumerableGeneric.TypeSpec;
			TypeManager.generic_nullable_type = Nullable.TypeSpec;
			TypeManager.expression_type = ExpressionGeneric.TypeSpec;
		}
	}

	public class PredefinedType
	{
		string name;
		string ns;
		int arity;
		MemberKind kind;
		ModuleContainer module;
		protected TypeSpec type;

		public PredefinedType (ModuleContainer module, MemberKind kind, string ns, string name, int arity)
			: this (module, kind, ns, name)
		{
			this.arity = arity;
		}

		public PredefinedType (ModuleContainer module, MemberKind kind, string ns, string name)
		{
			this.module = module;
			this.kind = kind;
			this.name = name;
			this.ns = ns;
		}

		#region Properties

		public int Arity {
			get {
				return arity;
			}
		}

		public bool IsDefined {
			get {
				return type != null;
			}
		}

		public string Name {
			get {
				return name;
			}
		}

		public string Namespace {
			get {
				return ns;
			}
		}

		public TypeSpec TypeSpec {
			get {
				return type;
			}
		}

		#endregion

		public bool Define ()
		{
			if (type != null)
				return true;

			Namespace type_ns = module.GlobalRootNamespace.GetNamespace (ns, true);
			var te = type_ns.LookupType (module.Compiler, name, arity, true, Location.Null);
			if (te == null)
				return false;

			if (te.Type.Kind != kind)
				return false;

			type = te.Type;
			return true;
		}

		public FieldSpec GetField (string name, TypeSpec memberType, Location loc)
		{
			return TypeManager.GetPredefinedField (type, name, loc, memberType);
		}

		public string GetSignatureForError ()
		{
			return ns + "." + name;
		}

		public static TypeSpec Resolve (ModuleContainer module, MemberKind kind, string ns, string name, int arity, Location loc)
		{
			Namespace type_ns = module.GlobalRootNamespace.GetNamespace (ns, true);
			var te = type_ns.LookupType (module.Compiler, name, arity, false, Location.Null);
			if (te == null) {
				module.Compiler.Report.Error (518, loc, "The predefined type `{0}.{1}' is not defined or imported", ns, name);
				return null;
			}

			var type = te.Type;
			if (type.Kind != kind) {
				module.Compiler.Report.Error (520, loc, "The predefined type `{0}.{1}' is not declared correctly", ns, name);
				return null;
			}

			return type;
		}

		public TypeSpec Resolve (Location loc)
		{
			if (type == null)
				type = Resolve (module, kind, ns, name, arity, loc);

			return type;
		}
	}

	partial class TypeManager {
	//
	// A list of core types that the compiler requires or uses
	//
	static public BuildinTypeSpec object_type;
	static public BuildinTypeSpec value_type;
	static public BuildinTypeSpec string_type;
	static public BuildinTypeSpec int32_type;
	static public BuildinTypeSpec uint32_type;
	static public BuildinTypeSpec int64_type;
	static public BuildinTypeSpec uint64_type;
	static public BuildinTypeSpec float_type;
	static public BuildinTypeSpec double_type;
	static public BuildinTypeSpec char_type;
	static public BuildinTypeSpec short_type;
	static public BuildinTypeSpec decimal_type;
	static public BuildinTypeSpec bool_type;
	static public BuildinTypeSpec sbyte_type;
	static public BuildinTypeSpec byte_type;
	static public BuildinTypeSpec ushort_type;
	static public BuildinTypeSpec enum_type;
	static public BuildinTypeSpec delegate_type;
	static public BuildinTypeSpec multicast_delegate_type;
	static public BuildinTypeSpec void_type;
	static public BuildinTypeSpec array_type;
	static public BuildinTypeSpec runtime_handle_type;
	static public BuildinTypeSpec type_type;
	static public BuildinTypeSpec ienumerator_type;
	static public BuildinTypeSpec ienumerable_type;
	static public BuildinTypeSpec idisposable_type;
	static public BuildinTypeSpec intptr_type;
	static public BuildinTypeSpec uintptr_type;
	static public BuildinTypeSpec runtime_field_handle_type;
	static public BuildinTypeSpec attribute_type;
	static public BuildinTypeSpec exception_type;


	static public TypeSpec typed_reference_type;
	static public TypeSpec arg_iterator_type;
	static public TypeSpec mbr_type;
	static public TypeSpec generic_ilist_type;
	static public TypeSpec generic_icollection_type;
	static public TypeSpec generic_ienumerator_type;
	static public TypeSpec generic_ienumerable_type;
	static public TypeSpec generic_nullable_type;
	static internal TypeSpec expression_type;

	//
	// These methods are called by code generated by the compiler
	//
	static public FieldSpec string_empty;
	static public MethodSpec system_type_get_type_from_handle;
	static public MethodSpec bool_movenext_void;
	static public MethodSpec void_dispose_void;
	static public MethodSpec void_monitor_enter_object;
	static public MethodSpec void_monitor_exit_object;
	static public MethodSpec void_initializearray_array_fieldhandle;
	static public MethodSpec delegate_combine_delegate_delegate;
	static public MethodSpec delegate_remove_delegate_delegate;
	static public PropertySpec int_get_offset_to_string_data;
	static public MethodSpec int_interlocked_compare_exchange;
	public static MethodSpec gen_interlocked_compare_exchange;
	static public PropertySpec ienumerator_getcurrent;
	public static MethodSpec methodbase_get_type_from_handle;
	public static MethodSpec methodbase_get_type_from_handle_generic;
	public static MethodSpec fieldinfo_get_field_from_handle;
	public static MethodSpec fieldinfo_get_field_from_handle_generic;
	public static MethodSpec activator_create_instance;

	//
	// The constructors.
	//
	static public MethodSpec void_decimal_ctor_five_args;
	static public MethodSpec void_decimal_ctor_int_arg;
	public static MethodSpec void_decimal_ctor_long_arg;

	static TypeManager ()
	{
		Reset ();
	}

	static public void Reset ()
	{
//		object_type = null;
	
		// TODO: I am really bored by all this static stuff
		system_type_get_type_from_handle =
		bool_movenext_void =
		void_dispose_void =
		void_monitor_enter_object =
		void_monitor_exit_object =
		void_initializearray_array_fieldhandle =
		int_interlocked_compare_exchange =
		gen_interlocked_compare_exchange =
		methodbase_get_type_from_handle =
		methodbase_get_type_from_handle_generic =
		fieldinfo_get_field_from_handle =
		fieldinfo_get_field_from_handle_generic =
		activator_create_instance =
		delegate_combine_delegate_delegate =
		delegate_remove_delegate_delegate = null;

		int_get_offset_to_string_data =
		ienumerator_getcurrent = null;

		void_decimal_ctor_five_args =
		void_decimal_ctor_int_arg =
		void_decimal_ctor_long_arg = null;

		string_empty = null;

		typed_reference_type = arg_iterator_type = mbr_type =
		generic_ilist_type = generic_icollection_type = generic_ienumerator_type =
		generic_ienumerable_type = generic_nullable_type = expression_type = null;
	}

	/// <summary>
	///   Returns the C# name of a type if possible, or the full type name otherwise
	/// </summary>
	static public string CSharpName (TypeSpec t)
	{
		return t.GetSignatureForError ();
	}

	static public string CSharpName (IList<TypeSpec> types)
	{
		if (types.Count == 0)
			return string.Empty;

		StringBuilder sb = new StringBuilder ();
		for (int i = 0; i < types.Count; ++i) {
			if (i > 0)
				sb.Append (",");

			sb.Append (CSharpName (types [i]));
		}
		return sb.ToString ();
	}

	static public string GetFullNameSignature (MemberSpec mi)
	{
		return mi.GetSignatureForError ();
	}

	static public string CSharpSignature (MemberSpec mb)
	{
		return mb.GetSignatureForError ();
	}

	static MemberSpec GetPredefinedMember (TypeSpec t, MemberFilter filter, bool optional, Location loc)
	{
		var member = MemberCache.FindMember (t, filter, BindingRestriction.DeclaredOnly);

		if (member != null && member.IsAccessible (InternalType.FakeInternalType))
			return member;

		if (optional)
			return member;

		string method_args = null;
		if (filter.Parameters != null)
			method_args = filter.Parameters.GetSignatureForError ();

		RootContext.ToplevelTypes.Compiler.Report.Error (656, loc, "The compiler required member `{0}.{1}{2}' could not be found or is inaccessible",
			TypeManager.CSharpName (t), filter.Name, method_args);

		return null;
	}

	//
	// Returns the ConstructorInfo for "args"
	//
	public static MethodSpec GetPredefinedConstructor (TypeSpec t, Location loc, params TypeSpec [] args)
	{
		var pc = ParametersCompiled.CreateFullyResolved (args);
		return GetPredefinedMember (t, MemberFilter.Constructor (pc), false, loc) as MethodSpec;
	}

	//
	// Returns the method specification for a method named `name' defined
	// in type `t' which takes arguments of types `args'
	//
	public static MethodSpec GetPredefinedMethod (TypeSpec t, string name, Location loc, params TypeSpec [] args)
	{
		var pc = ParametersCompiled.CreateFullyResolved (args);
		return GetPredefinedMethod (t, MemberFilter.Method (name, 0, pc, null), false, loc);
	}

	public static MethodSpec GetPredefinedMethod (TypeSpec t, MemberFilter filter, Location loc)
	{
		return GetPredefinedMethod (t, filter, false, loc);
	}

	public static MethodSpec GetPredefinedMethod (TypeSpec t, MemberFilter filter, bool optional, Location loc)
	{
		return GetPredefinedMember (t, filter, optional, loc) as MethodSpec;
	}

	public static FieldSpec GetPredefinedField (TypeSpec t, string name, Location loc, TypeSpec type)
	{
		return GetPredefinedMember (t, MemberFilter.Field (name, type), false, loc) as FieldSpec;
	}

	public static PropertySpec GetPredefinedProperty (TypeSpec t, string name, Location loc, TypeSpec type)
	{
		return GetPredefinedMember (t, MemberFilter.Property (name, type), false, loc) as PropertySpec;
	}

	public static bool IsBuiltinType (TypeSpec t)
	{
		if (t == object_type || t == string_type || t == int32_type || t == uint32_type ||
		    t == int64_type || t == uint64_type || t == float_type || t == double_type ||
		    t == char_type || t == short_type || t == decimal_type || t == bool_type ||
		    t == sbyte_type || t == byte_type || t == ushort_type || t == void_type)
			return true;
		else
			return false;
	}

	//
	// This is like IsBuiltinType, but lacks decimal_type, we should also clean up
	// the pieces in the code where we use IsBuiltinType and special case decimal_type.
	// 
	public static bool IsPrimitiveType (TypeSpec t)
	{
		return (t == int32_type || t == uint32_type ||
		    t == int64_type || t == uint64_type || t == float_type || t == double_type ||
		    t == char_type || t == short_type || t == bool_type ||
		    t == sbyte_type || t == byte_type || t == ushort_type);
	}

	// Obsolete
	public static bool IsDelegateType (TypeSpec t)
	{
		return t.IsDelegate;
	}
	
	// Obsolete
	public static bool IsEnumType (TypeSpec t)
	{
		return t.IsEnum;
	}

	public static bool IsBuiltinOrEnum (TypeSpec t)
	{
		if (IsBuiltinType (t))
			return true;
		
		if (IsEnumType (t))
			return true;

		return false;
	}

	//
	// Whether a type is unmanaged.  This is used by the unsafe code (25.2)
	//
	public static bool IsUnmanagedType (TypeSpec t)
	{
		var ds = t.MemberDefinition as DeclSpace;
		if (ds != null)
			return ds.IsUnmanagedType ();

		// some builtins that are not unmanaged types
		if (t == TypeManager.object_type || t == TypeManager.string_type)
			return false;

		if (IsBuiltinOrEnum (t))
			return true;

		// Someone did the work of checking if the ElementType of t is unmanaged.  Let's not repeat it.
		if (t.IsPointer)
			return IsUnmanagedType (GetElementType (t));

		if (!IsValueType (t))
			return false;

		if (t.IsNested && t.DeclaringType.IsGenericOrParentIsGeneric)
			return false;

		return true;
	}

	//
	// Null is considered to be a reference type
	//			
	public static bool IsReferenceType (TypeSpec t)
	{
		if (t.IsGenericParameter)
			return ((TypeParameterSpec) t).IsReferenceType;

		return !t.IsStruct && !IsEnumType (t);
	}			
		
	public static bool IsValueType (TypeSpec t)
	{
		if (t.IsGenericParameter)
			return ((TypeParameterSpec) t).IsValueType;

		return t.IsStruct || IsEnumType (t);
	}

	public static bool IsStruct (TypeSpec t)
	{
		return t.IsStruct;
	}

	public static bool IsFamilyAccessible (TypeSpec type, TypeSpec parent)
	{
//		TypeParameter tparam = LookupTypeParameter (type);
//		TypeParameter pparam = LookupTypeParameter (parent);

		if (type.Kind == MemberKind.TypeParameter && parent.Kind == MemberKind.TypeParameter) { // (tparam != null) && (pparam != null)) {
			if (type == parent)
				return true;

			throw new NotImplementedException ("net");
//			return tparam.IsSubclassOf (parent);
		}

		do {
			if (IsInstantiationOfSameGenericType (type, parent))
				return true;

			type = type.BaseType;
		} while (type != null);

		return false;
	}

	//
	// Checks whether `type' is a subclass or nested child of `base_type'.
	//
	public static bool IsNestedFamilyAccessible (TypeSpec type, TypeSpec base_type)
	{
		do {
			if (IsFamilyAccessible (type, base_type))
				return true;

			// Handle nested types.
			type = type.DeclaringType;
		} while (type != null);

		return false;
	}

	//
	// Checks whether `type' is a nested child of `parent'.
	//
	public static bool IsNestedChildOf (TypeSpec type, ITypeDefinition parent)
	{
		if (type == null)
			return false;

		if (type.MemberDefinition == parent)
			return false;

		type = type.DeclaringType;
		while (type != null) {
			if (type.MemberDefinition == parent)
				return true;

			type = type.DeclaringType;
		}

		return false;
	}

	public static bool IsSpecialType (TypeSpec t)
	{
		return t == arg_iterator_type || t == typed_reference_type;
	}

	public static TypeSpec GetElementType (TypeSpec t)
	{
		return ((ElementTypeSpec)t).Element;
	}

	/// <summary>
	/// This method is not implemented by MS runtime for dynamic types
	/// </summary>
	public static bool HasElementType (TypeSpec t)
	{
		return t is ElementTypeSpec;
	}

	static NumberFormatInfo nf_provider = CultureInfo.CurrentCulture.NumberFormat;

	// This is a custom version of Convert.ChangeType() which works
	// with the TypeBuilder defined types when compiling corlib.
	public static object ChangeType (object value, TypeSpec targetType, out bool error)
	{
		IConvertible convert_value = value as IConvertible;
		
		if (convert_value == null){
			error = true;
			return null;
		}
		
		//
		// We cannot rely on build-in type conversions as they are
		// more limited than what C# supports.
		// See char -> float/decimal/double conversion
		//
		error = false;
		try {
			if (targetType == TypeManager.bool_type)
				return convert_value.ToBoolean (nf_provider);
			if (targetType == TypeManager.byte_type)
				return convert_value.ToByte (nf_provider);
			if (targetType == TypeManager.char_type)
				return convert_value.ToChar (nf_provider);
			if (targetType == TypeManager.short_type)
				return convert_value.ToInt16 (nf_provider);
			if (targetType == TypeManager.int32_type)
				return convert_value.ToInt32 (nf_provider);
			if (targetType == TypeManager.int64_type)
				return convert_value.ToInt64 (nf_provider);
			if (targetType == TypeManager.sbyte_type)
				return convert_value.ToSByte (nf_provider);

			if (targetType == TypeManager.decimal_type) {
				if (convert_value.GetType () == typeof (char))
					return (decimal) convert_value.ToInt32 (nf_provider);
				return convert_value.ToDecimal (nf_provider);
			}

			if (targetType == TypeManager.double_type) {
				if (convert_value.GetType () == typeof (char))
					return (double) convert_value.ToInt32 (nf_provider);
				return convert_value.ToDouble (nf_provider);
			}

			if (targetType == TypeManager.float_type) {
				if (convert_value.GetType () == typeof (char))
					return (float)convert_value.ToInt32 (nf_provider);
				return convert_value.ToSingle (nf_provider);
			}

			if (targetType == TypeManager.string_type)
				return convert_value.ToString (nf_provider);
			if (targetType == TypeManager.ushort_type)
				return convert_value.ToUInt16 (nf_provider);
			if (targetType == TypeManager.uint32_type)
				return convert_value.ToUInt32 (nf_provider);
			if (targetType == TypeManager.uint64_type)
				return convert_value.ToUInt64 (nf_provider);
			if (targetType == TypeManager.object_type)
				return value;

			error = true;
		} catch {
			error = true;
		}
		return null;
	}

	/// <summary>
	///   Utility function that can be used to probe whether a type
	///   is managed or not.  
	/// </summary>
	public static bool VerifyUnmanaged (CompilerContext ctx, TypeSpec t, Location loc)
	{
		while (t.IsPointer)
			t = GetElementType (t);

		if (IsUnmanagedType (t))
			return true;

		ctx.Report.SymbolRelatedToPreviousError (t);
		ctx.Report.Error (208, loc,
			"Cannot take the address of, get the size of, or declare a pointer to a managed type `{0}'",
			CSharpName (t));

		return false;	
	}
#region Generics
	// This method always return false for non-generic compiler,
	// while Type.IsGenericParameter is returned if it is supported.
	public static bool IsGenericParameter (TypeSpec type)
	{
		return type.IsGenericParameter;
	}

	public static bool IsGenericType (TypeSpec type)
	{
		return type.IsGeneric;
	}

	public static TypeSpec[] GetTypeArguments (TypeSpec t)
	{
		// TODO: return empty array !!
		return t.TypeArguments;
	}

	/// <summary>
	///   Check whether `type' and `parent' are both instantiations of the same
	///   generic type.  Note that we do not check the type parameters here.
	/// </summary>
	public static bool IsInstantiationOfSameGenericType (TypeSpec type, TypeSpec parent)
	{
		return type == parent || type.MemberDefinition == parent.MemberDefinition;
	}

	public static bool IsNullableType (TypeSpec t)
	{
		return generic_nullable_type == t.GetDefinition ();
	}
#endregion
}

}
