using Microsoft.Dafny;
using Microsoft.Dafny.Plugins;
using Repair.Visitor;
using Repair.Scanner;
using Repair.Mutator;
using Repair.Templates;
using Path = System.IO.Path;
using PluginConfiguration = Microsoft.Dafny.LanguageServer.Plugins.PluginConfiguration;

namespace Repair;

public class Repair : PluginConfiguration
{
    private bool _scan;
    private bool _mutate;
    private bool _tmpRepair;
    private bool _analyze;

    private List<string> OperatorsInUse { get; set; } = [];
    private string MutationTargetMethod { get; set; } = "";
    private int MutationTargetLine { get; set; } = -1;
    private (int, int) MutationTargetLineRange { get; set; } = (-1, -1);
    private (int, int) MutationTargetPosRange { get; set; } = (-1, -1);
    private string MutationTargetURI { get; set; } = "";
    private int NumMutations { get; set; } = -1;
    private string? MutationTargetPos { get; set; }
    private string? MutationOperator { get; set; }
    private string? MutationArg { get; set; }
    private string? StateTemplate { get; set; }
    private (int, string, bool?) SnapshotTarget { get; set; } = (-1, "", null);
    private (string, string) StateChangingTargetAssign { get; set; } = ("", "");
    private (string, string) TemplateReplacementExprs { get; set; } = ("", "");
    
    public override void ParseArguments(string[] args) {
        if (args.Length == 0) return;
        if (args[0] == "scan") {
            _scan = true;
            ParseScanArguments(args);
        } else if (args[0] == "scanSnap") {
            _scan = true;
            ParseScanSnapArguments(args);
        } else if (args[0] == "mut" && args.Length >= 2) {
            _mutate = true;
            ParseMutArguments(args);
        } else if (args[0].StartsWith("tpl")) {
            _tmpRepair = true;
            ParseTemplateRepairArguments(args);
        } else if (args[0] == "analyze") {
            _analyze = true;
            if (args.Length == 1) return;
            MutationTargetURI = args[1];
        }
    }

    private void ParseScanArguments(string[] args) {
        if (args.Length == 1) return;
        foreach (var arg in args) {
            if (arg.StartsWith("uri:")) {
                MutationTargetURI = arg[4..];
            } else if (arg.StartsWith("method:")) {
                MutationTargetMethod = arg[7..];
            } else if (arg.StartsWith("line:")) {
                MutationTargetLine = int.Parse(arg[5..]);
            } else if (arg.StartsWith("lineRange:") && arg.Contains('-')) {
                var positions = arg[10..].Split("-");
                if (int.TryParse(positions[0], out var startPost) &&
                    int.TryParse(positions[1], out var endPost))
                    MutationTargetLineRange = (startPost, endPost);
            } else if (arg.StartsWith("posRange:") && arg.Contains('-')) {
                var positions = arg[9..].Split("-");
                if (int.TryParse(positions[0], out var startPost) && 
                    int.TryParse(positions[1], out var endPost))
                    MutationTargetPosRange = (startPost, endPost);
            } else if (IsValidOperator(arg)) {
                OperatorsInUse.Add(arg);
            }
        }
    }
    
    private void ParseScanSnapArguments(string[] args) {
        if (args.Length < 4) return;
        ParseSnapshotArguments(args[1..]);
    }

    private void ParseMutArguments(string[] args) {
        foreach (var (arg, i) in args.Select((arg, i) => (arg, i))) {
            if (i == 0) continue;
            
            if (args.Length == 2) {
                NumMutations = int.Parse(arg);
            } else if (MutationTargetPos == null) {
                MutationTargetPos = arg;
            } else if (MutationOperator == null) {
                MutationOperator = arg;
            } else if (args.Length == 4) {
                MutationArg = arg;
            } else {
                MutationArg = string.Join(" ", args[i..]);
                break;
            }
        }
    }
    
