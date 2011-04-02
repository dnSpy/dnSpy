//
// doc.cs: Support for XML documentation comment.
//
// Author:
//	Atsushi Enomoto <atsushi@ximian.com>
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2004 Novell, Inc.
//
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Linq;


namespace Mono.CSharp
{
	//
	// Implements XML documentation generation.
	//
	class DocumentationBuilder
	{
		//
		// Used to create element which helps well-formedness checking.
		//
		readonly XmlDocument XmlDocumentation;

		readonly ModuleContainer module;

		//
		// The output for XML documentation.
		//
		public XmlWriter XmlCommentOutput;

		static readonly string line_head = Environment.NewLine + "            ";
		static readonly char[] wsChars = new char[] { ' ', '\t', '\n', '\r' };

		//
		// Stores XmlDocuments that are included in XML documentation.
		// Keys are included filenames, values are XmlDocuments.
		//
		Dictionary<string, XmlDocument> StoredDocuments = new Dictionary<string, XmlDocument> ();

		public DocumentationBuilder (ModuleContainer module)
		{
			this.module = module;
			XmlDocumentation = new XmlDocument ();
			XmlDocumentation.PreserveWhitespace = false;
		}

		Report Report {
			get {
				return module.Compiler.Report;
			}
		}

		XmlNode GetDocCommentNode (MemberCore mc, string name)
		{
			// FIXME: It could be even optimizable as not
			// to use XmlDocument. But anyways the nodes
			// are not kept in memory.
			XmlDocument doc = XmlDocumentation;
			try {
				XmlElement el = doc.CreateElement ("member");
				el.SetAttribute ("name", name);
				string normalized = mc.DocComment;
				el.InnerXml = normalized;
				// csc keeps lines as written in the sources
				// and inserts formatting indentation (which 
				// is different from XmlTextWriter.Formatting
				// one), but when a start tag contains an 
				// endline, it joins the next line. We don't
				// have to follow such a hacky behavior.
				string [] split =
					normalized.Split ('\n');
				int j = 0;
				for (int i = 0; i < split.Length; i++) {
					string s = split [i].TrimEnd ();
					if (s.Length > 0)
						split [j++] = s;
				}
				el.InnerXml = line_head + String.Join (
					line_head, split, 0, j);
				return el;
			} catch (Exception ex) {
				Report.Warning (1570, 1, mc.Location, "XML documentation comment on `{0}' is not well-formed XML markup ({1})",
					mc.GetSignatureForError (), ex.Message);

				return doc.CreateComment (String.Format ("FIXME: Invalid documentation markup was found for member {0}", name));
			}
		}

		//
		// Generates xml doc comments (if any), and if required,
		// handle warning report.
		//
		internal void GenerateDocumentationForMember (MemberCore mc)
		{
			string name = mc.GetDocCommentName ();

			XmlNode n = GetDocCommentNode (mc, name);

			XmlElement el = n as XmlElement;
			if (el != null) {
				mc.OnGenerateDocComment (el);

				// FIXME: it could be done with XmlReader
				XmlNodeList nl = n.SelectNodes (".//include");
				if (nl.Count > 0) {
					// It could result in current node removal, so prepare another list to iterate.
					var al = new List<XmlNode> (nl.Count);
					foreach (XmlNode inc in nl)
						al.Add (inc);
					foreach (XmlElement inc in al)
						if (!HandleInclude (mc, inc))
							inc.ParentNode.RemoveChild (inc);
				}

				// FIXME: it could be done with XmlReader
				DeclSpace ds_target = mc as DeclSpace;
				if (ds_target == null)
					ds_target = mc.Parent;

				foreach (XmlElement see in n.SelectNodes (".//see"))
					HandleSee (mc, ds_target, see);
				foreach (XmlElement seealso in n.SelectNodes (".//seealso"))
					HandleSeeAlso (mc, ds_target, seealso);
				foreach (XmlElement see in n.SelectNodes (".//exception"))
					HandleException (mc, ds_target, see);
			}

			n.WriteTo (XmlCommentOutput);
		}

