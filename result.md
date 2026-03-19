## Results001

Here’s a quick diff-style assessment of printcs-20260303-02.out versus the earlier issues we flagged (ignoring non-code lines like timing, headers, and params):

Fixed
- Missing semicolons after Invoke calls
  - Issue156_InvokeAction.InvokeActionConstantIsSupported now ends with a semicolon:
    (default(Action<object, object>)/*...*/).Invoke((object)4,(object)2);
  - Same fix in the LightExpression variant.
- Missing semicolons on several serializer calls
  - Issue248 tests (Test_1..Test_4) now end invocations with ; (e.g., serializer.WriteDecimalByVal(data.Field1);).
- Illegal bare expression statements changed to valid statements in some places
  - ArithmeticOperationsTests.Can_modulus_custom_in_Action now uses a discard assignment: _ = a % b; (valid).
  - Several assignments inside lambdas are now written as _ = myVar = 3; (valid, though the _ = is optional).

Still not fixed (representative examples)
- Multi-dimensional arrays declared with single-dim type
  - AssignTests.Array_multi_dimensional_index_assign_value_type_block still has int[] int_arr_0; with new int[2,1] and int_arr_0[1,0] = 5; — should be int[,] int_arr_0.
- Reserved keyword used as a label
  - Issue495 still contains return:; which is illegal; label names must not be keywords (e.g., ReturnLabel:).
- Ref/out misuse around ref returns and ref assignments
  - Issue365_Working_with_ref_return_values has varByRef = ref ref pp.GetParamValueByRef(); (double ref) and _ = ref pp.GetParamValueByRef().Value = 7; (invalid ref-qualified property assignment target).
- Incorrect ref assignment to an out variable
  - Issue237 deserializers still have bytesRead = ref reader.Consumed; — should be bytesRead = reader.Consumed;.
- Unknown/typo identifier in serializer
  - Issue248 uses indata.Field1 / indata.NestedTest.Field1 — likely meant data.Field1 or in data.Field1.
- Func<int> bodies with no return
  - BlockTests.Block_local_variable_assignment still lacks a return value.
- as usage on a non-nullable value to nullable conversion
  - Issue455 uses x = (long)12345 as int?; — invalid; long must be boxed or use a normal cast ((int?)(int)12345) or (object)longValue as int?.
- Bare expression statements that remain invalid in at least one case
  - ArithmeticOperationsTests.Can_modulus_custom_in_Action_block still has a % b; without _ =; only assignment, call, increment/decrement, await, or new are valid statement expressions.

Net result
- Some clear syntax errors have been addressed (most notably the missing semicolons and some bare-expression statements).
- Several critical compile blockers remain (multi-dim array type, reserved label name, ref/ref misuse, bytesRead assignment with ref, indata identifier, missing returns). These will still prevent compiling the snippets as-is.

If you want, I can prepare a concrete patch list of transformation rules to make the emitter produce compilable code (e.g., map int[] => int[,] when seeing [,], normalize labels, rewrite ref/ref patterns, fix bytesRead assignment, and ensure all Func<T> blocks return a value), or set up an automated per-snippet checker. Toggle to Act mode and I’ll implement it.

## Results002

Invalid (not compile-able) snippets found in `DebugOutputs/printcs_20260310-01.out`:

1. **Issue495_Incomplete_pattern_detection...ReturnGotoFromTryCatchWithAssign_ShouldBeDetectedAsError1007**
   - Contains label `return:;`
   - `return` is a C# keyword and cannot be used as a label identifier.

2. **AssignTests.Array_multi_dimensional_index_assign_value_type_block** (appears multiple times)
   - Declares `int[,] int[,]_0 = null;`
   - Invalid variable declaration/identifier syntax (`int[,]_0` cannot be an identifier).

3. **Issue487_Fix_ToCSharpString_output_for_boolean_equality_expressions.Original_case**
   - Snippet is `var @cs = (x.MyTestBool);`
   - `x` is not declared in the snippet, so it is not compile-able as printed.

4. **Issue237_Trying_to_implement_For_Foreach_loop_but_getting_an_InvalidProgramException_thrown.Should_Deserialize_Simple** (both `Word` and `Simple` versions)
   - Reuses label `bool_0:` multiple times in the same method body.
   - C# labels must be unique per method.

