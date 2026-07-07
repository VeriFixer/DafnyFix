using Microsoft.Dafny;

namespace Repair.Templates;

public class Template3(int snapTargetPos, string snapTargetPred, bool snapTargetVal, ErrorReporter reporter) 
    : Template(snapTargetPos, reporter)
{
    protected override void InstantiateTemplate() {
        throw new NotImplementedException();
    }
}