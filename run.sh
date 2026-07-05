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
FAULT_LOC_SCRIPT="$SCRIPT_DIR/verifixer_fault_localization/src/runners/run_1_model_1_example.py"

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
echo $OUT_DIR

# ------------------------------------------------------------------------------ Setup

mkdir -p "$OUT_DIR"
mkdir -p "$OUT_DIR/repairs"
mkdir -p "$OUT_DIR/failed-repairs/"
mkdir -p "$OUT_DIR/failed-repairs/valid"
mkdir -p "$OUT_DIR/failed-repairs/invalid"
mkdir -p "$OUT_DIR/failed-repairs/timed-out"

# ------------------------------------------------------------------------------ Utils

run_fault_localization() {
    output=$(python $FAULT_LOC_SCRIPT CNTM $PROGRAM)
    predictions=$(echo "$output" | grep Predictions | sed 's/.*\[\(.*\)\]/\1/')
    echo $predictions
}

scan_program() {
    local line="$1"

    dotnet "$SCRIPT_DIR/dafny/Binaries/Dafny.dll" verify $PROGRAM --allow-warnings \
        --plugin "$SCRIPT_DIR/repair/bin/Debug/net8.0/repair.dll","scan line:$line" > /dev/null
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

        output=$(dotnet "$SCRIPT_DIR/dafny/Binaries/Dafny.dll" verify $PROGRAM --allow-warnings \
            --plugin "$SCRIPT_DIR/repair/bin/Debug/net8.0/repair.dll","mut $pos $op $arg" 2>/dev/null)
        mutant_outcome_msg=$(process_output "$output")
        echo $mutant_outcome_msg
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

# Fault localization
echo "Running fault localization on $PROGRAM"
predictions=$(run_fault_localization)
echo "PREDICTIONS: $predictions"

IFS=', ' read -ra lines <<< "$predictions"
length=${#lines[@]}
lines_explored=0
# exam_num_lines=$(echo "scale=2; $length * $MIN_PERCENTAGE_TO_EXPLORE" | bc) # TODO: find easier way or install in docker
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