    private void ParseTemplateRepairArguments(string[] args) {
        foreach (var (arg, i) in args.Select((arg, i) => (arg, i))) {
            if (StateTemplate == null) {
                StateTemplate = arg;
            } else if (StateTemplate == "tpl3" || 
                (StateChangingTargetAssign.Item1 != "" && 
                 StateChangingTargetAssign.Item2 != "")) {
                ParseSnapshotArguments(args[i..]);
                break;
            } else if (StateTemplate == "tpl5") {
                if (SnapshotTarget.Item1 == -1 && int.TryParse(arg, out var snapPos)) {
                    SnapshotTarget = (snapPos, SnapshotTarget.Item2, SnapshotTarget.Item3);
                } else {
                    var exprs = arg.Split("<->");
                    if (exprs.Length < 2) continue;
                    TemplateReplacementExprs = (exprs[0], exprs[1]);   
                }
            } else if (StateChangingTargetAssign.Item1 == "") {
                StateChangingTargetAssign = (arg, StateChangingTargetAssign.Item2);
            } else if (StateChangingTargetAssign.Item2 == "") {
                StateChangingTargetAssign = (StateChangingTargetAssign.Item1, arg);
            }
        }
    }

    private void ParseSnapshotArguments(string[] args) {
        foreach (var (arg, i) in args.Select((arg, i) => (arg, i))) {
            if (SnapshotTarget.Item1 == -1) {
                if (int.TryParse(arg, out var snapPos))
                    SnapshotTarget = (snapPos, SnapshotTarget.Item2, SnapshotTarget.Item3);
            } else if (SnapshotTarget.Item2 == "") {
                SnapshotTarget = (SnapshotTarget.Item1, arg, SnapshotTarget.Item3);
            } else if (i == args.Length - 1 && bool.TryParse(arg, out var snapVal)) {
                SnapshotTarget = (SnapshotTarget.Item1, SnapshotTarget.Item2, snapVal);
            } else {
                SnapshotTarget = (SnapshotTarget.Item1, $"{SnapshotTarget.Item2} {arg}", SnapshotTarget.Item3);
            }
        }   
    }

    public override Rewriter[] GetRewriters(ErrorReporter reporter) {
        return _mutate ? [new MutantGenerator(NumMutations, MutationTargetPos, MutationOperator, MutationArg, reporter)] : 
            _tmpRepair ? [new StateTemplateInstantiator(StateTemplate, SnapshotTarget, StateChangingTargetAssign, TemplateReplacementExprs, reporter)] :
            _scan ? 
                [new MutationTargetScanner(MutationTargetURI, MutationTargetMethod, 
                    MutationTargetLine, MutationTargetLineRange, MutationTargetPosRange, 
                    SnapshotTarget, OperatorsInUse, reporter)] : 
                [];
    }

    private bool IsValidOperator(string operatorName) {
        return operatorName == "AOR" || operatorName == "ROR" || operatorName == "COR" || operatorName == "LOR" ||
               operatorName == "SOR" || operatorName == "BBR" || operatorName == "AOI" || operatorName == "COI" ||
               operatorName == "LOI" || operatorName == "AOD" || operatorName == "COD" || operatorName == "LOD" ||
               operatorName == "LVR" || operatorName == "EVR" || operatorName == "VER" || operatorName == "LSR" ||
               operatorName == "LBI" || operatorName == "MRR" || operatorName == "MAP" || operatorName == "MNR" ||
               operatorName == "MCR" || operatorName == "MVR" || operatorName == "SAR" || operatorName == "CIR" ||
               operatorName == "CBR" || operatorName == "CBE" || operatorName == "TAR" || operatorName == "DCR" ||
               operatorName == "FAR" || operatorName == "SDL" || operatorName == "VDL" || operatorName == "SLD" ||
               operatorName == "ODL" || operatorName == "THI" || operatorName == "THD" || operatorName == "AMR" ||
               operatorName == "MMR" || operatorName == "PRV" || operatorName == "SWS" || operatorName == "SWV";
    }
}

