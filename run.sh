#!/usr/bin/env bash
#
# ------------------------------------------------------------------------------
# This script generates repair candidates for a given faulty Dafny program (which does not verify)
#
# Usage:
# run.sh
#   <full path to the program under repair, e.g., $SCRIPT_DIR/dataset/abs.dfy> 
#   [--run_dir <the directory where the script should be run, e.g., $SCRIPT_DIR (by deafult)>]
#   [help]
# ------------------------------------------------------------------------------ General utils

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" > /dev/null 2>&1 && pwd)"
DAFNY_BIN="$SCRIPT_DIR/dafny/Binaries/Dafny.dll"
FAULT_LOC_SCRIPT="$SCRIPT_DIR/verifixer_fault_localization/src/runners/run_1_model_1_example.py"
STATE_FAULT_LOC_SCRIPT="$SCRIPT_DIR/verifixer_fault_localization/src/runners/run_cntm_snap_intersection.py"
REPAIR_BIN="$SCRIPT_DIR/repair/bin/Debug/net8.0/repair.dll"
TEST_GEN_BIN="$SCRIPT_DIR/build_output/DafnyTestGen/DafnyCBT.dll"
SNAPSHOT_INJECTOR_SCRIPT="$SCRIPT_DIR/repair/inject-snapshot-predicate.py"

die() {
  echo "$@" >&2
  exit 1
}

# ------------------------------------------------------------------------------ Args

USAGE="Usage: ${BASH_SOURCE[0]}
    <full path to the program under repair, e.g., $SCRIPT_DIR/dataset/abs.dfy> 
    [--min_lines <the minimum number of faulty lines to explore, e.g., 5 (by default)>]
    [--min_states <the minimum number of faulty program states to explore, e.g., 10 (by default)>]
    [--run_dir <the directory where the script should be run, e.g., $SCRIPT_DIR (by deafult)>]
    [help]"

if [ "$#" -ne "1" ] && [ "$#" -ne "3" ] && [ "$#" -ne "5" ] && [ "$#" -ne "7" ]; then
  die "$USAGE"
fi
if [ "$#" -eq "1" ] && [ "$1" = "--help" ]; then
    echo "$USAGE"
    exit 0
fi

PROGRAM=$1;
PROGRAM="$(cd "$(dirname "$PROGRAM")" && pwd)/$(basename "$PROGRAM")" # Get full path
shift
MIN_LINES_TO_EXPLORE=5
MIN_PERCENTAGE_TO_EXPLORE=15
MIN_STATES_TO_EXPLORE=10
REPAIR_FILE=""
OUT_DIR="$SCRIPT_DIR/repairs"
RUN_DIR="$SCRIPT_DIR"

while [[ "$1" = --* ]]; do
  OPTION=$1; shift
  case $OPTION in
    (--min_lines)
      MIN_LINES_TO_EXPLORE=$1;
      shift;;
    (--min_states)
      MIN_STATES_TO_EXPLORE=$1;
      shift;;
    (--run_dir)
      RUN_DIR=$1;
      shift;;
    (--help)
      echo "$USAGE";
      exit 0;;
    (*)
      die "$USAGE";;
  esac
done

# ------------------------------------------------------------------------------ Setup

mkdir -p "$OUT_DIR"
mkdir -p "$OUT_DIR/repairs"
mkdir -p "$OUT_DIR/failed-repairs/"
mkdir -p "$OUT_DIR/failed-repairs/valid"
mkdir -p "$OUT_DIR/failed-repairs/invalid"
mkdir -p "$OUT_DIR/failed-repairs/timed-out"
mkdir -p "$RUN_DIR"

# ------------------------------------------------------------------------------ Utils

run_cntm_fault_localization() {
    output=$(python $FAULT_LOC_SCRIPT CNTM "$PROGRAM")
    predictions=$(echo "$output" | grep Predictions | cut -d ':' -f 2 | sed -n 's/^[^[]*\[\(.*\)\][^]]*$/\1/p')
    echo $predictions
}

run_snap_fault_localization() {
    gen_tests
    python $STATE_FAULT_LOC_SCRIPT "$PROGRAM" \
        --min_lines_to_explore $MIN_LINES_TO_EXPLORE \
        --min_states_to_explore $MIN_STATES_TO_EXPLORE
}

