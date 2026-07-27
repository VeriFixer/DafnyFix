using Microsoft.Dafny;

namespace Repair.Templates;

public class Template2(int snapTargetPos, string snapTargetPred, bool snapTargetVal, 
    string stateChangingTargetAssignVar, string stateChangingTargetAssignType, ErrorReporter reporter) 
    : StateChangingAssignTemplate(snapTargetPos, snapTargetPred, stateChangingTargetAssignVar, stateChangingTargetAssignType, reporter)
{
    protected override void InstantiateTemplate() {
        if (SuspiciousStmt == null || SuspiciousBlockStmt == null || SnapTargetPred == null)
            return;

        Statement? newStmt = stateChangingTargetAssignVar != "-" ? 
            CreateStateChangingAssignment() : 
            CreateStateChangingReturn();
        var faultyStmtIdx = SuspiciousBlockStmt.Body.IndexOf(SuspiciousStmt);
        if (newStmt == null || faultyStmtIdx == -1) 
            return;
        
        var snapTargetValLiteral = new LiteralExpr(null, snapTargetVal);
        var ifSnapPredStmtGuard = new BinaryExpr(null, 
            BinaryExpr.Opcode.Eq, SnapTargetPred, snapTargetValLiteral);
        var ifSnapPredStmtBody = new BlockStmt(null, [newStmt]);
        var ifSnapPredStmt = new IfStmt(null, 
            false, ifSnapPredStmtGuard, 
            ifSnapPredStmtBody, null);
        SuspiciousBlockStmt.Body.Insert(faultyStmtIdx, ifSnapPredStmt);
    }
}