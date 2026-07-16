using Microsoft.Dafny;

namespace Repair.Templates;

public class TemplateFactory(ErrorReporter reporter)
{
    public Template? Create(string templateType, (int, string, bool?) snapshotTarget, 
        (string, string) stateChangingTargetAssign, (string, string, string, bool) stateChangingTargetIfStmt,
        (string, int, string) templateReplacementExprs, ErrorReporter reporter)
    {
        var snapTargetPos = snapshotTarget.Item1;
        var snapTargetPred = snapshotTarget.Item2;
        var snapTargetVal = snapshotTarget.Item3;
        var stateChangingTargetAssignVar = stateChangingTargetAssign.Item1;
        var stateChangingTargetAssignType = stateChangingTargetAssign.Item2;
        var ifGuardExpr = stateChangingTargetIfStmt.Item1;
        var ifBodyAssignLhs = stateChangingTargetIfStmt.Item2;
        var ifBodyAssignRhs = stateChangingTargetIfStmt.Item3;
        var innerIfStmt = stateChangingTargetIfStmt.Item4;
        var toReplaceExpr = templateReplacementExprs.Item1;
        var toReplaceAssignRhsIdx = templateReplacementExprs.Item2;
        var replacementExpr = templateReplacementExprs.Item3;
        
        return templateType switch {
            "tpl1" => new Template1(snapTargetPos, stateChangingTargetAssignVar, stateChangingTargetAssignType, reporter),
            "tpl2" => snapTargetVal != null ? new Template2(snapTargetPos, snapTargetPred, (bool)snapTargetVal, 
                stateChangingTargetAssignVar, stateChangingTargetAssignType, reporter) : null,
            "tpl3" => snapTargetVal != null ? new Template3(snapTargetPos, snapTargetPred, (bool)snapTargetVal, reporter) : null,
            "tpl4" => snapTargetVal != null ? new Template4(snapTargetPos, snapTargetPred, (bool)snapTargetVal, 
                stateChangingTargetAssignVar, stateChangingTargetAssignType, reporter) : null,
            "tpl5" => new Template5(snapTargetPos, toReplaceExpr, toReplaceAssignRhsIdx, replacementExpr, reporter),
            "tpl6" => new Template6(snapTargetPos, ifGuardExpr, ifBodyAssignLhs, ifBodyAssignRhs, innerIfStmt, reporter),
            _ => null
        };
    }
}