gen_tests() {
    program_name=$(basename "$PROGRAM" .dfy)
    program_dir=$(dirname "$PROGRAM")
    test_file="$program_dir/$program_name.test.dfy"

    if [ ! -f $test_file ]; then
        dotnet "$TEST_GEN_BIN" "$PROGRAM" -o "$test_file" \
            --grouping by-status --skip-on-exception --comment-uncompilable -n 20 > /dev/null
        sed -i '1,5d' "$test_file"
    fi
}

scan_program() {
    local arg="$1"
    local program="$2"
    if [[ -z "$program" ]]; then
        program=$PROGRAM
    fi

    dotnet "$DAFNY_BIN" verify "$program" --allow-warnings \
        --plugin "$REPAIR_BIN","scan $arg" > /dev/null
}

scan_state_template_repairs() {
    local line="$1"
    local pred="$2"
    local value="$3"

    python "$SNAPSHOT_INJECTOR_SCRIPT" "$PROGRAM" "$pred"
    instrumented_program="$(basename $PROGRAM .dfy)__instrumented_helper.dfy"

    dotnet "$DAFNY_BIN" verify "$instrumented_program" --allow-warnings \
        --plugin "$REPAIR_BIN","scanSnap $line $pred $value" > /dev/null

    rm -f "$instrumented_program"
}

mutate_program() {
    local program="$1"
    if [[ -z "$program" ]]; then
        program=$PROGRAM
    fi

    IFS=','
    while read pos op arg;
    do
        if [[ -z $arg ]]; then 
            echo Mutating position $pos: operator $op
        else
            echo Mutating position $pos: operator $op, argument $arg
        fi

        output=$(dotnet "$DAFNY_BIN" verify "$program" --allow-warnings \
            --plugin "$REPAIR_BIN","mut $pos $op $arg" 2>/dev/null)
        mutant_outcome_msg=$(process_output "$output")
        echo $mutant_outcome_msg
        echo
        rm -f elapsed-time.csv
    done < targets.csv
}

apply_repair_templates() {
    IFS=','
    while read -ra args; do
        plugin_args=$(echo "${args[*]}" | sed 's/,/ /g')
        echo Applying repair template $plugin_args

        template_type=("${args[@]:0:1}")
        snap_pred=""
        if [ "$template_type" = "tpl3" ]; then
            snap_pred=("${args[@]:2:1}")
        elif [ "$template_type" = "tpl2" ] || [ "$template_type" = "tpl4" ]; then
            snap_pred=("${args[@]:4:1}")
        elif [ "$template_type" = "tpl5" ]; then
            snap_pred=("${args[@]:2:1}")
            if [[ "$snap_pred" =~ \<-\> ]]; then
                snap_pred=$(echo "$snap_pred" | awk -F '<->' '{print $2}')
            else
                snap_pred=("${args[@]:3:1}")
            fi
        elif [ "$template_type" = "tpl6" ]; then
            snap_pred=("${args[@]:2:1}, ${args[@]:4:1}")
            plugin_args="${args[@]:0:1} ${args[@]:1:1} guard:${args[@]:2:1} body:${args[@]:3:1} ${args[@]:4:1} ${args[@]:5:1}"
        fi
        if [[ ! -z "$snap_pred" ]]; then
            # Instrument input program with snapshot predicate to facilitate parsing into Dafny expression
            python "$SNAPSHOT_INJECTOR_SCRIPT" "$PROGRAM" "$snap_pred"
            instrumented_program="$(basename $PROGRAM .dfy)__instrumented_helper.dfy"
        else
            instrumented_program="$PROGRAM"
        fi

        output=$(dotnet "$DAFNY_BIN" verify "$instrumented_program" --allow-warnings \
            --plugin "$REPAIR_BIN","$plugin_args")

        if [ "$instrumented_program" != "$PROGRAM" ]; then
            rm -f "$instrumented_program"
        fi
        process_output "$output"
        echo
        
        has_successful_repair_using_template=$(got_successful_repair_using_template)
        if [[ -z $has_successful_repair_using_template ]]; then
            echo $(mutate_repair_template)
        fi
        rm -f elapsed-time.csv
    done < template-targets.csv
}

