# PR #559 rebase, 25 October 2026

Frozen performance source `00af81b4470d1ef158c28d9f6c5a03c5142acb31` is based directly on rewritten #558 `dd989683d484bfc4a8c13e65373f528314885f24`, itself based on current upstream master `0f37356fc9ca2c05b085f94592cfaf492f78eacf`.

The retained production changes are direct named LightExpression field lookup and direct collection of parameter/constant binary left operands. The old constructor signature-pool production patch is superseded by master and was removed; its alternating-signature regression remains. Returning the same signature from both helper and root caller would be incorrect after this rebase. No compatibility fixes from #558 are claimed as performance changes here.

Release solution build passed. Native Standard/Light suites passed on .NET 10 and .NET 8, including registered regression fixtures. Their legacy runner counters are ['1734'] and ['1733']. Framework counters are ['1051', '1617'], using current/default-thread English UI culture for existing English exception-message assertions. Imported runtime suite: 35,088 passed, 59 documented skips, zero failures or crashes. Exact logs are in `validation-logs.zip`.

The earlier Rebase-20261024 and Optimize-20261024 measurements remain historical and retain their original source IDs. In particular, their constructor-pool allocation savings are already in current master, and are not an incremental gain from this PR. Fresh matched current-master/#558/#559 measurements and further candidate screening are being recorded separately; no fresh timing claim is made by this validation commit.
