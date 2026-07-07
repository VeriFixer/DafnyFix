using Microsoft.Dafny;

namespace Repair.Templates;

public class Template1(int snapTargetPos, string stateChangingTargetAssignVar, string stateChangingTargetAssignType, ErrorReporter reporter) 
    : StateChangingAssignTemplate(snapTargetPos, stateChangingTargetAssignVar, stateChangingTargetAssignType, reporter)
{
    protected override void InstantiateTemplate() {
        throw new NotImplementedException();
    }
}