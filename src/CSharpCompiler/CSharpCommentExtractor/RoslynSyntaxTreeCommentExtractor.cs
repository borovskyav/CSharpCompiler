using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpCompiler.CSharpCommentExtractor;

internal class RoslynSyntaxTreeCommentExtractor : ICSharpCommentExtractor
{
    public Task<IReadOnlyList<string>> ExtractAsync(IReadOnlyList<SyntaxTree> syntaxTrees, CancellationToken token = default)
    {
        var list = new List<string>();
        foreach(var tree in syntaxTrees)
        {
            var walker = new CommentsWalker();
            walker.Visit(tree.GetRoot(token));
            list.AddRange(walker.GetComments());
        }

        return Task.FromResult((IReadOnlyList<string>)list);
    }

    private class CommentsWalker : CSharpSyntaxWalker
    {
        public CommentsWalker()
            : base(SyntaxWalkerDepth.StructuredTrivia)
        {
            comments = new List<string>();
        }

        public IReadOnlyList<string> GetComments()
            => comments;

        public override void VisitTrivia(SyntaxTrivia trivia)
        {
            if(trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                comments.Add(trivia.ToString());
            else if(trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                comments.Add(trivia.ToString());
            base.VisitTrivia(trivia);
        }

        private readonly List<string> comments;
    }
}