using Microsoft.Dafny;

namespace Repair.Templates;

public class Template3(int snapTargetPos, string snapTargetPred, bool snapTargetVal, ErrorReporter reporter) 
    : Template(snapTargetPos, snapTargetPred, "", reporter)
{
    protected override void InstantiateTemplate() {
        if (SuspiciousStmt == null || SuspiciousBlockStmt == null)
            return;
        
        var faultyStmtIdx = SuspiciousBlockStmt.Body.IndexOf(SuspiciousStmt);
        if (faultyStmtIdx == -1) 
            return;
        SuspiciousBlockStmt.Body.RemoveAt(faultyStmtIdx);
        
        var snapTargetValLiteral = new LiteralExpr(null, snapTargetVal);
        var ifSnapPredStmtGuard = new BinaryExpr(null, 
            BinaryExpr.Opcode.Eq, SnapTargetPred, snapTargetValLiteral);
        var ifSnapPredStmtGuardNeg = new UnaryOpExpr(null, 
            UnaryOpExpr.Opcode.Not, ifSnapPredStmtGuard);
        var ifSnapPredStmtBody = new BlockStmt(null, [SuspiciousStmt]);
        var ifSnapPredStmt = new IfStmt(null, 
            false, ifSnapPredStmtGuardNeg, 
            ifSnapPredStmtBody, null);
        SuspiciousBlockStmt.Body.Insert(faultyStmtIdx, ifSnapPredStmt);
    }
}