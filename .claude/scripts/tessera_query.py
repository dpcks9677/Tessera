# Deterministic lookup of task rows and markdown headings in the plan docs.
#
# Agents used to grep or read whole plan files to find one task row or one
# section. This CLI answers those questions with a few lines of output.
# Standard library only; other modules may import sections().

import argparse
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SEPARATOR = re.compile(r"^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)*\|?\s*$")
FENCE = re.compile(r"^\s*(```|~~~)")
HEADING = re.compile(r"^(#{1,6})\s+(.*?)(?:\s+#+)?\s*$")
ID_HEADERS = ("ID", "작업 ID")
TITLE_HEADERS = ("작업", "작업명", "마일스톤")


def scan_files():
    files = sorted((ROOT / "docs" / "plan").glob("**/*.md"))
    files += sorted((ROOT / "docs" / "archive" / "milestones").glob("*.md"))
    return files


def split_cells(line):
    text = line.strip()
    if text.startswith("|"):
        text = text[1:]
    if text.endswith("|"):
        text = text[:-1]
    return [c.strip() for c in text.split("|")]


def clean(cell):
    return cell.replace("**", "").replace("`", "").strip()


def rel(path):
    try:
        return path.resolve().relative_to(ROOT).as_posix()
    except ValueError:
        return path.as_posix()


def task_rows(path):
    """Yield (id, state, title, 'rel:line', doc) for every task-table row."""
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    i = 0
    while i < len(lines):
        if lines[i].lstrip().startswith("|") and i + 1 < len(lines) and SEPARATOR.match(lines[i + 1]):
            headers = split_cells(lines[i])
            id_col = next((n for n, h in enumerate(headers) if h in ID_HEADERS), None)
            state_col = next((n for n, h in enumerate(headers) if h == "상태"), None)
            j = i + 2
            if id_col is None or state_col is None:
                i = j
                continue
            title_col = next((n for n, h in enumerate(headers) if h in TITLE_HEADERS), id_col + 1)
            doc_col = next((n for n, h in enumerate(headers) if h == "문서"), None)

            def cell(cells, col):
                return cells[col] if col is not None and col < len(cells) else ""

            while j < len(lines) and lines[j].lstrip().startswith("|"):
                cells = split_cells(lines[j])
                yield (
                    clean(cell(cells, id_col)),
                    clean(cell(cells, state_col)),
                    cell(cells, title_col),
                    f"{rel(path)}:{j + 1}",
                    cell(cells, doc_col),
                )
                j += 1
            i = j
        else:
            i += 1


def all_rows():
    for path in scan_files():
        yield from task_rows(path)


def sections(path):
    """Return [(line_number, level, heading_text)], ignoring fenced code blocks."""
    result = []
    fence = None
    text = Path(path).read_text(encoding="utf-8-sig")
    for number, line in enumerate(text.splitlines(), 1):
        m = FENCE.match(line)
        if m:
            marker = m.group(1)
            if fence is None:
                fence = marker
            elif fence == marker:
                fence = None
            continue
        if fence:
            continue
        h = HEADING.match(line)
        if h:
            result.append((number, len(h.group(1)), h.group(2)))
    return result


def print_row(row):
    print("\t".join(row))


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description="Query plan docs for task rows and headings.")
    sub = parser.add_subparsers(dest="cmd", required=True)
    p_task = sub.add_parser("task")
    p_task.add_argument("id")
    p_tasks = sub.add_parser("tasks")
    p_tasks.add_argument("--plan")
    p_tasks.add_argument("--state")
    p_sec = sub.add_parser("sections")
    p_sec.add_argument("path")
    args = parser.parse_args()

    if args.cmd == "task":
        found = [r for r in all_rows() if r[0].lower() == args.id.lower()]
        if not found:
            print(f"NOT FOUND {args.id}", file=sys.stderr)
            sys.exit(1)
        for r in found:
            print_row(r)
    elif args.cmd == "tasks":
        states = {s.strip().lower() for s in args.state.split(",")} if args.state else None
        count = 0
        for r in all_rows():
            # Match whole path segments so "m1" does not also select m10~m19.
            if args.plan and f"/{args.plan.strip('/')}/" not in f"/{r[3].rsplit(':', 1)[0]}/":
                continue
            if states and r[1].lower() not in states:
                continue
            print_row(r)
            count += 1
        print(f"COUNT\t{count}")
    else:
        for number, level, text in sections(args.path):
            print(f"{number}\t{level}\t{text}")


if __name__ == "__main__":
    main()
