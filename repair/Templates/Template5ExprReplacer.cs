using Microsoft.Dafny;
using Repair.Mutator;

namespace Repair.Templates;

public class Template5ExprReplacer(string replacementTargetPos, string toReplaceExpr, int toReplaceAssignRhsIdx,  
    Expression replacementExpr, List<Node> candidateNodesToReplace, ErrorReporter reporter)
    : ExprReplacementMutator(replacementTargetPos, reporter)
{
    private bool _replaced;
    private AssignStatement? _parentAssign;
        
    protected override Expression CreateMutatedExpression(Expression originalExpr) {
        TargetExpression = null;
        _replaced = true;
        return replacementExpr;
    }
    
    private bool IsTarget(Expression expr) {
        if (toReplaceExpr != "")
            return expr.ToString() == toReplaceExpr;
        if (toReplaceAssignRhsIdx != -1 && _parentAssign != null) {
            var targetRhs = _parentAssign.Rhss[toReplaceAssignRhsIdx];
            string rhsStr = "";
            if (targetRhs is ExprRhs exprRhs) {
                rhsStr = exprRhs.Expr.ToString();
            } else if (targetRhs is TypeRhs typeRhs) {
                rhsStr = typeRhs.ToString();
            }
            return expr.ToString() == rhsStr;
        }
        return false;
    }

    protected override void HandleExpression(Expression expr) {
        if ((candidateNodesToReplace.Contains(expr) || candidateNodesToReplace.Count == 0 || 
             _parentAssign != null) && !_replaced && IsTarget(expr)) 
        {
            TargetExpression = expr;
            return;
        }
        base.HandleExpression(expr);
    }

    /// ---------------
    /// Public wrappers
    /// ---------------
    public void HandleStatement_(Statement stmt) {
        if (stmt is AssignStatement aStmt && toReplaceAssignRhsIdx != -1)
            _parentAssign = aStmt;
        HandleStatement(stmt);
        _parentAssign = null;
    }
}