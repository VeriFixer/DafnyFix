using Microsoft.Dafny;

namespace Repair.Templates;

public class Template1(int snapTargetPos, string stateChangingTargetAssignVar, string stateChangingTargetAssignType, ErrorReporter reporter) : Template(snapTargetPos, reporter)
{
    protected override void InstantiateTemplate() {
        throw new NotImplementedException();
    }
}