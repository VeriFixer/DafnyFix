using Microsoft.Dafny;

namespace Repair.Templates;

public class Template6(int snapTargetPos, string ifGuardExpr, string assignLhs, string assignRhs, bool innerIfStmt, ErrorReporter reporter) 
    : Template(snapTargetPos, ifGuardExpr, assignRhs, reporter)
{
    protected override void InstantiateTemplate() {
        if (SnapTargetPred == null || AdditionalExpr == null || 
            SuspiciousStmt == null || SuspiciousBlockStmt == null) 
            return;
        
        var ifStmt = CreateStateChangingIfStmt();
        if (ifStmt == null) return;

        if (innerIfStmt) {
            var faultyStmtIdx = SuspiciousBlockStmt.Body.IndexOf(SuspiciousStmt);
            if (faultyStmtIdx != -1) 
                SuspiciousBlockStmt.Body.Insert(faultyStmtIdx + 1, ifStmt);
        } else if (TargetIfStmt != null) {
            if (TargetIfStmt.Els == null) {
                TargetIfStmt.Els = ifStmt;
            } else {
                var targetIfStmtIdx = SuspiciousBlockStmt.Body.IndexOf(TargetIfStmt);
                if (targetIfStmtIdx != -1)
                    SuspiciousBlockStmt.Body.Insert(targetIfStmtIdx + 1, ifStmt);
            }
        }
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