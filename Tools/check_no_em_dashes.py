#!/usr/bin/env python3
"""Fails if any tracked text file (or the GDD Word file) contains an em dash.

The project rule: no em dashes anywhere, in code, game text, docs or pull requests. Use a hyphen, a colon
or a new sentence instead. Run it with:   python Tools/check_no_em_dashes.py
Exit code 0 means clean, 1 means at least one em dash was found (each one is printed with file and line).
The dash is built from its number below, so this file itself stays clean.
"""
import subprocess
import sys
import zipfile
import re
from pathlib import Path

EM_DASH = chr(0x2014)
TEXT_SUFFIXES = {".cs", ".md", ".py", ".sh", ".txt", ".csv", ".json", ".asset", ".yml", ".yaml", ".csproj", ".sln", ".shader", ".uss", ".uxml"}


def tracked_files(root: Path):
    out = subprocess.run(["git", "ls-files"], cwd=root, capture_output=True, text=True, check=True).stdout
    return [root / line for line in out.splitlines() if line]


def text_lines(path: Path):
    """The lines of a text file, or of the visible text of a .docx."""
    if path.suffix == ".docx":
        xml = zipfile.ZipFile(path).read("word/document.xml").decode("utf8")
        text = "\n".join("".join(re.findall(r"<w:t[^>]*>(.*?)</w:t>", p, flags=re.S))
                         for p in re.findall(r"<w:p[ >].*?</w:p>", xml, flags=re.S))
        return text.splitlines()
    return path.read_text(encoding="utf8", errors="ignore").splitlines()


def main() -> int:
    root = Path(__file__).resolve().parent.parent
    found = 0
    for path in tracked_files(root):
        if path.suffix not in TEXT_SUFFIXES and path.suffix != ".docx":
            continue
        if not path.exists():
            continue
        for number, line in enumerate(text_lines(path), 1):
            if EM_DASH in line:
                found += 1
                print(f"EM DASH  {path.relative_to(root)}:{number}: {line.strip()[:100]}")
    print("no em dashes" if found == 0 else f"{found} line(s) with an em dash")
    return 0 if found == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
