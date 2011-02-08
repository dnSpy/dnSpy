// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Visitors
{
	/// <summary>
	/// Renames local variables if they conflict with other locals, fields or parameters.
	/// </summary>
	static class ToVBNetRenameConflictingVariablesVisitor
	{
		public static void RenameConflicting(ParametrizedNode method)
		{
			// variable name => case sensitive variable name
			// value is null if there are multiple casings for the variable -> the variable is conflicting
			Dictionary<string, string> caseInsensitive = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			
			LookupTableVisitor ltv = new LookupTableVisitor(SupportedLanguage.CSharp);
			method.AcceptVisitor(ltv, null);
			
			// add method parameters to caseInsensitive
			foreach (ParameterDeclarationExpression pde in method.Parameters) {
				AddVariableToDict(caseInsensitive, pde.ParameterName, true);
			}
			
			// add local variables to caseInsensitive
			foreach (KeyValuePair<string, List<LocalLookupVariable>> var in ltv.Variables) {
				AddVariableToDict(caseInsensitive, var.Key, true);
			}
			
			// add used identifiers to caseInsensitive
			FindIdentifiersVisitor fvv = new FindIdentifiersVisitor();
			method.AcceptVisitor(fvv, null);
			
			foreach (KeyValuePair<string, string> pair in fvv.usedIdentifiers) {
				AddVariableToDict(caseInsensitive, pair.Key, false);
			}
			
			int index = 0;
			foreach (ParameterDeclarationExpression pde in method.Parameters) {
				if (caseInsensitive[pde.ParameterName] == null) {
					RenameVariable(method, pde.ParameterName, ref index);
				}
			}
			foreach (KeyValuePair<string, List<LocalLookupVariable>> var in ltv.Variables) {
				if (caseInsensitive[var.Key] == null) {
					RenameVariable(method, var.Key, ref index);
				}
			}
		}
		
		static void RenameVariable(INode method, string from, ref int index)
		{
			index += 1;
			method.AcceptVisitor(new RenameLocalVariableVisitor(from, from + "__" + index, StringComparer.InvariantCulture), null);
		}
		
		static void AddVariableToDict(Dictionary<string, string> caseInsensitive, string varName, bool hasDeclaration)
		{
			string existing;
			if (caseInsensitive.TryGetValue(varName, out existing)) {
				if (existing != null && existing != varName) {
					caseInsensitive[varName] = null;
				}
			} else {
				if (hasDeclaration) {
					caseInsensitive.Add(varName, varName);
				}
			}
		}
		
		sealed class FindIdentifiersVisitor : AbstractAstVisitor
		{
			// use dictionary as HashSet to remember used identifiers
			internal readonly Dictionary<string, string> usedIdentifiers = new Dictionary<string, string>();
			
			public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
			{
				usedIdentifiers[identifierExpression.Identifier] = null;
				return null;
			}
		}
	}
}
