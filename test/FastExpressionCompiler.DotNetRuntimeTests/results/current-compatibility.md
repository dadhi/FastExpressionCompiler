# Current compatibility validation

The adapter requires strict FEC compilation and never substitutes System compilation. Native runners cover Standard and LightExpression; imported System.Linq.Expressions tests cover the Standard input API.

The imported suite retains upstream interpreter flags as repeated FEC executions. FEC has no interpreter.

## Exclusions

System-specific IL, closure-layout and interpreter-internal assertions are excluded only where dedicated behavior tests verify the supported FEC implementation. Interpreter-only nested exception handling inside filters is rejected by FEC and System's IL compiler. The previously deferred deep-tree stress case remains excluded without claiming correct FEC behavior.

Current validation results are listed in the pull request description. See the [test instructions](../README.md) for reproduction commands.
