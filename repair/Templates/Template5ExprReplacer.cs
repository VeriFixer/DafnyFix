using Microsoft.Dafny;
using Repair.Mutator;

namespace Repair.Templates;

public class Template5ExprReplacer(string replacementTargetPos, string toReplaceExpr, Expression replacementExpr, List<Node> candidateNodesToReplace, ErrorReporter reporter)
    : ExprReplacementMutator(replacementTargetPos, reporter)
{
    public bool Replaced; 
        
    protected override Expression CreateMutatedExpression(Expression originalExpr) {
        TargetExpression = null;
        Replaced = true;
        return replacementExpr;
    }
    
    private bool IsTarget(Expression expr) {
        return expr.ToString() == toReplaceExpr;
    }

    protected override void HandleExpression(Expression expr) {
        if ((candidateNodesToReplace.Contains(expr) || candidateNodesToReplace.Count == 0) && IsTarget(expr)) {
            TargetExpression = expr;
            return;
        }
        base.HandleExpression(expr);
    }

    /// ---------------
    /// Public wrappers
    /// ---------------
    public void HandleStatement_(Statement stmt) {
        HandleStatement(stmt);
    }
}