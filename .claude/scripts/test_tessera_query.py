# Regression tests for tessera_query.py.
# Run: python -m unittest discover -s .claude/scripts -p "test_*.py"

import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

SCRIPTS = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPTS))

import tessera_query as tq  # noqa: E402


class TempDocTest(unittest.TestCase):
    def setUp(self):
        self._dir = tempfile.TemporaryDirectory()
        self.addCleanup(self._dir.cleanup)

    def write(self, text, encoding="utf-8"):
        path = Path(self._dir.name) / "doc.md"
        path.write_text(text, encoding=encoding)
        return path


class SectionsTest(TempDocTest):
    def test_keeps_hash_inside_heading_text(self):
        path = self.write("## C#\n")
        self.assertEqual(tq.sections(path), [(1, 2, "C#")])

    def test_strips_closing_hashes(self):
        path = self.write("## Title ##\n")
        self.assertEqual(tq.sections(path), [(1, 2, "Title")])

    def test_reads_first_heading_after_bom(self):
        path = self.write("# First\n", encoding="utf-8-sig")
        self.assertEqual(tq.sections(path), [(1, 1, "First")])

    def test_ignores_headings_in_fences(self):
        path = self.write("# Real\n```\n# Fake\n```\n## After\n")
        self.assertEqual(tq.sections(path), [(1, 1, "Real"), (5, 2, "After")])


class TaskRowsTest(TempDocTest):
    def test_reads_table_after_bom(self):
        path = self.write("| ID | 상태 | 작업 |\n|---|---|---|\n| M1-T1 | `DONE` | 첫 작업 |\n", encoding="utf-8-sig")
        rows = list(tq.task_rows(path))
        self.assertEqual(len(rows), 1)
        self.assertEqual(rows[0][:3], ("M1-T1", "DONE", "첫 작업"))


class InPlanTest(unittest.TestCase):
    def test_matches_whole_segment_only(self):
        self.assertTrue(tq.in_plan("docs/plan/m1/README.md:10", "m1"))
        self.assertFalse(tq.in_plan("docs/plan/m17/README.md:10", "m1"))
        self.assertTrue(tq.in_plan("docs/plan/m17/README.md:10", "m17/"))


class CliTest(unittest.TestCase):
    def test_empty_state_is_rejected(self):
        result = subprocess.run(
            [sys.executable, str(SCRIPTS / "tessera_query.py"), "tasks", "--state", ""],
            capture_output=True,
        )
        self.assertEqual(result.returncode, 2)


if __name__ == "__main__":
    unittest.main()
