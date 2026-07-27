using Microsoft.Dafny;

namespace Repair.Mutator;

// StmtInsertionMutator
public class ReversedStmtDeletionMutator(string mutationTargetPos, string arg, ErrorReporter reporter) 
    : Mutator("-1", reporter)
{
    private Statement? _suspiciousStmt;
    private BlockStmt? _suspiciousBlockStmt;
    private Statement? _toBeInsertedStmt;
    private BlockStmt? _currentBlockStmt;
    
    private bool CheckIsTargetPos(Statement stmt) {
        if (!int.TryParse(mutationTargetPos, out var targetPos))
            return false;
        if (stmt.StartToken.line  <= targetPos && 
            stmt.EndToken.line >= targetPos)
        {
            _suspiciousStmt = stmt;
            _suspiciousBlockStmt = _currentBlockStmt;
            return true;
        }
        return false;
    }

    private void CheckIsTargetStmt(Statement stmt) {
        var positions = arg.Split("-");
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

    private void InsertBreakOrContinueStmt() {
        var newStmt = arg switch {
            "break" => new BreakOrContinueStmt(null, 1, false),
            "continue" => new BreakOrContinueStmt(null, 1, true),
            _ => null
        };
        
        if (newStmt == null || _suspiciousStmt is not IfStmt ifStmt) return;
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
            var isSuspiciousStmt = CheckIsTargetPos(stmt);
            CheckIsTargetStmt(stmt);
            HandleStatement(stmt);
            
            if (TargetFound()) return;
            if (arg != "break" && arg != "continue" && (_suspiciousStmt == null || 
                  _suspiciousBlockStmt == null || _toBeInsertedStmt == null)) continue;
            if ((arg == "break" || arg == "continue") && !isSuspiciousStmt) continue;
            TargetStatement = _suspiciousStmt;

            if (arg == "break" || arg == "continue") {
                InsertBreakOrContinueStmt();
            } else if (_suspiciousStmt is IfStmt ifStmt && _toBeInsertedStmt is ReturnStmt) {
                InsertAtBottomOfIfStmt(ifStmt);
            } else {
                InsertRegularStmt();
            }
            return;
        }
    }
}