		//
		// Processes "include" element. Check included file and
		// embed the document content inside this documentation node.
		//
		bool HandleInclude (MemberCore mc, XmlElement el)
		{
			bool keep_include_node = false;
			string file = el.GetAttribute ("file");
			string path = el.GetAttribute ("path");
			if (file == "") {
				Report.Warning (1590, 1, mc.Location, "Invalid XML `include' element. Missing `file' attribute");
				el.ParentNode.InsertBefore (el.OwnerDocument.CreateComment (" Include tag is invalid "), el);
				keep_include_node = true;
			}
			else if (path.Length == 0) {
				Report.Warning (1590, 1, mc.Location, "Invalid XML `include' element. Missing `path' attribute");
				el.ParentNode.InsertBefore (el.OwnerDocument.CreateComment (" Include tag is invalid "), el);
				keep_include_node = true;
			}
			else {
				XmlDocument doc;
				if (!StoredDocuments.TryGetValue (file, out doc)) {
					try {
						doc = new XmlDocument ();
						doc.Load (file);
						StoredDocuments.Add (file, doc);
					} catch (Exception) {
						el.ParentNode.InsertBefore (el.OwnerDocument.CreateComment (String.Format (" Badly formed XML in at comment file `{0}': cannot be included ", file)), el);
						Report.Warning (1592, 1, mc.Location, "Badly formed XML in included comments file -- `{0}'", file);
					}
				}
				if (doc != null) {
					try {
						XmlNodeList nl = doc.SelectNodes (path);
						if (nl.Count == 0) {
							el.ParentNode.InsertBefore (el.OwnerDocument.CreateComment (" No matching elements were found for the include tag embedded here. "), el);
					
							keep_include_node = true;
						}
						foreach (XmlNode n in nl)
							el.ParentNode.InsertBefore (el.OwnerDocument.ImportNode (n, true), el);
					} catch (Exception ex) {
						el.ParentNode.InsertBefore (el.OwnerDocument.CreateComment (" Failed to insert some or all of included XML "), el);
						Report.Warning (1589, 1, mc.Location, "Unable to include XML fragment `{0}' of file `{1}' ({2})", path, file, ex.Message);
					}
				}
			}
			return keep_include_node;
		}

		//
		// Handles <see> elements.
		//
		void HandleSee (MemberCore mc, DeclSpace ds, XmlElement see)
		{
			HandleXrefCommon (mc, ds, see);
		}

		//
		// Handles <seealso> elements.
		//
		void HandleSeeAlso (MemberCore mc, DeclSpace ds, XmlElement seealso)
		{
			HandleXrefCommon (mc, ds, seealso);
		}

		//
		// Handles <exception> elements.
		//
		void HandleException (MemberCore mc, DeclSpace ds, XmlElement seealso)
		{
			HandleXrefCommon (mc, ds, seealso);
		}

		//
		// returns a full runtime type name from a name which might
		// be C# specific type name.
		//
		TypeSpec FindDocumentedType (MemberCore mc, string name, DeclSpace ds, string cref)
		{
			bool is_array = false;
			string identifier = name;
			if (name [name.Length - 1] == ']') {
				string tmp = name.Substring (0, name.Length - 1).Trim (wsChars);
				if (tmp [tmp.Length - 1] == '[') {
					identifier = tmp.Substring (0, tmp.Length - 1).Trim (wsChars);
					is_array = true;
				}
			}
			TypeSpec t = FindDocumentedTypeNonArray (mc, identifier, ds, cref);
			if (t != null && is_array)
				t = ArrayContainer.MakeType (mc.Module, t);
			return t;
		}

