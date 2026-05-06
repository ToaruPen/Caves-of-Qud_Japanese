# PR Review Workflow

Use this guide for PR mechanics: local preflight, review feedback, force-push
state, and review convergence. Translation ownership decisions still belong in
`docs/RULES.md`.

## Local preflight

Before publishing or updating a broad PR that touches C# patches, script tools,
or localization assets, prefer the CI-like local gate:

```bash
just pr-check
```

This gate intentionally uses the same Release build/test shape as CI for the
QudJP assemblies. Debug `just check` can pass while Release analyzers still fail.

Localization asset changes also need a changed release-note fragment under
`docs/release-notes/unreleased/*.md`; `just pr-check` verifies that requirement
against `origin/main..HEAD`.

## Review checkout

Before addressing PR review feedback, confirm the checkout matches the PR head
branch:

```bash
git status --short --branch
gh pr view <number> --json headRefName
```

If the active checkout has unrelated dirty work, either use a separate worktree
or stage only explicit paths for the PR. Do not commit review fixes from an
unrelated branch just because the patch applies cleanly.

## Route-family feedback

For CodeRabbit or reviewer feedback on route ownership, do not stop at the exact
literal or line named in the comment. Treat the comment as evidence of a route
family contract issue, then check:

- sibling owner tokens
- parser branches
- punctuation and casing variants
- scoped dictionary homes
- owner-vs-sink boundaries that share the same behavior

## CodeRabbit state

When interpreting CodeRabbit state after force-pushes, report the current check
status separately from `reviewDecision` and old review bodies. The latest review
body can describe an older commit range even when the current CodeRabbit status
context is passing.

Use `gh pr view` for the current PR head and check rollup:

```bash
gh pr view <number> --json headRefOid,reviewDecision,statusCheckRollup,latestReviews
```

The CodeRabbit status context is named `CodeRabbit`. Treat it as current only
when it belongs to the PR head you are reporting.

Inspect the latest review bodies in addition to thread state. CodeRabbit can
put actionable non-inline notes, such as duplicate-comment summaries, in a
review body without leaving a separate unresolved review thread. Treat a
current-head review body with actionable text as open review work even when the
review-thread count is zero.

Use review-thread state, latest review bodies, and current checks together to
decide whether actionable comments remain. The practical convergence check is:

- unresolved actionable review threads are zero
- latest current-head review bodies contain no actionable non-threaded notes
- checks on the current head pass
- the current `CodeRabbit` status context is `SUCCESS`

Use GitHub review threads as the source for unresolved-thread state:

```bash
gh api graphql -f owner=ToaruPen -f repo=coq-japanese_stable -F number=<number> -f query='
query($owner:String!, $repo:String!, $number:Int!) {
  repository(owner:$owner, name:$repo) {
    pullRequest(number:$number) {
      reviewThreads(first:100) {
        nodes { isResolved isOutdated }
      }
    }
  }
}'
```

If those are true but `reviewDecision` still says `CHANGES_REQUESTED`, say the
code-side findings appear converged and GitHub's approval decision is still
lagging or blocked by older reviews. Do not call the PR fully approved until an
approval review or the repository's accepted alternate review path exists.
