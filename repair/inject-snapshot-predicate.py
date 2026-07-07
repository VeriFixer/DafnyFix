import sys
import os

def find_method_body(program_file):
    with open(program_file, 'r') as file:
        method_body_beginning_idx = 0
        lines = file.readlines()
        for line in lines:
            if "{" in line and not "}" in line and not ":" in line:
                initial_content = lines[:method_body_beginning_idx + 1]
                final_content = []
                if method_body_beginning_idx < len(lines) - 1:
                    final_content = lines[method_body_beginning_idx + 1:]
                return initial_content, final_content
            method_body_beginning_idx += 1


def inject_snapshot_predicate(program_file, initial_content, final_content, snapshot_pred):
    with open(f"{os.path.basename(program_file)[:-4]}__instrumented_helper.dfy", 'w') as file:
        for line in initial_content:
            file.write(line)
        file.write(f"  print {snapshot_pred};\n")
        for line in final_content:
            file.write(line)
        file.close()


def main():
    if len(sys.argv) != 3:
        print("Usage:\n python inject-snapshot-predicate.py <program-file> <snapshot-predicate>")
        return
    program_file = sys.argv[1]
    snapshot_pred = sys.argv[2]

    initial_content, final_content = find_method_body(program_file)
    inject_snapshot_predicate(program_file, initial_content, final_content, snapshot_pred)


if __name__ == "__main__":
    main()