		TypeSpec FindDocumentedTypeNonArray (MemberCore mc, string identifier, DeclSpace ds, string cref)
		{
			var types = module.Compiler.BuiltinTypes;
			switch (identifier) {
			case "int":
				return types.Int;
			case "uint":
				return types.UInt;
			case "short":
				return types.Short;
			case "ushort":
				return types.UShort;
			case "long":
				return types.Long;
			case "ulong":
				return types.ULong;
			case "float":
				return types.Float;
			case "double":
				return types.Double;
			case "char":
				return types.Char;
			case "decimal":
				return types.Decimal;
			case "byte":
				return types.Byte;
			case "sbyte":
				return types.SByte;
			case "object":
				return types.Object;
			case "bool":
				return types.Bool;
			case "string":
				return types.String;
			case "void":
				return types.Void;
			}
			FullNamedExpression e = ds.LookupNamespaceOrType (identifier, 0, mc.Location, false);
			if (e != null) {
				if (!(e is TypeExpr))
					return null;
				return e.Type;
			}
			int index = identifier.LastIndexOf ('.');
			if (index < 0)
				return null;

			var nsName = identifier.Substring (0, index);
			var typeName = identifier.Substring (index + 1);
			Namespace ns = ds.NamespaceEntry.NS.GetNamespace (nsName, false);
			ns = ns ?? mc.Module.GlobalRootNamespace.GetNamespace(nsName, false);
			if (ns != null) {
				var te = ns.LookupType(mc, typeName, 0, true, mc.Location);
				if(te != null)
					return te.Type;
			}

			int warn;
			TypeSpec parent = FindDocumentedType (mc, identifier.Substring (0, index), ds, cref);
			if (parent == null)
				return null;
			// no need to detect warning 419 here
			var ts = FindDocumentedMember (mc, parent,
				identifier.Substring (index + 1),
				null, ds, out warn, cref, false, null) as TypeSpec;
			if (ts != null)
				return ts;
			return null;
		}

		//
		// Returns a MemberInfo that is referenced in XML documentation
		// (by "see" or "seealso" elements).
		//
		MemberSpec FindDocumentedMember (MemberCore mc,
			TypeSpec type, string member_name, AParametersCollection param_list, 
			DeclSpace ds, out int warning_type, string cref,
			bool warn419, string name_for_error)
		{
//			for (; type != null; type = type.DeclaringType) {
				var mi = FindDocumentedMemberNoNest (
					mc, type, member_name, param_list, ds,
					out warning_type, cref, warn419,
					name_for_error);
				if (mi != null)
					return mi; // new FoundMember (type, mi);
//			}
			warning_type = 0;
			return null;
		}

		MemberSpec FindDocumentedMemberNoNest (
			MemberCore mc, TypeSpec type, string member_name,
			AParametersCollection param_list, DeclSpace ds, out int warning_type, 
			string cref, bool warn419, string name_for_error)
		{
			warning_type = 0;
//			var filter = new MemberFilter (member_name, 0, MemberKind.All, param_list, null);
			IList<MemberSpec> found = null;
			while (type != null && found == null) {
				found = MemberCache.FindMembers (type, member_name, false);
				type = type.DeclaringType;
			}

			if (found == null)
				return null;

			if (warn419 && found.Count > 1) {
				Report419 (mc, name_for_error, found.ToArray ());
			}

			return found [0];

/*
			if (param_list == null) {
				// search for fields/events etc.
				mis = TypeManager.MemberLookup (type, null,
					type, MemberKind.All,
					BindingRestriction.None,
					member_name, null);
				mis = FilterOverridenMembersOut (mis);
				if (mis == null || mis.Length == 0)
					return null;
				if (warn419 && IsAmbiguous (mis))
					Report419 (mc, name_for_error, mis, Report);
				return mis [0];
			}

			MethodSignature msig = new MethodSignature (member_name, null, param_list);
			mis = FindMethodBase (type, 
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance,
				msig);

			if (warn419 && mis.Length > 0) {
				if (IsAmbiguous (mis))
					Report419 (mc, name_for_error, mis, Report);
				return mis [0];
			}

			// search for operators (whose parameters exactly
			// matches with the list) and possibly report CS1581.
			string oper = null;
			string return_type_name = null;
			if (member_name.StartsWith ("implicit operator ")) {
				Operator.GetMetadataName (Operator.OpType.Implicit);
				return_type_name = member_name.Substring (18).Trim (wsChars);
			}
			else if (member_name.StartsWith ("explicit operator ")) {
				oper = Operator.GetMetadataName (Operator.OpType.Explicit);
				return_type_name = member_name.Substring (18).Trim (wsChars);
			}
			else if (member_name.StartsWith ("operator ")) {
				oper = member_name.Substring (9).Trim (wsChars);
				switch (oper) {
				// either unary or binary
				case "+":
					oper = param_list.Length == 2 ?
						Operator.GetMetadataName (Operator.OpType.Addition) :
						Operator.GetMetadataName (Operator.OpType.UnaryPlus);
					break;
				case "-":
					oper = param_list.Length == 2 ?
						Operator.GetMetadataName (Operator.OpType.Subtraction) :
						Operator.GetMetadataName (Operator.OpType.UnaryNegation);
					break;
				default:
					oper = Operator.GetMetadataName (oper);
					if (oper != null)
						break;

					warning_type = 1584;
					Report.Warning (1020, 1, mc.Location, "Overloadable {0} operator is expected", param_list.Length == 2 ? "binary" : "unary");
					Report.Warning (1584, 1, mc.Location, "XML comment on `{0}' has syntactically incorrect cref attribute `{1}'",
						mc.GetSignatureForError (), cref);
					return null;
				}
			}
			// here we still don't consider return type (to
			// detect CS1581 or CS1002+CS1584).
			msig = new MethodSignature (oper, null, param_list);

			mis = FindMethodBase (type, 
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance,
				msig);
			if (mis.Length == 0)
				return null; // CS1574
			var mi = mis [0];
			TypeSpec expected = mi is MethodSpec ?
				((MethodSpec) mi).ReturnType :
				mi is PropertySpec ?
				((PropertySpec) mi).PropertyType :
				null;
			if (return_type_name != null) {
				TypeSpec returnType = FindDocumentedType (mc, return_type_name, ds, cref, Report);
				if (returnType == null || returnType != expected) {
					warning_type = 1581;
					Report.Warning (1581, 1, mc.Location, "Invalid return type in XML comment cref attribute `{0}'", cref);
					return null;
				}
			}
			return mis [0];
*/ 
		}

