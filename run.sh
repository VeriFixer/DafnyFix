#!/usr/bin/env bash
#
# ------------------------------------------------------------------------------
# This script generates repair candidates for a given faulty Dafny program (which does not verify)
#
# Usage:
# run.sh
#   <full path to the program under repair, e.g., $SCRIPT_DIR/dataset/abs.dfy> 
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
    [help]"

if [ "$#" -ne "1" ] || [ "$1" = "--help" ]; then
  die "$USAGE"
fi

PROGRAM=$1;
PROGRAM="$(cd "$(dirname "$PROGRAM")" && pwd)/$(basename "$PROGRAM")" # Get full path
MIN_LINES_TO_EXPLORE=3
MIN_PERCENTAGE_TO_EXPLORE=15
OUT_DIR="$SCRIPT_DIR/repairs"

# ------------------------------------------------------------------------------ Setup

mkdir -p "$OUT_DIR"
mkdir -p "$OUT_DIR/repairs"
mkdir -p "$OUT_DIR/failed-repairs/"
mkdir -p "$OUT_DIR/failed-repairs/valid"
mkdir -p "$OUT_DIR/failed-repairs/invalid"
mkdir -p "$OUT_DIR/failed-repairs/timed-out"

# ------------------------------------------------------------------------------ Utils

run_cntm_fault_localization() {
    output=$(python $FAULT_LOC_SCRIPT CNTM "$PROGRAM")
    predictions=$(echo "$output" | grep Predictions | sed 's/.*\[\(.*\)\]/\1/')
    echo $predictions
}

run_snap_fault_localization() {
    test_file=$(gen_tests)
    output=$(python $STATE_FAULT_LOC_SCRIPT "$PROGRAM")
    predictions=$(echo "$output" | grep Predictions | sed 's/.*\[\(.*\)\]/\1/')
    echo $predictions
}

gen_tests() {
    program_name=$(basename "$PROGRAM" .dfy)
    program_dir=$(dirname "$PROGRAM")
    test_file="$program_dir/$program_name.test.dfy"

    if [ ! -f $test_file ]; then
        dotnet "$TEST_GEN_BIN" "$PROGRAM" -o "$test_file" \
            --grouping by-status --skip-on-exception --comment-uncompilable -n 20
        sed -i '1,5d' "$test_file"
    fi

    echo $test_file
}

scan_program() {
    local line="$1"

    dotnet "$DAFNY_BIN" verify "$PROGRAM" --allow-warnings \
        --plugin "$REPAIR_BIN","scan line:$line" > /dev/null
}

scan_state_template_repairs() {
    local line="$1"
    local pred="$2"
    local value="$3"

    dotnet "$DAFNY_BIN" verify "$PROGRAM" --allow-warnings \
        --plugin "$REPAIR_BIN","scanSnap $line $pred $value" > /dev/null
}

mutate_program() {
    IFS=','
    while read pos op arg;
    do
        if [[ -z $arg ]]; then 
            echo Mutating position $pos: operator $op
        else
            echo Mutating position $pos: operator $op, argument $arg
        fi

        output=$(dotnet "$DAFNY_BIN" verify "$PROGRAM" --allow-warnings \
            --plugin "$REPAIR_BIN","mut $pos $op $arg" 2>/dev/null)
        mutant_outcome_msg=$(process_output "$output")
        echo $mutant_outcome_msg
        echo
        rm elapsed-time.csv
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
            rm "$instrumented_program"
        fi
        repair_outcome_msg=$(process_output "$output")
        echo $repair_outcome_msg
        echo
        rm elapsed-time.csv
    done < targets.csv
}

process_output() {
    local output="$1"

    verification_finished=$(echo $output | grep "Dafny program verifier finished")
    verified=$(echo $output | grep "Dafny program verifier finished.*0 errors")
    timed_out=$(echo $output | grep "Dafny program verifier finished.*time out")
    output=$(echo $output | tail -1)

    COLOR='\033[0;31m'; if [[ -n $verified ]]; then COLOR='\033[0;32m'; fi
    if [[ -z $verification_finished ]]; then # verification did not finish due to invalid program
        echo -e "${COLOR}Repair is invalid\033[0m"
        if [ -f *.dfy ]; then
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

        mv *.dfy $output_dir
    fi
}

# ------------------------------------------------------------------------------ Main

# Basic fault localization
echo "Running fault localization on $PROGRAM"
predictions=$(run_cntm_fault_localization)
echo "PREDICTIONS: $predictions"

IFS=', ' read -ra lines <<< "$predictions"
length=${#lines[@]}
lines_explored=0
exam_num_lines=$(( (length * MIN_PERCENTAGE_TO_EXPLORE + 50) / 100 ))
for line in "${lines[@]}"; do
    echo "Scanning mutation targets for program line $line"
    scan_program $line
    mutate_program
    rm targets.csv

    lines_explored=$((lines_explored+1))
    if [ "$lines_explored" -ge "$MIN_LINES_TO_EXPLORE" ] && [ "$lines_explored" -ge "$exam_num_lines" ]; then
        break
    fi
done
IFS=$' \t\n'
echo


# State fault localization
echo "Running state-based fault localization on $PROGRAM"
predictions=$(run_snap_fault_localization)
echo "PREDICTIONS: $predictions"

predictions_clean=$(echo "$predictions" | sed "s/'), ('/|/g" | sed "s/^('//" | sed "s/')$//")
IFS='|' read -ra snapshots <<< "$predictions_clean"
for snapshot in "${snapshots[@]}"; do
    IFS=',' read -r line pred value <<< "$snapshot"
    # Trim whitespace and remove quotes
    line=$(echo "$line" | sed "s/^[[:space:]]*'//; s/'[[:space:]]*$//")
    pred=$(echo "$pred" | sed "s/^[[:space:]]*'//; s/'[[:space:]]*$//")
    value=$(echo "$value" | sed "s/^[[:space:]]*'//; s/'[[:space:]]*$//")

    echo "Scanning state-based template repairs for snapshot ($line, $pred, $value)"
    scan_state_template_repairs "$line" "$pred" "$value"
    apply_repair_templates
    rm targets.csv
done