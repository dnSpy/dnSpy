//
// doc.cs: Support for XML documentation comment.
//
// Authors:
//	Atsushi Enomoto <atsushi@ximian.com>
//  Marek Safar (marek.safar@gmail.com>
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
		XmlWriter XmlCommentOutput;

		static readonly string line_head = Environment.NewLine + "            ";

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

		public MemberName ParsedName {
			get; set;
		}

		public List<DocumentationParameter> ParsedParameters {
			get; set;
		}

		public TypeExpression ParsedBuiltinType {
			get; set;
		}

		public Operator.OpType? ParsedOperator {
			get; set;
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
			string name = mc.DocCommentHeader + mc.GetSignatureForDocumentation ();

			XmlNode n = GetDocCommentNode (mc, name);

			XmlElement el = n as XmlElement;
			if (el != null) {
				var pm = mc as IParametersMember;
				if (pm != null) {
					CheckParametersComments (mc, pm, el);
				}

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

		FullNamedExpression ResolveMemberName (IMemberContext context, MemberName mn)
		{
			if (mn.Left == null)
				return context.LookupNamespaceOrType (mn.Name, mn.Arity, LookupMode.Probing, Location.Null);

			var left = ResolveMemberName (context, mn.Left);
			var ns = left as Namespace;
			if (ns != null)
				return ns.LookupTypeOrNamespace (context, mn.Name, mn.Arity, LookupMode.Probing, Location.Null);

			TypeExpr texpr = left as TypeExpr;
			if (texpr != null) {
				var found = MemberCache.FindNestedType (texpr.Type, ParsedName.Name, ParsedName.Arity);
				if (found != null)
					return new TypeExpression (found, Location.Null);

				return null;
			}

			return left;
		}

		//
		// Processes "see" or "seealso" elements from cref attribute.
		//
		void HandleXrefCommon (MemberCore mc, DeclSpace ds, XmlElement xref)
		{
			string cref = xref.GetAttribute ("cref");
			// when, XmlReader, "if (cref == null)"
			if (!xref.HasAttribute ("cref"))
				return;

			// Nothing to be resolved the reference is marked explicitly
			if (cref.Length > 2 && cref [1] == ':')
				return;

			// Additional symbols for < and > are allowed for easier XML typing
			cref = cref.Replace ('{', '<').Replace ('}', '>');

			var encoding = module.Compiler.Settings.Encoding;
			var s = new MemoryStream (encoding.GetBytes (cref));
			SeekableStreamReader seekable = new SeekableStreamReader (s, encoding);

			var source_file = new CompilationSourceFile ("{documentation}", "", 1);
			var doc_module = new ModuleContainer (module.Compiler);
			doc_module.DocumentationBuilder = this;
			source_file.NamespaceContainer = new NamespaceContainer (null, doc_module, null, source_file);

			Report parse_report = new Report (new NullReportPrinter ());
			var parser = new CSharpParser (seekable, source_file, parse_report);
			ParsedParameters = null;
			ParsedName = null;
			ParsedBuiltinType = null;
			ParsedOperator = null;
			parser.Lexer.putback_char = Tokenizer.DocumentationXref;
			parser.Lexer.parsing_generic_declaration_doc = true;
			parser.parse ();
			if (parse_report.Errors > 0) {
				Report.Warning (1584, 1, mc.Location, "XML comment on `{0}' has syntactically incorrect cref attribute `{1}'",
					mc.GetSignatureForError (), cref);

				xref.SetAttribute ("cref", "!:" + cref);
				return;
			}

			MemberSpec member;
			string prefix = null;
			FullNamedExpression fne = null;

			//
			// Try built-in type first because we are using ParsedName as identifier of
			// member names on built-in types
			//
			if (ParsedBuiltinType != null && (ParsedParameters == null || ParsedName != null)) {
				member = ParsedBuiltinType.Type;
			} else {
				member = null;
			}

			if (ParsedName != null || ParsedOperator.HasValue) {
				TypeSpec type = null;
				string member_name = null;

				if (member == null) {
					if (ParsedOperator.HasValue) {
						type = mc.CurrentType;
					} else if (ParsedName.Left != null) {
						fne = ResolveMemberName (mc, ParsedName.Left);
						if (fne != null) {
							var ns = fne as Namespace;
							if (ns != null) {
								fne = ns.LookupTypeOrNamespace (mc, ParsedName.Name, ParsedName.Arity, LookupMode.Probing, Location.Null);
								if (fne != null) {
									member = fne.Type;
								}
							} else {
								type = fne.Type;
							}
						}
					} else {
						fne = ResolveMemberName (mc, ParsedName);
						if (fne == null) {
							type = mc.CurrentType;
						} else if (ParsedParameters == null) {
							member = fne.Type;
						} else if (fne.Type.MemberDefinition == mc.CurrentType.MemberDefinition) {
							member_name = Constructor.ConstructorName;
							type = fne.Type;
						}
					}
				} else {
					type = (TypeSpec) member;
					member = null;
				}

				if (ParsedParameters != null) {
					var old_printer = mc.Module.Compiler.Report.SetPrinter (new NullReportPrinter ());
					foreach (var pp in ParsedParameters) {
						pp.Resolve (mc);
					}
					mc.Module.Compiler.Report.SetPrinter (old_printer);
				}

				if (type != null) {
					if (member_name == null)
						member_name = ParsedOperator.HasValue ?
							Operator.GetMetadataName (ParsedOperator.Value) : ParsedName.Name;

					int parsed_param_count;
					if (ParsedOperator == Operator.OpType.Explicit || ParsedOperator == Operator.OpType.Implicit) {
						parsed_param_count = ParsedParameters.Count - 1;
					} else if (ParsedParameters != null) {
						parsed_param_count = ParsedParameters.Count;
					} else {
						parsed_param_count = 0;
					}

					int parameters_match = -1;
					do {
						var members = MemberCache.FindMembers (type, member_name, true);
						if (members != null) {
							foreach (var m in members) {
								if (ParsedName != null && m.Arity != ParsedName.Arity)
									continue;

								if (ParsedParameters != null) {
									IParametersMember pm = m as IParametersMember;
									if (pm == null)
										continue;

									if (m.Kind == MemberKind.Operator && !ParsedOperator.HasValue)
										continue;

									int i;
									for (i = 0; i < parsed_param_count; ++i) {
										var pparam = ParsedParameters[i];

										if (i >= pm.Parameters.Count || pparam == null ||
											pparam.TypeSpec != pm.Parameters.Types[i] ||
											(pparam.Modifier & Parameter.Modifier.SignatureMask) != (pm.Parameters.FixedParameters[i].ModFlags & Parameter.Modifier.SignatureMask)) {

											if (i > parameters_match) {
												parameters_match = i;
											}

											i = -1;
											break;
										}
									}

									if (i < 0)
										continue;

									if (ParsedOperator == Operator.OpType.Explicit || ParsedOperator == Operator.OpType.Implicit) {
										if (pm.MemberType != ParsedParameters[parsed_param_count].TypeSpec) {
											parameters_match = parsed_param_count + 1;
											continue;
										}
									} else {
										if (parsed_param_count != pm.Parameters.Count)
											continue;
									}
								}

								if (member != null) {
									Report.Warning (419, 3, mc.Location,
										"Ambiguous reference in cref attribute `{0}'. Assuming `{1}' but other overloads including `{2}' have also matched",
										cref, member.GetSignatureForError (), m.GetSignatureForError ());

									break;
								}

								member = m;
							}
						}

						// Continue with parent type for nested types
						if (member == null) {
							type = type.DeclaringType;
						} else {
							type = null;
						}
					} while (type != null);

					if (member == null && parameters_match >= 0) {
						for (int i = parameters_match; i < parsed_param_count; ++i) {
							Report.Warning (1580, 1, mc.Location, "Invalid type for parameter `{0}' in XML comment cref attribute `{1}'",
									(i + 1).ToString (), cref);
						}

						if (parameters_match == parsed_param_count + 1) {
							Report.Warning (1581, 1, mc.Location, "Invalid return type in XML comment cref attribute `{0}'", cref);
						}
					}
				}
			}

			if (member == null) {
				Report.Warning (1574, 1, mc.Location, "XML comment on `{0}' has cref attribute `{1}' that could not be resolved",
					mc.GetSignatureForError (), cref);
				cref = "!:" + cref;
			} else if (member == InternalType.Namespace) {
				cref = "N:" + fne.GetSignatureForError ();
			} else {
				prefix = GetMemberDocHead (member);
				cref = prefix + member.GetSignatureForDocumentation ();
			}

			xref.SetAttribute ("cref", cref);
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

			throw new NotImplementedException (type.GetType ().ToString ());
		}

		//
		// Raised (and passed an XmlElement that contains the comment)
		// when GenerateDocComment is writing documentation expectedly.
		//
		// FIXME: with a few effort, it could be done with XmlReader,
		// that means removal of DOM use.
		//
		void CheckParametersComments (MemberCore member, IParametersMember paramMember, XmlElement el)
		{
			HashSet<string> found_tags = null;
			foreach (XmlElement pelem in el.SelectNodes ("param")) {
				string xname = pelem.GetAttribute ("name");
				if (xname.Length == 0)
					continue; // really? but MS looks doing so

				if (found_tags == null) {
					found_tags = new HashSet<string> ();
				}

				if (xname != "" && paramMember.Parameters.GetParameterIndexByName (xname) < 0) {
					Report.Warning (1572, 2, member.Location,
						"XML comment on `{0}' has a param tag for `{1}', but there is no parameter by that name",
						member.GetSignatureForError (), xname);
					continue;
				}

				if (found_tags.Contains (xname)) {
					Report.Warning (1571, 2, member.Location,
						"XML comment on `{0}' has a duplicate param tag for `{1}'",
						member.GetSignatureForError (), xname);
					continue;
				}

				found_tags.Add (xname);
			}

			if (found_tags != null) {
				foreach (Parameter p in paramMember.Parameters.FixedParameters) {
					if (!found_tags.Contains (p.Name) && !(p is ArglistParameter))
						Report.Warning (1573, 4, member.Location,
							"Parameter `{0}' has no matching param tag in the XML comment for `{1}'",
							p.Name, member.GetSignatureForError ());
				}
			}
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
				Report.Error (1569, "Error generating XML documentation file `{0}' (`{1}')", xmlFileName, ex.Message);
				return false;
			} finally {
				if (w != null)
					w.Close ();
			}
		}
	}

	class DocumentationParameter
	{
		public readonly Parameter.Modifier Modifier;
		public FullNamedExpression Type;
		TypeSpec type;

		public DocumentationParameter (Parameter.Modifier modifier, FullNamedExpression type)
			: this (type)
		{
			this.Modifier = modifier;
		}

		public DocumentationParameter (FullNamedExpression type)
		{
			this.Type = type;
		}

		public TypeSpec TypeSpec {
			get {
				return type;
			}
		}

		public void Resolve (IMemberContext context)
		{
			type = Type.ResolveAsType (context);
		}
	}
}
