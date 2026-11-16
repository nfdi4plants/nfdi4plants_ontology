"""
fix_def_sources.py
------------------
Finds native DPBO terms with empty def sources `[]`, infers the source
from xrefs, URLs in comments/definitions, and CURIE patterns in comments,
then writes a fixed OBO file and a detailed before/after log.
"""

import re
import sys

INPUT_FILE  = "dpbo.obo"
OUTPUT_FILE = "dpbo_fixed.obo"
LOG_FILE    = "fix_def_sources_log.txt"

# Map URL fragments to CURIE prefixes
URL_TO_PREFIX = {
    "purl.obolibrary.org/obo/PECO_": "PECO",
    "purl.obolibrary.org/obo/ENVO_": "ENVO",
    "purl.obolibrary.org/obo/NCIT_": "NCIT",
    "purl.obolibrary.org/obo/OBI_":  "OBI",
    "purl.obolibrary.org/obo/EFO_":  "EFO",
    "purl.obolibrary.org/obo/GO_":   "GO",
    "purl.obolibrary.org/obo/RO_":   "RO",
    "purl.obolibrary.org/obo/UO_":   "UO",
    "purl.obolibrary.org/obo/CHEBI_":"CHEBI",
    "purl.obolibrary.org/obo/CHMO_": "CHMO",
    "purl.obolibrary.org/obo/MS_":   "MS",
    "ebi.ac.uk/efo/EFO_":            "EFO",
    "edamontology.org/":             "EDAM",
    "allotrope.org":                 "Allotrope",
}

CURIE_PATTERN = re.compile(
    r"\b([A-Z][A-Z0-9]+):(\d{4,})\b"
)

def extract_sources(block):
    """
    Try to infer sources from:
    1. xref: lines
    2. URLs in def/comment that map to known prefixes
    3. CURIEs mentioned in comment lines (skipping DPBO itself)
    """
    sources = []
    seen = set()

    def add(s):
        if s and s not in seen:
            seen.add(s)
            sources.append(s)

    # 1. xref: lines
    for xref in re.findall(r"^xref: (\S+)", block, re.MULTILINE):
        add(xref)

    # 2. URLs embedded anywhere in the block
    for fragment, prefix in URL_TO_PREFIX.items():
        for digits in re.findall(
            rf"{re.escape(fragment)}(\d+)", block
        ):
            add(f"{prefix}:{digits}")

    # 3. CURIEs in comment lines only (avoid false positives from def text)
    for line in block.splitlines():
        if line.startswith("comment:"):
            for ns, digits in CURIE_PATTERN.findall(line):
                if ns != "DPBO":
                    add(f"{ns}:{digits}")

    return sources


def process(input_path, output_path, log_path):
    with open(input_path, "r", encoding="utf-8") as f:
        raw = f.read()

    # Split on blank lines between stanzas, preserving the header
    blocks = raw.split("\n\n")

    fixed_blocks = []
    log_entries  = []
    stats = {"fixed": 0, "no_source": 0, "skipped": 0}

    for block in blocks:
        # Only act on Term stanzas
        if not block.startswith("[Term]"):
            fixed_blocks.append(block)
            continue

        id_match = re.search(r"^id: (\S+)", block, re.MULTILINE)
        if not id_match:
            fixed_blocks.append(block)
            continue

        term_id   = id_match.group(1)
        name_match = re.search(r"^name: (.+)", block, re.MULTILINE)
        term_name  = name_match.group(1).strip() if name_match else "?"

        # Only touch native DPBO terms
        if not term_id.startswith("DPBO:"):
            fixed_blocks.append(block)
            stats["skipped"] += 1
            continue

        # Only touch terms with empty def sources
        empty_def = re.search(r'(def: ".*?") \[\]', block, re.DOTALL)
        if not empty_def:
            fixed_blocks.append(block)
            continue

        original_def = empty_def.group(0)
        sources = extract_sources(block)

        if sources:
            source_str  = ", ".join(sources)
            new_def     = f'{empty_def.group(1)} [{source_str}]'
            fixed_block = block.replace(original_def, new_def, 1)
            fixed_blocks.append(fixed_block)
            stats["fixed"] += 1
            log_entries.append(
                f"[FIXED]  {term_id} — {term_name}\n"
                f"  BEFORE: {original_def}\n"
                f"  AFTER:  {new_def}\n"
            )
        else:
            fixed_blocks.append(block)
            stats["no_source"] += 1
            log_entries.append(
                f"[MANUAL] {term_id} — {term_name}\n"
                f"  def: {original_def}\n"
                f"  → No source could be inferred. Needs manual review.\n"
            )

    # Write fixed OBO
    with open(output_path, "w", encoding="utf-8") as f:
        f.write("\n\n".join(fixed_blocks))

    # Write log
    with open(log_path, "w", encoding="utf-8") as f:
        f.write("=" * 70 + "\n")
        f.write("fix_def_sources.py — run log\n")
        f.write("=" * 70 + "\n\n")
        f.write(f"Input:   {input_path}\n")
        f.write(f"Output:  {output_path}\n\n")
        f.write(f"Summary:\n")
        f.write(f"  Fixed automatically : {stats['fixed']}\n")
        f.write(f"  Needs manual review : {stats['no_source']}\n")
        f.write(f"  Skipped (non-DPBO)  : {stats['skipped']}\n\n")
        f.write("=" * 70 + "\n\n")
        f.write("\n".join(log_entries))

    print(f"Done.")
    print(f"  Fixed automatically : {stats['fixed']}")
    print(f"  Needs manual review : {stats['no_source']}")
    print(f"  Output → {output_path}")
    print(f"  Log    → {log_path}")


if __name__ == "__main__":
    input_file = sys.argv[1] if len(sys.argv) > 1 else INPUT_FILE
    process(input_file, OUTPUT_FILE, LOG_FILE)