5. **Issue237_Trying_to_implement_For_Foreach_loop_but_getting_an_InvalidProgramException_thrown.Try_compare_strings**
   - Reuses label `string_0:` twice in the same method body.
   - Duplicate label in same method is a compile error.

6. **Issue237_Trying_to_implement_For_Foreach_loop_but_getting_an_InvalidProgramException_thrown.Conditional_with_Equal_true_should_shortcircuit_to_Brtrue_or_Brfalse**
   - Reuses label `string_0:` twice in the same method body.
   - Duplicate label in same method is a compile error.

7. **Issue320_Bad_label_content_in_ILGenerator_when_creating_through_DynamicModule.Test_instance_call**
   - `Func<int>` body has no `return` statement:
     ```csharp
     var @cs = (Func<int>)(() => //int
     {
         int ret = default;
         if (true)
         {
             ret = new TestMethods(314).InstanceMethod();
         }
         int_0:;
     });
     ```
   - Not all code paths return a value.

8. **Issue321_Call_with_out_parameter_to_field_type_that_is_not_value_type_fails.Test_outparameter**
   - Uses `out default(TestPOD)...` as out argument target.
   - `out` argument must be an assignable variable/storage location, not a member on a temporary `default(...)` value.

9. **Issue365_Working_with_ref_return_values.Test_access_ref_returning_method_then_property**
   - Contains `ref pp.GetParamValueByRef().Value = 7;`
   - Invalid C# assignment syntax (cannot use `ref <expr> = <value>` like this).

10. **Issue439_Support_unused_Field_access_in_Block.Original_case**
    - Contains bare statement `testClass.Result0;`
    - Member-access expression alone is not a valid statement expression in C#.

## Results003

Review of `printcs_20260312-01.out` (delta vs Results002):

### Fixed since Results002

1. **AssignTests.Array_multi_dimensional_index_assign_value_type_block**
   - Previously had invalid declaration like `int[,] int[,]_0 = null;`.
   - Now emitted as valid declaration, e.g. `int[,] int__a_0 = null;`.

2. **Issue237_* label-collision cases**
   - The previously reported duplicate labels (`bool_0:` / `string_0:` duplicates in same body) are no longer present in the shown snippets.

### Still invalid in 20260312-01

1. **Issue495_Incomplete_pattern_detection...ReturnGotoFromTryCatchWithAssign_ShouldBeDetectedAsError1007**
   - Still contains label `return:;`.
   - `return` is a keyword and cannot be used as a label identifier.

2. **Issue487_Fix_ToCSharpString_output_for_boolean_equality_expressions.Original_case**
   - Snippet is `var @cs = x.MyTestBool;`
   - `x` is undeclared in the emitted snippet.

3. **Issue320_Bad_label_content_in_ILGenerator_when_creating_through_DynamicModule.Test_instance_call**
   - `Func<int>` body still ends without a `return` statement on all paths.

4. **Issue321_Call_with_out_parameter_to_field_type_that_is_not_value_type_fails.Test_outparameter**
   - Still uses `out default(TestPOD)...` as out target.
   - `out` requires an assignable variable/storage location.

5. **Issue365_Working_with_ref_return_values.Test_access_ref_returning_method_then_property**
   - Still contains `ref pp.GetParamValueByRef().Value = 7;`
   - Invalid assignment form in C#.

6. **Issue439_Support_unused_Field_access_in_Block.Original_case**
   - Still contains bare member access statement `testClass.Result0;`.

### New invalids noticed in 20260312-01 (not listed in Results002)

1. **Issue428_Expression_Switch_without_a_default_case_incorrectly_calls_first_case_for_unmatched_values**
   - Emitted switch has non-empty `case 1:` directly followed by `case 2:` without `break/goto/return`.
   - This is C# compile error: control cannot fall through from one case label to another.

2. **Issue422_InvalidProgramException_when_having_TryCatch_Default_in_Catch.Original_case_but_comparing_with_non_null_left_operand**
   - Uses `left == ...` where `left` is not declared in snippet.

### Net

