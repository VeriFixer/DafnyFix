using Microsoft.Dafny;

namespace Repair.Mutator;

// this mutation operator replaces a literal expression with another non-literal one appearing elsewhere in the code
public class ReversedExprValueReplacementMutator(string mutationTargetPos, string targetStmtPos, ErrorReporter reporter) 
    : ExprReplacementMutator("-1", reporter)
{
    private Expression? _replacementExpr;
    private ChainingExpression? _chainingExpressionParent;
    private bool _mutated;
    
    protected override Expression CreateMutatedExpression(Expression originalExpr) {
        if (_replacementExpr == null) return originalExpr;
        
        Expression mutatedExpr;
        if (_chainingExpressionParent != null) {
            var operands = _chainingExpressionParent.Operands;
            foreach (var (e, i) in operands.Select((e, i) => (e, i)).ToList()) {
                if (e != TargetExpression) 
                    continue;
                operands[i] = _replacementExpr;
            }
            mutatedExpr = new ChainingExpression(_chainingExpressionParent.Origin, operands, 
                _chainingExpressionParent.Operators, _chainingExpressionParent.OperatorLocs, 
                _chainingExpressionParent.PrefixLimits);
            
        } else {
            mutatedExpr = _replacementExpr;
        }

        _mutated = true;
        TargetExpression = null;
        return mutatedExpr;
    }
    
    private bool IsTarget(Expression expr) {
        var positions = mutationTargetPos.Split("-");
        if (positions.Length < 2) return false;
        var startPosition = int.Parse(positions[0]);
        var endPosition = int.Parse(positions[1]);

        return expr.StartToken.pos == startPosition &&
               expr.EndToken.pos == endPosition;
    }

    private void CheckIsReplacement(Expression expr) {
        var positions = targetStmtPos.Split("-");
        if (positions.Length < 2) return;
        var startPosition = int.Parse(positions[0]);
        var endPosition = int.Parse(positions[1]);

        if (expr.StartToken.pos == startPosition &&
            expr.EndToken.pos == endPosition) 
            _replacementExpr = expr;
    }

    /// ----------------------------
    /// Group of expression visitors
    /// ----------------------------
    public override void Mutate(Program program) {
        base.Find(program);
        if (!_mutated)
            base.Find(program);
    }
    
    protected override void HandleExpression(Expression expr) {
        CheckIsReplacement(expr);
        if (_replacementExpr != null && IsTarget(expr)) {
            TargetExpression = expr;
            return;
        }
        base.HandleExpression(expr);
    }
    
    protected override void VisitExpression(ChainingExpression cExpr) {
        foreach (var operand in cExpr.Operands) {
            if (operand != null && _replacementExpr != null && IsTarget(operand)) {
                TargetExpression = operand;
                _chainingExpressionParent = cExpr;
                return;
            }
        }
    }
    
    /// ---------------------------------
    /// Enable finding of invariant exprs
    /// ---------------------------------
    private void VisitStatement(LoopStmt loopStmt) {
        if (loopStmt.Decreases.Expressions == null) return;
        foreach (var invariant in loopStmt.Invariants)
            HandleExpression(invariant.E);
        foreach (var decreases in loopStmt.Decreases.Expressions)
            HandleExpression(decreases);
        if (loopStmt.Mod.Expressions != null) {
            foreach (var modifies in loopStmt.Mod.Expressions)
                HandleExpression(modifies.E);
        }
    }
    
    protected override void VisitStatement(WhileStmt whileStmt) {
        VisitStatement(whileStmt as LoopStmt);
        base.VisitStatement(whileStmt);
    }
    
    protected override void VisitStatement(ForLoopStmt forStmt) {
        VisitStatement(forStmt as LoopStmt);
        base.VisitStatement(forStmt);
    }
    
    protected override void VisitStatement(AlternativeLoopStmt altLStmt) {
        VisitStatement(altLStmt as LoopStmt);
        base.VisitStatement(altLStmt);
    }
}