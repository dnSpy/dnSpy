// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using dnSpy.Roslyn.EditorFeatures.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Roslyn.EditorFeatures.SmartIndent
{
    [Export(typeof(ISmartIndentProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class SmartIndentProvider : ISmartIndentProvider
    {
        public ISmartIndent CreateSmartIndent(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            //if (!textView.TextBuffer.GetFeatureOnOffOption(InternalFeatureOnOffOptions.SmartIndenter))
            //{
            //    return null;
            //}

            return new SmartIndent(textView);
        }
    }
}
