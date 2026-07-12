using Microsoft.Dafny;
using Repair.Scanner;
using Type = Microsoft.Dafny.Type;

namespace Repair.Templates;

public class StateTemplateTargetScanner(int snapTargetPos, string snapTargetPred, bool snapTargetVal, ErrorReporter reporter) 
    : Visitor.Visitor("-1", reporter)
{
    private List<List<string>> Targets { get; } = [];
    private static readonly List<string> _templates = ["tpl1", "tpl2", "tpl3", "tpl4"];
    public static Expression? SnapTargetPred;
    private readonly List<string> _snapTargetPred = [];

    public void ScanStateBasedTemplates() {
        var snapPredSubexpressions = FindVarSnapPredSubexpressions();
        foreach (var template in _templates) {
            if (template == "tpl3") {
                Targets.Add([template, $"{snapTargetPos}", snapTargetPred, $"{snapTargetVal}"]);
                continue;
            }
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
                        } else if (uType.Name == "string") { // string type
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
    
    public void ExportTargets() {
        using StreamWriter sw = File.CreateText("targets.csv");
        foreach (var target in Targets) {
            var line = string.Join(",", target);
            sw.WriteLine(line);
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
}