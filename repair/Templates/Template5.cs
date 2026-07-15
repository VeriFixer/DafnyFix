using Microsoft.Dafny;

namespace Repair.Templates;

public class Template5(int snapTargetPos, string toReplaceExpr, int toReplaceAssignRhsIdx, string replacementExpr, ErrorReporter reporter) 
    : Template(snapTargetPos, replacementExpr, "", reporter)
{
    protected override void InstantiateTemplate() {
        if (SnapTargetPred == null || SuspiciousStmt == null) return;

        List<Node> candidateNodesToReplace = [];
        if (SuspiciousStmt is IfStmt ifStmt && ifStmt.Guard != null) {
            candidateNodesToReplace = [ifStmt.Guard];
        } else if (SuspiciousStmt is WhileStmt whileStmt) {
            candidateNodesToReplace = [whileStmt.Guard];
        } else if (SuspiciousStmt is ForLoopStmt forStmt) {
            candidateNodesToReplace = [forStmt.Start, forStmt.End];
        } else if (toReplaceAssignRhsIdx != -1) {
            candidateNodesToReplace = [SuspiciousStmt];
        }
        
        var replacer = new Template5ExprReplacer("-1", 
            toReplaceExpr, toReplaceAssignRhsIdx, SnapTargetPred, candidateNodesToReplace, reporter);
        replacer.HandleStatement_(SuspiciousStmt);
    }
}