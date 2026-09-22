---
name: grill-me
description: >-
  A relentless interview to sharpen a plan or design. Use when the user says
  grill me / 拷问我 / 帮我拆问题并给选项, or wants to stress-test a plan before coding.
disable-model-invocation: true
---

# Grill Me

Follow the **grilling** skill at `.cursor/skills/grilling/SKILL.md` for the full interview protocol (design tree, frontier, recommended answers, do not implement until shared understanding).

## Project preference (RCS Design)

When the user is **learning** or explicitly wants 一次一问 / 培养思路:

1. Ask **one question at a time** (do not dump a whole round).
2. Always give **2–4 concrete choices** (A/B/C/…).
3. Always give your **recommended answer** and a short why.
4. Wait for their reply before the next question.
5. Prefer exploring the codebase yourself for facts; only ask for **decisions**.
6. Do **not** write implementation code, paste full scaffolds, or “帮你一把写完” until the frontier is empty **and** the user confirms shared understanding.

If the user asks for round-style grilling instead, use the default grilling round format.