		//
		// Processes "see" or "seealso" elements.
		// Checks cref attribute.
		//
		void HandleXrefCommon (MemberCore mc, DeclSpace ds, XmlElement xref)
		{
			string cref = xref.GetAttribute ("cref").Trim (wsChars);
			// when, XmlReader, "if (cref == null)"
			if (!xref.HasAttribute ("cref"))
				return;
			if (cref.Length == 0)
				Report.Warning (1001, 1, mc.Location, "Identifier expected");
				// ... and continue until CS1584.

			string signature; // "x:" are stripped
			string name; // method invokation "(...)" are removed
			string parameters; // method parameter list

			// When it found '?:' ('T:' 'M:' 'F:' 'P:' 'E:' etc.),
			// MS ignores not only its member kind, but also
			// the entire syntax correctness. Nor it also does
			// type fullname resolution i.e. "T:List(int)" is kept
			// as T:List(int), not
			// T:System.Collections.Generic.List&lt;System.Int32&gt;
			if (cref.Length > 2 && cref [1] == ':')
				return;
			else
				signature = cref;

			// Also note that without "T:" any generic type 
			// indication fails.

			int parens_pos = signature.IndexOf ('(');
			int brace_pos = parens_pos >= 0 ? -1 :
				signature.IndexOf ('[');
			if (parens_pos > 0 && signature [signature.Length - 1] == ')') {
				name = signature.Substring (0, parens_pos).Trim (wsChars);
				parameters = signature.Substring (parens_pos + 1, signature.Length - parens_pos - 2).Trim (wsChars);
			}
			else if (brace_pos > 0 && signature [signature.Length - 1] == ']') {
				name = signature.Substring (0, brace_pos).Trim (wsChars);
				parameters = signature.Substring (brace_pos + 1, signature.Length - brace_pos - 2).Trim (wsChars);
			}
			else {
				name = signature;
				parameters = null;
			}
			Normalize (mc, ref name);

			string identifier = GetBodyIdentifierFromName (name);

			// Check if identifier is valid.
			// This check is not necessary to mark as error, but
			// csc specially reports CS1584 for wrong identifiers.
			string [] name_elems = identifier.Split ('.');
			for (int i = 0; i < name_elems.Length; i++) {
				string nameElem = GetBodyIdentifierFromName (name_elems [i]);
				if (i > 0)
					Normalize (mc, ref nameElem);
				if (!Tokenizer.IsValidIdentifier (nameElem)
					&& nameElem.IndexOf ("operator") < 0) {
					Report.Warning (1584, 1, mc.Location, "XML comment on `{0}' has syntactically incorrect cref attribute `{1}'",
						mc.GetSignatureForError (), cref);
					xref.SetAttribute ("cref", "!:" + signature);
					return;
				}
			}

			// check if parameters are valid
			AParametersCollection parameter_types;
			if (parameters == null)
				parameter_types = null;
			else if (parameters.Length == 0)
				parameter_types = ParametersCompiled.EmptyReadOnlyParameters;
			else {
				string [] param_list = parameters.Split (',');
				var plist = new List<TypeSpec> ();
				for (int i = 0; i < param_list.Length; i++) {
					string param_type_name = param_list [i].Trim (wsChars);
					Normalize (mc, ref param_type_name);
					TypeSpec param_type = FindDocumentedType (mc, param_type_name, ds, cref);
					if (param_type == null) {
						Report.Warning (1580, 1, mc.Location, "Invalid type for parameter `{0}' in XML comment cref attribute `{1}'",
							(i + 1).ToString (), cref);
						return;
					}
					plist.Add (param_type);
				}

				parameter_types = ParametersCompiled.CreateFullyResolved (plist.ToArray ());
			}

			TypeSpec type = FindDocumentedType (mc, name, ds, cref);
			if (type != null
				// delegate must not be referenced with args
				&& (!type.IsDelegate
				|| parameter_types == null)) {
				string result = GetSignatureForDoc (type)
					+ (brace_pos < 0 ? String.Empty : signature.Substring (brace_pos));
				xref.SetAttribute ("cref", "T:" + result);
				return; // a type
			}

			int period = name.LastIndexOf ('.');
			if (period > 0) {
				string typeName = name.Substring (0, period);
				string member_name = name.Substring (period + 1);
				string lookup_name = member_name == "this" ? MemberCache.IndexerNameAlias : member_name;
				Normalize (mc, ref lookup_name);
				Normalize (mc, ref member_name);
				type = FindDocumentedType (mc, typeName, ds, cref);
				int warn_result;
				if (type != null) {
					var mi = FindDocumentedMember (mc, type, lookup_name, parameter_types, ds, out warn_result, cref, true, name);
					if (warn_result > 0)
						return;
					if (mi != null) {
						// we cannot use 'type' directly
						// to get its name, since mi
						// could be from DeclaringType
						// for nested types.
						xref.SetAttribute ("cref", GetMemberDocHead (mi) + GetSignatureForDoc (mi.DeclaringType) + "." + member_name + GetParametersFormatted (mi));
						return; // a member of a type
					}
				}
			} else {
				int warn_result;
				var mi = FindDocumentedMember (mc, ds.PartialContainer.Definition, name, parameter_types, ds, out warn_result, cref, true, name);

				if (warn_result > 0)
					return;
				if (mi != null) {
					// we cannot use 'type' directly
					// to get its name, since mi
					// could be from DeclaringType
					// for nested types.
					xref.SetAttribute ("cref", GetMemberDocHead (mi) + GetSignatureForDoc (mi.DeclaringType) + "." + name + GetParametersFormatted (mi));
					return; // local member name
				}
			}

			// It still might be part of namespace name.
			Namespace ns = ds.NamespaceEntry.NS.GetNamespace (name, false);
			if (ns != null) {
				xref.SetAttribute ("cref", "N:" + ns.GetSignatureForError ());
				return; // a namespace
			}
			if (mc.Module.GlobalRootNamespace.IsNamespace (name)) {
				xref.SetAttribute ("cref", "N:" + name);
				return; // a namespace
			}

			Report.Warning (1574, 1, mc.Location, "XML comment on `{0}' has cref attribute `{1}' that could not be resolved",
				mc.GetSignatureForError (), cref);

			xref.SetAttribute ("cref", "!:" + name);
		}

