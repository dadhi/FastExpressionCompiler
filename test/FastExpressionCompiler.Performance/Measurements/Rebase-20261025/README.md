# PR #559 rebase, 25 October 2026

Frozen performance source `00af81b4470d1ef158c28d9f6c5a03c5142acb31` is based directly on rewritten #558 `dd989683d484bfc4a8c13e65373f528314885f24`, itself based on current upstream master `0f37356fc9ca2c05b085f94592cfaf492f78eacf`.

The retained production changes are direct named LightExpression field lookup and direct collection of parameter/constant binary left operands. The old constructor signature-pool production patch is superseded by master and was removed; its alternating-signature regression remains. Returning the same signature from both helper and root caller would be incorrect after this rebase. No compatibility fixes from #558 are claimed as performance changes here.

Release solution build passed. Native Standard/Light suites passed on .NET 10 and .NET 8, including registered regression fixtures. Their legacy runner counters are ['1734'] and ['1733']. Framework counters are ['1051', '1617'], using current/default-thread English UI culture for existing English exception-message assertions. Imported runtime suite: 35,088 passed, 59 documented skips, zero failures or crashes. Exact logs are in `validation-logs.zip`.

The published performance comparison contains only current master, current #558, and final #559. Obsolete previous-branch reports are removed. Matched measurements separate tree construction, compilation, compilation plus first invocation, warm execution and managed allocation; System is an unchanged standalone reference.
