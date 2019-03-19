// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace dnSpy.Roslyn.Internal.QuickInfo
{
    internal abstract partial class AbstractSemanticQuickInfoProvider
    {
        private class ErrorVisitor : SymbolVisitor<bool>
        {
            private static ErrorVisitor s_instance = new ErrorVisitor();

            public static bool ContainsError(ISymbol symbol)
            {
                return s_instance.Visit(symbol);
            }

            public override bool DefaultVisit(ISymbol symbol)
            {
                return true;
            }

            public override bool VisitAlias(IAliasSymbol symbol)
            {
                return false;
            }

            public override bool VisitArrayType(IArrayTypeSymbol symbol)
            {
                return Visit(symbol.ElementType);
            }

            public override bool VisitEvent(IEventSymbol symbol)
            {
                return Visit(symbol.Type);
            }

            public override bool VisitField(IFieldSymbol symbol)
            {
                return Visit(symbol.Type);
            }

            public override bool VisitLocal(ILocalSymbol symbol)
            {
                return Visit(symbol.Type);
            }

            public override bool VisitMethod(IMethodSymbol symbol)
            {
                foreach (var parameter in symbol.Parameters)
                {
                    if (!Visit(parameter))
                    {
                        return true;
                    }
                }

                foreach (var typeParameter in symbol.TypeParameters)
                {
                    if (!Visit(typeParameter))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override bool VisitNamedType(INamedTypeSymbol symbol)
            {
                foreach (var typeParameter in symbol.TypeArguments.Concat(symbol.TypeParameters))
                {
                    if (Visit(typeParameter))
                    {
                        return true;
                    }
                }

                return symbol.IsErrorType();
            }

            public override bool VisitParameter(IParameterSymbol symbol)
            {
                return Visit(symbol.Type);
            }

            public override bool VisitProperty(IPropertySymbol symbol)
            {
                return Visit(symbol.Type);
            }

            public override bool VisitPointerType(IPointerTypeSymbol symbol)
            {
                return Visit(symbol.PointedAtType);
            }
        }
    }
}