		static string GetParametersFormatted (MemberSpec mi)
		{
			var pm = mi as IParametersMember;
			if (pm == null || pm.Parameters.IsEmpty)
				return string.Empty;

			AParametersCollection parameters = pm.Parameters;
/*
			if (parameters == null || parameters.Count == 0)
				return String.Empty;
*/
			StringBuilder sb = new StringBuilder ();
			sb.Append ('(');
			for (int i = 0; i < parameters.Count; i++) {
//				if (is_setter && i + 1 == parameters.Count)
//					break; // skip "value".
				if (i > 0)
					sb.Append (',');
				TypeSpec t = parameters.Types [i];
				sb.Append (GetSignatureForDoc (t));
			}
			sb.Append (')');
			return sb.ToString ();
		}

		static string GetBodyIdentifierFromName (string name)
		{
			string identifier = name;

			if (name.Length > 0 && name [name.Length - 1] == ']') {
				string tmp = name.Substring (0, name.Length - 1).Trim (wsChars);
				int last = tmp.LastIndexOf ('[');
				if (last > 0)
					identifier = tmp.Substring (0, last).Trim (wsChars);
			}

			return identifier;
		}

		void Report419 (MemberCore mc, string member_name, MemberSpec [] mis)
		{
			Report.Warning (419, 3, mc.Location, 
				"Ambiguous reference in cref attribute `{0}'. Assuming `{1}' but other overloads including `{2}' have also matched",
				member_name,
				TypeManager.GetFullNameSignature (mis [0]),
				TypeManager.GetFullNameSignature (mis [1]));
		}

