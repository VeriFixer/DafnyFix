using Microsoft.Dafny;

namespace Repair.Templates;

public class Template4(int snapTargetPos, string snapTargetPred, bool snapTargetVal, 
    string stateChangingTargetAssignVar, string stateChangingTargetAssignType, ErrorReporter reporter) 
    : StateChangingAssignTemplate(snapTargetPos, snapTargetPred, stateChangingTargetAssignVar, stateChangingTargetAssignType, reporter)
{
    protected override void InstantiateTemplate() {
        if (SuspiciousStmt == null || SuspiciousBlockStmt == null || SnapTargetPred == null)
            return;

        var assign = CreateStateChangingAssignment();
        var faultyStmtIdx = SuspiciousBlockStmt.Body.IndexOf(SuspiciousStmt);
        if (assign == null || faultyStmtIdx == -1) 
            return;
        SuspiciousBlockStmt.Body.RemoveAt(faultyStmtIdx);
        
        var snapTargetValLiteral = new LiteralExpr(null, snapTargetVal);
        var ifSnapPredStmtGuard = new BinaryExpr(null, 
            BinaryExpr.Opcode.Eq, SnapTargetPred, snapTargetValLiteral);
        var ifSnapPredStmtThnBody = new BlockStmt(null, [assign]);
        var ifSnapPredStmtElsBody = new BlockStmt(null, [SuspiciousStmt]);
        var ifSnapPredStmt = new IfStmt(null, 
            false, ifSnapPredStmtGuard, 
            ifSnapPredStmtThnBody, ifSnapPredStmtElsBody);
        SuspiciousBlockStmt.Body.Insert(faultyStmtIdx, ifSnapPredStmt);
    }
}