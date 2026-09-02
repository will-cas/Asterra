#!/usr/bin/env python3
"""Deprecated. Forwards to export_art_review.py."""
from pathlib import Path
import runpy

runpy.run_path(str(Path(__file__).resolve().with_name("export_art_review.py")), run_name="__main__")
