#!/usr/bin/env python3
"""Fails when platform/framework concepts leak into DevTools contract assemblies."""

from pathlib import Path
import sys
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]

strict_contract_dirs = ("DevTools.Screenshot.Sharp",)
dependency_only_contract_dirs = ("DevTools.HostLogging.Sharp",)

forbidden_tokens = (
    "Avalonia",
    "Microsoft.Maui",
    "Microsoft.UI",
    "Microsoft.Win32",
    "System.Windows",
    "Windows.Storage",
    "WinRT",
    "WPF",
    "WinUI",
    "HWND",
    "WindowHandle",
)

violations: list[str] = []


def check_project_dependencies(contract_name: str, *, allow_dependencies: bool) -> None:
    project = root / contract_name / f"{contract_name}.csproj"
    tree = ET.parse(project)
    for item_name in ("ProjectReference", "PackageReference", "FrameworkReference"):
        for item in tree.findall(f".//{item_name}"):
            include = item.attrib.get("Include", "<unknown>")
            if allow_dependencies:
                for token in forbidden_tokens:
                    if token.casefold() in include.casefold():
                        violations.append(
                            f"{project.relative_to(root)} contains forbidden {item_name}: {include}"
                        )
            else:
                violations.append(
                    f"{project.relative_to(root)} contains forbidden {item_name}: {include}"
                )


for contract_name in strict_contract_dirs:
    contracts = root / contract_name
    for path in contracts.glob("*.cs"):
        if path.name == "AssemblyInfo.cs":
            continue
        text = path.read_text(encoding="utf-8")
        for token in forbidden_tokens:
            if token.casefold() in text.casefold():
                violations.append(f"{path.relative_to(root)} contains {token!r}")

    check_project_dependencies(contract_name, allow_dependencies=False)

for contract_name in dependency_only_contract_dirs:
    check_project_dependencies(contract_name, allow_dependencies=True)

if violations:
    print("Contract boundary violations:", file=sys.stderr)
    for violation in violations:
        print(f"- {violation}", file=sys.stderr)
    raise SystemExit(1)

print("Contract boundary OK")
