using Microsoft.Dafny;

namespace Repair.Templates;

public class Template1(int snapTargetPos, string stateChangingTargetAssignVar, string stateChangingTargetAssignType, ErrorReporter reporter) 
    : StateChangingAssignTemplate(snapTargetPos, "", stateChangingTargetAssignVar, stateChangingTargetAssignType, reporter)
{
    protected override void InstantiateTemplate() {
        if (SuspiciousStmt == null || SuspiciousBlockStmt == null) return;
        
        var assign = CreateStateChangingAssignment();
        var faultyStmtIdx = SuspiciousBlockStmt.Body.IndexOf(SuspiciousStmt);
        if (assign == null || faultyStmtIdx == -1) 
            return;
        SuspiciousBlockStmt.Body.Insert(faultyStmtIdx, assign);
    }
}