mutate_repair_template() {
    if [ ! -f state-changing-assign.txt ]; then
        exit 1
    fi

    program_name=$(basename $PROGRAM)
    diff=$(diff -Z "$REPAIR_FILE" "$RUN_DIR/original/$program_name")
    context_lines=$(echo "$diff" | grep -v '^[<>-]')
    state_changing_assign=$(cat state-changing-assign.txt)
    assign_lines=$(grep -n "$state_changing_assign" "$REPAIR_FILE" | cut -f1 -d:)
    scan_arg=""

    while IFS= read -r context_line; do
        repair_lines=$(echo "$context_line" | sed 's/[adc].*//' | sed 's/,/-/g')
        assign_repair_lines_intersection=$(echo "$assign_lines" | grep "$repair_lines")

        if [[ "$repair_lines" == *-* ]]; then
            range_start="${repair_lines%-*}"
            range_end="${repair_lines#*-}"
            assign_line=$(echo "$assign_lines" | awk -v s="$range_start" -v e="$range_end" \
                '$1 >= s && $1 <= e { print $1; exit }')
            if [[ -n "$assign_line" ]]; then
                echo "Scanning mutation targets for program line $assign_line"
                scan_arg="line:$assign_line"
            fi
        elif [[ -n $assign_repair_lines_intersection ]]; then
            echo "Scanning mutation targets for program line $assign_repair_lines_intersection"
            scan_arg="line:$repair_lines"
        fi
    done <<< "$context_lines"

    if [[ -n $scan_arg ]]; then
        scan_program "$scan_arg" "$REPAIR_FILE"
        mutate_program "$REPAIR_FILE"
        echo; echo
        rm -f targets.csv
    fi
}

process_output() {
    local output="$1"

    verification_finished=$(echo $output | grep "Dafny program verifier finished")
    verified=$(echo $output | grep "Dafny program verifier finished.*0 errors")
    timed_out=$(echo $output | grep "Dafny program verifier finished.*time out")
    output=$(echo $output | tail -1)
    REPAIR_FILE=""

    COLOR='\033[0;31m'; if [[ -n $verified ]]; then COLOR='\033[0;32m'; fi
    if [[ -z $verification_finished ]]; then # verification did not finish due to invalid program
        echo -e "${COLOR}Repair is invalid\033[0m"
        if [ -f *.dfy ]; then
            REPAIR_FILE=$(basename *.dfy)
            REPAIR_FILE="$OUT_DIR/failed-repairs/invalid/$REPAIR_FILE"
            mv *.dfy "$OUT_DIR/failed-repairs/invalid"
        fi
    elif [ -f *.dfy ]; then
        echo -e "${COLOR}${output}\033[0m"
        output_dir=""
        if [[ -n $timed_out ]]; then
            echo -e "${COLOR}Repair verification timed out\033[0m"
            output_dir="$OUT_DIR/failed-repairs/timed-out"
        elif [[ -n $verified ]]; then 
            echo -e "${COLOR}Verification succeeded: program successfully repaired\033[0m"
            output_dir="$OUT_DIR/repairs"
        else 
            echo -e "${COLOR}Repair verification failed\033[0m"
            output_dir="$OUT_DIR/failed-repairs/valid"
        fi

        REPAIR_FILE=$(basename *.dfy)
        REPAIR_FILE="$output_dir/$REPAIR_FILE"
        mv *.dfy $output_dir
    fi
}

got_successful_repair() {
    repairs_dir="$OUT_DIR/repairs"
    program_name=$(basename $PROGRAM .dfy)
    repair_file="$repairs_dir/$program_name"
    has_repair_files=$(ls $repair_file* 2> /dev/null)
    if [[ -n $has_repair_files ]]; then
        echo Has repair
    fi
}

got_successful_repair_using_template() {
    repair_dir=$(dirname $REPAIR_FILE)
    if [ "$repair_dir" == "$OUT_DIR/repairs" ]; then
        echo Has repair
    fi
}

# ------------------------------------------------------------------------------ Main

