//
// attribute.cs: Attribute Handler
//
// Author: Ravi Pratap (ravi@ximian.com)
//         Marek Safar (marek.safar@seznam.cz)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
// Copyright 2003-2008 Novell, Inc.
//

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security; 
using System.Security.Permissions;
using System.Text;
using System.IO;

#if STATIC
using SecurityType = System.Collections.Generic.List<IKVM.Reflection.Emit.CustomAttributeBuilder>;
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using SecurityType = System.Collections.Generic.Dictionary<System.Security.Permissions.SecurityAction, System.Security.PermissionSet>;
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Mono.CSharp {

	/// <summary>
	///   Base class for objects that can have Attributes applied to them.
	/// </summary>
	public abstract class Attributable {
		//
		// Holds all attributes attached to this element
		//
 		protected Attributes attributes;

		public void AddAttributes (Attributes attrs, IMemberContext context)
		{
			if (attrs == null)
				return;

			if (attributes == null)
				attributes = attrs;
			else
				throw new NotImplementedException ();

			attributes.AttachTo (this, context);
		}

		public Attributes OptAttributes {
			get {
				return attributes;
			}
			set {
				attributes = value;
			}
		}

		/// <summary>
		/// Use member-specific procedure to apply attribute @a in @cb to the entity being built in @builder
		/// </summary>
		public abstract void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa);

		/// <summary>
		/// Returns one AttributeTarget for this element.
		/// </summary>
		public abstract AttributeTargets AttributeTargets { get; }

		public abstract bool IsClsComplianceRequired ();

		/// <summary>
		/// Gets list of valid attribute targets for explicit target declaration.
		/// The first array item is default target. Don't break this rule.
		/// </summary>
		public abstract string[] ValidAttributeTargets { get; }
	};

	public class Attribute : Expression
	{
		public readonly string ExplicitTarget;
		public AttributeTargets Target;
		readonly ATypeNameExpression expression;

		public Arguments PosArguments;
		public Arguments NamedArguments;

		bool resolve_error;
		bool arg_resolved;
		readonly bool nameEscaped;

		//
		// An attribute can be attached to multiple targets (e.g. multiple fields)
		//
		protected Attributable[] targets;

		//
		// A member context for the attribute, it's much easier to hold it here
		// than trying to pull it during resolve
		//
		IMemberContext context;

		public static readonly AttributeUsageAttribute DefaultUsageAttribute = new AttributeUsageAttribute (AttributeTargets.All);
		static Assembly orig_sec_assembly;
		public static readonly object[] EmptyObject = new object [0];

		List<KeyValuePair<MemberExpr, NamedArgument>> named_values;

		// Cache for parameter-less attributes
		static Dictionary<TypeSpec, MethodSpec> att_cache;

		public Attribute (string target, ATypeNameExpression expr, Arguments[] args, Location loc, bool nameEscaped)
		{
			this.expression = expr;
			if (args != null) {
				PosArguments = args [0];
				NamedArguments = args [1];				
			}
			this.loc = loc;
			ExplicitTarget = target;
			this.nameEscaped = nameEscaped;
		}

		void AddModuleCharSet (ResolveContext rc)
		{
			const string dll_import_char_set = "CharSet";

			//
			// Only when not customized by user
			//
			if (HasField (dll_import_char_set))
				return;

			if (!rc.Module.PredefinedTypes.CharSet.IsDefined) {
				return;
			}

			if (NamedArguments == null)
				NamedArguments = new Arguments (1);

			var value = Constant.CreateConstant (rc, rc.Module.PredefinedTypes.CharSet.TypeSpec, rc.Module.DefaultCharSet, Location);
			NamedArguments.Add (new NamedArgument (dll_import_char_set, loc, value));
		}

		public Attribute Clone ()
		{
			Attribute a = new Attribute (ExplicitTarget, expression, null, loc, nameEscaped);
			a.PosArguments = PosArguments;
			a.NamedArguments = NamedArguments;
			return a;
		}

		static Attribute ()
		{
			Reset ();
		}

		public static void Reset ()
		{
			att_cache = new Dictionary<TypeSpec, MethodSpec> ();
		}

		//
		// When the same attribute is attached to multiple fiels
		// we use @target field as a list of targets. The attribute
		// has to be resolved only once but emitted for each target.
		//
		public void AttachTo (Attributable target, IMemberContext context)
		{
			if (this.targets == null) {
				this.targets = new Attributable[] { target };
				this.context = context;
				return;
			}

			// Resize target array
			Attributable[] new_array = new Attributable [this.targets.Length + 1];
			targets.CopyTo (new_array, 0);
			new_array [targets.Length] = target;
			this.targets = new_array;

			// No need to update context, different targets cannot have
			// different contexts, it's enough to remove same attributes
			// from secondary members.

			target.OptAttributes = null;
		}

		public ResolveContext CreateResolveContext ()
		{
			return new ResolveContext (context, ResolveContext.Options.ConstantScope);
		}

		static void Error_InvalidNamedArgument (ResolveContext rc, NamedArgument name)
		{
			rc.Report.Error (617, name.Location, "`{0}' is not a valid named attribute argument. Named attribute arguments " +
				      "must be fields which are not readonly, static, const or read-write properties which are " +
				      "public and not static",
			      name.Name);
		}

		static void Error_InvalidNamedArgumentType (ResolveContext rc, NamedArgument name)
		{
			rc.Report.Error (655, name.Location,
				"`{0}' is not a valid named attribute argument because it is not a valid attribute parameter type",
				name.Name);
		}

		public static void Error_AttributeArgumentIsDynamic (IMemberContext context, Location loc)
		{
			context.Compiler.Report.Error (1982, loc, "An attribute argument cannot be dynamic expression");
		}
		
		public void Error_MissingGuidAttribute ()
		{
			Report.Error (596, Location, "The Guid attribute must be specified with the ComImport attribute");
		}

		public void Error_MisusedExtensionAttribute ()
		{
			Report.Error (1112, Location, "Do not use `{0}' directly. Use parameter modifier `this' instead", GetSignatureForError ());
		}

		public void Error_MisusedDynamicAttribute ()
		{
			Report.Error (1970, loc, "Do not use `{0}' directly. Use `dynamic' keyword instead", GetSignatureForError ());
		}

		/// <summary>
		/// This is rather hack. We report many emit attribute error with same error to be compatible with
		/// csc. But because csc has to report them this way because error came from ilasm we needn't.
		/// </summary>
		public void Error_AttributeEmitError (string inner)
		{
			Report.Error (647, Location, "Error during emitting `{0}' attribute. The reason is `{1}'",
				      TypeManager.CSharpName (Type), inner);
		}

		public void Error_InvalidSecurityParent ()
		{
			Error_AttributeEmitError ("it is attached to invalid parent");
		}

		Attributable Owner {
			get {
				return targets [0];
			}
		}

		protected virtual TypeExpr ResolveAsTypeTerminal (Expression expr, IMemberContext ec)
		{
			return expr.ResolveAsTypeTerminal (ec, false);
		}

		TypeSpec ResolvePossibleAttributeType (ATypeNameExpression expr, ref bool is_attr)
		{
			TypeExpr te = ResolveAsTypeTerminal (expr, context);
			if (te == null)
				return null;

			TypeSpec t = te.Type;
			if (t.IsAttribute) {
				is_attr = true;
			} else {
				Report.SymbolRelatedToPreviousError (t);
				Report.Error (616, Location, "`{0}': is not an attribute class", TypeManager.CSharpName (t));
			}
			return t;
		}

		/// <summary>
		///   Tries to resolve the type of the attribute. Flags an error if it can't, and complain is true.
		/// </summary>
		void ResolveAttributeType ()
		{
			SessionReportPrinter resolve_printer = new SessionReportPrinter ();
			ReportPrinter prev_recorder = context.Compiler.Report.SetPrinter (resolve_printer);

			bool t1_is_attr = false;
			bool t2_is_attr = false;
			TypeSpec t1, t2;
			ATypeNameExpression expanded = null;

			try {
				t1 = ResolvePossibleAttributeType (expression, ref t1_is_attr);

				if (nameEscaped) {
					t2 = null;
				} else {
					expanded = (ATypeNameExpression) expression.Clone (null);
					expanded.Name += "Attribute";

					t2 = ResolvePossibleAttributeType (expanded, ref t2_is_attr);
				}

				resolve_printer.EndSession ();
			} finally {
				context.Compiler.Report.SetPrinter (prev_recorder);
			}

			if (t1_is_attr && t2_is_attr) {
				Report.Error (1614, Location, "`{0}' is ambiguous between `{1}' and `{2}'. Use either `@{0}' or `{0}Attribute'",
					GetSignatureForError (), expression.GetSignatureForError (), expanded.GetSignatureForError ());
				resolve_error = true;
				return;
			}

			if (t1_is_attr) {
				Type = t1;
				return;
			}

			if (t2_is_attr) {
				Type = t2;
				return;
			}

			resolve_printer.Merge (prev_recorder);
			resolve_error = true;
		}

		public virtual TypeSpec ResolveType ()
		{
			if (Type == null && !resolve_error)
				ResolveAttributeType ();
			return Type;
		}

		public override string GetSignatureForError ()
		{
			if (Type != null)
				return TypeManager.CSharpName (Type);

			return expression.GetSignatureForError ();
		}

		public bool HasSecurityAttribute {
			get {
				PredefinedAttribute pa = context.Module.PredefinedAttributes.Security;
				return pa.IsDefined && TypeSpec.IsBaseClass (type, pa.TypeSpec, false);
			}
		}

		public bool IsValidSecurityAttribute ()
		{
			return HasSecurityAttribute && IsSecurityActionValid (false);
		}

		static bool IsValidArgumentType (TypeSpec t)
		{
			if (t.IsArray)
				t = TypeManager.GetElementType (t);

			return t == TypeManager.string_type ||
				TypeManager.IsPrimitiveType (t) ||
				TypeManager.IsEnumType (t) ||
				t == TypeManager.object_type ||
				t == TypeManager.type_type;
		}

		// TODO: Don't use this ambiguous value
		public string Name {
			get { return expression.Name; }
		}

		public Report Report {
			get { return context.Compiler.Report; }
		}

		public MethodSpec Resolve ()
		{
			if (resolve_error)
				return null;

			resolve_error = true;
			arg_resolved = true;

			if (Type == null) {
				ResolveAttributeType ();
				if (Type == null)
					return null;
			}

			if (Type.IsAbstract) {
				Report.Error (653, Location, "Cannot apply attribute class `{0}' because it is abstract", GetSignatureForError ());
				return null;
			}

			ObsoleteAttribute obsolete_attr = Type.GetAttributeObsolete ();
			if (obsolete_attr != null) {
				AttributeTester.Report_ObsoleteMessage (obsolete_attr, TypeManager.CSharpName (Type), Location, Report);
			}

			MethodSpec ctor;
			// Try if the attribute is simple has been resolved before
			if (PosArguments == null && NamedArguments == null) {
				if (att_cache.TryGetValue (Type, out ctor)) {
					resolve_error = false;
					return ctor;
				}
			}

			ResolveContext rc = CreateResolveContext ();
			ctor = ResolveConstructor (rc);
			if (ctor == null) {
				return null;
			}

			//
			// Add [module: DefaultCharSet] to all DllImport import attributes
			//
			var module = context.Module;
			if (Type == module.PredefinedAttributes.DllImport && module.HasDefaultCharSet) {
				AddModuleCharSet (rc);
			}

			if (NamedArguments != null && !ResolveNamedArguments (rc)) {
				return null;
			}

			resolve_error = false;
			return ctor;
		}

		protected virtual MethodSpec ResolveConstructor (ResolveContext ec)
		{
			if (PosArguments != null) {
				bool dynamic;
				PosArguments.Resolve (ec, out dynamic);
				if (dynamic) {
					Error_AttributeArgumentIsDynamic (ec.MemberContext, loc);
					return null;
				}
			}

			return ConstructorLookup (ec, Type, ref PosArguments, loc);
		}

		protected virtual bool ResolveNamedArguments (ResolveContext ec)
		{
			int named_arg_count = NamedArguments.Count;
			var seen_names = new List<string> (named_arg_count);

			named_values = new List<KeyValuePair<MemberExpr, NamedArgument>> (named_arg_count);
			
			foreach (NamedArgument a in NamedArguments) {
				string name = a.Name;
				if (seen_names.Contains (name)) {
					ec.Report.Error (643, a.Location, "Duplicate named attribute `{0}' argument", name);
					continue;
				}			
	
				seen_names.Add (name);

				a.Resolve (ec);

				Expression member = Expression.MemberLookup (ec, ec.CurrentType, Type, name, 0, MemberLookupRestrictions.ExactArity, loc);

				if (member == null) {
					member = Expression.MemberLookup (null, ec.CurrentType, Type, name, 0, MemberLookupRestrictions.ExactArity, loc);

					if (member != null) {
						// TODO: ec.Report.SymbolRelatedToPreviousError (member);
						Expression.ErrorIsInaccesible (ec, member.GetSignatureForError (), loc);
						return false;
					}
				}

				if (member == null){
					Expression.Error_TypeDoesNotContainDefinition (ec, Location, Type, name);
					return false;
				}
				
				if (!(member is PropertyExpr || member is FieldExpr)) {
					Error_InvalidNamedArgument (ec, a);
					return false;
				}

				ObsoleteAttribute obsolete_attr;

				if (member is PropertyExpr) {
					var pi = ((PropertyExpr) member).PropertyInfo;

					if (!pi.HasSet || !pi.HasGet || pi.IsStatic || !pi.Get.IsPublic || !pi.Set.IsPublic) {
						ec.Report.SymbolRelatedToPreviousError (pi);
						Error_InvalidNamedArgument (ec, a);
						return false;
					}

					if (!IsValidArgumentType (member.Type)) {
						ec.Report.SymbolRelatedToPreviousError (pi);
						Error_InvalidNamedArgumentType (ec, a);
						return false;
					}

					obsolete_attr = pi.GetAttributeObsolete ();
					pi.MemberDefinition.SetIsAssigned ();
				} else {
					var fi = ((FieldExpr) member).Spec;

					if (fi.IsReadOnly || fi.IsStatic || !fi.IsPublic) {
						Error_InvalidNamedArgument (ec, a);
						return false;
					}

					if (!IsValidArgumentType (member.Type)) {
						ec.Report.SymbolRelatedToPreviousError (fi);
						Error_InvalidNamedArgumentType (ec, a);
						return false;
					}

					obsolete_attr = fi.GetAttributeObsolete ();
					fi.MemberDefinition.SetIsAssigned ();
				}

				if (obsolete_attr != null && !context.IsObsolete)
					AttributeTester.Report_ObsoleteMessage (obsolete_attr, member.GetSignatureForError (), member.Location, Report);

				if (a.Type != member.Type) {
					a.Expr = Convert.ImplicitConversionRequired (ec, a.Expr, member.Type, a.Expr.Location);
				}

				if (a.Expr != null)
					named_values.Add (new KeyValuePair<MemberExpr, NamedArgument> ((MemberExpr) member, a));
			}

			return true;
		}

		/// <summary>
		///   Get a string containing a list of valid targets for the attribute 'attr'
		/// </summary>
		public string GetValidTargets ()
		{
			StringBuilder sb = new StringBuilder ();
			AttributeTargets targets = Type.GetAttributeUsage (context.Module.PredefinedAttributes.AttributeUsage).ValidOn;

			if ((targets & AttributeTargets.Assembly) != 0)
				sb.Append ("assembly, ");

			if ((targets & AttributeTargets.Module) != 0)
				sb.Append ("module, ");

			if ((targets & AttributeTargets.Class) != 0)
				sb.Append ("class, ");

			if ((targets & AttributeTargets.Struct) != 0)
				sb.Append ("struct, ");

			if ((targets & AttributeTargets.Enum) != 0)
				sb.Append ("enum, ");

			if ((targets & AttributeTargets.Constructor) != 0)
				sb.Append ("constructor, ");

			if ((targets & AttributeTargets.Method) != 0)
				sb.Append ("method, ");

			if ((targets & AttributeTargets.Property) != 0)
				sb.Append ("property, indexer, ");

			if ((targets & AttributeTargets.Field) != 0)
				sb.Append ("field, ");

			if ((targets & AttributeTargets.Event) != 0)
				sb.Append ("event, ");

			if ((targets & AttributeTargets.Interface) != 0)
				sb.Append ("interface, ");

			if ((targets & AttributeTargets.Parameter) != 0)
				sb.Append ("parameter, ");

			if ((targets & AttributeTargets.Delegate) != 0)
				sb.Append ("delegate, ");

			if ((targets & AttributeTargets.ReturnValue) != 0)
				sb.Append ("return, ");

			if ((targets & AttributeTargets.GenericParameter) != 0)
				sb.Append ("type parameter, ");

			return sb.Remove (sb.Length - 2, 2).ToString ();
		}

		public AttributeUsageAttribute GetAttributeUsageAttribute ()
		{
			if (!arg_resolved)
				// TODO: It is not neccessary to call whole Resolve (ApplyAttribute does it now) we need only ctor args.
				// But because a lot of attribute class code must be rewritten will be better to wait...
				Resolve ();

			if (resolve_error)
				return DefaultUsageAttribute;

			AttributeUsageAttribute usage_attribute = new AttributeUsageAttribute ((AttributeTargets)((Constant) PosArguments [0].Expr).GetValue ());

			var field = GetNamedValue ("AllowMultiple") as BoolConstant;
			if (field != null)
				usage_attribute.AllowMultiple = field.Value;

			field = GetNamedValue ("Inherited") as BoolConstant;
			if (field != null)
				usage_attribute.Inherited = field.Value;

			return usage_attribute;
		}

		/// <summary>
		/// Returns custom name of indexer
		/// </summary>
		public string GetIndexerAttributeValue ()
		{
			if (!arg_resolved)
				// TODO: It is not neccessary to call whole Resolve (ApplyAttribute does it now) we need only ctor args.
				// But because a lot of attribute class code must be rewritten will be better to wait...
				Resolve ();

			if (resolve_error)
				return null;

			return ((Constant) PosArguments [0].Expr).GetValue () as string;
		}

		/// <summary>
		/// Returns condition of ConditionalAttribute
		/// </summary>
		public string GetConditionalAttributeValue ()
		{
			if (!arg_resolved)
				// TODO: It is not neccessary to call whole Resolve (ApplyAttribute does it now) we need only ctor args.
				// But because a lot of attribute class code must be rewritten will be better to wait...
				Resolve ();

			if (resolve_error)
				return null;

			return ((Constant) PosArguments[0].Expr).GetValue () as string;
		}

		/// <summary>
		/// Creates the instance of ObsoleteAttribute from this attribute instance
		/// </summary>
		public ObsoleteAttribute GetObsoleteAttribute ()
		{
			if (!arg_resolved) {
				// corlib only case when obsolete is used before is resolved
				var c = type.MemberDefinition as Class;
				if (c != null && !c.HasMembersDefined)
					c.Define ();
				
				// TODO: It is not neccessary to call whole Resolve (ApplyAttribute does it now) we need only ctor args.
				// But because a lot of attribute class code must be rewritten will be better to wait...
				Resolve ();
			}

			if (resolve_error)
				return null;

			if (PosArguments == null)
				return new ObsoleteAttribute ();

			string msg = ((Constant) PosArguments[0].Expr).GetValue () as string;
			if (PosArguments.Count == 1)
				return new ObsoleteAttribute (msg);

			return new ObsoleteAttribute (msg, ((BoolConstant) PosArguments[1].Expr).Value);
		}

		/// <summary>
		/// Returns value of CLSCompliantAttribute contructor parameter but because the method can be called
		/// before ApplyAttribute. We need to resolve the arguments.
		/// This situation occurs when class deps is differs from Emit order.  
		/// </summary>
		public bool GetClsCompliantAttributeValue ()
		{
			if (!arg_resolved)
				// TODO: It is not neccessary to call whole Resolve (ApplyAttribute does it now) we need only ctor args.
				// But because a lot of attribute class code must be rewritten will be better to wait...
				Resolve ();

			if (resolve_error)
				return false;

			return ((BoolConstant) PosArguments[0].Expr).Value;
		}

		public TypeSpec GetCoClassAttributeValue ()
		{
			if (!arg_resolved)
				Resolve ();

			if (resolve_error)
				return null;

			return GetArgumentType ();
		}

		public bool CheckTarget ()
		{
			string[] valid_targets = Owner.ValidAttributeTargets;
			if (ExplicitTarget == null || ExplicitTarget == valid_targets [0]) {
				Target = Owner.AttributeTargets;
				return true;
			}

			// TODO: we can skip the first item
			if (Array.Exists (valid_targets, i => i == ExplicitTarget)) {
				switch (ExplicitTarget) {
				case "return": Target = AttributeTargets.ReturnValue; return true;
				case "param": Target = AttributeTargets.Parameter; return true;
				case "field": Target = AttributeTargets.Field; return true;
				case "method": Target = AttributeTargets.Method; return true;
				case "property": Target = AttributeTargets.Property; return true;
				case "module": Target = AttributeTargets.Module; return true;
				}
				throw new InternalErrorException ("Unknown explicit target: " + ExplicitTarget);
			}
				
			StringBuilder sb = new StringBuilder ();
			foreach (string s in valid_targets) {
				sb.Append (s);
				sb.Append (", ");
			}
			sb.Remove (sb.Length - 2, 2);
			Report.Error (657, Location, "`{0}' is not a valid attribute location for this declaration. " +
				"Valid attribute locations for this declaration are `{1}'", ExplicitTarget, sb.ToString ());
			return false;
		}

		/// <summary>
		/// Tests permitted SecurityAction for assembly or other types
		/// </summary>
		protected virtual bool IsSecurityActionValid (bool for_assembly)
		{
			SecurityAction action = GetSecurityActionValue ();

			switch (action) {
#pragma warning disable 618
			case SecurityAction.Demand:
			case SecurityAction.Assert:
			case SecurityAction.Deny:
			case SecurityAction.PermitOnly:
			case SecurityAction.LinkDemand:
			case SecurityAction.InheritanceDemand:
				if (!for_assembly)
					return true;
				break;

			case SecurityAction.RequestMinimum:
			case SecurityAction.RequestOptional:
			case SecurityAction.RequestRefuse:
				if (for_assembly)
					return true;
				break;
#pragma warning restore 618

			default:
				Error_AttributeEmitError ("SecurityAction is out of range");
				return false;
			}

			Error_AttributeEmitError (String.Concat ("SecurityAction `", action, "' is not valid for this declaration"));
			return false;
		}

		System.Security.Permissions.SecurityAction GetSecurityActionValue ()
		{
			return (SecurityAction) ((Constant) PosArguments[0].Expr).GetValue ();
		}

		/// <summary>
		/// Creates instance of SecurityAttribute class and add result of CreatePermission method to permission table.
		/// </summary>
		/// <returns></returns>
		public void ExtractSecurityPermissionSet (MethodSpec ctor, ref SecurityType permissions)
		{
#if STATIC
			object[] values = new object [PosArguments.Count];
			for (int i = 0; i < values.Length; ++i)
				values [i] = ((Constant) PosArguments [i].Expr).GetValue ();

			PropertyInfo[] prop;
			object[] prop_values;
			if (named_values == null) {
				prop = null;
				prop_values = null;
			} else {
				prop = new PropertyInfo[named_values.Count];
				prop_values = new object [named_values.Count];
				for (int i = 0; i < prop.Length; ++i) {
					prop [i] = ((PropertyExpr) named_values [i].Key).PropertyInfo.MetaInfo;
					prop_values [i] = ((Constant) named_values [i].Value.Expr).GetValue ();
				}
			}

			if (permissions == null)
				permissions = new SecurityType ();

			var cab = new CustomAttributeBuilder ((ConstructorInfo) ctor.GetMetaInfo (), values, prop, prop_values);
			permissions.Add (cab);
#else
			Type orig_assembly_type = null;

			if (Type.MemberDefinition is TypeContainer) {
				if (!RootContext.StdLib) {
					orig_assembly_type = System.Type.GetType (Type.GetMetaInfo ().FullName);
				} else {
					string orig_version_path = Environment.GetEnvironmentVariable ("__SECURITY_BOOTSTRAP_DB");
					if (orig_version_path == null) {
						Error_AttributeEmitError ("security custom attributes can not be referenced from defining assembly");
						return;
					}

					if (orig_sec_assembly == null) {
						string file = Path.Combine (orig_version_path, Path.GetFileName (RootContext.OutputFile));
						orig_sec_assembly = Assembly.LoadFile (file);
					}

					orig_assembly_type = orig_sec_assembly.GetType (Type.GetMetaInfo ().FullName, true);
					if (orig_assembly_type == null) {
						Report.Warning (-112, 1, Location, "Self-referenced security attribute `{0}' " +
								"was not found in previous version of assembly");
						return;
					}
				}
			}

			SecurityAttribute sa;
			object[] args;

			// For all non-selfreferencing security attributes we can avoid all hacks
			if (orig_assembly_type == null) {
				args = new object[PosArguments.Count];
				for (int j = 0; j < args.Length; ++j) {
					args[j] = ((Constant) PosArguments[j].Expr).GetValue ();
				}

				sa = (SecurityAttribute) Activator.CreateInstance (Type.GetMetaInfo (), args);

				if (named_values != null) {
					for (int i = 0; i < named_values.Count; ++i) {
						PropertyInfo pi = ((PropertyExpr) named_values[i].Key).PropertyInfo.MetaInfo;
						pi.SetValue (sa, ((Constant) named_values [i].Value.Expr).GetValue (), null);
					}
				}
			} else {
				// HACK: All security attributes have same ctor syntax
				args = new object[] { GetSecurityActionValue () };
				sa = (SecurityAttribute) Activator.CreateInstance (orig_assembly_type, args);

				// All types are from newly created assembly but for invocation with old one we need to convert them
				if (named_values != null) {
					for (int i = 0; i < named_values.Count; ++i) {
						PropertyInfo emited_pi = ((PropertyExpr) named_values[i].Key).PropertyInfo.MetaInfo;
						// FIXME: We are missing return type filter
						// TODO: pi can be null
						PropertyInfo pi = orig_assembly_type.GetProperty (emited_pi.Name);

						pi.SetValue (sa, ((Constant) named_values[i].Value.Expr).GetValue (), null);
					}
				}
			}

			IPermission perm;
			perm = sa.CreatePermission ();
			SecurityAction action = (SecurityAction) args [0];

			// IS is correct because for corlib we are using an instance from old corlib
			if (!(perm is System.Security.CodeAccessPermission)) {
				switch (action) {
				case SecurityAction.Demand:
					action = (SecurityAction)13;
					break;
				case SecurityAction.LinkDemand:
					action = (SecurityAction)14;
					break;
				case SecurityAction.InheritanceDemand:
					action = (SecurityAction)15;
					break;
				}
			}

			if (permissions == null)
				permissions = new SecurityType ();

			PermissionSet ps;
			if (!permissions.TryGetValue (action, out ps)) {
				if (sa is PermissionSetAttribute)
					ps = new PermissionSet (sa.Unrestricted ? PermissionState.Unrestricted : PermissionState.None);
				else
					ps = new PermissionSet (PermissionState.None);

				permissions.Add (action, ps);
			} else if (!ps.IsUnrestricted () && (sa is PermissionSetAttribute) && sa.Unrestricted) {
				ps = ps.Union (new PermissionSet (PermissionState.Unrestricted));
				permissions [action] = ps;
			}
			ps.AddPermission (perm);
#endif
		}

		public Constant GetNamedValue (string name)
		{
			if (named_values == null)
				return null;

			for (int i = 0; i < named_values.Count; ++i) {
				if (named_values [i].Value.Name == name)
					return named_values [i].Value.Expr as Constant;
			}

			return null;
		}

		public CharSet GetCharSetValue ()
		{
			return (CharSet)System.Enum.Parse (typeof (CharSet), ((Constant) PosArguments [0].Expr).GetValue ().ToString ());
		}

		public bool HasField (string fieldName)
		{
			if (named_values == null)
				return false;

			foreach (var na in named_values) {
				if (na.Value.Name == fieldName)
					return true;
			}

			return false;
		}

		//
		// Returns true for MethodImplAttribute with MethodImplOptions.InternalCall value
		// 
		public bool IsInternalCall ()
		{
			MethodImplOptions options = 0;
			if (PosArguments.Count == 1) {
				options = (MethodImplOptions) System.Enum.Parse (typeof (MethodImplOptions), ((Constant) PosArguments[0].Expr).GetValue ().ToString ());
			} else if (HasField ("Value")) {
				var named = GetNamedValue ("Value");
				options = (MethodImplOptions) System.Enum.Parse (typeof (MethodImplOptions), named.GetValue ().ToString ());
			}

			return (options & MethodImplOptions.InternalCall) != 0;
		}

		//
		// Returns true for StructLayoutAttribute with LayoutKind.Explicit value
		// 
		public bool IsExplicitLayoutKind ()
		{
			if (PosArguments == null || PosArguments.Count != 1)
				return false;

			var value = (LayoutKind) System.Enum.Parse (typeof (LayoutKind), ((Constant) PosArguments[0].Expr).GetValue ().ToString ());
			return value == LayoutKind.Explicit;
		}

		public Expression GetParameterDefaultValue ()
		{
			if (PosArguments == null)
				return null;

			return PosArguments[0].Expr;
		}

		public override bool Equals (object obj)
		{
			Attribute a = obj as Attribute;
			if (a == null)
				return false;

			return Type == a.Type && Target == a.Target;
		}

		public override int GetHashCode ()
		{
			return type.GetHashCode () ^ Target.GetHashCode ();
		}

		/// <summary>
		/// Emit attribute for Attributable symbol
		/// </summary>
		public void Emit (Dictionary<Attribute, List<Attribute>> allEmitted)
		{
			var ctor = Resolve ();
			if (ctor == null)
				return;

			var predefined = context.Module.PredefinedAttributes;

			AttributeUsageAttribute usage_attr = Type.GetAttributeUsage (predefined.AttributeUsage);
			if ((usage_attr.ValidOn & Target) == 0) {
				Report.Error (592, Location, "The attribute `{0}' is not valid on this declaration type. " +
					      "It is valid on `{1}' declarations only",
					GetSignatureForError (), GetValidTargets ());
				return;
			}

			byte[] cdata;
			if (PosArguments == null && named_values == null) {
				cdata = AttributeEncoder.Empty;
			} else {
				AttributeEncoder encoder = new AttributeEncoder ();

				if (PosArguments != null) {
					var param_types = ctor.Parameters.Types;
					for (int j = 0; j < PosArguments.Count; ++j) {
						var pt = param_types[j];
						var arg_expr = PosArguments[j].Expr;
						if (j == 0) {
							if (Type == predefined.IndexerName || Type == predefined.Conditional) {
								string v = ((StringConstant) arg_expr).Value;
								if (!Tokenizer.IsValidIdentifier (v) || Tokenizer.IsKeyword (v)) {
									context.Compiler.Report.Error (633, arg_expr.Location,
										"The argument to the `{0}' attribute must be a valid identifier", GetSignatureForError ());
								}
							} else if (Type == predefined.Guid) {
								try {
									string v = ((StringConstant) arg_expr).Value;
									new Guid (v);
								} catch (Exception e) {
									Error_AttributeEmitError (e.Message);
									return;
								}
							} else if (Type == predefined.AttributeUsage) {
								int v = ((IntConstant) ((EnumConstant) arg_expr).Child).Value;
								if (v == 0) {
									context.Compiler.Report.Error (591, Location, "Invalid value for argument to `{0}' attribute",
										"System.AttributeUsage");
								}
							} else if (Type == predefined.MarshalAs) {
								if (PosArguments.Count == 1) {
									var u_type = (UnmanagedType) System.Enum.Parse (typeof (UnmanagedType), ((Constant) PosArguments[0].Expr).GetValue ().ToString ());
									if (u_type == UnmanagedType.ByValArray && !(Owner is FieldBase)) {
										Error_AttributeEmitError ("Specified unmanaged type is only valid on fields");
									}
								}
							} else if (Type == predefined.DllImport) {
								if (PosArguments.Count == 1) {
									var value = ((Constant) PosArguments[0].Expr).GetValue () as string;
									if (string.IsNullOrEmpty (value))
										Error_AttributeEmitError ("DllName cannot be empty");
								}
							} else if (Type == predefined.MethodImpl && pt == TypeManager.short_type &&
								!System.Enum.IsDefined (typeof (MethodImplOptions), ((Constant) arg_expr).GetValue ().ToString ())) {
								Error_AttributeEmitError ("Incorrect argument value.");
								return;
							}
						}

						arg_expr.EncodeAttributeValue (context, encoder, pt);
					}
				}

				if (named_values != null) {
					encoder.Encode ((ushort) named_values.Count);
					foreach (var na in named_values) {
						if (na.Key is FieldExpr)
							encoder.Encode ((byte) 0x53);
						else
							encoder.Encode ((byte) 0x54);

						encoder.Encode (na.Key.Type);
						encoder.Encode (na.Value.Name);
						na.Value.Expr.EncodeAttributeValue (context, encoder, na.Key.Type);
					}
				} else {
					encoder.EncodeEmptyNamedArguments ();
				}

				cdata = encoder.ToArray ();
			}

			try {
				foreach (Attributable target in targets)
					target.ApplyAttributeBuilder (this, ctor, cdata, predefined);
			} catch (Exception e) {
				Error_AttributeEmitError (e.Message);
				return;
			}

			if (!usage_attr.AllowMultiple && allEmitted != null) {
				if (allEmitted.ContainsKey (this)) {
					var a = allEmitted [this];
					if (a == null) {
						a = new List<Attribute> (2);
						allEmitted [this] = a;
					}
					a.Add (this);
				} else {
					allEmitted.Add (this, null);
				}
			}

			if (!RootContext.VerifyClsCompliance)
				return;

			// Here we are testing attribute arguments for array usage (error 3016)
			if (Owner.IsClsComplianceRequired ()) {
				if (PosArguments != null)
					PosArguments.CheckArrayAsAttribute (context.Compiler);
			
				if (NamedArguments == null)
					return;

				NamedArguments.CheckArrayAsAttribute (context.Compiler);
			}
		}

		private Expression GetValue () 
		{
			if (PosArguments == null || PosArguments.Count < 1)
				return null;

			return PosArguments [0].Expr;
		}

		public string GetString () 
		{
			Expression e = GetValue ();
			if (e is StringConstant)
				return ((StringConstant)e).Value;
			return null;
		}

		public bool GetBoolean () 
		{
			Expression e = GetValue ();
			if (e is BoolConstant)
				return ((BoolConstant)e).Value;
			return false;
		}

		public TypeSpec GetArgumentType ()
		{
			TypeOf e = GetValue () as TypeOf;
			if (e == null)
				return null;
			return e.TypeArgument;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new NotSupportedException ("ET");
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			throw new NotImplementedException ();
		}

		public override void Emit (EmitContext ec)
		{
			throw new NotImplementedException ();
		}
	}
	

	/// <summary>
	/// For global attributes (assembly, module) we need special handling.
	/// Attributes can be located in the several files
	/// </summary>
	public class GlobalAttribute : Attribute
	{
		public readonly NamespaceEntry ns;

		public GlobalAttribute (NamespaceEntry ns, string target, ATypeNameExpression expression,
					Arguments[] args, Location loc, bool nameEscaped):
			base (target, expression, args, loc, nameEscaped)
		{
			this.ns = ns;
		}

		void Enter ()
		{
			// RootContext.ToplevelTypes has a single NamespaceEntry which gets overwritten
			// each time a new file is parsed.  However, we need to use the NamespaceEntry
			// in effect where the attribute was used.  Since code elsewhere cannot assume
			// that the NamespaceEntry is right, just overwrite it.
			//
			// Precondition: RootContext.ToplevelTypes == null

			if (RootContext.ToplevelTypes.NamespaceEntry != null)
				throw new InternalErrorException (Location + " non-null NamespaceEntry");

			RootContext.ToplevelTypes.NamespaceEntry = ns;
		}

		protected override bool IsSecurityActionValid (bool for_assembly)
		{
			return base.IsSecurityActionValid (true);
		}

		void Leave ()
		{
			RootContext.ToplevelTypes.NamespaceEntry = null;
		}

		protected override TypeExpr ResolveAsTypeTerminal (Expression expr, IMemberContext ec)
		{
			try {
				Enter ();
				return base.ResolveAsTypeTerminal (expr, ec);
			}
			finally {
				Leave ();
			}
		}

		protected override MethodSpec ResolveConstructor (ResolveContext ec)
		{
			try {
				Enter ();
				return base.ResolveConstructor (ec);
			}
			finally {
				Leave ();
			}
		}

		protected override bool ResolveNamedArguments (ResolveContext ec)
		{
			try {
				Enter ();
				return base.ResolveNamedArguments (ec);
			}
			finally {
				Leave ();
			}
		}
	}

	public class Attributes {
		public readonly List<Attribute> Attrs;

		public Attributes (Attribute a)
		{
			Attrs = new List<Attribute> ();
			Attrs.Add (a);
		}

		public Attributes (List<Attribute> attrs)
		{
			Attrs = attrs;
		}

		public void AddAttributes (List<Attribute> attrs)
		{
			Attrs.AddRange (attrs);
		}

		public void AttachTo (Attributable attributable, IMemberContext context)
		{
			foreach (Attribute a in Attrs)
				a.AttachTo (attributable, context);
		}

		public Attributes Clone ()
		{
			var al = new List<Attribute> (Attrs.Count);
			foreach (Attribute a in Attrs)
				al.Add (a.Clone ());

			return new Attributes (al);
		}

		/// <summary>
		/// Checks whether attribute target is valid for the current element
		/// </summary>
		public bool CheckTargets ()
		{
			foreach (Attribute a in Attrs) {
				if (!a.CheckTarget ())
					return false;
			}
			return true;
		}

		public Attribute Search (PredefinedAttribute t)
		{
			return Search (null, t);
		}

		public Attribute Search (string explicitTarget, PredefinedAttribute t)
		{
			foreach (Attribute a in Attrs) {
				if (explicitTarget != null && a.ExplicitTarget != explicitTarget)
					continue;

				if (a.ResolveType () == t)
					return a;
			}
			return null;
		}

		/// <summary>
		/// Returns all attributes of type 't'. Use it when attribute is AllowMultiple = true
		/// </summary>
		public Attribute[] SearchMulti (PredefinedAttribute t)
		{
			List<Attribute> ar = null;

			foreach (Attribute a in Attrs) {
				if (a.ResolveType () == t) {
					if (ar == null)
						ar = new List<Attribute> (Attrs.Count);
					ar.Add (a);
				}
			}

			return ar == null ? null : ar.ToArray ();
		}

		public void Emit ()
		{
			CheckTargets ();

			Dictionary<Attribute, List<Attribute>> ld = Attrs.Count > 1 ? new Dictionary<Attribute, List<Attribute>> () : null;

			foreach (Attribute a in Attrs)
				a.Emit (ld);

			if (ld == null || ld.Count == 0)
				return;

			foreach (var d in ld) {
				if (d.Value == null)
					continue;

				Attribute a = d.Key;

				foreach (Attribute collision in d.Value)
					a.Report.SymbolRelatedToPreviousError (collision.Location, "");

				a.Report.Error (579, a.Location, "The attribute `{0}' cannot be applied multiple times",
					a.GetSignatureForError ());
			}
		}

		public bool Contains (PredefinedAttribute t)
		{
			return Search (t) != null;
		}
	}

	public sealed class AttributeEncoder
	{
		[Flags]
		public enum EncodedTypeProperties
		{
			None = 0,
			DynamicType = 1,
			TypeParameter = 1 << 1
		}

		public static readonly byte[] Empty;

		byte[] buffer;
		int pos;
		const ushort Version = 1;

		static AttributeEncoder ()
		{
			Empty = new byte[4];
			Empty[0] = (byte) Version;
		}

		public AttributeEncoder ()
		{
			buffer = new byte[32];
			Encode (Version);
		}

		public void Encode (bool value)
		{
			Encode (value ? (byte) 1 : (byte) 0);
		}

		public void Encode (byte value)
		{
			if (pos == buffer.Length)
				Grow (1);

			buffer [pos++] = value;
		}

		public void Encode (sbyte value)
		{
			Encode ((byte) value);
		}

		public void Encode (short value)
		{
			if (pos + 2 > buffer.Length)
				Grow (2);

			buffer[pos++] = (byte) value;
			buffer[pos++] = (byte) (value >> 8);
		}

		public void Encode (ushort value)
		{
			Encode ((short) value);
		}

		public void Encode (int value)
		{
			if (pos + 4 > buffer.Length)
				Grow (4);

			buffer[pos++] = (byte) value;
			buffer[pos++] = (byte) (value >> 8);
			buffer[pos++] = (byte) (value >> 16);
			buffer[pos++] = (byte) (value >> 24);
		}

		public void Encode (uint value)
		{
			Encode ((int) value);
		}

		public void Encode (long value)
		{
			if (pos + 8 > buffer.Length)
				Grow (8);

			buffer[pos++] = (byte) value;
			buffer[pos++] = (byte) (value >> 8);
			buffer[pos++] = (byte) (value >> 16);
			buffer[pos++] = (byte) (value >> 24);
			buffer[pos++] = (byte) (value >> 32);
			buffer[pos++] = (byte) (value >> 40);
			buffer[pos++] = (byte) (value >> 48);
			buffer[pos++] = (byte) (value >> 56);
		}

		public void Encode (ulong value)
		{
			Encode ((long) value);
		}

		public void Encode (float value)
		{
			Encode (SingleConverter.SingleToInt32Bits (value));
		}

		public void Encode (double value)
		{
			Encode (BitConverter.DoubleToInt64Bits (value));
		}

		public void Encode (string value)
		{
			if (value == null) {
				Encode ((byte) 0xFF);
				return;
			}

			var buf = Encoding.UTF8.GetBytes(value);
			WriteCompressedValue (buf.Length);

			if (pos + buf.Length > buffer.Length)
				Grow (buf.Length);

			Buffer.BlockCopy (buf, 0, buffer, pos, buf.Length);
			pos += buf.Length;
		}

		public EncodedTypeProperties Encode (TypeSpec type)
		{
			if (type == TypeManager.bool_type) {
				Encode ((byte) 0x02);
			} else if (type == TypeManager.char_type) {
				Encode ((byte) 0x03);
			} else if (type == TypeManager.sbyte_type) {
				Encode ((byte) 0x04);
			} else if (type == TypeManager.byte_type) {
				Encode ((byte) 0x05);
			} else if (type == TypeManager.short_type) {
				Encode ((byte) 0x06);
			} else if (type == TypeManager.ushort_type) {
				Encode ((byte) 0x07);
			} else if (type == TypeManager.int32_type) {
				Encode ((byte) 0x08);
			} else if (type == TypeManager.uint32_type) {
				Encode ((byte) 0x09);
			} else if (type == TypeManager.int64_type) {
				Encode ((byte) 0x0A);
			} else if (type == TypeManager.uint64_type) {
				Encode ((byte) 0x0B);
			} else if (type == TypeManager.float_type) {
				Encode ((byte) 0x0C);
			} else if (type == TypeManager.double_type) {
				Encode ((byte) 0x0D);
			} else if (type == TypeManager.string_type) {
				Encode ((byte) 0x0E);
			} else if (type == TypeManager.type_type) {
				Encode ((byte) 0x50);
			} else if (type == TypeManager.object_type) {
				Encode ((byte) 0x51);
			} else if (TypeManager.IsEnumType (type)) {
				Encode ((byte) 0x55);
				EncodeTypeName (type);
			} else if (type.IsArray) {
				Encode ((byte) 0x1D);
				return Encode (TypeManager.GetElementType (type));
			} else if (type == InternalType.Dynamic) {
				Encode ((byte) 0x51);
				return EncodedTypeProperties.DynamicType;
			}

			return EncodedTypeProperties.None;
		}

		public void EncodeTypeName (TypeSpec type)
		{
			var old_type = type.GetMetaInfo ();
			Encode (type.MemberDefinition.IsImported ? old_type.AssemblyQualifiedName : old_type.FullName);
		}

		//
		// Encodes single property named argument per call
		//
		public void EncodeNamedPropertyArgument (PropertySpec property, Constant value)
		{
			Encode ((ushort) 1);	// length
			Encode ((byte) 0x54); // property
			Encode (property.MemberType);
			Encode (property.Name);
			value.EncodeAttributeValue (null, this, property.MemberType);
		}

		//
		// Encodes single field named argument per call
		//
		public void EncodeNamedFieldArgument (FieldSpec field, Constant value)
		{
			Encode ((ushort) 1);	// length
			Encode ((byte) 0x53); // field
			Encode (field.MemberType);
			Encode (field.Name);
			value.EncodeAttributeValue (null, this, field.MemberType);
		}

		public void EncodeNamedArguments<T> (T[] members, Constant[] values) where T : MemberSpec, IInterfaceMemberSpec
		{
			Encode ((ushort) members.Length);

			for (int i = 0; i < members.Length; ++i)
			{
				var member = members[i];

				if (member.Kind == MemberKind.Field)
					Encode ((byte) 0x53);
				else if (member.Kind == MemberKind.Property)
					Encode ((byte) 0x54);
				else
					throw new NotImplementedException (member.Kind.ToString ());

				Encode (member.MemberType);
				Encode (member.Name);
				values [i].EncodeAttributeValue (null, this, member.MemberType);
			}
		}

		public void EncodeEmptyNamedArguments ()
		{
			Encode ((ushort) 0);
		}

		void Grow (int inc)
		{
			int size = System.Math.Max (pos * 4, pos + inc + 2);
			Array.Resize (ref buffer, size);
		}

		void WriteCompressedValue (int value)
		{
			if (value < 0x80) {
				Encode ((byte) value);
				return;
			}

			if (value < 0x4000) {
				Encode ((byte) (0x80 | (value >> 8)));
				Encode ((byte) value);
				return;
			}

			Encode (value);
		}

		public byte[] ToArray ()
		{
			byte[] buf = new byte[pos];
			Array.Copy (buffer, buf, pos);
			return buf;
		}
	}


	/// <summary>
	/// Helper class for attribute verification routine.
	/// </summary>
	static class AttributeTester
	{
		public enum Result {
			Ok,
			RefOutArrayError,
			ArrayArrayError
		}

		/// <summary>
		/// Returns true if parameters of two compared methods are CLS-Compliant.
		/// It tests differing only in ref or out, or in array rank.
		/// </summary>
		public static Result AreOverloadedMethodParamsClsCompliant (AParametersCollection pa, AParametersCollection pb) 
		{
			TypeSpec [] types_a = pa.Types;
			TypeSpec [] types_b = pb.Types;
			if (types_a == null || types_b == null)
				return Result.Ok;

			if (types_a.Length != types_b.Length)
				return Result.Ok;

			Result result = Result.Ok;
			for (int i = 0; i < types_b.Length; ++i) {
				TypeSpec aType = types_a [i];
				TypeSpec bType = types_b [i];

				var ac_a = aType as ArrayContainer;
				var ac_b = aType as ArrayContainer;

				if (ac_a != null && ac_b != null) {
					if (ac_a.Rank != ac_b.Rank && ac_a.Element == ac_b.Element) {
						result = Result.RefOutArrayError;
						continue;
					}

					if (ac_a.Element.IsArray || ac_b.Element.IsArray) {
						result = Result.ArrayArrayError;
						continue;
					}
				}

				if (aType != bType)
					return Result.Ok;

				const Parameter.Modifier out_ref_mod = (Parameter.Modifier.OUTMASK | Parameter.Modifier.REFMASK);
				if ((pa.FixedParameters[i].ModFlags & out_ref_mod) != (pb.FixedParameters[i].ModFlags & out_ref_mod))
					result = Result.RefOutArrayError;
			}
			return result;
		}

		/// <summary>
		/// Common method for Obsolete error/warning reporting.
		/// </summary>
		public static void Report_ObsoleteMessage (ObsoleteAttribute oa, string member, Location loc, Report Report)
		{
			if (oa.IsError) {
				Report.Error (619, loc, "`{0}' is obsolete: `{1}'", member, oa.Message);
				return;
			}

			if (oa.Message == null || oa.Message.Length == 0) {
				Report.Warning (612, 1, loc, "`{0}' is obsolete", member);
				return;
			}
			Report.Warning (618, 2, loc, "`{0}' is obsolete: `{1}'", member, oa.Message);
		}
	}

	//
	// Predefined attribute types
	//
	public class PredefinedAttributes
	{
		// Build-in attributes
		public readonly PredefinedAttribute ParamArray;
		public readonly PredefinedAttribute Out;

		// Optional attributes
		public readonly PredefinedAttribute Obsolete;
		public readonly PredefinedAttribute DllImport;
		public readonly PredefinedAttribute MethodImpl;
		public readonly PredefinedAttribute MarshalAs;
		public readonly PredefinedAttribute In;
		public readonly PredefinedAttribute IndexerName;
		public readonly PredefinedAttribute Conditional;
		public readonly PredefinedAttribute CLSCompliant;
		public readonly PredefinedAttribute Security;
		public readonly PredefinedAttribute Required;
		public readonly PredefinedAttribute Guid;
		public readonly PredefinedAttribute AssemblyCulture;
		public readonly PredefinedAttribute AssemblyVersion;
		public readonly PredefinedAttribute AssemblyAlgorithmId;
		public readonly PredefinedAttribute AssemblyFlags;
		public readonly PredefinedAttribute AssemblyFileVersion;
		public readonly PredefinedAttribute ComImport;
		public readonly PredefinedAttribute CoClass;
		public readonly PredefinedAttribute AttributeUsage;
		public readonly PredefinedAttribute DefaultParameterValue;
		public readonly PredefinedAttribute OptionalParameter;
		public readonly PredefinedAttribute UnverifiableCode;

		// New in .NET 2.0
		public readonly PredefinedAttribute DefaultCharset;
		public readonly PredefinedAttribute TypeForwarder;
		public readonly PredefinedAttribute FixedBuffer;
		public readonly PredefinedAttribute CompilerGenerated;
		public readonly PredefinedAttribute InternalsVisibleTo;
		public readonly PredefinedAttribute RuntimeCompatibility;
		public readonly PredefinedAttribute DebuggerHidden;
		public readonly PredefinedAttribute UnsafeValueType;

		// New in .NET 3.5
		public readonly PredefinedAttribute Extension;

		// New in .NET 4.0
		public readonly PredefinedDynamicAttribute Dynamic;

		//
		// Optional types which are used as types and for member lookup
		//
		public readonly PredefinedAttribute DefaultMember;
		public readonly PredefinedDecimalAttribute DecimalConstant;
		public readonly PredefinedAttribute StructLayout;
		public readonly PredefinedAttribute FieldOffset;

		public PredefinedAttributes (ModuleContainer module)
		{
			ParamArray = new PredefinedAttribute (module, "System", "ParamArrayAttribute");
			Out = new PredefinedAttribute (module, "System.Runtime.InteropServices", "OutAttribute");
			ParamArray.Resolve (Location.Null);
			Out.Resolve (Location.Null);

			Obsolete = new PredefinedAttribute (module, "System", "ObsoleteAttribute");
			DllImport = new PredefinedAttribute (module, "System.Runtime.InteropServices", "DllImportAttribute");
			MethodImpl = new PredefinedAttribute (module, "System.Runtime.CompilerServices", "MethodImplAttribute");
			MarshalAs = new PredefinedAttribute (module, "System.Runtime.InteropServices", "MarshalAsAttribute");
			In = new PredefinedAttribute (module, "System.Runtime.InteropServices", "InAttribute");
			IndexerName = new PredefinedAttribute (module, "System.Runtime.CompilerServices", "IndexerNameAttribute");
			Conditional = new PredefinedAttribute (module, "System.Diagnostics", "ConditionalAttribute");
			CLSCompliant = new PredefinedAttribute (module, "System", "CLSCompliantAttribute");
			Security = new PredefinedAttribute (module, "System.Security.Permissions", "SecurityAttribute");
			Required = new PredefinedAttribute (module, "System.Runtime.CompilerServices", "RequiredAttributeAttribute");
			Guid = new PredefinedAttribute (module, "System.Runtime.InteropServices", "GuidAttribute");
			AssemblyCulture = new PredefinedAttribute (module, "System.Reflection", "AssemblyCultureAttribute");
			AssemblyVersion = new PredefinedAttribute (module, "System.Reflection", "AssemblyVersionAttribute");
			AssemblyAlgorithmId = new PredefinedAttribute (module, "System.Reflection", "AssemblyAlgorithmIdAttribute");
			AssemblyFlags = new PredefinedAttribute (module, "System.Reflection", "AssemblyFlagsAttribute");
			AssemblyFileVersion = new PredefinedAttribute (module, "System.Reflection", "AssemblyFileVersionAttribute");
			ComImport = new PredefinedAttribute (module, "System.Runtime.InteropServices", "ComImportAttribute");
			CoClass = new PredefinedAttribute (module, "System.Runtime.InteropServices", "CoClassAttribute");
			AttributeUsage = new PredefinedAttribute (module, "System", "AttributeUsageAttribute");
			DefaultParameterValue = new PredefinedAttribute (module, "System.Runtime.InteropServices", "DefaultParameterValueAttribute");
			OptionalParameter = new PredefinedAttribute (module, "System.Runtime.InteropServices", "OptionalAttribute");
			UnverifiableCode = new PredefinedAttribute (module, "System.Security", "UnverifiableCodeAttribute");

			DefaultCharset = new PredefinedAttribute (module, "System.Runtime.InteropServices", "DefaultCharSetAttribute");
			TypeForwarder = new PredefinedAttribute (module, "System.Runtime.CompilerServices", "TypeForwardedToAttribute");
			FixedBuffer = new PredefinedAttribute (module, "System.Runtime.CompilerServices", "FixedBufferAttribute");
			CompilerGenerated = new PredefinedAttribute (module, "System.Runtime.CompilerServices", "CompilerGeneratedAttribute");
			InternalsVisibleTo = new PredefinedAttribute (module, "System.Runtime.CompilerServices", "InternalsVisibleToAttribute");
			RuntimeCompatibility = new PredefinedAttribute (module, "System.Runtime.CompilerServices", "RuntimeCompatibilityAttribute");
			DebuggerHidden = new PredefinedAttribute (module, "System.Diagnostics", "DebuggerHiddenAttribute");
			UnsafeValueType = new PredefinedAttribute (module, "System.Runtime.CompilerServices", "UnsafeValueTypeAttribute");

			Extension = new PredefinedAttribute (module, "System.Runtime.CompilerServices", "ExtensionAttribute");

			Dynamic = new PredefinedDynamicAttribute (module, "System.Runtime.CompilerServices", "DynamicAttribute");

			DefaultMember = new PredefinedAttribute (module, "System.Reflection", "DefaultMemberAttribute");
			DecimalConstant = new PredefinedDecimalAttribute (module, "System.Runtime.CompilerServices", "DecimalConstantAttribute");
			StructLayout = new PredefinedAttribute (module, "System.Runtime.InteropServices", "StructLayoutAttribute");
			FieldOffset = new PredefinedAttribute (module, "System.Runtime.InteropServices", "FieldOffsetAttribute");

			// TODO: Should define only attributes which are used for comparison
			const System.Reflection.BindingFlags all_fields = System.Reflection.BindingFlags.Public |
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly;

			foreach (var fi in GetType ().GetFields (all_fields)) {
				((PredefinedAttribute) fi.GetValue (this)).Define ();
			}
		}
	}

	public class PredefinedAttribute : PredefinedType
	{
		protected MethodSpec ctor;
		List<PropertySpec> properties;

		public PredefinedAttribute (ModuleContainer module, string ns, string name)
			: base (module, MemberKind.Class, ns, name)
		{
		}

		#region Properties

		public MethodSpec Constructor {
			get {
				return ctor;
			}
		}

		#endregion

		public static bool operator == (TypeSpec type, PredefinedAttribute pa)
		{
			return type == pa.type && pa.type != null;
		}

		public static bool operator != (TypeSpec type, PredefinedAttribute pa)
		{
			return type != pa.type;
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			throw new NotSupportedException ();
		}

		public void EmitAttribute (ConstructorBuilder builder)
		{
			if (ResolveBuilder ())
				builder.SetCustomAttribute (GetCtorMetaInfo (), AttributeEncoder.Empty);
		}

		public void EmitAttribute (MethodBuilder builder)
		{
			if (ResolveBuilder ())
				builder.SetCustomAttribute (GetCtorMetaInfo (), AttributeEncoder.Empty);
		}

		public void EmitAttribute (PropertyBuilder builder)
		{
			if (ResolveBuilder ())
				builder.SetCustomAttribute (GetCtorMetaInfo (), AttributeEncoder.Empty);
		}

		public void EmitAttribute (FieldBuilder builder)
		{
			if (ResolveBuilder ())
				builder.SetCustomAttribute (GetCtorMetaInfo (), AttributeEncoder.Empty);
		}

		public void EmitAttribute (FieldBuilder builder, AttributeEncoder argsEncoded)
		{
			builder.SetCustomAttribute (GetCtorMetaInfo (), argsEncoded.ToArray ());
		}

		public void EmitAttribute (TypeBuilder builder)
		{
			if (ResolveBuilder ())
				builder.SetCustomAttribute (GetCtorMetaInfo (), AttributeEncoder.Empty);
		}

		public void EmitAttribute (TypeBuilder builder, AttributeEncoder argsEncoded)
		{
			builder.SetCustomAttribute (GetCtorMetaInfo (), argsEncoded.ToArray ());
		}

		public void EmitAttribute (AssemblyBuilder builder)
		{
			if (ResolveBuilder ())
				builder.SetCustomAttribute (GetCtorMetaInfo (), AttributeEncoder.Empty);
		}

		public void EmitAttribute (ModuleBuilder builder)
		{
			if (ResolveBuilder ())
				builder.SetCustomAttribute (GetCtorMetaInfo (), AttributeEncoder.Empty);
		}

		public void EmitAttribute (ParameterBuilder builder)
		{
			if (ResolveBuilder ())
				builder.SetCustomAttribute (GetCtorMetaInfo (), AttributeEncoder.Empty);
		}

		public void EmitAttribute (ParameterBuilder builder, AttributeEncoder argsEncoded)
		{
			builder.SetCustomAttribute (GetCtorMetaInfo (), argsEncoded.ToArray ());
		}

		ConstructorInfo GetCtorMetaInfo ()
		{
			return (ConstructorInfo) ctor.GetMetaInfo ();
		}

		public PropertySpec GetProperty (string name, TypeSpec memberType, Location loc)
		{
			PropertySpec spec;

			if (properties != null) {
				spec = properties.Find (l => l.Name == name);
			} else {
				spec = null;
			}

			if (spec == null) {
				spec = TypeManager.GetPredefinedProperty (type, name, loc, memberType);

				if (spec != null) {
					if (properties == null) {
						properties = new List<PropertySpec> ();
					}

					properties.Add (spec);
				}
			}

			return spec;
		}

		public bool ResolveBuilder ()
		{
			if (ctor != null)
				return true;

			//
			// Handle all parameter-less attributes as optional
			//
			if (!IsDefined)
				return false;

			ctor = TypeManager.GetPredefinedConstructor (type, Location.Null, TypeSpec.EmptyTypes);
			return ctor != null;
		}

		public bool ResolveConstructor (Location loc, params TypeSpec[] argType)
		{
			if (ctor != null)
				throw new InternalErrorException ("Predefined ctor redefined");

			if (Resolve (loc) == null)
				return false;

			ctor = TypeManager.GetPredefinedConstructor (type, loc, argType);
			return ctor != null;
		}
	}

	public class PredefinedDecimalAttribute : PredefinedAttribute
	{
		public PredefinedDecimalAttribute (ModuleContainer module, string ns, string name)
			: base (module, ns, name)
		{
		}

		public void EmitAttribute (ParameterBuilder builder, decimal value, Location loc)
		{
			if (Resolve (loc) == null)
				return;

			if (ctor == null && !ResolveConstructor (loc, TypeManager.byte_type, TypeManager.byte_type, TypeManager.uint32_type, TypeManager.uint32_type, TypeManager.uint32_type))
				return;

			int[] bits = decimal.GetBits (value);
			AttributeEncoder encoder = new AttributeEncoder ();
			encoder.Encode ((byte) (bits[3] >> 16));
			encoder.Encode ((byte) (bits[3] >> 31));
			encoder.Encode ((uint) bits[2]);
			encoder.Encode ((uint) bits[1]);
			encoder.Encode ((uint) bits[0]);
			encoder.EncodeEmptyNamedArguments ();

			EmitAttribute (builder, encoder);
		}

		public void EmitAttribute (FieldBuilder builder, decimal value, Location loc)
		{
			if (Resolve (loc) == null)
				return;

			if (ctor == null && !ResolveConstructor (loc, TypeManager.byte_type, TypeManager.byte_type, TypeManager.uint32_type, TypeManager.uint32_type, TypeManager.uint32_type))
				return;

			int[] bits = decimal.GetBits (value);
			AttributeEncoder encoder = new AttributeEncoder ();
			encoder.Encode ((byte) (bits[3] >> 16));
			encoder.Encode ((byte) (bits[3] >> 31));
			encoder.Encode ((uint) bits[2]);
			encoder.Encode ((uint) bits[1]);
			encoder.Encode ((uint) bits[0]);
			encoder.EncodeEmptyNamedArguments ();

			EmitAttribute (builder, encoder);
		}
	}

	public class PredefinedDynamicAttribute : PredefinedAttribute
	{
		MethodSpec tctor;

		public PredefinedDynamicAttribute (ModuleContainer module, string ns, string name)
			: base (module, ns, name)
		{
		}

		public void EmitAttribute (FieldBuilder builder, TypeSpec type, Location loc)
		{
			if (ResolveTransformationCtor (loc)) {
				var cab = new CustomAttributeBuilder ((ConstructorInfo) tctor.GetMetaInfo (), new object[] { GetTransformationFlags (type) });
				builder.SetCustomAttribute (cab);
			}
		}

		public void EmitAttribute (ParameterBuilder builder, TypeSpec type, Location loc)
		{
			if (ResolveTransformationCtor (loc)) {
				var cab = new CustomAttributeBuilder ((ConstructorInfo) tctor.GetMetaInfo (), new object[] { GetTransformationFlags (type) });
				builder.SetCustomAttribute (cab);
			}
		}

		public void EmitAttribute (PropertyBuilder builder, TypeSpec type, Location loc)
		{
			if (ResolveTransformationCtor (loc)) {
				var cab = new CustomAttributeBuilder ((ConstructorInfo) tctor.GetMetaInfo (), new object[] { GetTransformationFlags (type) });
				builder.SetCustomAttribute (cab);
			}
		}

		public void EmitAttribute (TypeBuilder builder, TypeSpec type, Location loc)
		{
			if (ResolveTransformationCtor (loc)) {
				var cab = new CustomAttributeBuilder ((ConstructorInfo) tctor.GetMetaInfo (), new object[] { GetTransformationFlags (type) });
				builder.SetCustomAttribute (cab);
			}
		}

		//
		// When any element of the type is a dynamic type
		//
		// This method builds a transformation array for dynamic types
		// used in places where DynamicAttribute cannot be applied to.
		// It uses bool flag when type is of dynamic type and each
		// section always starts with "false" for some reason.
		//
		// LAMESPEC: This should be part of C# specification
		// 
		// Example: Func<dynamic, int, dynamic[]>
		// Transformation: { false, true, false, false, true }
		//
		static bool[] GetTransformationFlags (TypeSpec t)
		{
			bool[] element;
			var ac = t as ArrayContainer;
			if (ac != null) {
				element = GetTransformationFlags (ac.Element);
				if (element == null)
					return null;

				bool[] res = new bool[element.Length + 1];
				res[0] = false;
				Array.Copy (element, 0, res, 1, element.Length);
				return res;
			}

			if (t == null)
				return null;

			if (t.IsGeneric) {
				List<bool> transform = null;
				var targs = t.TypeArguments;
				for (int i = 0; i < targs.Length; ++i) {
					element = GetTransformationFlags (targs[i]);
					if (element != null) {
						if (transform == null) {
							transform = new List<bool> ();
							for (int ii = 0; ii <= i; ++ii)
								transform.Add (false);
						}

						transform.AddRange (element);
					} else if (transform != null) {
						transform.Add (false);
					}
				}

				if (transform != null)
					return transform.ToArray ();
			}

			if (t == InternalType.Dynamic)
				return new bool[] { true };

			return null;
		}

		bool ResolveTransformationCtor (Location loc)
		{
			if (tctor != null)
				return true;

			if (Resolve (loc) == null)
				return false;

			tctor = TypeManager.GetPredefinedConstructor (type, Location.Null, ArrayContainer.MakeType (TypeManager.bool_type));
			return tctor != null;
		}
	}
}
