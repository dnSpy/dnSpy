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
// Copyright 2011 Xamarin Inc
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
using BadImageFormat = IKVM.Reflection.BadImageFormatException;
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using SecurityType = System.Collections.Generic.Dictionary<System.Security.Permissions.SecurityAction, System.Security.PermissionSet>;
using BadImageFormat = System.BadImageFormatException;
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
				attributes.AddAttributes (attrs.Attrs);
			attrs.AttachTo (this, context);
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

	public class Attribute
	{
		public readonly string ExplicitTarget;
		public AttributeTargets Target;
		readonly ATypeNameExpression expression;

		Arguments pos_args, named_args;

		bool resolve_error;
		bool arg_resolved;
		readonly bool nameEscaped;
		readonly Location loc;
		public TypeSpec Type;	

		//
		// An attribute can be attached to multiple targets (e.g. multiple fields)
		//
		Attributable[] targets;

		//
		// A member context for the attribute, it's much easier to hold it here
		// than trying to pull it during resolve
		//
		IMemberContext context;

		public static readonly AttributeUsageAttribute DefaultUsageAttribute = new AttributeUsageAttribute (AttributeTargets.All);
		public static readonly object[] EmptyObject = new object [0];

		List<KeyValuePair<MemberExpr, NamedArgument>> named_values;

		public Attribute (string target, ATypeNameExpression expr, Arguments[] args, Location loc, bool nameEscaped)
		{
			this.expression = expr;
			if (args != null) {
				pos_args = args[0];
				named_args = args[1];
			}
			this.loc = loc;
			ExplicitTarget = target;
			this.nameEscaped = nameEscaped;
		}

		public Location Location {
			get {
				return loc;
			}
		}

		public Arguments NamedArguments {
			get {
				return named_args;
			}
		}

		public Arguments PositionalArguments {
			get {
				return pos_args;
			}
		}

		public ATypeNameExpression TypeExpression {
			get {
				return expression;
			}
		}

		void AddModuleCharSet (ResolveContext rc)
		{
			const string dll_import_char_set = "CharSet";

			//
			// Only when not customized by user
			//
			if (HasField (dll_import_char_set))
				return;

			if (!rc.Module.PredefinedTypes.CharSet.Define ()) {
				return;
			}

			if (NamedArguments == null)
				named_args = new Arguments (1);

			var value = Constant.CreateConstant (rc.Module.PredefinedTypes.CharSet.TypeSpec, rc.Module.DefaultCharSet, Location);
			NamedArguments.Add (new NamedArgument (dll_import_char_set, loc, value));
		}

		public Attribute Clone ()
		{
			Attribute a = new Attribute (ExplicitTarget, expression, null, loc, nameEscaped);
			a.pos_args = pos_args;
			a.named_args = NamedArguments;
			return a;
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

			// When re-attaching global attributes
			if (context is NamespaceContainer) {
				this.targets[0] = target;
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
			context.Module.Compiler.Report.Error (1982, loc, "An attribute argument cannot be dynamic expression");
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

		/// <summary>
		///   Tries to resolve the type of the attribute. Flags an error if it can't, and complain is true.
		/// </summary>
		void ResolveAttributeType ()
		{
			SessionReportPrinter resolve_printer = new SessionReportPrinter ();
			ReportPrinter prev_recorder = Report.SetPrinter (resolve_printer);

			bool t1_is_attr = false;
			bool t2_is_attr = false;
			TypeSpec t1, t2;
			ATypeNameExpression expanded = null;

			// TODO: Additional warnings such as CS0436 are swallowed because we don't
			// print on success

			try {
				t1 = expression.ResolveAsType (context);
				if (t1 != null)
					t1_is_attr = t1.IsAttribute;

				resolve_printer.EndSession ();

				if (nameEscaped) {
					t2 = null;
				} else {
					expanded = (ATypeNameExpression) expression.Clone (null);
					expanded.Name += "Attribute";

					t2 = expanded.ResolveAsType (context);
					if (t2 != null)
						t2_is_attr = t2.IsAttribute;
				}
			} finally {
				context.Module.Compiler.Report.SetPrinter (prev_recorder);
			}

			if (t1_is_attr && t2_is_attr && t1 != t2) {
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

			resolve_error = true;

			if (t1 != null) {
				resolve_printer.Merge (prev_recorder);

				Report.SymbolRelatedToPreviousError (t1);
				Report.Error (616, Location, "`{0}': is not an attribute class", t1.GetSignatureForError ());
				return;
			}

			if (t2 != null) {
				Report.SymbolRelatedToPreviousError (t2);
				Report.Error (616, Location, "`{0}': is not an attribute class", t2.GetSignatureForError ());
				return;
			}

			resolve_printer.Merge (prev_recorder);
		}

		public TypeSpec ResolveType ()
		{
			if (Type == null && !resolve_error)
				ResolveAttributeType ();
			return Type;
		}

		public string GetSignatureForError ()
		{
			if (Type != null)
				return TypeManager.CSharpName (Type);

			return expression.GetSignatureForError ();
		}

		public bool HasSecurityAttribute {
			get {
				PredefinedAttribute pa = context.Module.PredefinedAttributes.Security;
				return pa.IsDefined && TypeSpec.IsBaseClass (Type, pa.TypeSpec, false);
			}
		}

		public bool IsValidSecurityAttribute ()
		{
			return HasSecurityAttribute && IsSecurityActionValid ();
		}

		static bool IsValidArgumentType (TypeSpec t)
		{
			if (t.IsArray) {
				var ac = (ArrayContainer) t;
				if (ac.Rank > 1)
					return false;

				t = ac.Element;
			}

			switch (t.BuiltinType) {
			case BuiltinTypeSpec.Type.Int:
			case BuiltinTypeSpec.Type.UInt:
			case BuiltinTypeSpec.Type.Long:
			case BuiltinTypeSpec.Type.ULong:
			case BuiltinTypeSpec.Type.Float:
			case BuiltinTypeSpec.Type.Double:
			case BuiltinTypeSpec.Type.Char:
			case BuiltinTypeSpec.Type.Short:
			case BuiltinTypeSpec.Type.Bool:
			case BuiltinTypeSpec.Type.SByte:
			case BuiltinTypeSpec.Type.Byte:
			case BuiltinTypeSpec.Type.UShort:

			case BuiltinTypeSpec.Type.String:
			case BuiltinTypeSpec.Type.Object:
			case BuiltinTypeSpec.Type.Dynamic:
			case BuiltinTypeSpec.Type.Type:
				return true;
			}

			return t.IsEnum;
		}

		// TODO: Don't use this ambiguous value
		public string Name {
			get { return expression.Name; }
		}

		public ATypeNameExpression TypeNameExpression {
			get {
				return expression;
			}
		}

		public Report Report {
			get { return context.Module.Compiler.Report; }
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

			ResolveContext rc = null;

			MethodSpec ctor;
			// Try if the attribute is simple and has been resolved before
			if (pos_args != null || !context.Module.AttributeConstructorCache.TryGetValue (Type, out ctor)) {
				rc = CreateResolveContext ();
				ctor = ResolveConstructor (rc);
				if (ctor == null) {
					return null;
				}

				if (pos_args == null && ctor.Parameters.IsEmpty)
					context.Module.AttributeConstructorCache.Add (Type, ctor);
			}

			//
			// Add [module: DefaultCharSet] to all DllImport import attributes
			//
			var module = context.Module;
			if ((Type == module.PredefinedAttributes.DllImport || Type == module.PredefinedAttributes.UnmanagedFunctionPointer) && module.HasDefaultCharSet) {
				if (rc == null)
					rc = CreateResolveContext ();

				AddModuleCharSet (rc);
			}

			if (NamedArguments != null) {
				if (rc == null)
					rc = CreateResolveContext ();

				if (!ResolveNamedArguments (rc))
					return null;
			}

			resolve_error = false;
			return ctor;
		}

		MethodSpec ResolveConstructor (ResolveContext ec)
		{
			if (pos_args != null) {
				bool dynamic;
				pos_args.Resolve (ec, out dynamic);
				if (dynamic) {
					Error_AttributeArgumentIsDynamic (ec.MemberContext, loc);
					return null;
				}
			}

			return Expression.ConstructorLookup (ec, Type, ref pos_args, loc);
		}

		bool ResolveNamedArguments (ResolveContext ec)
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

				Expression member = Expression.MemberLookup (ec, false, Type, name, 0, Expression.MemberLookupRestrictions.ExactArity, loc);

				if (member == null) {
					member = Expression.MemberLookup (ec, true, Type, name, 0, Expression.MemberLookupRestrictions.ExactArity, loc);

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

			AttributeUsageAttribute usage_attribute = new AttributeUsageAttribute ((AttributeTargets) ((Constant) pos_args[0].Expr).GetValue ());

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

			if (resolve_error || pos_args.Count != 1 || !(pos_args[0].Expr is Constant))
				return null;

			return ((Constant) pos_args[0].Expr).GetValue () as string;
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

			return ((Constant) pos_args[0].Expr).GetValue () as string;
		}

		/// <summary>
		/// Creates the instance of ObsoleteAttribute from this attribute instance
		/// </summary>
		public ObsoleteAttribute GetObsoleteAttribute ()
		{
			if (!arg_resolved) {
				// corlib only case when obsolete is used before is resolved
				var c = Type.MemberDefinition as Class;
				if (c != null && !c.HasMembersDefined)
					c.Define ();
				
				// TODO: It is not neccessary to call whole Resolve (ApplyAttribute does it now) we need only ctor args.
				// But because a lot of attribute class code must be rewritten will be better to wait...
				Resolve ();
			}

			if (resolve_error)
				return null;

			if (pos_args == null)
				return new ObsoleteAttribute ();

			string msg = ((Constant) pos_args[0].Expr).GetValue () as string;
			if (pos_args.Count == 1)
				return new ObsoleteAttribute (msg);

			return new ObsoleteAttribute (msg, ((BoolConstant) pos_args[1].Expr).Value);
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

			return ((BoolConstant) pos_args[0].Expr).Value;
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
			Report.Warning (657, 1, Location,
				"`{0}' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are `{1}'. All attributes in this section will be ignored",
				ExplicitTarget, sb.ToString ());
			return false;
		}

		/// <summary>
		/// Tests permitted SecurityAction for assembly or other types
		/// </summary>
		bool IsSecurityActionValid ()
		{
			SecurityAction action = GetSecurityActionValue ();
			bool for_assembly = Target == AttributeTargets.Assembly || Target == AttributeTargets.Module;

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
			return (SecurityAction) ((Constant) pos_args[0].Expr).GetValue ();
		}

		/// <summary>
		/// Creates instance of SecurityAttribute class and add result of CreatePermission method to permission table.
		/// </summary>
		/// <returns></returns>
		public void ExtractSecurityPermissionSet (MethodSpec ctor, ref SecurityType permissions)
		{
#if STATIC
			object[] values = new object[pos_args.Count];
			for (int i = 0; i < values.Length; ++i)
				values[i] = ((Constant) pos_args[i].Expr).GetValue ();

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
			throw new NotSupportedException ();
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
			return (CharSet) System.Enum.Parse (typeof (CharSet), ((Constant) pos_args[0].Expr).GetValue ().ToString ());
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
			return (GetMethodImplOptions () & MethodImplOptions.InternalCall) != 0;
		}

		public MethodImplOptions GetMethodImplOptions ()
		{
			MethodImplOptions options = 0;
			if (pos_args.Count == 1) {
				options = (MethodImplOptions) System.Enum.Parse (typeof (MethodImplOptions), ((Constant) pos_args[0].Expr).GetValue ().ToString ());
			} else if (HasField ("Value")) {
				var named = GetNamedValue ("Value");
				options = (MethodImplOptions) System.Enum.Parse (typeof (MethodImplOptions), named.GetValue ().ToString ());
			}

			return options;
		}

		//
		// Returns true for StructLayoutAttribute with LayoutKind.Explicit value
		// 
		public bool IsExplicitLayoutKind ()
		{
			if (pos_args == null || pos_args.Count != 1)
				return false;

			var value = (LayoutKind) System.Enum.Parse (typeof (LayoutKind), ((Constant) pos_args[0].Expr).GetValue ().ToString ());
			return value == LayoutKind.Explicit;
		}

		public Expression GetParameterDefaultValue ()
		{
			if (pos_args == null)
				return null;

			return pos_args[0].Expr;
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
			return Type.GetHashCode () ^ Target.GetHashCode ();
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
			if (pos_args == null && named_values == null) {
				cdata = AttributeEncoder.Empty;
			} else {
				AttributeEncoder encoder = new AttributeEncoder ();

				if (pos_args != null) {
					var param_types = ctor.Parameters.Types;
					for (int j = 0; j < pos_args.Count; ++j) {
						var pt = param_types[j];
						var arg_expr = pos_args[j].Expr;
						if (j == 0) {
							if ((Type == predefined.IndexerName || Type == predefined.Conditional) && arg_expr is Constant) {
								string v = ((Constant) arg_expr).GetValue () as string;
								if (!Tokenizer.IsValidIdentifier (v) || (Type == predefined.IndexerName && Tokenizer.IsKeyword (v))) {
									context.Module.Compiler.Report.Error (633, arg_expr.Location,
										"The argument to the `{0}' attribute must be a valid identifier", GetSignatureForError ());
									return;
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
									context.Module.Compiler.Report.Error (591, Location, "Invalid value for argument to `{0}' attribute",
										"System.AttributeUsage");
								}
							} else if (Type == predefined.MarshalAs) {
								if (pos_args.Count == 1) {
									var u_type = (UnmanagedType) System.Enum.Parse (typeof (UnmanagedType), ((Constant) pos_args[0].Expr).GetValue ().ToString ());
									if (u_type == UnmanagedType.ByValArray && !(Owner is FieldBase)) {
										Error_AttributeEmitError ("Specified unmanaged type is only valid on fields");
									}
								}
							} else if (Type == predefined.DllImport) {
								if (pos_args.Count == 1 && pos_args[0].Expr is Constant) {
									var value = ((Constant) pos_args[0].Expr).GetValue () as string;
									if (string.IsNullOrEmpty (value))
										Error_AttributeEmitError ("DllName cannot be empty or null");
								}
							} else if (Type == predefined.MethodImpl && pt.BuiltinType == BuiltinTypeSpec.Type.Short &&
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
				if (e is BadImageFormat && Report.Errors > 0)
					return;

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

			if (!context.Module.Compiler.Settings.VerifyClsCompliance)
				return;

			// Here we are testing attribute arguments for array usage (error 3016)
			if (Owner.IsClsComplianceRequired ()) {
				if (pos_args != null)
					pos_args.CheckArrayAsAttribute (context.Module.Compiler);
			
				if (NamedArguments == null)
					return;

				NamedArguments.CheckArrayAsAttribute (context.Module.Compiler);
			}
		}

		private Expression GetValue () 
		{
			if (pos_args == null || pos_args.Count < 1)
				return null;

			return pos_args[0].Expr;
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
	}
	
	public class Attributes
	{
		public readonly List<Attribute> Attrs;
#if FULL_AST
		public readonly List<List<Attribute>> Sections = new List<List<Attribute>> ();
#endif

		public Attributes (Attribute a)
		{
			Attrs = new List<Attribute> ();
			Attrs.Add (a);
			
#if FULL_AST
			Sections.Add (Attrs);
#endif
		}

		public Attributes (List<Attribute> attrs)
		{
			Attrs = attrs;
#if FULL_AST
			Sections.Add (attrs);
#endif
		}

		public void AddAttribute (Attribute attr)
		{
			Attrs.Add (attr);
		}

		public void AddAttributes (List<Attribute> attrs)
		{
#if FULL_AST
			Sections.Add (attrs);
#else
			Attrs.AddRange (attrs);
#endif
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
			for (int i = 0; i < Attrs.Count; ++i) {
				if (!Attrs [i].CheckTarget ())
					Attrs.RemoveAt (i--);
			}

			return true;
		}

		public void ConvertGlobalAttributes (TypeContainer member, NamespaceContainer currentNamespace, bool isGlobal)
		{
			var member_explicit_targets = member.ValidAttributeTargets;
			for (int i = 0; i < Attrs.Count; ++i) {
				var attr = Attrs[0];
				if (attr.ExplicitTarget == null)
					continue;

				int ii;
				for (ii = 0; ii < member_explicit_targets.Length; ++ii) {
					if (attr.ExplicitTarget == member_explicit_targets[ii]) {
						ii = -1;
						break;
					}
				}

				if (ii < 0 || !isGlobal)
					continue;

				member.Module.AddAttribute (attr, currentNamespace);
				Attrs.RemoveAt (i);
				--i;
			}
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
			switch (type.BuiltinType) {
			case BuiltinTypeSpec.Type.Bool:
				Encode ((byte) 0x02);
				break;
			case BuiltinTypeSpec.Type.Char:
				Encode ((byte) 0x03);
				break;
			case BuiltinTypeSpec.Type.SByte:
				Encode ((byte) 0x04);
				break;
			case BuiltinTypeSpec.Type.Byte:
				Encode ((byte) 0x05);
				break;
			case BuiltinTypeSpec.Type.Short:
				Encode ((byte) 0x06);
				break;
			case BuiltinTypeSpec.Type.UShort:
				Encode ((byte) 0x07);
				break;
			case BuiltinTypeSpec.Type.Int:
				Encode ((byte) 0x08);
				break;
			case BuiltinTypeSpec.Type.UInt:
				Encode ((byte) 0x09);
				break;
			case BuiltinTypeSpec.Type.Long:
				Encode ((byte) 0x0A);
				break;
			case BuiltinTypeSpec.Type.ULong:
				Encode ((byte) 0x0B);
				break;
			case BuiltinTypeSpec.Type.Float:
				Encode ((byte) 0x0C);
				break;
			case BuiltinTypeSpec.Type.Double:
				Encode ((byte) 0x0D);
				break;
			case BuiltinTypeSpec.Type.String:
				Encode ((byte) 0x0E);
				break;
			case BuiltinTypeSpec.Type.Type:
				Encode ((byte) 0x50);
				break;
			case BuiltinTypeSpec.Type.Object:
				Encode ((byte) 0x51);
				break;
			case BuiltinTypeSpec.Type.Dynamic:
				Encode ((byte) 0x51);
				return EncodedTypeProperties.DynamicType;
			default:
				if (type.IsArray) {
					Encode ((byte) 0x1D);
					return Encode (TypeManager.GetElementType (type));
				}

				if (type.Kind == MemberKind.Enum) {
					Encode ((byte) 0x55);
					EncodeTypeName (type);
				}

				break;
			}

			return EncodedTypeProperties.None;
		}

		public void EncodeTypeName (TypeSpec type)
		{
			var old_type = type.GetMetaInfo ();
			Encode (type.MemberDefinition.IsImported ? old_type.AssemblyQualifiedName : old_type.FullName);
		}

		public void EncodeTypeName (TypeContainer type)
		{
			Encode (type.GetSignatureForMetadata ());
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
		public readonly PredefinedAttribute DefaultCharset;
		public readonly PredefinedAttribute TypeForwarder;
		public readonly PredefinedAttribute FixedBuffer;
		public readonly PredefinedAttribute CompilerGenerated;
		public readonly PredefinedAttribute InternalsVisibleTo;
		public readonly PredefinedAttribute RuntimeCompatibility;
		public readonly PredefinedAttribute DebuggerHidden;
		public readonly PredefinedAttribute UnsafeValueType;
		public readonly PredefinedAttribute UnmanagedFunctionPointer;
		public readonly PredefinedDebuggerBrowsableAttribute DebuggerBrowsable;

		// New in .NET 3.5
		public readonly PredefinedAttribute Extension;

		// New in .NET 4.0
		public readonly PredefinedDynamicAttribute Dynamic;

		// New in .NET 4.5
		public readonly PredefinedStateMachineAttribute AsyncStateMachine;
		public readonly PredefinedStateMachineAttribute IteratorStateMachine;

		//
		// Optional types which are used as types and for member lookup
		//
		public readonly PredefinedAttribute DefaultMember;
		public readonly PredefinedDecimalAttribute DecimalConstant;
		public readonly PredefinedAttribute StructLayout;
		public readonly PredefinedAttribute FieldOffset;
		public readonly PredefinedAttribute CallerMemberNameAttribute;
		public readonly PredefinedAttribute CallerLineNumberAttribute;
		public readonly PredefinedAttribute CallerFilePathAttribute;

		public PredefinedAttributes (ModuleContainer module)
		{
			ParamArray = new PredefinedAttribute (module, "System", "ParamArrayAttribute");
			Out = new PredefinedAttribute (module, "System.Runtime.InteropServices", "OutAttribute");
			ParamArray.Resolve ();
			Out.Resolve ();

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
			UnmanagedFunctionPointer = new PredefinedAttribute (module, "System.Runtime.InteropServices", "UnmanagedFunctionPointerAttribute");
			DebuggerBrowsable = new PredefinedDebuggerBrowsableAttribute (module, "System.Diagnostics", "DebuggerBrowsableAttribute");

			Extension = new PredefinedAttribute (module, "System.Runtime.CompilerServices", "ExtensionAttribute");

			Dynamic = new PredefinedDynamicAttribute (module, "System.Runtime.CompilerServices", "DynamicAttribute");

			DefaultMember = new PredefinedAttribute (module, "System.Reflection", "DefaultMemberAttribute");
			DecimalConstant = new PredefinedDecimalAttribute (module, "System.Runtime.CompilerServices", "DecimalConstantAttribute");
			StructLayout = new PredefinedAttribute (module, "System.Runtime.InteropServices", "StructLayoutAttribute");
			FieldOffset = new PredefinedAttribute (module, "System.Runtime.InteropServices", "FieldOffsetAttribute");

			AsyncStateMachine = new PredefinedStateMachineAttribute (module, "System.Runtime.CompilerServices", "AsyncStateMachineAttribute");
			IteratorStateMachine = new PredefinedStateMachineAttribute (module, "System.Runtime.CompilerServices", "IteratorStateMachineAttribute") {
				IsIterator = true
			};

			CallerMemberNameAttribute = new PredefinedAttribute (module, "System.Runtime.CompilerServices", "CallerMemberNameAttribute");
			CallerLineNumberAttribute = new PredefinedAttribute (module, "System.Runtime.CompilerServices", "CallerLineNumberAttribute");
			CallerFilePathAttribute = new PredefinedAttribute (module, "System.Runtime.CompilerServices", "CallerFilePathAttribute");

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

		public void EmitAttribute (TypeBuilder builder)
		{
			if (ResolveBuilder ())
				builder.SetCustomAttribute (GetCtorMetaInfo (), AttributeEncoder.Empty);
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

		ConstructorInfo GetCtorMetaInfo ()
		{
			return (ConstructorInfo) ctor.GetMetaInfo ();
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

			ctor = (MethodSpec) MemberCache.FindMember (type, MemberFilter.Constructor (ParametersCompiled.EmptyReadOnlyParameters), BindingRestriction.DeclaredOnly);
			return ctor != null;
		}
	}

	public class PredefinedDebuggerBrowsableAttribute : PredefinedAttribute
	{
		public PredefinedDebuggerBrowsableAttribute (ModuleContainer module, string ns, string name)
			: base (module, ns, name)
		{
		}

		public void EmitAttribute (FieldBuilder builder, System.Diagnostics.DebuggerBrowsableState state)
		{
			var ctor = module.PredefinedMembers.DebuggerBrowsableAttributeCtor.Get ();
			if (ctor == null)
				return;

			AttributeEncoder encoder = new AttributeEncoder ();
			encoder.Encode ((int) state);
			encoder.EncodeEmptyNamedArguments ();

			builder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), encoder.ToArray ());
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
			var ctor = module.PredefinedMembers.DecimalConstantAttributeCtor.Resolve (loc);
			if (ctor == null)
				return;

			int[] bits = decimal.GetBits (value);
			AttributeEncoder encoder = new AttributeEncoder ();
			encoder.Encode ((byte) (bits[3] >> 16));
			encoder.Encode ((byte) (bits[3] >> 31));
			encoder.Encode ((uint) bits[2]);
			encoder.Encode ((uint) bits[1]);
			encoder.Encode ((uint) bits[0]);
			encoder.EncodeEmptyNamedArguments ();

			builder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), encoder.ToArray ());
		}

		public void EmitAttribute (FieldBuilder builder, decimal value, Location loc)
		{
			var ctor = module.PredefinedMembers.DecimalConstantAttributeCtor.Resolve (loc);
			if (ctor == null)
				return;

			int[] bits = decimal.GetBits (value);
			AttributeEncoder encoder = new AttributeEncoder ();
			encoder.Encode ((byte) (bits[3] >> 16));
			encoder.Encode ((byte) (bits[3] >> 31));
			encoder.Encode ((uint) bits[2]);
			encoder.Encode ((uint) bits[1]);
			encoder.Encode ((uint) bits[0]);
			encoder.EncodeEmptyNamedArguments ();

			builder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), encoder.ToArray ());
		}
	}

	public class PredefinedStateMachineAttribute : PredefinedAttribute
	{
		public PredefinedStateMachineAttribute (ModuleContainer module, string ns, string name)
			: base (module, ns, name)
		{
		}

		public bool IsIterator { get; set; }

		public void EmitAttribute (MethodBuilder builder, StateMachine type)
		{
			var predefined_ctor = IsIterator ?
				module.PredefinedMembers.IteratorStateMachineAttributeCtor :
				module.PredefinedMembers.AsyncStateMachineAttributeCtor;

			var ctor = predefined_ctor.Get ();

			if (ctor == null)
				return;

			AttributeEncoder encoder = new AttributeEncoder ();
			encoder.EncodeTypeName (type);
			encoder.EncodeEmptyNamedArguments ();

			builder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), encoder.ToArray ());
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

			if (t.BuiltinType == BuiltinTypeSpec.Type.Dynamic)
				return new bool[] { true };

			return null;
		}

		bool ResolveTransformationCtor (Location loc)
		{
			if (tctor != null)
				return true;

			tctor = module.PredefinedMembers.DynamicAttributeCtor.Resolve (loc);
			return tctor != null;
		}
	}
}