		//
		// Get a prefix from member type for XML documentation (used
		// to formalize cref target name).
		//
		static string GetMemberDocHead (MemberSpec type)
		{
			if (type is FieldSpec)
				return "F:";
			if (type is MethodSpec)
				return "M:";
			if (type is EventSpec)
				return "E:";
			if (type is PropertySpec)
				return "P:";
			if (type is TypeSpec)
				return "T:";

			return "!:";
		}

		// MethodCore

		//
		// Returns a string that represents the signature for this 
		// member which should be used in XML documentation.
		//
		public static string GetMethodDocCommentName (MemberCore mc, ParametersCompiled parameters)
		{
			IParameterData [] plist = parameters.FixedParameters;
			string paramSpec = String.Empty;
			if (plist != null) {
				StringBuilder psb = new StringBuilder ();
				int i = 0;
				foreach (Parameter p in plist) {
					psb.Append (psb.Length != 0 ? "," : "(");
					psb.Append (GetSignatureForDoc (parameters.Types [i++]));
					if ((p.ModFlags & Parameter.Modifier.ISBYREF) != 0)
						psb.Append ('@');
				}
				paramSpec = psb.ToString ();
			}

			if (paramSpec.Length > 0)
				paramSpec += ")";

			string name = mc.Name;
			if (mc is Constructor)
				name = "#ctor";
			else if (mc is InterfaceMemberBase) {
				var imb = (InterfaceMemberBase) mc;
				name = imb.GetFullName (imb.ShortName);
			}
			name = name.Replace ('.', '#');

			if (mc.MemberName.TypeArguments != null && mc.MemberName.TypeArguments.Count > 0)
				name += "``" + mc.MemberName.CountTypeArguments;

			string suffix = String.Empty;
			Operator op = mc as Operator;
			if (op != null) {
				switch (op.OperatorType) {
				case Operator.OpType.Implicit:
				case Operator.OpType.Explicit:
					suffix = "~" + GetSignatureForDoc (op.ReturnType);
					break;
				}
			}
			return String.Concat (mc.DocCommentHeader, mc.Parent.Name, ".", name, paramSpec, suffix);
		}

		static string GetSignatureForDoc (TypeSpec type)
		{
			var tp = type as TypeParameterSpec;
			if (tp != null) {
				int c = 0;
				type = type.DeclaringType;
				while (type != null && type.DeclaringType != null) {
					type = type.DeclaringType;
					c += type.MemberDefinition.TypeParametersCount;
				}
				var prefix = tp.IsMethodOwned ? "``" : "`";
				return prefix + (c + tp.DeclaredPosition);
			}

			var pp = type as PointerContainer;
			if (pp != null)
				return GetSignatureForDoc (pp.Element) + "*";

			ArrayContainer ap = type as ArrayContainer;
			if (ap != null)
				return GetSignatureForDoc (ap.Element) +
					ArrayContainer.GetPostfixSignature (ap.Rank);

			if (TypeManager.IsGenericType (type)) {
				string g = type.MemberDefinition.Namespace;
				if (g != null && g.Length > 0)
					g += '.';
				int idx = type.Name.LastIndexOf ('`');
				g += (idx < 0 ? type.Name : type.Name.Substring (0, idx)) + '{';
				int argpos = 0;
				foreach (TypeSpec t in TypeManager.GetTypeArguments (type))
					g += (argpos++ > 0 ? "," : String.Empty) + GetSignatureForDoc (t);
				g += '}';
				return g;
			}

			string name = type.GetMetaInfo ().FullName != null ? type.GetMetaInfo ().FullName : type.Name;
			return name.Replace ("+", ".").Replace ('&', '@');
		}

