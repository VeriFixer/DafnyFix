using Microsoft.Dafny;

namespace Repair.Templates;

public class Template6(int snapTargetPos, string ifGuardExpr, string assignLhs, string assignRhs, ErrorReporter reporter) 
    : Template(snapTargetPos, ifGuardExpr, assignRhs, reporter)
{
    protected override void InstantiateTemplate() {
        if (SnapTargetPred == null || AdditionalExpr == null || 
            SuspiciousStmt == null || SuspiciousBlockStmt == null) 
            return;
        
        var ifStmt = CreateStateChangingIfStmt();
        var faultyStmtIdx = SuspiciousBlockStmt.Body.IndexOf(SuspiciousStmt);
        if (ifStmt == null || faultyStmtIdx == -1) 
            return;
        SuspiciousBlockStmt.Body.Insert(faultyStmtIdx + 1, ifStmt);
    }

    private IfStmt? CreateStateChangingIfStmt() {
        if (SnapTargetPred == null || AdditionalExpr == null) return null;
        
        var assignLhsExpr = new NameSegment(null, assignLhs, null);
        var assignRhsExpr = new ExprRhs(null, AdditionalExpr);
        var ifStmtBodyAssign = new AssignStatement(null, [assignLhsExpr], [assignRhsExpr]);
        var ifStmtBody = new BlockStmt(null, [ifStmtBodyAssign]);
        return new IfStmt(null, false, SnapTargetPred, ifStmtBody, null);
    }
}