public class MutationTargetScanner(string mutationTargetURI, string mutationTargetMethod, 
    int mutationTargetLine, (int, int) mutationTargetLineRange, (int, int) mutationTargetPosRange, 
    (int, string, bool?) snapshotTarget, List<string> operatorsInUse, ErrorReporter reporter) 
    : Rewriter(reporter)
{
    public static bool FirstCall = true;
    
    public override void PreResolve(Program program) {
        var specHelperFinder = new SpecHelperFinder(Reporter);
        specHelperFinder.Find(program);
        
        var targetScanner = new PreResolveTargetScanner(mutationTargetURI, mutationTargetMethod, 
            mutationTargetLine, mutationTargetLineRange, mutationTargetPosRange, 
            snapshotTarget, operatorsInUse, Reporter);
        targetScanner.Find(program);
        targetScanner.ExportTargets();
        
        // save original code but post serialization to perform diffs
        StoreProgram(program);
    }

    public override void PostResolve(ModuleDefinition module) {
        var targetScanner = new PostResolveTargetScanner(mutationTargetURI, 
            mutationTargetLine, mutationTargetLineRange, mutationTargetPosRange, 
            snapshotTarget, operatorsInUse, Reporter);
        targetScanner.Find(module);
        targetScanner.ExportTargets();
        FirstCall = false;
    }

    public override void PostResolve(Program program) {
        if (snapshotTarget == (-1, "", null) || snapshotTarget.Item3 == null)
            return;
        var stateTemplateTargetScanner = new StateTemplateTargetScanner(
            snapshotTarget.Item1, snapshotTarget.Item2, 
            (bool)snapshotTarget.Item3, reporter);
        stateTemplateTargetScanner.ScanStateBasedTemplates();
        stateTemplateTargetScanner.ExportTargets();
    }

    private void StoreProgram(Program program) {
        var stringWriter = new StringWriter();
        var printer = new Printer(stringWriter, program.Options, PrintModes.Serialization);
        printer.PrintProgram(program, false);
        var programText = stringWriter.ToString();
        
        var filename = Path.GetFileNameWithoutExtension(program.Name) + ".dfy";
        Directory.CreateDirectory("original");
        File.WriteAllText(Path.Combine("original", filename), programText);
    }
}

public class MutantGenerator(int numMutations, string mutationTargetPos, string mutationOperator, string? mutationArg, ErrorReporter reporter) : Rewriter(reporter)
{
    public static List<Node> MutatedNodes { get; private set; } = [];
    public static int NumMutations = 0; // incremented upon mutating in child mutator classes
    private static bool _generatedAllMutations = true;
    private string _mutationTargetPos = mutationTargetPos;
    private string _mutationOperator = mutationOperator;
    private string? _mutationArg = mutationArg;
    private readonly List<(string, string, string)> _usedTargets = [];
    
    public override void PreResolve(Program program) {
        if (numMutations == -1) {
            MutateProgram(program);
        } else {
            var allTargets = ImportTargets();
            var toTryTargets = new List<(string, string, string)>(allTargets); // copy of allTargets
            var rand = new Random();   
            while (NumMutations < numMutations && toTryTargets.Count != 0) {
                var initialCount = NumMutations;
                var targetIdx = rand.Next(toTryTargets.Count);
                _mutationTargetPos = toTryTargets[targetIdx].Item1;
                _mutationOperator = toTryTargets[targetIdx].Item2;
                _mutationArg = toTryTargets[targetIdx].Item3;
                toTryTargets.RemoveAt(targetIdx);
                if (_mutationOperator == "VDL") continue; // too difficult to conciliate with other types of mutation
                
                MutateProgram(program);
                if (initialCount < NumMutations)
                    _usedTargets.Add((_mutationTargetPos, _mutationOperator, _mutationArg));
            }
            
            // check if expected number of mutations was reached
            if (NumMutations != numMutations)
                _generatedAllMutations = false;
            ExportUpdatedTargets(allTargets);
        }
        StoreProgram(program);
    }

    private void MutateProgram(Program program) {
        if (_mutationOperator == "VDL" || _mutationOperator == "ODL") {
            var specHelperFinder = new SpecHelperFinder(Reporter);
            specHelperFinder.Find(program);
        }
        var mutatorFactory = new MutatorFactory(Reporter);
        var mutator = mutatorFactory.Create(_mutationTargetPos, _mutationOperator, _mutationArg);
        mutator?.Mutate(program);
    }

