# Generate Implementation Tasks

Generate an implementation plan for the approved feature.

Inputs

- Constitution
- Architecture
- Technology
- Repository Structure
- Approved Specification

Rules

Tasks must:

- Follow the specification.
- Be independently implementable.
- Be independently testable.
- Have a single responsibility.
- Be ordered by dependency.

Do NOT generate code.

Output

tasks.md

Each task should include:

- Id
- Name
- Description
- Dependencies
- Expected Outcome