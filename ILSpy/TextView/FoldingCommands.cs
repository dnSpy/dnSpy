/*
 * Created by SharpDevelop.
 * User: Ronny Klier
 * Date: 24.05.2012
 * Time: 23:44
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.AvalonEdit.Folding;

namespace ICSharpCode.ILSpy.TextView
{
  [ExportContextMenuEntryAttribute(Header = "Toggle All Folding", Category = "Folding")]
  internal sealed class ToggleAllContextMenuEntry : IContextMenuEntry
  {
    public bool IsVisible(TextViewContext context)
    {
      return context.TextView != null;				
    }
    
    public bool IsEnabled(TextViewContext context)
    {
      return context.TextView != null && context.TextView.FoldingManager != null;
    }
    
    public void Execute(TextViewContext context)
    {
      if (null == context.TextView)
        return;
      FoldingManager foldingManager = context.TextView.FoldingManager;
      if (null == foldingManager)
        return;
        bool doFold = true;
        foreach (FoldingSection fm in foldingManager.AllFoldings) {
          if (fm.IsFolded) {
            doFold = false;
            break;
          }
        }
        foreach (FoldingSection fm in foldingManager.AllFoldings) {
          fm.IsFolded = doFold;
        }
    }
  }

  [ExportContextMenuEntryAttribute(Header = "Toggle Folding", Category = "Folding")]
  internal sealed class ToggleContextMenuEntry : IContextMenuEntry
  {
    public bool IsVisible(TextViewContext context)
    {
      return context.TextView != null;				
    }
    
    public bool IsEnabled(TextViewContext context)
    {
      return context.TextView != null && context.TextView.FoldingManager != null;
    }
    
    public void Execute(TextViewContext context)
    {
      if (null == context.TextView)
        return;
      FoldingManager foldingManager = context.TextView.FoldingManager;
      if (null == foldingManager)
        return;
      int offset = context.TextView.CurrentOffset;
      FoldingSection folding = foldingManager.GetNextFolding(offset);
      if (folding == null || folding.StartOffset != offset) {
        // no folding found on current line: find innermost folding containing the caret
        folding = foldingManager.GetFoldingsContaining(offset).LastOrDefault();
      }
      if (folding != null) {
        folding.IsFolded = !folding.IsFolded;
      }
    }
  }
  
}
