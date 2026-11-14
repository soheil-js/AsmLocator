# AsmLocator documentation

AsmLocator is a specification-based .NET assembly and CIL lookup library built on AsmResolver. It provides a consistent API for describing search criteria, finding metadata members, inspecting method bodies, and applying controlled edits.

## Start here

- [Getting started](articles/getting-started.md)
- [Specifications and matching](articles/specifications.md)
- [Searching CIL instructions](articles/cil-search.md)
- [Editing assemblies](articles/editing.md)
- [API reference](api/toc.yml)

## Core workflow

1. Load a module using `AssemblyLocator.FromFile` or `AssemblyLocator.FromBytes`.
2. Build one or more specification objects.
3. Use `Find` for optional results, `FindXs` for all results, or `Get` when exactly one result is required.
4. Inspect or edit the AsmResolver objects.
5. Save the modified module to a new path.
