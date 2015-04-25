//
// modifiers.cs: Modifiers handling
//
// Authors: Miguel de Icaza (miguel@gnu.org)
//          Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001, 2002, 2003 Ximian, Inc (http://www.ximian.com)
// Copyright 2004-2010 Novell, Inc
//

using System;

#if STATIC
using IKVM.Reflection;
#else
using System.Reflection;
#endif

namespace Mono.CSharp
{
	[Flags]
	public enum Modifiers
	{
		PROTECTED = 0x0001,
		PUBLIC    = 0x0002,
		PRIVATE   = 0x0004,
		INTERNAL  = 0x0008,
		NEW       = 0x0010,
		ABSTRACT  = 0x0020,
		SEALED    = 0x0040,
		STATIC    = 0x0080,
		READONLY  = 0x0100,
		VIRTUAL   = 0x0200,
		OVERRIDE  = 0x0400,
		EXTERN    = 0x0800,
		VOLATILE  = 0x1000,
		UNSAFE    = 0x2000,
		ASYNC     = 0x4000,
		TOP       = 0x8000,

		//
		// Compiler specific flags
		//
		PROPERTY_CUSTOM 		= 0x10000,

		PARTIAL					= 0x20000,
		DEFAULT_ACCESS_MODIFIER	= 0x40000,
		METHOD_EXTENSION		= 0x80000,
		COMPILER_GENERATED		= 0x100000,
		BACKING_FIELD			= 0x200000,
		DEBUGGER_HIDDEN			= 0x400000,
		DEBUGGER_STEP_THROUGH	= 0x800000,

		AccessibilityMask = PUBLIC | PROTECTED | INTERNAL | PRIVATE,
		AllowedExplicitImplFlags = UNSAFE | EXTERN,
	}

	static class ModifiersExtensions
	{
		public static string AccessibilityName (Modifiers mod)
		{
			switch (mod & Modifiers.AccessibilityMask) {
			case Modifiers.PUBLIC:
				return "public";
			case Modifiers.PROTECTED:
				return "protected";
			case Modifiers.PROTECTED | Modifiers.INTERNAL:
				return "protected internal";
			case Modifiers.INTERNAL:
				return "internal";
			case Modifiers.PRIVATE:
				return "private";
			default:
				throw new NotImplementedException (mod.ToString ());
			}
		}

		static public string Name (Modifiers i)
		{
			string s = "";
			
			switch (i) {
			case Modifiers.NEW:
				s = "new"; break;
			case Modifiers.PUBLIC:
				s = "public"; break;
			case Modifiers.PROTECTED:
				s = "protected"; break;
			case Modifiers.INTERNAL:
				s = "internal"; break;
			case Modifiers.PRIVATE:
				s = "private"; break;
			case Modifiers.ABSTRACT:
				s = "abstract"; break;
			case Modifiers.SEALED:
				s = "sealed"; break;
			case Modifiers.STATIC:
				s = "static"; break;
			case Modifiers.READONLY:
				s = "readonly"; break;
			case Modifiers.VIRTUAL:
				s = "virtual"; break;
			case Modifiers.OVERRIDE:
				s = "override"; break;
			case Modifiers.EXTERN:
				s = "extern"; break;
			case Modifiers.VOLATILE:
				s = "volatile"; break;
			case Modifiers.UNSAFE:
				s = "unsafe"; break;
			case Modifiers.ASYNC:
				s = "async"; break;
			}

			return s;
		}

		//
		// Used by custom property accessors to check whether @modA is more restrictive than @modB
		//
		public static bool IsRestrictedModifier (Modifiers modA, Modifiers modB)
		{
			Modifiers flags = 0;

			if ((modB & Modifiers.PUBLIC) != 0) {
				flags = Modifiers.PROTECTED | Modifiers.INTERNAL | Modifiers.PRIVATE;
			} else if ((modB & Modifiers.PROTECTED) != 0) {
				if ((modB & Modifiers.INTERNAL) != 0)
					flags = Modifiers.PROTECTED | Modifiers.INTERNAL;

				flags |= Modifiers.PRIVATE;
			} else if ((modB & Modifiers.INTERNAL) != 0)
				flags = Modifiers.PRIVATE;

			return modB != modA && (modA & (~flags)) == 0;
		}

