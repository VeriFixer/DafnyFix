using Microsoft.Dafny;

namespace Repair.Templates;

public class TemplateFactory(ErrorReporter reporter)
{
    public Template? Create(string templateType, 
        int snapTargetPos, string snapTargetPred, bool? snapTargetVal, 
        string stateChangingTargetAssignVar, string stateChangingTargetAssignType,
        string toReplaceExpr, int toReplaceAssignRhsIdx, string replacementExpr)
    {
        return templateType switch {
            "tpl1" => new Template1(snapTargetPos, stateChangingTargetAssignVar, stateChangingTargetAssignType, reporter),
            "tpl2" => snapTargetVal != null ? new Template2(snapTargetPos, snapTargetPred, (bool)snapTargetVal, 
                stateChangingTargetAssignVar, stateChangingTargetAssignType, reporter) : null,
            "tpl3" => snapTargetVal != null ? new Template3(snapTargetPos, snapTargetPred, (bool)snapTargetVal, reporter) : null,
            "tpl4" => snapTargetVal != null ? new Template4(snapTargetPos, snapTargetPred, (bool)snapTargetVal, 
                stateChangingTargetAssignVar, stateChangingTargetAssignType, reporter) : null,
            "tpl5" => new Template5(snapTargetPos, toReplaceExpr, toReplaceAssignRhsIdx, replacementExpr, reporter),
            _ => null
        };
    }
}