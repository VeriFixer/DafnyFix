using Microsoft.Dafny;

namespace Repair.Templates;

public class Template4(int snapTargetPos, string snapTargetPred, bool snapTargetVal, 
    string stateChangingTargetAssignVar, string stateChangingTargetAssignType, ErrorReporter reporter) 
    : StateChangingAssignTemplate(snapTargetPos, stateChangingTargetAssignVar, stateChangingTargetAssignType, reporter)
{
    protected override void InstantiateTemplate() {
        throw new NotImplementedException();
    }
}