using Microsoft.Dafny;
using Repair.Scanner;
using Type = Microsoft.Dafny.Type;

namespace Repair.Templates;

public class StateTemplateTargetScanner(int snapTargetPos, string snapTargetPred, bool snapTargetVal, ErrorReporter reporter) 
    : Visitor.Visitor("-1", reporter)
{
    private List<List<string>> Targets { get; } = [];
    private static readonly List<string> _assignChangingTemplates = ["tpl1", "tpl2", "tpl4"];
    public static Node? SuspiciousNode;
    public static Expression? SnapTargetPred;
    private readonly List<string> _snapTargetPred = [];

    public void ScanStateBasedTemplates() {
        Targets.Add(["tpl3", $"{snapTargetPos}", snapTargetPred, $"{snapTargetVal}"]);
        ScanAssignChangingTemplates();
        ScanExprUpdatingTemplates();
        ScanImplicationToIfStmtTemplate();
    }

    private void ScanAssignChangingTemplates() {
        var snapPredSubexpressions = FindVarSnapPredSubexpressions();
        
        foreach (var template in _assignChangingTemplates) {
            foreach (var (var, type) in snapPredSubexpressions) {
                var typeStr = "";
                switch (type) {
                    case IntType: typeStr = "int"; break;
                    case RealType: typeStr = "real"; break;
                    case BoolType: typeStr = "bool"; break;
                    case BitvectorType: typeStr = "bv"; break;
                    case CharType: typeStr = "char"; break;
                    case SetType: typeStr = "set"; break;
                    case MultiSetType: typeStr = "multiset"; break;
                    case SeqType: typeStr = "seq"; break;
                    case MapType: typeStr = "map"; break;
                    case UserDefinedType uType:
                        if (uType.Name == "nat") {
                            typeStr = "int";
                        } else if (uType.Name == "string") {
                            typeStr = "string";
                        } else if (type.IsArrayType) {
                            typeStr = "array";
                        }
                        break;
                }
                List<string> newTarget = [template, var, typeStr, $"{snapTargetPos}"];
                if (template != "tpl1")
                    newTarget.AddRange([snapTargetPred, $"{snapTargetVal}"]);
                Targets.Add(newTarget);
            }
        }
    }

    private void ScanExprUpdatingTemplates() {
        if (SnapTargetPred is not BinaryExpr bExpr) return;
        
        // expr replacement
        var updatingCandidateNodes = GetUpdatingCandidateNodes();
        foreach (var candidate in updatingCandidateNodes) {
            var suspiciousExpr = FindSuspiciousExpr(candidate, bExpr.E0);
            if (suspiciousExpr != null) {
                var toReplaceExpr = bExpr.E0.ToString();
                var replacementExpr = bExpr.E1.ToString();
                Targets.Add(["tpl5", $"{snapTargetPos}", $"{toReplaceExpr}<->{replacementExpr}"]);
            }
            suspiciousExpr = FindSuspiciousExpr(candidate, bExpr.E1);
            if (suspiciousExpr != null) {
                var toReplaceExpr = bExpr.E1.ToString();
                var replacementExpr = bExpr.E0.ToString();
                Targets.Add(["tpl5", $"{snapTargetPos}", $"{toReplaceExpr}<->{replacementExpr}"]);
            }
        }
        
        // assign rhs replacement
        if (SuspiciousNode == null) return;
        var suspiciousRhs = FindSuspiciousRhs(SuspiciousNode, bExpr.E0);
        if (suspiciousRhs != -1)
            Targets.Add(["tpl5",  $"{snapTargetPos}", $"{suspiciousRhs}", $"{bExpr.E1}"]);
        suspiciousRhs = FindSuspiciousRhs(SuspiciousNode, bExpr.E1);
        if (suspiciousRhs != -1)
            Targets.Add(["tpl5",  $"{snapTargetPos}", $"{suspiciousRhs}", $"{bExpr.E0}"]);
    }

    private void ScanImplicationToIfStmtTemplate() {
        if (SnapTargetPred is not BinaryExpr outerBExpr || 
            outerBExpr.Op != BinaryExpr.Opcode.Imp) return;
        if (outerBExpr.E1 is not BinaryExpr innerBExpr || 
            innerBExpr.Op != BinaryExpr.Opcode.Eq) return;
        
        var snapPredSubexpressions = FindVarSnapPredSubexpressions()
            .Select(e => e.Item1).ToList();
        var suspiciousIdentifier = snapPredSubexpressions.Find(e => e == innerBExpr.E0.ToString());
        if (suspiciousIdentifier != null) // TODO: check if it's inside an if
            Targets.Add(["tpl6",  $"{snapTargetPos}", $"{outerBExpr.E0}", $"{suspiciousIdentifier}", $"{innerBExpr.E1}"]);
        
        snapPredSubexpressions = FindVarSnapPredSubexpressions()
            .Select(e => e.Item1).ToList();
        suspiciousIdentifier = snapPredSubexpressions.Find(e => e == innerBExpr.E1.ToString());
        if (suspiciousIdentifier != null) // TODO: check if it's inside an if
            Targets.Add(["tpl6",  $"{snapTargetPos}", $"{outerBExpr.E1}", $"{suspiciousIdentifier}", $"{innerBExpr.E0}"]);
    }

    private List<(string, Type)> FindVarSnapPredSubexpressions() {
        if (SnapTargetPred == null) return [];
        HandleExpression(SnapTargetPred);
        return PostResolveTargetScanner.AssignableIdentifiers
            .Where(id => _snapTargetPred.Contains(id.Item1) && 
                         id.Item3 <= snapTargetPos && 
                         id.Item4 >= snapTargetPos)
            .Select(id => (id.Item1, id.Item2))
            .DistinctBy(id => id.Item1)
            .ToList();
    }

    protected override void VisitExpression(NameSegment nSegExpr) {
        _snapTargetPred.Add(nSegExpr.Name);
        base.VisitExpression(nSegExpr);
    }

    private List<Node> GetUpdatingCandidateNodes() {
        return SuspiciousNode switch {
            IfStmt { Guard: not null } ifStmt => [ifStmt.Guard],
            WhileStmt whileStmt => [whileStmt.Guard],
            ForLoopStmt forStmt => [forStmt.Start, forStmt.End],
            _ => SuspiciousNode != null ? [SuspiciousNode] : []
        };
    }

    private Node? FindSuspiciousExpr(Node rootNode, Expression snapTargetPredHS) {
        if (rootNode.ToString() == snapTargetPredHS.ToString()) 
            return rootNode;
        
        List<INode> children;
        if (rootNode is AssignStatement aStmt) {
            children = aStmt.Rhss.Concat(aStmt.Rhss.SelectMany(rhs => rhs.Children)).ToList();
        } else if (rootNode is VarDeclStmt vDeclStmt) {
            children = vDeclStmt.Assign != null ? [vDeclStmt.Assign] : [];
        } else {
            children = new List<INode>(rootNode.Children);
            if (rootNode is ParensExpression parensExpr) 
                children.Add(parensExpr.E);
        }
        
        foreach (var child in children) {
            if (child is not Node childNode || child.ToString() == snapTargetPred) 
                continue;
            var suspiciousExpr = FindSuspiciousExpr(childNode, snapTargetPredHS);
            if (suspiciousExpr != null && suspiciousExpr is Expression)
                return suspiciousExpr;
        }
        return null;
    }
    
    private int FindSuspiciousRhs(Node suspiciousNode, Expression snapTargetPredLhs) {
        if (suspiciousNode is VarDeclStmt { Assign: not null } vDeclStmt) {
            return FindSuspiciousRhs(vDeclStmt.Assign, snapTargetPredLhs);
        }
        if (suspiciousNode is AssignStatement aStmt) {
            var lhsMatchIdx = aStmt.Lhss.Select(lhs => lhs.ToString())
                .ToList().IndexOf(snapTargetPredLhs.ToString());
            if (lhsMatchIdx != -1)
                return lhsMatchIdx;
        }
        return -1;
    }
    
    public void ExportTargets() {
        using StreamWriter sw = File.CreateText("targets.csv");
        foreach (var target in Targets) {
            var line = string.Join(",", target);
            sw.WriteLine(line);
        }
    }
}