pushd . > /dev/null 2>&1
cd "$RUN_DIR"

# Basic mutation-based fault localization
echo "Running fault localization on $PROGRAM"
predictions=$(run_cntm_fault_localization)
echo "PREDICTIONS: $predictions"

IFS=', ' read -ra lines <<< "$predictions"
length=${#lines[@]}
lines_explored=0
exam_num_lines=$(( (length * MIN_PERCENTAGE_TO_EXPLORE + 50) / 100 ))
for line in "${lines[@]}"; do
    echo "Scanning mutation targets for program line $line"
    scan_program "line:$line"
    mutate_program
    rm -f targets.csv

    lines_explored=$((lines_explored+1))
    if [ "$lines_explored" -ge "$MIN_LINES_TO_EXPLORE" ] && [ "$lines_explored" -ge "$exam_num_lines" ]; then
        break
    fi
done
IFS=$' \t\n'
echo


# State-based fault localization
echo "Running state-based fault localization on $PROGRAM"
all_predictions=$(run_snap_fault_localization)
predictions=$(echo -e "$all_predictions" | cat -v | \
    grep -E '^\^\[\[1mPredictions\^\[\[0m(\s*)?:' | \
    cut -d ':' -f 2- | sed -n 's/^[^[]*\[\(.*\)\][^]]*$/\1/p')
additional_predictions=$(echo -e "$all_predictions" | cat -v | \
    grep -E '^\^\[\[1mAdditional Predictions\^\[\[0m(\s*)?:' | \
    cut -d ':' -f 2- | sed -n 's/^[^[]*\[\(.*\)\][^]]*$/\1/p')
echo "PREDICTIONS: $predictions"

predictions_clean=$(echo "$predictions" | sed "s/'), ('/~/g" | sed "s/^('//" | sed "s/')$//")
IFS='~' read -ra snapshots <<< "$predictions_clean"
for snapshot in "${snapshots[@]}"; do
    IFS=',' read -r line pred value <<< "$snapshot"
    # Trim whitespace and remove quotes
    line=$(echo "$line" | sed "s/^[[:space:]]*[\"']//; s/[\"'][[:space:]]*\$//")
    pred=$(echo "$pred" | sed "s/^[[:space:]]*[\"']//; s/[\"'][[:space:]]*\$//")
    value=$(echo "$value" | sed "s/^[[:space:]]*[\"']//; s/[\"'][[:space:]]*\$//")

    echo "Scanning state-based template repairs for snapshot ($line, $pred, $value)"
    scan_state_template_repairs "$line" "$pred" "$value"
    mv targets.csv template-targets.csv
    apply_repair_templates
    rm -f template-targets.csv
done

has_successful_repairs=$(got_successful_repair)
if [[ -z $has_successful_repairs ]]; then
    echo
    echo "Will explore additional snapshots until repair is found"
    echo "ADDITIONAL PREDICTIONS: $additional_predictions"

    predictions_clean=$(echo "$additional_predictions" | sed "s/'), ('/~/g" | sed "s/^('//" | sed "s/')$//")
    IFS='~' read -ra snapshots <<< "$predictions_clean"
    for snapshot in "${snapshots[@]}"; do
        IFS=',' read -r line pred value <<< "$snapshot"
        # Trim whitespace and remove quotes
        line=$(echo "$line" | sed "s/^[[:space:]]*[\"']//; s/[\"'][[:space:]]*\$//")
        pred=$(echo "$pred" | sed "s/^[[:space:]]*[\"']//; s/[\"'][[:space:]]*\$//")
        value=$(echo "$value" | sed "s/^[[:space:]]*[\"']//; s/[\"'][[:space:]]*\$//")

        echo "Scanning state-based template repairs for snapshot ($line, $pred, $value)"
        scan_state_template_repairs "$line" "$pred" "$value"
        mv targets.csv template-targets.csv
        apply_repair_templates
        rm -f template-targets.csv

        has_successful_repairs=$(got_successful_repair)
        if [[ -n $has_successful_repairs ]]; then
            break
        fi
    done
fi

popd > /dev/null 2>&1
echo "[INFO] Job finished"
echo "DONE!"
exit 0