using Microsoft.BaseTypes;
using Microsoft.Dafny;

namespace Repair.Mutator;

public class DecrementMutator(string mutationTargetPos, string type, ErrorReporter reporter) 
    : ExprReplacementMutator(mutationTargetPos, reporter)
{
    private ChainingExpression? _chainingExpressionParent;
    
    protected override Expression CreateMutatedExpression(Expression originalExpr) {
        Expression mutatedExpr;
        if (_chainingExpressionParent != null) {
            var operands = _chainingExpressionParent.Operands;
            foreach (var (e, i) in operands.Select((e, i) => (e, i)).ToList()) {
                if (e != TargetExpression || TargetExpression is not NameSegment nSegExpr) 
                    continue;
                operands[i] = new BinaryExpr(originalExpr.Origin, 
                    BinaryExpr.Opcode.Sub, nSegExpr, CreateOneLiteral(originalExpr.Origin));
            }
            mutatedExpr = new ChainingExpression(_chainingExpressionParent.Origin, operands, 
                _chainingExpressionParent.Operators, _chainingExpressionParent.OperatorLocs, 
                _chainingExpressionParent.PrefixLimits);
            
        } else {
            var nSegExpr = originalExpr as NameSegment;
            mutatedExpr = new BinaryExpr(originalExpr.Origin, 
                BinaryExpr.Opcode.Sub, nSegExpr, CreateOneLiteral(originalExpr.Origin));
        }
        
        TargetExpression = null;
        MutantGenerator.NumMutations++;
        MutantGenerator.MutatedNodes.Add(mutatedExpr);
        return mutatedExpr;
    }
    
    private LiteralExpr CreateOneLiteral(IOrigin origin) {
        if (type == "int")
            return new LiteralExpr(origin, 1);
        return new LiteralExpr(origin, BigDec.FromInt(1));
    }
    
    private bool IsTarget(NameSegment nSegExpr) {
        return nSegExpr.Center.pos == int.Parse(MutationTargetPos) &&
               !AlreadyMutated(nSegExpr) && !ContainsMutatedChildren(nSegExpr);
    }
    
    /// ------------------
    /// Overriden visitors
    /// ------------------
    protected override void VisitExpression(NameSegment nSegExpr) {
        if (IsTarget(nSegExpr)) {
            TargetExpression = nSegExpr;
            return;
        }
        base.VisitExpression(nSegExpr);
    }
    
    protected override void VisitExpression(ChainingExpression cExpr) {
        foreach (var operand in cExpr.Operands) {
            if (operand is NameSegment nSegExpr && IsTarget(nSegExpr)) {
                TargetExpression = operand;
                _chainingExpressionParent = cExpr;
                return;
            }
        }
    }
}