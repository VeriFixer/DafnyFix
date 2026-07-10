#!/usr/bin/env bash
#
# ------------------------------------------------------------------------------
# This script generates a dataset of mutants of a given dataset of Dafny programs. 
# By default, one mutation at a time is applied per program, following the principles 
# of mutation testing. We also provide an option to apply more than one mutation at 
# a time, which can be useful for a variety of use cases, e.g., for building a dataset 
# of multi-fault programs for APR.
#
# Usage:
# run.sh
#   <full path to the folder with the base dataset, e.g., $SCRIPT_DIR/../DafnyBench/DafnyBench/dataset/ground_truth/> 
#   [help]
# ------------------------------------------------------------------------------ General utils

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" > /dev/null 2>&1 && pwd)"

die() {
  echo "$@" >&2
  exit 1
}

# ------------------------------------------------------------------------------ Args

USAGE="Usage: ${BASH_SOURCE[0]}
   <full path to the folder with the base dataset, e.g., $SCRIPT_DIR/../DafnyBench/DafnyBench/dataset/ground_truth> 
   [help]"

if [ "$#" -ne "1" ]; then
  die "$USAGE"
fi
if [ "$#" -eq "1" ] && [ "$1" = "--help" ]; then
    echo "$USAGE"
    exit 0
fi

INPUT_DATASET_DIR=$1

# ------------------------------------------------------------------------- Main

# Create jobs' directories
jobs_dir_path="$SCRIPT_DIR/jobs"
master_job_script_file_path="$SCRIPT_DIR/run.sh"
[ -s "$master_job_script_file_path" ] || die "[ERROR] $master_job_script_file_path does not exist or it is empty!"
mkdir -p "$jobs_dir_path"

dataset_files=$(find "$INPUT_DATASET_DIR" -maxdepth 1 -type f -name "*.dfy" -not -name "*.test.dfy")

# Create set of jobs
for program_file in $dataset_files; do
  echo "[DEBUG] $program_file"
  program_name=$(basename "$program_file" .dfy)

  job_script_dir_path="$jobs_dir_path/$program_name"
  job_script_file_path="$job_script_dir_path/job.sh"
  job_log_file_path="$job_script_dir_path/job.log"
  mkdir -p "$job_script_dir_path"
  rm -f "$job_log_file_path"
  touch "$job_script_file_path" "$job_log_file_path"

  echo "#!/usr/bin/env bash" > "$job_script_file_path"
  echo "#"                  >> "$job_script_file_path"
  echo "# timefactor:1"     >> "$job_script_file_path"
  echo "bash $master_job_script_file_path \
    \"$program_file\" \
    --run_dir \"$job_script_dir_path\" > \"$job_log_file_path\" 2>&1" >> "$job_script_file_path"
done

echo "Jobs have been created. Please run the $SCRIPT_DIR/run-jobs.sh script on the generated jobs, e.g., $SCRIPT_DIR/run-jobs.sh --jobs_dir_path $jobs_dir_path."

echo "DONE!"
exit 0