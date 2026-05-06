---
name: code-audit
description: Performs a comprehensive, read-only technical audit of Unity/C# codebases.
when_to_use: When I ask to "run code audit," "check," or "deep dive" into the game code.
---

# Unity/C# Deep-Dive Audit Protocol (READ-ONLY)

## ⚠️ STRICT OPERATIONAL CONSTRAINT
- **READ-ONLY:** Do not edit, overwrite, or delete any code. 
- **NO AUTO-FIXES:** Your task is to diagnose, not to treat. Do not generate refactored versions of the files unless explicitly asked later.

## 1. Audit Scope
Scan the project for the following Unity-specific performance and stability issues:
- **Frame-Rate Killers:** `GetComponent`, `Find`, or complex math inside `Update()`.
- **GC Pressure:** LINQ, `new` keyword, or string concatenation in high-frequency loops.
- **Physics:** Inefficient usage of `FixedUpdate`, `Raycasts` without layers, or missing `Rigidbody` components on moving colliders.
- **Architecture:** "Manager-hell" Singletons, improper `[SerializeField]` usage, and lack of `const/readonly` for static values.

## 2. Requirement: Full Issue Transparency
Do not group issues into a generic summary. **Every unique error or optimization opportunity must be listed individually.**

## 3. Reporting Structure
For every issue found, use the following format:

---
### [Severity Score] | [File Name] : [Line Number]
- **The Issue:** Clear description of the code pattern found.
- **The Cause:** Explain *why* this is a problem in Unity (e.g., "Triggers the Garbage Collector every frame").
- **The Fix:** Provide a step-by-step conceptual instruction on how to resolve it (e.g., "Cache the reference in Awake() instead of calling GetComponent in Update").
- **Sample Fix Logic:** Provide a brief snippet showing the *concept* (not a file rewrite).

## 4. Final Audit Summary
After the detailed individual entries, provide:
1. **Critical Count:** Total number of 🔴 Red flags.
2. **Performance Outlook:** How this code will likely perform on mid-range hardware.
3. **The "Big Three":** The three most impactful changes they should make first.