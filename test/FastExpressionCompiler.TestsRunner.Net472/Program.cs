using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FastExpressionCompiler.IssueTests;

namespace FastExpressionCompiler.UnitTests
{
    public class Program
    {
        public static void Main()
        {
            // new Issue414_Incorrect_il_when_passing_by_ref_value().Run();
            // new Issue55_CompileFast_crash_with_ref_parameter().Run();

            RunTestsX();

            RunAllTests();
        }

        public static void RunTestsX()
        {
            var totalStopwatch = Stopwatch.StartNew();

            Console.WriteLine("""

            //## TestX runs on FEC tests (UnitTests and IssueTests) and FEC.LightExpression tests in ||
            """);

            var lightTestsStopwatch = Stopwatch.StartNew();
            var lightTestsThread = new Thread(RunLightExpressionTests) { IsBackground = false, Name = "Tests - FEC.LightExpression" };
            static void RunLightExpressionTests(object state)
            {
                var justLightTestsStopwatch = Stopwatch.StartNew();

                var t = (LightExpression.TestRun)state;
                GeneratedTests.RunLight(t);

                Console.WriteLine($"Just LightExpression tests are passing in {justLightTestsStopwatch.ElapsedMilliseconds} ms.");
            }
            var lightTests = new LightExpression.TestRun();
            lightTestsThread.Start(lightTests);

            var fecTestsStopwatch = Stopwatch.StartNew();

            var fecTests = new TestRun();
            GeneratedTests.RunStandard(fecTests);

            Console.WriteLine($"FEC tests are passing in {fecTestsStopwatch.ElapsedMilliseconds} ms.");

            var waitForLightStopwatch = Stopwatch.StartNew();
            lightTestsThread.Join(); // wait for the light tests to finish
            Console.WriteLine($"LightExpression tests + Thread New, Start, Join: {lightTestsStopwatch.ElapsedMilliseconds} ms.");
            Console.WriteLine($"--> waited for the LightExpression tests to finish after FEC tests complete for {waitForLightStopwatch.ElapsedMilliseconds} ms.");

            Console.WriteLine();
            if (fecTests.FailedTestCount > 0 || lightTests.FailedTestCount > 0)
            {
                // todo: @wip output the failed tests
                Console.WriteLine("ERROR: Some tests are FAILED!");
                Environment.Exit(1); // exit with error
                return;
            }

            var totalTestCount = fecTests.TotalTestCount + lightTests.TotalTestCount;
            Console.WriteLine($"//TestX: {totalTestCount,-4} tests are passing in {totalStopwatch.ElapsedMilliseconds} ms.");
        }

        public static void RunAllTests()
        {
            var failed = false;
            var totalTestsPassed = 0;
            void Run(Func<int> run, string name = null)
            {
                try
                {
                    var testsPassed = run();
                    System.Threading.Interlocked.Add(ref totalTestsPassed, testsPassed);
#if DEBUG
                    // we don't need to list the tests one-by-one on CI, and it makes avoiding it saves 30% of time
                    var testsName = name ?? run.Method.DeclaringType.FullName;
                    Console.WriteLine($"{testsPassed,-4} of {testsName}");
#endif
                }
                catch (Exception ex)
                {
                    failed = true;
                    var testsName = name ?? run.Method.DeclaringType.FullName;
                    Console.WriteLine($"""
                    --------------------------------------------
                    ERROR: Tests `{testsName}` failed with
                    {ex}
                    --------------------------------------------
                    """);
                }
            }

            Console.WriteLine("""

            //## .NET Framework 4.72: Running UnitTests and IssueTests in parallel...
            """);

            var sw = Stopwatch.StartNew();

            var unitTests = Task.Run(() =>
            {
                GeneratedTests.RunUnitLegacy(Run);

                Console.WriteLine($"{Environment.NewLine}//UnitTests are passing in {sw.ElapsedMilliseconds} ms.");
            });

            var issueTests = Task.Run(() =>
            {
                GeneratedTests.RunIssueLegacy(Run);

                Console.WriteLine($"{Environment.NewLine}//IssueTests are passing in {sw.ElapsedMilliseconds} ms.");
            });

            Task.WaitAll(unitTests, issueTests);
            Console.WriteLine();
            if (failed)
            {
                Console.WriteLine("//ERROR: Some tests are FAILED!");
                Environment.Exit(1); // error
                return;
            }

            Console.WriteLine($"//ALL {totalTestsPassed,-4} tests are passing in {sw.ElapsedMilliseconds} ms.");
        }
    }
}
