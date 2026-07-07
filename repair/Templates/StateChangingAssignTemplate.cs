using Microsoft.BaseTypes;
using Microsoft.Dafny;

namespace Repair.Templates;

public abstract class StateChangingAssignTemplate(int snapTargetPos, string snapTargetPred, 
    string stateChangingTargetAssignVar, string stateChangingTargetAssignType, ErrorReporter reporter) 
    : Template(snapTargetPos, snapTargetPred, reporter)
{
    public AssignStatement? CreateStateChangingAssignment() {
        return stateChangingTargetAssignType switch {
            "int" or "nat" => CreateIntegerAssignment(),
            "real" => CreateRealAssignment(),
            "bool" => CreateBoolAssignment(),
            "bv" => CreateBitVectorAssignment(),
            "char" => CreateCharAssignment(),
            "string" => CreateStringAssignment(),
            "set" => CreateSetAssignment(),
            "multiset" => CreateMultisetAssignment(),
            "seq" => CreateSeqAssignment(),
            "map" => CreateMapAssignment(),
            "array" => CreateArrayAssignment(),
            _ => null
        };
    }

    private AssignStatement CreateAssignment(AssignmentRhs aRhs) {
        var varNameSegment = new NameSegment(null, stateChangingTargetAssignVar, null);
        return new AssignStatement(null, [varNameSegment], [aRhs]);
    }

    private AssignStatement CreateIntegerAssignment() {
        var zeroLiteral = new LiteralExpr(null, 0);
        return CreateAssignment(new ExprRhs(zeroLiteral));
    }
    
    private AssignStatement CreateRealAssignment() {
        var zeroLiteral = new LiteralExpr(null, BigDec.FromString("0.0"));
        return CreateAssignment(new ExprRhs(zeroLiteral));
    }
    
    private AssignStatement CreateBoolAssignment() {
        var trueLiteral = new LiteralExpr(null, true);
        return CreateAssignment(new ExprRhs(trueLiteral));
    }
    
    private AssignStatement CreateBitVectorAssignment() {
        var zeroLiteral = new LiteralExpr(null, BigDec.FromString("0"));
        return CreateAssignment(new ExprRhs(zeroLiteral));
    }
    
    private AssignStatement CreateCharAssignment() {
        var charLiteral = new CharLiteralExpr(null, " ");
        return CreateAssignment(new ExprRhs(charLiteral));
    }
    
    private AssignStatement CreateStringAssignment() {
        var strLiteral = new StringLiteralExpr(null, "", false);
        return CreateAssignment(new ExprRhs(strLiteral));
    }
    
    private AssignStatement CreateSetAssignment() {
        var emptySetExpr = new SetDisplayExpr(null, true, []);
        return CreateAssignment(new ExprRhs(emptySetExpr));
    }
    
    private AssignStatement CreateMultisetAssignment() {
        var emptyMultisetExpr = new MultiSetDisplayExpr(null, []);
        return CreateAssignment(new ExprRhs(emptyMultisetExpr));
    }
    
    private AssignStatement CreateSeqAssignment() {
        var emptySeqExpr = new SeqDisplayExpr(null, []);
        return CreateAssignment(new ExprRhs(emptySeqExpr));
    }
    
    private AssignStatement CreateMapAssignment() {
        var emptyMapExpr = new MapDisplayExpr(null, true, []);
        return CreateAssignment(new ExprRhs(emptyMapExpr));
    }
    
    private AssignStatement CreateArrayAssignment() {
        var emptyArrayExpr = new TypeRhs(null, 
            new IntType(null), 
            new LiteralExpr(null, 0), 
            []
        );
        return CreateAssignment(emptyArrayExpr);
    }
}