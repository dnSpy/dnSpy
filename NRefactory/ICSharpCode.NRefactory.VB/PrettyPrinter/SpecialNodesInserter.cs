// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB.PrettyPrinter
{
//	public class SpecialOutputVisitor : ISpecialVisitor
//	{
//		readonly IOutputFormatter formatter;
//		
//		public SpecialOutputVisitor(IOutputFormatter formatter)
//		{
//			this.formatter = formatter;
//		}
//		
//		public bool ForceWriteInPreviousLine;
//		
//		public object Visit(ISpecial special, object data)
//		{
//			Console.WriteLine("Warning: SpecialOutputVisitor.Visit(ISpecial) called with " + special);
//			return data;
//		}
//		
//		public object Visit(BlankLine special, object data)
//		{
//			formatter.PrintBlankLine(ForceWriteInPreviousLine);
//			return data;
//		}
//		
//		public object Visit(Comment special, object data)
//		{
//			formatter.PrintComment(special, ForceWriteInPreviousLine);
//			return data;
//		}
//		
//		public object Visit(PreprocessingDirective special, object data)
//		{
//			formatter.PrintPreprocessingDirective(special, ForceWriteInPreviousLine);
//			return data;
//		}
//	}
//	
//	/// <summary>
//	/// This class inserts specials between INodes.
//	/// </summary>
//	public sealed class SpecialNodesInserter : IDisposable
//	{
//		IEnumerator<ISpecial> enumerator;
//		SpecialOutputVisitor visitor;
//		bool available; // true when more specials are available
//		
//		public SpecialNodesInserter(IEnumerable<ISpecial> specials, SpecialOutputVisitor visitor)
//		{
//			if (specials == null) throw new ArgumentNullException("specials");
//			if (visitor == null) throw new ArgumentNullException("visitor");
//			enumerator = specials.GetEnumerator();
//			this.visitor = visitor;
//			available = enumerator.MoveNext();
//		}
//		
//		void WriteCurrent()
//		{
//			enumerator.Current.AcceptVisitor(visitor, null);
//			available = enumerator.MoveNext();
//		}
//		
//		AttributedNode currentAttributedNode;
//		
//		/// <summary>
//		/// Writes all specials up to the start position of the node.
//		/// </summary>
//		public void AcceptNodeStart(INode node)
//		{
//			if (node is AttributedNode) {
//				currentAttributedNode = node as AttributedNode;
//				if (currentAttributedNode.Attributes.Count == 0) {
//					AcceptPoint(node.StartLocation);
//					currentAttributedNode = null;
//				}
//			} else {
//				AcceptPoint(node.StartLocation);
//			}
//		}
//		
//		/// <summary>
//		/// Writes all specials up to the end position of the node.
//		/// </summary>
//		public void AcceptNodeEnd(INode node)
//		{
//			visitor.ForceWriteInPreviousLine = true;
//			AcceptPoint(node.EndLocation);
//			visitor.ForceWriteInPreviousLine = false;
//			if (currentAttributedNode != null) {
//				if (node == currentAttributedNode.Attributes[currentAttributedNode.Attributes.Count - 1]) {
//					AcceptPoint(currentAttributedNode.StartLocation);
//					currentAttributedNode = null;
//				}
//			}
//		}
//		
//		/// <summary>
//		/// Writes all specials up to the specified location.
//		/// </summary>
//		public void AcceptPoint(Location loc)
//		{
//			while (available && enumerator.Current.StartPosition <= loc) {
//				WriteCurrent();
//			}
//		}
//		
//		/// <summary>
//		/// Outputs all missing specials to the writer.
//		/// </summary>
//		public void Finish()
//		{
//			while (available) {
//				WriteCurrent();
//			}
//		}
//		
//		void IDisposable.Dispose()
//		{
//			Finish();
//		}
//		
//		/// <summary>
//		/// Registers a new SpecialNodesInserter with the output visitor.
//		/// Make sure to call Finish() (or Dispose()) on the returned SpecialNodesInserter
//		/// when the output is finished.
//		/// </summary>
//		public static SpecialNodesInserter Install(IEnumerable<ISpecial> specials, IOutputDomVisitor outputVisitor)
//		{
//			SpecialNodesInserter sni = new SpecialNodesInserter(specials, new SpecialOutputVisitor(outputVisitor.OutputFormatter));
//			outputVisitor.BeforeNodeVisit += sni.AcceptNodeStart;
//			outputVisitor.AfterNodeVisit  += sni.AcceptNodeEnd;
//			return sni;
//		}
//	}
}
