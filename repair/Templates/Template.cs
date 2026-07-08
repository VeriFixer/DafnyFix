using Microsoft.Dafny;

namespace Repair.Templates;

public abstract class Template(int snapTargetPos, string snapTargetPred, ErrorReporter reporter) : Visitor.Visitor("-1", reporter)
{
    protected Statement? SuspiciousStmt;
    protected BlockStmt? SuspiciousBlockStmt;
    private BlockStmt? _currentBlockStmt;
    protected Expression? SnapTargetPred;
    private (BlockStmt?, PrintStmt?) _toRemoveHelperPrintStmt;
    
    public void InstantiateTemplate(Program program) {
        base.Find(program);
        InstantiateTemplate();
    }

    protected abstract void InstantiateTemplate();
    
    protected override void HandleStatement(Statement stmt) {
        if (SnapTargetPred == null && stmt is PrintStmt prtStmt && 
            prtStmt.Args[0].ToString() == snapTargetPred) 
        {
            SnapTargetPred = prtStmt.Args[0];
            _toRemoveHelperPrintStmt = (_currentBlockStmt, prtStmt);
            return;
        }
        
        if (stmt.StartToken.line  <= snapTargetPos + 1 && 
            stmt.EndToken.line >= snapTargetPos + 1) // 1 offset due to inserted helper print stmt 
        {
            SuspiciousStmt = stmt;
            SuspiciousBlockStmt = _currentBlockStmt;
            if (stmt is VarDeclStmt) return;
        }
        base.HandleStatement(stmt);
    }

    protected override void HandleExpression(Expression expr) { }
    
    protected override void HandleBlock(BlockStmt blockStmt) {
        var prevCurrentBlockStmt = _currentBlockStmt;
        _currentBlockStmt = blockStmt;
        
        base.HandleBlock(blockStmt);
        
        if (_toRemoveHelperPrintStmt != (null, null) && _toRemoveHelperPrintStmt.Item1 == blockStmt) {
            blockStmt.Body.Remove(_toRemoveHelperPrintStmt.Item2);
            _toRemoveHelperPrintStmt = (null, null);
        }
        _currentBlockStmt = prevCurrentBlockStmt;
    }
}