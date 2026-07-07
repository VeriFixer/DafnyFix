using Microsoft.Dafny;
using Repair.Scanner;
using Type = Microsoft.Dafny.Type;

namespace Repair.Templates;

public class StateTemplateTargetScanner(int targetStatePos, string targetStatePred) 
{
    protected List<List<string>> Targets { get; } = [];
    private static readonly List<string> _templates = ["tpl1", "tpl2", "tpl3", "tpl4"];

    public void ScanStateBasedTemplates() {
        var snapPredSubexpressions = FindVarSnapPredSubexpressions();
        foreach (var template in _templates) {
            if (template == "tpl3") {
                Targets.Add([$"{targetStatePos}", template]);
                continue;
            }
            foreach (var (var, type) in snapPredSubexpressions) {
                var typeStr = "";
                switch (type) {
                    case IntType: typeStr = "int"; break;
                    case RealType: typeStr = "real"; break;
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
                Targets.Add([$"{targetStatePos}", template, var, typeStr]);
            }
        }
    }

    private List<(string, Type)> FindVarSnapPredSubexpressions() {
        var tokens = targetStatePred.Split([' ', '(', ')'], StringSplitOptions.RemoveEmptyEntries);
        return PostResolveTargetScanner.AssignableIdentifiers
            .Where(id => tokens.Contains(id.Item1) && 
                         id.Item3 <= targetStatePos && 
                         id.Item4 >= targetStatePos)
            .Select(id => (id.Item1, id.Item2)).ToList();
    }
    
    public void ExportTargets() {
        using StreamWriter sw = File.CreateText("targets.csv");
        foreach (var target in Targets) {
            var line = string.Join(",", target);
            sw.WriteLine(line);
        }
    }
}