- Good progress: multidimensional array declaration and previously duplicated labels appear fixed.
- Remaining compile blockers are still present (keyword-label, missing returns, invalid out target, invalid ref assignment form, bare member-access statement), plus at least two newly observed invalid patterns (switch fall-through and undeclared `left`).

## Results004

Review of `printcs_20260317-01.out` (delta vs Results003 / `printcs_20260312-01.out`):

### Fixed since Results003

1. **Issue495_Incomplete_pattern_detection...ReturnGotoFromTryCatchWithAssign_ShouldBeDetectedAsError1007**
   - Previously reported as invalid due to label `return:;`.
   - Now emitted as `@return:;` (verbatim identifier), which is valid C# label syntax.

### Still invalid (previously known, still present)

1. **Issue487_Fix_ToCSharpString_output_for_boolean_equality_expressions.Original_case**
   - Still emits `var _ = x.MyTestBool;` with `x` undeclared in the snippet.

2. **Issue320_Bad_label_content_in_ILGenerator_when_creating_through_DynamicModule.Test_instance_call**
   - `Func<int>` body still has no reachable `return` on all paths (ends with label only).

3. **Issue321_Call_with_out_parameter_to_field_type_that_is_not_value_type_fails.Test_outparameter**
   - Still uses `out default(TestPOD)...` as out argument target (not assignable).

4. **Issue439_Support_unused_Field_access_in_Block.Original_case**
   - Still contains bare member-access statement `testClass.Result0;` (invalid statement expression).

5. **Issue428_Expression_Switch_without_a_default_case_incorrectly_calls_first_case_for_unmatched_values**
   - Still has case-to-case fall-through in emitted C# without terminating jump/return.

6. **Issue422_InvalidProgramException_when_having_TryCatch_Default_in_Catch.Original_case_but_comparing_with_non_null_left_operand**
   - Still references undeclared identifier `left`.

### Previously reported invalid no longer observed in this output

1. **Issue365_Working_with_ref_return_values.Test_access_ref_returning_method_then_property**
   - Now emitted as `pp.GetParamValueByRef().Value = 7;` (the prior invalid `ref ... = ...` form is gone).

### New errors introduced in this run

- No additional *new* compile blockers were identified beyond the already-known set above.

## Results005

Review of `printcs_20260319-01.out` (delta vs Results004 / `printcs_20260317-01.out`):

### Fixed since Results004

1. **Issue428_Expression_Switch_without_a_default_case_incorrectly_calls_first_case_for_unmatched_values**
   - Previously invalid due to fall-through from `case 1` to `case 2`.
   - Now each case ends with `break;`, so the emitted switch is valid C#.

2. **Issue320_Bad_label_content_in_ILGenerator_when_creating_through_DynamicModule.Test_instance_call**
   - Previously `Func<int>` could end without a return.
   - Now emits `return ret;` after label, so all paths return an `int`.

### Still invalid (previously known, still present)

1. **Issue487_Fix_ToCSharpString_output_for_boolean_equality_expressions.Original_case**
   - Still emits `var _ = x.MyTestBool;` with `x` undeclared in the snippet.

2. **Issue321_Call_with_out_parameter_to_field_type_that_is_not_value_type_fails.Test_outparameter**
   - Still uses `out default(TestPOD)...` as out argument target (not assignable).

3. **Issue439_Support_unused_Field_access_in_Block.Original_case**
   - Still contains bare member-access statement `testClass.Result0;` (invalid statement expression).

4. **Issue422_InvalidProgramException_when_having_TryCatch_Default_in_Catch.Original_case_but_comparing_with_non_null_left_operand**
   - Still references undeclared identifier `left`.

### Still valid (previously fixed and remains fixed)

1. **Issue495_Incomplete_pattern_detection...ReturnGotoFromTryCatchWithAssign_ShouldBeDetectedAsError1007**
   - Label remains `@return:;` (valid verbatim identifier label).

2. **Issue365_Working_with_ref_return_values.Test_access_ref_returning_method_then_property**
   - Still emitted as `pp.GetParamValueByRef().Value = 7;` (valid assignment form).

### New errors introduced in this run

- No additional *new* compile blockers were identified beyond the remaining known set.