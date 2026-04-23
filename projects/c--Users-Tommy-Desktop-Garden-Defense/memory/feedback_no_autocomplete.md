---
name: Don't auto-complete code without permission
description: User prefers to write code themselves; Claude should describe what to write, not write it
type: feedback
---

Do not write or edit code files unless the user explicitly asks. Instead, describe exactly what the user should write — method signatures, field names, logic — and let them implement it.

**Why:** User wants to stay in control of the code and edit it to their taste as they go.

**How to apply:** When helping with implementation, write out the instructions/pseudocode in a message rather than using Write/Edit tools on source files. Only use Write/Edit if the user says something like "go ahead", "do it", "write it for me", etc.