    private List<(string, string, string)> ImportTargets() {
        if (!File.Exists("targets.csv")) return [];
        
        var targets = new List<(string, string, string)>();
        var lines = File.ReadAllLines("targets.csv");
        foreach (var line in lines) {
            var components = line.Split(',');
            if (components.Length < 2)
                continue;

            var mutationPos = components[0];
            var mutationOp = components[1];
            var mutationArg = components.Length > 2 ? components[2] : "";
            targets.Add((mutationPos, mutationOp, mutationArg));
        }
        return targets;
    }
    
    // Rewrites the targets that haven't yet been used to external control file
    private void ExportUpdatedTargets(List<(string, string, string)> allTargets) {
        using StreamWriter sw = File.CreateText("targets.csv");
        foreach (var target in allTargets) {
            if (_generatedAllMutations && _usedTargets.Contains(target)) continue;
            var line = target.Item1 + "," + target.Item2 + "," + target.Item3;
            sw.WriteLine(line);
        }
    }
    
    private void StoreProgram(Program program) {
        if (!_generatedAllMutations) return;
        
        var stringWriter = new StringWriter();
        var printer = new Printer(stringWriter, program.Options, PrintModes.Serialization);
        printer.PrintProgram(program, false);
        var programText = stringWriter.ToString();

        var filename = Path.GetFileNameWithoutExtension(program.Name);
        if (numMutations == -1) {
            filename += !string.IsNullOrEmpty(_mutationArg) ? 
                $"__{_mutationTargetPos}_{_mutationOperator}_{_mutationArg}.dfy" : 
                $"__{_mutationTargetPos}_{_mutationOperator}.dfy";   
        } else {
            foreach (var target in _usedTargets) {
                filename += target.Item3 != "" ? 
                    $"__{target.Item1}_{target.Item2}_{target.Item3}" : 
                    $"__{target.Item1}_{target.Item2}";
            }
            filename += ".dfy";
        }
        File.WriteAllText(filename, programText);
    }
}

public class StateTemplateInstantiator(string templateType, 
    (int, string, bool?) snapshotTarget, (string, string) stateChangingTargetAssign, 
    (string, string) templateReplacementExprs, ErrorReporter reporter) 
    : Rewriter(reporter)
{
    public override void PreResolve(Program program) {
        var templateFactory = new TemplateFactory(reporter);
        var template = templateFactory.Create(templateType, 
            snapshotTarget.Item1, snapshotTarget.Item2, snapshotTarget.Item3, 
            stateChangingTargetAssign.Item1, stateChangingTargetAssign.Item2,
            templateReplacementExprs.Item1, templateReplacementExprs.Item2);
        template?.InstantiateTemplate(program);
        StoreProgram(program);
    }
    
    private void StoreProgram(Program program) {
        var stringWriter = new StringWriter();
        var printer = new Printer(stringWriter, program.Options, PrintModes.Serialization);
        printer.PrintProgram(program, false);
        var programText = stringWriter.ToString();

        var filename = program.Name.Contains("__instrumented_helper") ? 
            Path.GetFileNameWithoutExtension(program.Name)[..^21] : // remove __instrumented_helper
            Path.GetFileNameWithoutExtension(program.Name);
        var snapshotStr = $"{snapshotTarget.Item1}{(templateType != "tpl1" && templateType != "tpl5" ? 
            $"_{snapshotTarget.Item2}_{snapshotTarget.Item3}" : "")}";
        var assignStr = templateType != "tpl3" && templateType != "tpl5" ? 
            $"__{stateChangingTargetAssign.Item1}" : "";
        var replacementStr = templateType == "tpl5" ? 
            $"__{templateReplacementExprs.Item1}_{templateReplacementExprs.Item2}" : "";
        filename += $"__{templateType}__{snapshotStr}{assignStr}{replacementStr}.dfy";
        filename = filename.Replace('/', '\\');
        File.WriteAllText(filename, programText);
    }
}
