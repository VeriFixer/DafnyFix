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
    
    public void ExportTargets() {
        using StreamWriter sw = File.CreateText("targets.csv");
        foreach (var target in Targets) {
            var line = string.Join(",", target);
            sw.WriteLine(line);
        }
    }
}