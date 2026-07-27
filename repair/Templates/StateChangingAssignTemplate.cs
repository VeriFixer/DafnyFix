using Microsoft.BaseTypes;
using Microsoft.Dafny;

namespace Repair.Templates;

public abstract class StateChangingAssignTemplate(int snapTargetPos, string snapTargetPred, 
    string stateChangingTargetAssignVar, string stateChangingTargetAssignType, ErrorReporter reporter) 
    : Template(snapTargetPos, snapTargetPred, "", reporter)
{
    public AssignStatement? CreateStateChangingAssignment() {
        var stateChangingAssignRhs = stateChangingTargetAssignType switch {
            "int" or "nat" => CreateIntegerAssignmentRhs(),
            "real" => CreateRealAssignmentRhs(),
            "bool" => CreateBoolAssignmentRhs(),
            "bv" => CreateBitVectorAssignmentRhs(),
            "char" => CreateCharAssignmentRhs(),
            "string" => CreateStringAssignmentRhs(),
            "set" => CreateSetAssignmentRhs(),
            "multiset" => CreateMultisetAssignmentRhs(),
            "seq" => CreateSeqAssignmentRhs(),
            "map" => CreateMapAssignmentRhs(),
            "array" => CreateArrayAssignmentRhs(),
            _ => null
        };
        if (stateChangingAssignRhs == null) return null;
        var varNameSegment = new NameSegment(null, stateChangingTargetAssignVar, null);
        var stateChangingAssign = new AssignStatement(null, [varNameSegment], [stateChangingAssignRhs]);
        
        using StreamWriter sw = File.CreateText("state-changing-assign.txt");
        sw.WriteLine(stateChangingAssign);
        return stateChangingAssign;
    }

    public ReturnStmt? CreateStateChangingReturn() {
        List<AssignmentRhs> returnRhs = [];
        var returnTypes = stateChangingTargetAssignType.Split("-");
        foreach (var returnType in returnTypes) {
            var stateChangingReturnVal = returnType switch {
                "int" or "nat" => CreateIntegerAssignmentRhs(),
                "real" => CreateRealAssignmentRhs(),
                "bool" => CreateBoolAssignmentRhs(),
                "bv" => CreateBitVectorAssignmentRhs(),
                "char" => CreateCharAssignmentRhs(),
                "string" => CreateStringAssignmentRhs(),
                "set" => CreateSetAssignmentRhs(),
                "multiset" => CreateMultisetAssignmentRhs(),
                "seq" => CreateSeqAssignmentRhs(),
                "map" => CreateMapAssignmentRhs(),
                "array" => CreateArrayAssignmentRhs(),
                _ => null
            };
            if (stateChangingReturnVal == null) return null;
            returnRhs.Add(stateChangingReturnVal);
        }
        var stateChangingReturn = new ReturnStmt(null, returnRhs);
        
        using StreamWriter sw = File.CreateText("state-changing-assign.txt");
        sw.WriteLine(stateChangingReturn);
        return stateChangingReturn;
    }

    private AssignStatement CreateAssignment(AssignmentRhs aRhs) {
        var varNameSegment = new NameSegment(null, stateChangingTargetAssignVar, null);
        return new AssignStatement(null, [varNameSegment], [aRhs]);
    }
    
    private AssignmentRhs CreateIntegerAssignmentRhs() { // TODO: change each of these to return AssignmentRhs
        var varNameSegment = new NameSegment(null, stateChangingTargetAssignVar, null);
        var oneLiteral = new LiteralExpr(null, 1);
        var addExpr = new BinaryExpr(null, BinaryExpr.Opcode.Add, varNameSegment, oneLiteral);
        return new ExprRhs(addExpr);
    }
    
    private AssignmentRhs CreateRealAssignmentRhs() {
        var varNameSegment = new NameSegment(null, stateChangingTargetAssignVar, null);
        var oneLiteral = new LiteralExpr(null, BigDec.FromString("1.0"));
        var addExpr = new BinaryExpr(null, BinaryExpr.Opcode.Add, varNameSegment, oneLiteral);
        return new ExprRhs(addExpr);
    }
    
    private AssignmentRhs CreateBoolAssignmentRhs() {
        var trueLiteral = new LiteralExpr(null, true);
        return new ExprRhs(trueLiteral);
    }
    
    private AssignmentRhs CreateBitVectorAssignmentRhs() {
        var zeroLiteral = new LiteralExpr(null, BigDec.FromString("0"));
        return new ExprRhs(zeroLiteral);
    }
    
    private AssignmentRhs CreateCharAssignmentRhs() {
        var charLiteral = new CharLiteralExpr(null, " ");
        return new ExprRhs(charLiteral);
    }
    
    private AssignmentRhs CreateStringAssignmentRhs() {
        var strLiteral = new StringLiteralExpr(null, "", false);
        return new ExprRhs(strLiteral);
    }
    
    private AssignmentRhs CreateSetAssignmentRhs() {
        var emptySetExpr = new SetDisplayExpr(null, true, []);
        return new ExprRhs(emptySetExpr);
    }
    
    private AssignmentRhs CreateMultisetAssignmentRhs() {
        var emptyMultisetExpr = new MultiSetDisplayExpr(null, []);
        return new ExprRhs(emptyMultisetExpr);
    }
    
    private AssignmentRhs CreateSeqAssignmentRhs() {
        var emptySeqExpr = new SeqDisplayExpr(null, []);
        return new ExprRhs(emptySeqExpr);
    }
    
    private AssignmentRhs CreateMapAssignmentRhs() {
        var emptyMapExpr = new MapDisplayExpr(null, true, []);
        return new ExprRhs(emptyMapExpr);
    }
    
    private TypeRhs CreateArrayAssignmentRhs() {
        return new TypeRhs(null, 
            new IntType(null), 
            new LiteralExpr(null, 0), 
            []
        );
    }
}