using Microsoft.Dafny;

namespace Repair.Templates;

public abstract class Template(int snapTargetPos, string snapTargetPred, string additionalExpr, ErrorReporter reporter) : Visitor.Visitor("-1", reporter)
{
    protected Statement? SuspiciousStmt;
    protected BlockStmt? SuspiciousBlockStmt;
    private BlockStmt? _currentBlockStmt;
    protected Expression? SnapTargetPred;
    protected Expression? AdditionalExpr;
    private (BlockStmt?, PrintStmt?) _toRemoveHelperPrintStmt;
    
    public void InstantiateTemplate(Program program) {
        if (this is not Template1) 
            snapTargetPos++; // 1 offset due to inserted helper print stmt 
        
        base.Find(program);
        InstantiateTemplate();
    }

    protected abstract void InstantiateTemplate();
    
    protected override void HandleStatement(Statement stmt) {
        if (stmt is PrintStmt prtStmt) {
            if (SnapTargetPred == null && prtStmt.Args[0].ToString() == snapTargetPred) {
                SnapTargetPred = prtStmt.Args[0];
                _toRemoveHelperPrintStmt = (_currentBlockStmt, prtStmt);
            }
            if (prtStmt.Args.Count > 1 && AdditionalExpr == null && 
                prtStmt.Args[1].ToString() == additionalExpr) {
                AdditionalExpr = prtStmt.Args[1];
            }
            return;
        }
        
        if (stmt.StartToken.line  <= snapTargetPos && 
            stmt.EndToken.line >= snapTargetPos)
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