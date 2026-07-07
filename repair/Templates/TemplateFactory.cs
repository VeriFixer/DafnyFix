using Microsoft.Dafny;

namespace Repair.Templates;

public class TemplateFactory(ErrorReporter reporter)
{
    public Template? Create(string templateType, 
        int snapTargetPos, string snapTargetPred, bool snapTargetVal, 
        string stateChangingTargetAssignVar, string stateChangingTargetAssignType)
    {
        return templateType switch {
            "tpl1" => new Template1(snapTargetPos, stateChangingTargetAssignVar, stateChangingTargetAssignType, reporter),
            "tpl2" => new Template2(snapTargetPos, snapTargetPred, snapTargetVal, 
                stateChangingTargetAssignVar, stateChangingTargetAssignType, reporter),
            "tpl3" => new Template3(snapTargetPos, snapTargetPred, snapTargetVal, reporter),
            "tpl4" => new Template4(snapTargetPos, snapTargetPred, snapTargetVal, 
                stateChangingTargetAssignVar, stateChangingTargetAssignType, reporter),
            _ => null
        };
    }
}