		//
		// Raised (and passed an XmlElement that contains the comment)
		// when GenerateDocComment is writing documentation expectedly.
		//
		// FIXME: with a few effort, it could be done with XmlReader,
		// that means removal of DOM use.
		//
		internal static void OnMethodGenerateDocComment (
			MethodCore mc, XmlElement el, Report Report)
		{
			var paramTags = new Dictionary<string, string> ();
			foreach (XmlElement pelem in el.SelectNodes ("param")) {
				string xname = pelem.GetAttribute ("name");
				if (xname.Length == 0)
					continue; // really? but MS looks doing so
				if (xname != "" && mc.ParameterInfo.GetParameterIndexByName (xname) < 0)
					Report.Warning (1572, 2, mc.Location, "XML comment on `{0}' has a param tag for `{1}', but there is no parameter by that name",
						mc.GetSignatureForError (), xname);
				else if (paramTags.ContainsKey (xname))
					Report.Warning (1571, 2, mc.Location, "XML comment on `{0}' has a duplicate param tag for `{1}'",
						mc.GetSignatureForError (), xname);
				paramTags [xname] = xname;
			}
			IParameterData [] plist = mc.ParameterInfo.FixedParameters;
			foreach (Parameter p in plist) {
				if (paramTags.Count > 0 && !paramTags.ContainsKey (p.Name))
					Report.Warning (1573, 4, mc.Location, "Parameter `{0}' has no matching param tag in the XML comment for `{1}'",
						p.Name, mc.GetSignatureForError ());
			}
		}

		void Normalize (MemberCore mc, ref string name)
		{
			if (name.Length > 0 && name [0] == '@')
				name = name.Substring (1);
			else if (name == "this")
				name = "Item";
			else if (Tokenizer.IsKeyword (name) && !IsTypeName (name))
				Report.Warning (1041, 1, mc.Location, "Identifier expected. `{0}' is a keyword", name);
		}

		private static bool IsTypeName (string name)
		{
			switch (name) {
			case "bool":
			case "byte":
			case "char":
			case "decimal":
			case "double":
			case "float":
			case "int":
			case "long":
			case "object":
			case "sbyte":
			case "short":
			case "string":
			case "uint":
			case "ulong":
			case "ushort":
			case "void":
				return true;
			}
			return false;
		}

		//
		// Outputs XML documentation comment from tokenized comments.
		//
		public bool OutputDocComment (string asmfilename, string xmlFileName)
		{
			XmlTextWriter w = null;
			try {
				w = new XmlTextWriter (xmlFileName, null);
				w.Indentation = 4;
				w.Formatting = Formatting.Indented;
				w.WriteStartDocument ();
				w.WriteStartElement ("doc");
				w.WriteStartElement ("assembly");
				w.WriteStartElement ("name");
				w.WriteString (Path.GetFileNameWithoutExtension (asmfilename));
				w.WriteEndElement (); // name
				w.WriteEndElement (); // assembly
				w.WriteStartElement ("members");
				XmlCommentOutput = w;
				module.GenerateDocComment (this);
				w.WriteFullEndElement (); // members
				w.WriteEndElement ();
				w.WriteWhitespace (Environment.NewLine);
				w.WriteEndDocument ();
				return true;
			} catch (Exception ex) {
				module.Compiler.Report.Error (1569, "Error generating XML documentation file `{0}' (`{1}')", xmlFileName, ex.Message);
				return false;
			} finally {
				if (w != null)
					w.Close ();
			}
		}
	}
}
