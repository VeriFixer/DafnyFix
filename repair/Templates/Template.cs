using Microsoft.Dafny;

namespace Repair.Templates;

public abstract class Template(int snapTargetPos, ErrorReporter reporter) : Visitor.Visitor("-1", reporter)
{
    protected Statement? SuspiciousStmt;
    protected BlockStmt? SuspiciousBlockStmt;
    private BlockStmt? _currentBlockStmt;
    
    public void InstantiateTemplate(Program program) {
        base.Find(program);
        InstantiateTemplate();
    }

    protected abstract void InstantiateTemplate();
    
    protected override void HandleStatement(Statement stmt) {
        if (stmt.StartToken.line  <= snapTargetPos && 
            stmt.EndToken.line >= snapTargetPos) 
        {
            SuspiciousStmt = stmt;
            SuspiciousBlockStmt = _currentBlockStmt;
        }
        base.HandleStatement(stmt);
    }

    // protected override void HandleExpression(Expression expr) { }
    
    protected override void HandleBlock(BlockStmt blockStmt) {
        var prevCurrentBlockStmt = _currentBlockStmt;
        _currentBlockStmt = blockStmt;
        base.HandleBlock(blockStmt);
        _currentBlockStmt = prevCurrentBlockStmt;
    }
}