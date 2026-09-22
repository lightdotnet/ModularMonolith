#!/usr/bin/env node
// Re-grounds Claude in real repo state after a compaction or a resumed session,
// when prior conversation turns have just been summarized away.
// Invoked by .claude/settings.json (PostCompact, SessionStart[resume]) as:
//   node .claude/hooks/context-recap.js <hookEventName>

const { execSync } = require("child_process");

const hookEventName = process.argv[2] || "PostCompact";

function run(cmd) {
  try {
    return execSync(cmd, { encoding: "utf8" }).trim();
  } catch {
    return null;
  }
}

const status = run("git status --short -b");
const verb = hookEventName === "SessionStart" ? "resumed" : "compacted";

const lines = [
  `Context was just ${verb}. Re-grounding in actual repo state (not a prior summary):`,
  "",
  status || "(git status unavailable)",
  "",
  "Before continuing: re-check root CLAUDE.md operating rules — Vietnamese input is normal, " +
    "everything written to the repo must be English; the code-change workflow gate (plan first, " +
    "wait for explicit user approval, implement, present for review — never auto-run tests or " +
    "update docs as a follow-on step); read only what the current task needs; prefer delegating to " +
    "the agents listed in CLAUDE.md §4 over broad inline analysis. If it's unclear which backend " +
    "module or client app the task applies to, ask rather than assuming.",
];

console.log(
  JSON.stringify({
    hookSpecificOutput: {
      hookEventName,
      additionalContext: lines.join("\n"),
    },
  })
);
