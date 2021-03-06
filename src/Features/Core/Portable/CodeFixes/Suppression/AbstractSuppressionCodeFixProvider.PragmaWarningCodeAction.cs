// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
    {
        internal sealed class PragmaWarningCodeAction : AbstractSuppressionCodeAction, IPragmaBasedCodeAction
        {
            private readonly SuppressionTargetInfo _suppressionTargetInfo;
            private readonly Document _document;
            private readonly Diagnostic _diagnostic;
            private readonly bool _forFixMultipleContext;

            internal PragmaWarningCodeAction(
                SuppressionTargetInfo suppressionTargetInfo,
                Document document,
                Diagnostic diagnostic,
                AbstractSuppressionCodeFixProvider fixer,
                bool forFixMultipleContext = false)
                : base(fixer, title: FeaturesResources.SuppressWithPragma)
            {
                _suppressionTargetInfo = suppressionTargetInfo;
                _document = document;
                _diagnostic = diagnostic;
                _forFixMultipleContext = forFixMultipleContext;
            }

            public PragmaWarningCodeAction CloneForFixMultipleContext()
            {
                return new PragmaWarningCodeAction(_suppressionTargetInfo, _document, _diagnostic, Fixer, forFixMultipleContext: true);

            }
            protected override string DiagnosticIdForEquivalenceKey =>
                _forFixMultipleContext ? string.Empty : _diagnostic.Id;

            protected async override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return await GetChangedDocumentAsync(includeStartTokenChange: true, includeEndTokenChange: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            public async Task<Document> GetChangedDocumentAsync(bool includeStartTokenChange, bool includeEndTokenChange, CancellationToken cancellationToken)
            {
                return await PragmaHelpers.GetChangeDocumentWithPragmaAdjustedAsync(
                    _document,
                    _diagnostic.Location.SourceSpan,
                    _suppressionTargetInfo,
                    (startToken, currentDiagnosticSpan) => includeStartTokenChange ? PragmaHelpers.GetNewStartTokenWithAddedPragma(startToken, currentDiagnosticSpan, _diagnostic, Fixer, FormatNode) : startToken,
                    (endToken, currentDiagnosticSpan) => includeEndTokenChange ? PragmaHelpers.GetNewEndTokenWithAddedPragma(endToken, currentDiagnosticSpan, _diagnostic, Fixer, FormatNode) : endToken,
                    cancellationToken).ConfigureAwait(false);
            }

            public SyntaxToken StartToken_TestOnly => _suppressionTargetInfo.StartToken;
            public SyntaxToken EndToken_TestOnly => _suppressionTargetInfo.EndToken;

            private SyntaxNode FormatNode(SyntaxNode node)
            {
                return Formatter.Format(node, _document.Project.Solution.Workspace);
            }
        }
    }
}