		public static TypeAttributes TypeAttr (Modifiers mod_flags, bool is_toplevel)
		{
			TypeAttributes t = 0;

			if (is_toplevel){
				if ((mod_flags & Modifiers.PUBLIC) != 0)
					t = TypeAttributes.Public;
				else if ((mod_flags & Modifiers.PRIVATE) != 0)
					t = TypeAttributes.NotPublic;
			} else {
				if ((mod_flags & Modifiers.PUBLIC) != 0)
					t = TypeAttributes.NestedPublic;
				else if ((mod_flags & Modifiers.PRIVATE) != 0)
					t = TypeAttributes.NestedPrivate;
				else if ((mod_flags & (Modifiers.PROTECTED | Modifiers.INTERNAL)) == (Modifiers.PROTECTED | Modifiers.INTERNAL))
					t = TypeAttributes.NestedFamORAssem;
				else if ((mod_flags & Modifiers.PROTECTED) != 0)
					t = TypeAttributes.NestedFamily;
				else if ((mod_flags & Modifiers.INTERNAL) != 0)
					t = TypeAttributes.NestedAssembly;
			}

			if ((mod_flags & Modifiers.SEALED) != 0)
				t |= TypeAttributes.Sealed;
			if ((mod_flags & Modifiers.ABSTRACT) != 0)
				t |= TypeAttributes.Abstract;

			return t;
		}

		public static FieldAttributes FieldAttr (Modifiers mod_flags)
		{
			FieldAttributes fa = 0;

			if ((mod_flags & Modifiers.PUBLIC) != 0)
				fa |= FieldAttributes.Public;
			if ((mod_flags & Modifiers.PRIVATE) != 0)
				fa |= FieldAttributes.Private;
			if ((mod_flags & Modifiers.PROTECTED) != 0) {
				if ((mod_flags & Modifiers.INTERNAL) != 0)
					fa |= FieldAttributes.FamORAssem;
				else 
					fa |= FieldAttributes.Family;
			} else {
				if ((mod_flags & Modifiers.INTERNAL) != 0)
					fa |= FieldAttributes.Assembly;
			}

			if ((mod_flags & Modifiers.STATIC) != 0)
				fa |= FieldAttributes.Static;
			if ((mod_flags & Modifiers.READONLY) != 0)
				fa |= FieldAttributes.InitOnly;

			return fa;
		}

		public static MethodAttributes MethodAttr (Modifiers mod_flags)
		{
			MethodAttributes ma = MethodAttributes.HideBySig;

			switch (mod_flags & Modifiers.AccessibilityMask) {
			case Modifiers.PUBLIC:
				ma |= MethodAttributes.Public;
				break;
			case Modifiers.PRIVATE:
				ma |= MethodAttributes.Private;
				break;
			case Modifiers.PROTECTED | Modifiers.INTERNAL:
				ma |= MethodAttributes.FamORAssem;
				break;
			case Modifiers.PROTECTED:
				ma |= MethodAttributes.Family;
				break;
			case Modifiers.INTERNAL:
				ma |= MethodAttributes.Assembly;
				break;
			default:
				throw new NotImplementedException (mod_flags.ToString ());
			}

			if ((mod_flags & Modifiers.STATIC) != 0)
				ma |= MethodAttributes.Static;
			if ((mod_flags & Modifiers.ABSTRACT) != 0) {
				ma |= MethodAttributes.Abstract | MethodAttributes.Virtual;
			}
			if ((mod_flags & Modifiers.SEALED) != 0)
				ma |= MethodAttributes.Final;

			if ((mod_flags & Modifiers.VIRTUAL) != 0)
				ma |= MethodAttributes.Virtual;

			if ((mod_flags & Modifiers.OVERRIDE) != 0) {
				ma |= MethodAttributes.Virtual;
			} else {
				if ((ma & MethodAttributes.Virtual) != 0)
					ma |= MethodAttributes.NewSlot;
			}
			
			return ma;
		}

		// <summary>
		//   Checks the object @mod modifiers to be in @allowed.
		//   Returns the new mask.  Side effect: reports any
		//   incorrect attributes. 
		// </summary>
		public static Modifiers Check (Modifiers allowed, Modifiers mod, Modifiers def_access, Location l, Report Report)
		{
			int invalid_flags = (~(int) allowed) & ((int) mod & ((int) Modifiers.TOP - 1));
			int i;

			if (invalid_flags == 0){
				//
				// If no accessibility bits provided
				// then provide the defaults.
				//
				if ((mod & Modifiers.AccessibilityMask) == 0) {
					mod |= def_access;
					if (def_access != 0)
						mod |= Modifiers.DEFAULT_ACCESS_MODIFIER;
					return mod;
				}

				return mod;
			}

			for (i = 1; i < (int) Modifiers.TOP; i <<= 1) {
				if ((i & invalid_flags) == 0)
					continue;

				Error_InvalidModifier ((Modifiers)i, l, Report);
			}

			return allowed & mod;
		}

		static void Error_InvalidModifier (Modifiers mod, Location l, Report Report)
		{
			Report.Error (106, l, "The modifier `{0}' is not valid for this item",
				Name (mod));
		}
	}
}
