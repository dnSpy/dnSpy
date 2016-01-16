using System.Linq;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.Decompiler.Ast.Transforms {
	class FlattenSwitchBlocks : IAstTransform
	{
		public void Run(AstNode compilationUnit)
		{
			foreach (var switchSection in compilationUnit.Descendants.OfType<SwitchSection>())
			{
				if (switchSection.Statements.Count != 1)
					continue;

				var blockStatement = switchSection.Statements.First() as BlockStatement;
				if (blockStatement == null || blockStatement.Statements.Any(st => st is VariableDeclarationStatement))
					continue;
				if (blockStatement.HiddenStart != null || blockStatement.HiddenEnd != null)
					continue;
				if (blockStatement.GetAllILRanges().Count > 0)
					continue;

				blockStatement.Remove();
				blockStatement.Statements.MoveTo(switchSection.Statements);
			}
		}
	}
}
