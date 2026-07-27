using Microsoft.Dafny;

namespace Repair.Mutator;

// StmtInsertionMutator
public class ReversedStmtDeletionMutator(string mutationTargetPos, string targetStmtPos, ErrorReporter reporter) 
    : Mutator("-1", reporter)
{
    private Statement? _suspiciousStmt;
    private BlockStmt? _suspiciousBlockStmt;
    private Statement? _toBeInsertedStmt;
    private BlockStmt? _currentBlockStmt;
    
    private void CheckIsTargetPos(Statement stmt) {
        if (!int.TryParse(mutationTargetPos, out var targetPos))
            return;
        if (stmt.StartToken.line  <= targetPos && 
            stmt.EndToken.line >= targetPos)
        {
            _suspiciousStmt = stmt;
            _suspiciousBlockStmt = _currentBlockStmt;
        }
    }

    private void CheckIsTargetStmt(Statement stmt) {
        var positions = targetStmtPos.Split("-");
        if (positions.Length < 2) return;
        var startPosition = int.Parse(positions[0]);
        var endPosition = int.Parse(positions[1]);
        
        if (stmt.StartToken.pos == startPosition && 
            stmt.EndToken.pos == endPosition)
            _toBeInsertedStmt = stmt;
    }

    private void InsertRegularStmt() {
        if (_suspiciousBlockStmt == null || _suspiciousStmt == null) return;
        
        var cloner = new Cloner();
        var newStmt = cloner.CloneStmt(_toBeInsertedStmt, false);
        var faultyStmtIdx = _suspiciousBlockStmt.Body.IndexOf(_suspiciousStmt);
        if (faultyStmtIdx != -1)
            _suspiciousBlockStmt.Body.Insert(faultyStmtIdx, newStmt);
    }

    private void InsertAtBottomOfIfStmt(IfStmt ifStmt) {
        var cloner = new Cloner();
        var newStmt = cloner.CloneStmt(_toBeInsertedStmt, false);
        ifStmt.Thn.Body.Add(newStmt);
    }
    
    /// ---------------------------
    /// Group of overriden visitors
    /// ---------------------------
    protected override void HandleBlock(BlockStmt blockStmt) {
        var prevCurrentBlockStmt = _currentBlockStmt;
        _currentBlockStmt = blockStmt;
        base.HandleBlock(blockStmt);
        _currentBlockStmt = prevCurrentBlockStmt;
    }
    
    protected override void HandleBlock(List<Statement> statements) {
        foreach (var stmt in statements) {
            CheckIsTargetPos(stmt);
            CheckIsTargetStmt(stmt);
            HandleStatement(stmt);
            
            if (!(_suspiciousStmt != null && _suspiciousBlockStmt != null && 
                  _toBeInsertedStmt != null)) continue;
            if (TargetFound()) return;
            TargetStatement = _suspiciousStmt;

            if (_suspiciousStmt is IfStmt ifStmt && _toBeInsertedStmt is ReturnStmt rStmt) {
                InsertAtBottomOfIfStmt(ifStmt);
            } else {
                InsertRegularStmt();
            }
            return;
        }
    }
}