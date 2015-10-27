// Python Tools for Visual Studio
// Copyright(c) Microsoft Corporation
// All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the License); you may not use
// this file except in compliance with the License. You may obtain a copy of the
// License at http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY
// IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.
//
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PythonTools.Debugger;
using Microsoft.PythonTools.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudioTools;
using TestUtilities;

namespace DebuggerTests {
    [TestClass, Ignore]
    public abstract class DebuggerTests : BaseDebuggerTests {
        [TestInitialize]
        public void CheckVersion() {
            if (Version == null) {
                Assert.Inconclusive("Required version of Python is not installed");
            }
        }

        internal override PythonVersion Version {
            get {
                throw new NotImplementedException("Do not invoke tests on base class");
            }
        }


        [TestMethod, Priority(3)]
        public void TestThreads() {
            // TODO: Thread creation tests w/ both thread.start_new_thread and threading module.
            Assert.Fail("TODO: Thread creation tests w/ both thread.start_new_thread and threading module.");
        }

        private string InfRepr {
            get {
                return Version.Version > PythonLanguageVersion.V25 ? "inf" : "1.#INF";
            }
        }

        #region Enum Children Tests

        [TestMethod, Priority(1)]
        [TestCategory("10s")]
        public void EnumChildrenTest() {
            const int lastLine = 41;

            ChildTest(EnumChildrenTestName, lastLine, "s", GetSetChildren(
                new ChildInfo("[0]", "next((v for i, v in enumerate(s) if i == 0))", Version.Version.Is3x() ? "frozenset({2, 3, 4})" : "frozenset([2, 3, 4])")));

            if (Version.Version.Is2x() && !(this is DebuggerTestsIpy)) {
                // IronPython unicode repr differs
                // 3.x: http://pytools.codeplex.com/workitem/76
                ChildTest(EnumChildrenTestName, lastLine, "cinst",
                    new ChildInfo("abc", null, "42", "0x2a"),
                    // "u\'привет мир\'"
                    new ChildInfo("uc", null, @"u'\xd0\xbf\xd1\x80\xd0\xb8\xd0\xb2\xd0\xb5\xd1\x82 \xd0\xbc\xd0\xb8\xd1\x80'"));
            }
            ChildTest(EnumChildrenTestName, lastLine, "c2inst",
                new ChildInfo("abc", null, "42", "0x2a"),
                new ChildInfo("oar", null, "100", "0x64"),
                new ChildInfo("self", null, "myrepr", "myhex"));
            ChildTest(EnumChildrenTestName, lastLine, "c3inst",
                new ChildInfo("_contents", null, "[1, 2]"),
                new ChildInfo("abc", null, "42", "0x2a"),
                new ChildInfo("[0]", null, "1"),
                new ChildInfo("[1]", null, "2"));
            ChildTest(EnumChildrenTestName, lastLine, "l", GetListChildren(
                new ChildInfo("[0]", null, "1"),
                new ChildInfo("[1]", null, "2")));
            ChildTest(EnumChildrenTestName, lastLine, "d1", GetDictChildren(
                new ChildInfo("[42]", null, "100", "0x64")));
            string itemsName = Version.Version == PythonLanguageVersion.V27 ? "viewitems" : "items";
            ChildTest(EnumChildrenTestName, lastLine, "d2", GetDictChildren(
                new ChildInfo("['abc']", null, "'fob'")));
            ChildTest(EnumChildrenTestName, lastLine, "d3", GetDictChildren(
                new ChildInfo("[" + InfRepr + "]", "next((v for i, (k, v) in enumerate(d3." + itemsName + "()) if i == 0))", "{42: 100}")));
            ChildTest(EnumChildrenTestName, lastLine, "i", null);
            ChildTest(EnumChildrenTestName, lastLine, "u1", null);
        }

        private ChildInfo[] GetSetChildren(ChildInfo items) {
            if (this is DebuggerTestsIpy) {
                return new ChildInfo[] { new ChildInfo("Count", null), items };
            }
            return new[] { items };
        }

        private ChildInfo[] GetListChildren(params ChildInfo[] items) {
            if (this is DebuggerTestsIpy) {
                var res = new List<ChildInfo>(items);
                res.Add(new ChildInfo("Count", null));
                res.Add(new ChildInfo("Item", null));
                return res.ToArray();
            }
            return items;
        }

        private ChildInfo[] GetDictChildren(params ChildInfo[] items) {
            var res = new List<ChildInfo>();

            if (Version.Version == PythonLanguageVersion.V27) {
                res.Add(new ChildInfo("viewitems()", null, ""));
            } else {
                res.Add(new ChildInfo("items()", null, ""));
            }

            res.AddRange(items);

            if (this is DebuggerTestsIpy) {
                res.Add(new ChildInfo("Count", null));
                res.Add(new ChildInfo("Item", null));
                res.Add(new ChildInfo("Keys", null));
                res.Add(new ChildInfo("Values", null));
            }

            return res.ToArray();
        }

        [TestMethod, Priority(1)]
        [TestCategory("10s")]
        public void EnumChildrenTestPrevFrame() {
            const int breakLine = 2;

            ChildTest("PrevFrame" + EnumChildrenTestName, breakLine, "s", 1, GetSetChildren(
                new ChildInfo("[0]", "next((v for i, v in enumerate(s) if i == 0))", Version.Version.Is3x() ? "frozenset({2, 3, 4})" : "frozenset([2, 3, 4])")));

            if (GetType() != typeof(DebuggerTestsIpy) && Version.Version.Is2x()) {
                // IronPython unicode repr differs
                // 3.x: http://pytools.codeplex.com/workitem/76
                ChildTest("PrevFrame" + EnumChildrenTestName, breakLine, "cinst", 1,
                    new ChildInfo("abc", null, "42", "0x2a"),
                    new ChildInfo("uc", null, "u\'привет мир\'"));
            }
            ChildTest("PrevFrame" + EnumChildrenTestName, breakLine, "c2inst", 1,
                new ChildInfo("abc", null, "42", "0x2a"),
                new ChildInfo("oar", null, "100", "0x64"),
                new ChildInfo("self", null, "myrepr", "myhex"));
            ChildTest("PrevFrame" + EnumChildrenTestName, breakLine, "l", 1, GetListChildren(
                new ChildInfo("[0]", null, "1"),
                new ChildInfo("[1]", null, "2")));
            ChildTest("PrevFrame" + EnumChildrenTestName, breakLine, "d1", 1, GetDictChildren(
                new ChildInfo("[42]", null, "100", "0x64")));
            string itemsName = Version.Version == PythonLanguageVersion.V27 ? "viewitems" : "items";
            ChildTest("PrevFrame" + EnumChildrenTestName, breakLine, "d2", 1, GetDictChildren(
                new ChildInfo("['abc']", null, "'fob'")));
            ChildTest("PrevFrame" + EnumChildrenTestName, breakLine, "d3", 1, GetDictChildren(
                new ChildInfo("[" + InfRepr + "]", "next((v for i, (k, v) in enumerate(d3." + itemsName + "()) if i == 0))", "{42: 100}")));
            ChildTest("PrevFrame" + EnumChildrenTestName, breakLine, "i", 1, null);
            ChildTest("PrevFrame" + EnumChildrenTestName, breakLine, "u1", 1, null);
        }

        [TestMethod, Priority(1)]
        public void GeneratorChildrenTest() {
            var children = new List<ChildInfo> {
                new ChildInfo("gi_code", null),
                new ChildInfo("gi_frame", null),
                new ChildInfo("gi_running", null),
                new ChildInfo("gi_yieldfrom", null),
                new ChildInfo("Results View", "tuple(a)", "Expanding the Results View will run the iterator")
            };

            if (Version.Version < PythonLanguageVersion.V26) {
                children.RemoveAll(c => c.Expression == "gi_code");
            }
            if (Version.Version < PythonLanguageVersion.V35) {
                children.RemoveAll(c => c.Expression == "gi_yieldfrom");
            }

            ChildTest("GeneratorTest.py", 6, "a", 0, children.ToArray());
        }

        public virtual string EnumChildrenTestName {
            get {
                return "EnumChildTest.py";
            }
        }

        private void ChildTest(string filename, int lineNo, string text, params ChildInfo[] children) {
            ChildTest(filename, lineNo, text, 0, children);
        }

        private void ChildTest(string filename, int lineNo, string text, int frame, params ChildInfo[] children) {
            var debugger = new PythonDebugger();
            PythonThread thread = null;
            var process = DebugProcess(debugger, DebuggerTestPath + filename, (newproc, newthread) => {
                var breakPoint = newproc.AddBreakPoint(filename, lineNo);
                breakPoint.Add();
                thread = newthread;
            });

            AutoResetEvent brkHit = new AutoResetEvent(false);
            process.BreakpointHit += (sender, args) => {
                brkHit.Set();
            };

            try {
                process.Start();

                AssertWaited(brkHit);

                var frames = thread.Frames;

                AutoResetEvent evalComplete = new AutoResetEvent(false);
                PythonEvaluationResult evalRes = null;
                Console.WriteLine("Executing {0}", text);
                frames[frame].ExecuteText(text, (completion) => {
                    evalRes = completion;
                    evalComplete.Set();
                });

                AssertWaited(evalComplete);
                Assert.IsNotNull(evalRes, "didn't get evaluation result");

                if (children == null) {
                    Assert.IsFalse(evalRes.IsExpandable, "result should not be expandable");
                    Assert.IsNull(evalRes.GetChildren(Int32.MaxValue), "result should not have children");
                } else {
                    Assert.IsNull(evalRes.ExceptionText, "exception while evaluating: " + evalRes.ExceptionText);
                    Assert.IsTrue(evalRes.IsExpandable, "result is not expandable");
                    var childrenReceived = new List<PythonEvaluationResult>(evalRes.GetChildren(Int32.MaxValue));

                    Console.WriteLine("{0} children received:", childrenReceived.Count);
                    foreach (var childReceived in childrenReceived) {
                        Console.WriteLine("\t{0}\t{1}\t{2}\t{3}", childReceived.ChildName, childReceived.Expression, childReceived.StringRepr, childReceived.HexRepr);
                    }

                    Assert.AreEqual(children.Length, childrenReceived.Count, "received incorrect number of children");

                    for (int i = 0; i < children.Length; i++) {
                        var curChild = children[i];
                        Console.WriteLine("Finding: <{0}> (Repr: <{1}>)", curChild.ChildText, curChild.Repr ?? "(null)");

                        bool foundChild = false;
                        for (int j = 0; j < childrenReceived.Count; j++) {
                            var curReceived = childrenReceived[j];
                            Console.WriteLine("Candidate: <{0}> (Repr: <{1}>)", curReceived.ChildName, curReceived.StringRepr ?? "(null)");
                            if (ChildrenMatch(curChild, curReceived)) {
                                foundChild = true;

                                string expr = curChild.Expression;
                                if (expr == null) {
                                    if (curChild.ChildText.StartsWith("[")) {
                                        expr = text + curChild.ChildText;
                                    } else {
                                        expr = text + "." + curChild.ChildText;
                                    }
                                }

                                Assert.AreEqual(expr, curReceived.Expression);
                                Assert.AreEqual(frames[frame], curReceived.Frame);
                                childrenReceived.RemoveAt(j);
                                break;
                            }
                        }

                        Assert.IsTrue(foundChild, "failed to find " + curChild.ChildText + " found " + String.Join(", ", childrenReceived.Select(x => x.ChildName)));
                    }

                    Assert.AreEqual(0, childrenReceived.Count, "there's still some children left over which we didn't find");
                }
            } finally {
                process.Continue();
                WaitForExit(process);
            }
        }

        private bool ChildrenMatch(ChildInfo curChild, PythonEvaluationResult curReceived) {
            return curReceived.ChildName == curChild.ChildText &&
                (curReceived.StringRepr == curChild.Repr || curChild.Repr == null) &&
                (Version.Version.Is3x() || (curChild.HexRepr == null || curChild.HexRepr == curReceived.HexRepr));// __hex__ no longer used in 3.x, http://mail.python.org/pipermail/python-list/2009-September/1218287.html
        }

        class ChildInfo {
            public readonly string ChildText;
            public readonly string Expression; // if null, compute automatically from parent expression and ChildText
            public readonly string Repr;
            public readonly string HexRepr;

            public ChildInfo(string childText, string expression, string value = null, string hexRepr = null) {
                ChildText = childText;
                Expression = expression;
                Repr = value;
                HexRepr = hexRepr;
            }
        }

        #endregion

        #region Set Next Line Tests

        [TestMethod, Priority(1)]
        public void SetNextLineTest() {
            if (GetType() == typeof(DebuggerTestsIpy)) {
                //http://ironpython.codeplex.com/workitem/30129
                return;
            }

            var debugger = new PythonDebugger();
            PythonThread thread = null;
            AutoResetEvent processLoaded = new AutoResetEvent(false);
            var process =
                DebugProcess(
                    debugger,
                    Path.Combine(DebuggerTestPath, "SetNextLine.py"),
                    resumeOnProcessLoaded: false,
                    onLoaded: (newproc, newthread) => {
                        thread = newthread;
                        processLoaded.Set();
                    }
                );

            AutoResetEvent brkHit = new AutoResetEvent(false);
            AutoResetEvent stepDone = new AutoResetEvent(false);
            process.BreakpointHit += (sender, args) => {
                brkHit.Set();
            };
            process.StepComplete += (sender, args) => {
                stepDone.Set();
            };

            try {
                process.Start();

                AssertWaited(processLoaded);

                var moduleFrame = thread.Frames[0];
                Assert.AreEqual(1, moduleFrame.StartLine);
                if (GetType() != typeof(DebuggerTestsIpy)) {
                    Assert.AreEqual(13, moduleFrame.EndLine);
                }

                // skip over def f()
                Assert.IsTrue(moduleFrame.SetLineNumber(6), "failed to set line number to 6");

                // set break point in g, run until we hit it.
                var newBp = process.AddBreakPoint("SetNextLine.py", 7);
                newBp.Add();

                process.Resume();
                AssertWaited(brkHit);

                thread.StepOver(); // step over x = 42
                AssertWaited(stepDone);

                // skip y = 100
                Assert.IsTrue(moduleFrame.SetLineNumber(9), "failed to set line number to 9");

                thread.StepOver(); // step over z = 200
                AssertWaited(stepDone);

                // z shouldn't be defined
                var frames = thread.Frames;
                new HashSet<string>(new[] { "x", "z" }).ContainsExactly(frames[0].Locals.Select(x => x.Expression));

                // set break point in module, run until we hit it.
                newBp = process.AddBreakPoint("SetNextLine.py", 13);
                newBp.Add();
                thread.Resume();
                AssertWaited(brkHit);

                // f shouldn't be defined.
                frames = thread.Frames;
                new HashSet<string>(new[] { "sys", "g" }).ContainsExactly(frames[0].Locals.Select(x => x.Expression));

                process.Continue();
            } finally {
                WaitForExit(process);
            }
        }

        #endregion

        #region BreakAll Tests


        [TestMethod, Priority(1)]
        public void TestBreakAll() {
            var debugger = new PythonDebugger();

            PythonThread thread = null;
            AutoResetEvent loaded = new AutoResetEvent(false);
            var process = DebugProcess(debugger, DebuggerTestPath + "BreakAllTest.py", (newproc, newthread) => {
                loaded.Set();
                thread = newthread;
            });

            try {
                process.Start();
                AssertWaited(loaded);

                // let loop run
                Thread.Sleep(500);
                AutoResetEvent breakComplete = new AutoResetEvent(false);
                PythonThread breakThread = null;
                process.AsyncBreakComplete += (sender, args) => {
                    breakThread = args.Thread;
                    breakComplete.Set();
                };

                process.Break();
                AssertWaited(breakComplete);

                Assert.AreEqual(thread, breakThread);

                process.Resume();
            } finally {
                TerminateProcess(process);
            }
        }

        [TestMethod, Priority(1)]
        public void TestBreakAllThreads() {
            var debugger = new PythonDebugger();

            PythonThread thread = null;
            AutoResetEvent loaded = new AutoResetEvent(false);
            var process = DebugProcess(debugger, DebuggerTestPath + "InfiniteThreads.py", (newproc, newthread) => {
                loaded.Set();
                thread = newthread;
            });

            try {
                process.Start();
                AssertWaited(loaded);

                AutoResetEvent breakComplete = new AutoResetEvent(false);
                process.AsyncBreakComplete += (sender, args) => {
                    breakComplete.Set();
                };

                // let loop run
                for (int i = 0; i < 20; i++) {
                    Thread.Sleep(50);

                    Debug.WriteLine(String.Format("Breaking {0}", i));
                    process.Break();
                    if (!breakComplete.WaitOne(10000)) {
                        Console.WriteLine("Failed to break");
                    }
                    process.Resume();
                    Debug.WriteLine(String.Format("Resumed {0}", i));
                }
            } finally {
                TerminateProcess(process);
            }
        }

        #endregion

        #region Eval Tests

        [TestMethod, Priority(1)]
        [TestCategory("10s")]
        public void EvalTest() {
            EvalTest("LocalsTest4.py", 2, "g", 1, EvalResult.Value("baz", "int", "42"));
            EvalTest("LocalsTest3.py", 2, "f", 0, EvalResult.Value("x", "int", "42"));
            EvalTest("LocalsTest3.py", 2, "f", 0, EvalResult.Exception("not_defined", "name 'not_defined' is not defined"));
            EvalTest("LocalsTest3.py", 2, "f", 0, EvalResult.ErrorExpression("/2", "unexpected token '/'\r\ninvalid syntax\r\n"));
        }

        // Test evaluation of objects of a type that does not have a __repr__ attribute (but does have repr()),
        // and which cannot be used with isinstance and issubclass.
        // https://pytools.codeplex.com/workitem/2770
        // https://pytools.codeplex.com/workitem/2772
        [TestMethod, Priority(1)]
        public void EvalPseudoTypeTest() {
            if (this is DebuggerTestsIpy) {
                return;
            }

            EvalTest("EvalPseudoType.py", 22, "<module>", 0, EvalResult.Value("obj", "PseudoType", "pseudo"));
        }

        [TestMethod, Priority(1)]
        [TestCategory("10s")]
        public void EvalRawTest() {
            EvalTest("EvalRawTest.py", 28, "<module>", 0, PythonEvaluationResultReprKind.Raw,
                EvalResult.Value("n", null, null, 0, PythonEvaluationResultFlags.None));

            EvalRawTest(28, "s", "fob");
            EvalRawTest(28, "u", "fob");
            EvalRawTest(28, "ds", "fob");

            if (Version.Version >= PythonLanguageVersion.V26) {
                EvalRawTest(28, "ba", "fob");
            }
        }

        private void EvalRawTest(int line, string expr, string expected) {
            var flags = PythonEvaluationResultFlags.Raw | PythonEvaluationResultFlags.HasRawRepr;

            EvalTest("EvalRawTest.py", line, "<module>", 0, PythonEvaluationResultReprKind.Raw,
                EvalResult.Value(expr, null, expected, expected.Length, flags, allowOtherFlags: true));
            EvalTest("EvalRawTest.py", line, "<module>", 0, PythonEvaluationResultReprKind.RawLen,
                EvalResult.Value(expr, null, null, expected.Length, flags, allowOtherFlags: true));
        }

        private void EvalTest(string filename, int lineNo, string frameName, int frameIndex, EvalResult eval) {
            EvalTest(filename, lineNo, frameName, frameIndex, PythonEvaluationResultReprKind.Normal, eval);
        }

        private void EvalTest(string filename, int lineNo, string frameName, int frameIndex, PythonEvaluationResultReprKind reprKind, EvalResult eval) {
            var debugger = new PythonDebugger();
            PythonThread thread = null;
            var process = DebugProcess(debugger, DebuggerTestPath + filename, (newproc, newthread) => {
                var breakPoint = newproc.AddBreakPoint(filename, lineNo);
                breakPoint.Add();
                thread = newthread;
            });

            AutoResetEvent brkHit = new AutoResetEvent(false);
            process.BreakpointHit += (sender, args) => {
                brkHit.Set();
            };

            try {
                process.Start();
                AssertWaited(brkHit);

                var frames = thread.Frames;

                PythonEvaluationResult obj = null;
                string errorMsg;
                if (eval.IsError) {
                    Assert.IsFalse(frames[frameIndex].TryParseText(eval.Expression, out errorMsg), "should not have been able to parse expression");
                    Assert.AreEqual(eval.ExceptionText, errorMsg);
                } else {
                    Assert.IsTrue(frames[frameIndex].TryParseText(eval.Expression, out errorMsg), "failed to parse expression");
                    Assert.IsNull(errorMsg);

                    Assert.AreEqual(frameName, frames[frameIndex].FunctionName);

                    var timeoutToken = new CancellationTokenSource(10000).Token;
                    obj = frames[frameIndex].ExecuteTextAsync(eval.Expression, reprKind)
                        .ContinueWith(t => t.Result, timeoutToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current)
                        .GetAwaiter().GetResult();

                    eval.Validate(obj);
                }

                process.Continue();
                WaitForExit(process);
                process = null;
            } finally {
                if (process != null) {
                    process.Terminate();
                    WaitForExit(process, assert: false);
                }
            }
        }


        #endregion

        #region Local Tests

        /// <summary>
        /// Verify it takes more than just an items() method for us to treat something like a dictionary.
        /// </summary>
        [TestMethod, Priority(1)]
        public void CloseToDictExpansionBug484() {
            PythonThread thread = RunAndBreak("LocalsTestBug484.py", 7);
            var process = thread.Process;
            try {
                var frames = thread.Frames;

                var obj = frames[0].Locals.First(x => x.Expression == "x");
                var children = obj.GetChildren(2000);
                int extraCount = 0;
                if (this is DebuggerTestsIpy) {
                    extraCount += 2;
                }
                Assert.AreEqual(extraCount + 3, children.Length);
                Assert.AreEqual("2", children[0 + extraCount].StringRepr);
                Assert.AreEqual("3", children[1 + extraCount].StringRepr);
                Assert.AreEqual("4", children[2 + extraCount].StringRepr);

                process.Continue();
            } finally {
                WaitForExit(process);
            }
        }

        protected virtual string UnassignedLocalRepr {
            get { return "<undefined>"; }
        }

        protected virtual string UnassignedLocalType {
            get { return null; }
        }

        [TestMethod, Priority(1)]
        [TestCategory("10s")]
        public void Locals() {
            new LocalsTest(this, "LocalsTest.py", 3) {
                Locals = { { "x", UnassignedLocalType, UnassignedLocalRepr } }
            }.Run();

            new LocalsTest(this, "LocalsTest2.py", 2) {
                Params = { { "x", "int", "42" } }
            }.Run();

            new LocalsTest(this, "LocalsTest3.py", 2) {
                Params = { { "x", "int", "42" } },
                Locals = { { "y", UnassignedLocalType, UnassignedLocalRepr } }
            }.Run();
        }

        /// <summary>
        /// https://pytools.codeplex.com/workitem/1347
        /// </summary>
        [TestMethod, Priority(1)]
        [TestCategory("10s")]
        public void LocalGlobalsTest() {
            var test = new LocalsTest(this, "LocalGlobalsTest.py", 3);
            test.Run();

            test.LineNo = 4;
            test.Locals.Add("x");
            test.Run();

            test.BreakFileName = "LocalGlobalsTestImported.py";
            test.LineNo = 5;
            test.Locals.Clear();
            test.Params.Add("self");
            test.Run();

            test.LineNo = 6;
            test.Locals.Add("x");
        }

        /// <summary>
        /// https://pytools.codeplex.com/workitem/1348
        /// </summary>
        [TestMethod, Priority(1)]
        public virtual void LocalClosureVarsTest() {
            var test = new LocalsTest(this, "LocalClosureVarsTest.py", 4) {
                Locals = { "x", "y" },
                Params = { "z" }
            };
            test.Run();

            test.BreakFileName = "LocalClosureVarsTestImported.py";
            test.LineNo = 6;
            test.Run();
        }

        /// <summary>
        /// https://pytools.codeplex.com/workitem/1710
        /// </summary>
        [TestMethod, Priority(1)]
        public virtual void LocalBuiltinUsageTest() {
            var test = new LocalsTest(this, "LocalBuiltinUsageTest.py", 4) {
                Params = { "start", "end" },
                Locals = { "i" }
            };
            test.Run();

            test.BreakFileName = "LocalBuiltinUsageTestImported.py";
            test.LineNo = 6;
            test.Params.Add("self");
            test.Run();
        }

        [TestMethod, Priority(1)]
        public void GlobalsTest() {
            var test = new LocalsTest(this, "GlobalsTest.py", 4) {
                Locals = { "x", "y", "__file__", "__name__", "__builtins__", "__doc__" }
            };

            if (Version.Version >= PythonLanguageVersion.V26) {
                test.Locals.Add("__package__");
            }
            if (Version.Version >= PythonLanguageVersion.V32) {
                test.Locals.Add("__cached__");
            }
            if (Version.Version >= PythonLanguageVersion.V33) {
                test.Locals.Add("__loader__");
            }
            if (Version.Version >= PythonLanguageVersion.V34) {
                test.Locals.Add("__spec__");
            }

            test.Run();
        }

        // https://pytools.codeplex.com/workitem/1334
        [TestMethod, Priority(1)]
        public void LocalBooleanTest() {
            var test = new LocalsTest(this, "LocalBooleanTest.py", 2) {
                Params = {
                    { "x", "bool", "True" },
                    { "y", "bool", "False" }
                }
            };

            foreach (var p in test.Params) {
                p.ValidateHexRepr = true;
                p.HexRepr = null;
            }

            test.Run();
        }

        [TestMethod, Priority(1)]
        public void LocalReprRestrictionsTest() {
            // https://pytools.codeplex.com/workitem/931
            var filename = DebuggerTestPath + "LocalReprRestrictionsTest.py";
            var debugger = new PythonDebugger();

            PythonThread thread = null;
            AutoResetEvent loaded = new AutoResetEvent(false);
            var process =
                DebugProcess(
                    debugger,
                    filename,
                    (newproc, newthread) => {
                        var bp = newproc.AddBreakPoint(filename, 22);
                        bp.Add();
                        thread = newthread;
                        loaded.Set();
                    }
                );

            AutoResetEvent breakpointHit = new AutoResetEvent(false);
            process.BreakpointHit += (sender, args) => {
                breakpointHit.Set();
            };

            process.Start();
            try {
                AssertWaited(breakpointHit);

                // Handle order inconsitencies accross interpreters
                var parms = thread.Frames[0].Parameters;
                foreach (var local in thread.Frames[0].Locals) {
                    switch (local.Expression) {
                        case "s":
                            Assert.AreEqual("'01234567890123456789012345678901234567890123456789'", local.StringRepr);
                            break;
                        case "sa1":
                            Assert.AreEqual("['0123456789012345678...123456789']", local.StringRepr);
                            break;
                        case "sa2":
                            Assert.AreEqual("[['0123456789012345678...123456789'], ['0123456789012345678...123456789']]", local.StringRepr);
                            break;
                        case "sa3":
                            Assert.AreEqual("[[[...], [...]], [[...], [...]], [[...], [...]]]", local.StringRepr);
                            break;
                        case "sa4":
                            Assert.AreEqual("[[[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], ...]", local.StringRepr);
                            break;
                        case "n":
                            Assert.AreEqual(
                                Version.Version < PythonLanguageVersion.V30 ?
                                "12345678901234567890123456789012345678901234567890L" :
                                "12345678901234567890123456789012345678901234567890",
                                local.StringRepr);
                            break;
                        case "na1":
                            Assert.AreEqual(
                                Version.Version < PythonLanguageVersion.V30 ?
                                "[12345678901234567890...234567890L]" :
                                "[12345678901234567890...1234567890]",
                                local.StringRepr);
                            break;
                        case "na2":
                            Assert.AreEqual(
                                Version.Version < PythonLanguageVersion.V30 ?
                                "[[12345678901234567890...234567890L], [12345678901234567890...234567890L]]" :
                                "[[12345678901234567890...1234567890], [12345678901234567890...1234567890]]",
                                local.StringRepr);
                            break;
                        case "na3":
                            Assert.AreEqual("[[[...], [...]], [[...], [...]], [[...], [...]]]", local.StringRepr);
                            break;
                        case "na4":
                            Assert.AreEqual("[[[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], ...]", local.StringRepr);
                            break;
                        case "c":
                            Assert.AreEqual("my_class: 0123456789012345678901234567890123456789", local.StringRepr);
                            break;
                        case "ca1":
                            Assert.AreEqual("[my_class: 0123456789...0123456789]", local.StringRepr);
                            break;
                        case "ca2":
                            Assert.AreEqual("[[my_class: 0123456789...0123456789], [my_class: 0123456789...0123456789]]", local.StringRepr);
                            break;
                        case "ca3":
                            Assert.AreEqual("[[[...], [...]], [[...], [...]], [[...], [...]]]", local.StringRepr);
                            break;
                        case "ca4":
                            Assert.AreEqual("[[[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], [[...], [...], [...]], ...]", local.StringRepr);
                            break;
                        case "da1":
                            Assert.AreEqual("{'0123456789012345678...123456789': '0123456789012345678...123456789'}", local.StringRepr);
                            break;
                        case "da2":
                            Assert.AreEqual("{'0123456789012345678...123456789': {'0123456789012345678...123456789': '0123456789012345678...123456789'}, '1': {'0123456789012345678...123456789': '0123456789012345678...123456789'}}", local.StringRepr);
                            break;
                        case "da3":
                            Assert.AreEqual("{'0123456789012345678...123456789': {'0123456789012345678...123456789': {...}, '1': {...}}, '1': {'0123456789012345678...123456789': {...}, '1': {...}}, '2': {'0123456789012345678...123456789': {...}, '1': {...}}}", local.StringRepr);
                            break;
                        case "da4":
                            Assert.AreEqual("{'01': {'0123456789012345678...123456789': {...}, '1': {...}, '2': {...}}, '0123456789012345678...123456789': {'0123456789012345678...123456789': {...}, '1': {...}, '2': {...}}, '02': {'0123456789012345678...123456789': {...}, '1': {...}, '2': {...}}, '03': {'0123456789012345678...123456789': {...}, '1': {...}, '2': {...}}, '04': {'0123456789012345678...123456789': {...}, '1': {...}, '2': {...}}, '05': {'0123456789012345678...123456789': {...}, '1': {...}, '2': {...}}, '06': {'0123456789012345678...123456789': {...}, '1': {...}, '2': {...}}, '07': {'0123456789012345678...123456789': {...}, '1': {...}, '2': {...}}, '08': {'0123456789012345678...123456789': {...}, '1': {...}, '2': {...}}, '09': {'0123456789012345678...123456789': {...}, '1': {...}, '2': {...}}, '10': {'0123456789012345678...123456789': {...}, '1': {...}, '2': {...}}, '11': {'0123456789012345678...123456789': {...}, '1': {...}, '2': {...}}, '12': {'0123456789012345678...123456789': {...}, '1': {...}, '2': {...}}, '13': {'0123456789012345678...123456789': {...}, '1': {...}, '2': {...}}, ...}", local.StringRepr);
                            break;
                        default:
                            break;
                    }
                }
            } finally {
                TerminateProcess(process);
            }
        }

        #endregion

        #region Stepping Tests

        [TestMethod, Priority(1)]
        [TestCategory("10s"), TestCategory("60s")]
        public void StepTest() {
            // Bug 1315: https://pytools.codeplex.com/workitem/1315
            StepTest(Path.Combine(DebuggerTestPath, "StepIntoThroughStdLib.py"),
                    new ExpectedStep(StepKind.Over, 1),     // step over import os
                    new ExpectedStep(StepKind.Over, 2),     // step over code =  ...
                    new ExpectedStep(StepKind.Over, 3),     // step over d = {}
                    new ExpectedStep(StepKind.Over, 4),     // step over exec(code, d, d)
                    new ExpectedStep(StepKind.Over, 6),     // step over def myfunc():
                    new ExpectedStep(StepKind.Into, 9),     // step d['f'](myfunc)
                    new ExpectedStep(StepKind.Over, 7),     // step over print('abc')
                    new ExpectedStep(StepKind.Resume, 10)     // wait for exit
                );
            // 1315 resurrected:
            StepTest(Path.Combine(DebuggerTestPath, "SteppingTestBug1315.py"),
                    new[] { 9 },
                    null,
                    new ExpectedStep(StepKind.Resume, 1),   // continue from import thread
                    new ExpectedStep(StepKind.Over, 9),
                    new ExpectedStep(StepKind.Resume, 10)
                );

            // Bug 507: http://pytools.codeplex.com/workitem/507
            StepTest(Path.Combine(DebuggerTestPath, "SteppingTestBug507.py"),
                    new ExpectedStep(StepKind.Over, 1),     // step over def add_two_numbers(x, y):
                    new ExpectedStep(StepKind.Over, 4),     // step over class Z(object):
                    new ExpectedStep(StepKind.Over, 9),     // step over p = Z()
                    new ExpectedStep(StepKind.Into, 10),     // step into print add_two_numbers(p.fob, 3)
                    new ExpectedStep(StepKind.Out, 7),     // step out return 7
                    new ExpectedStep(StepKind.Into, 10),     // step into add_two_numbers(p.fob, 3)
                    new ExpectedStep(StepKind.Resume, 2)     // wait for exit after return x + y
                );

            // Bug 508: http://pytools.codeplex.com/workitem/508
            StepTest(Path.Combine(DebuggerTestPath, "SteppingTestBug508.py"),
                    new ExpectedStep(StepKind.Into, 1),     // step print (should step over)
                    new ExpectedStep(StepKind.Resume, 2)     // step print (should step over)
                );

            // Bug 509: http://pytools.codeplex.com/workitem/509
            StepTest(Path.Combine(DebuggerTestPath, "SteppingTestBug509.py"),
                    new ExpectedStep(StepKind.Over, 1),     // step over def triangular_number
                    new ExpectedStep(StepKind.Into, 3),     // step into triangular_number
                    new ExpectedStep(StepKind.Into, 1),     // step over triangular_number
                    new ExpectedStep(StepKind.Into, 1),     // step into triangular_number
                    new ExpectedStep(StepKind.Into, 1),     // step into triangular_number
                    new ExpectedStep(StepKind.Resume, 1)    // let program exit
                );

            // Bug 503: http://pytools.codeplex.com/workitem/503
            StepTest(Path.Combine(DebuggerTestPath, "SteppingTestBug503.py"),
                new[] { 6, 12 },
                new Action<PythonProcess>[] {
                    (x) => {},
                    (x) => {},
                },
                new ExpectedStep(StepKind.Resume, 1),     // continue from def x1(y):
                new ExpectedStep(StepKind.Out, 6),     // step out after hitting breakpoint at return y
                new ExpectedStep(StepKind.Out, 3),     // step out z += 1
                new ExpectedStep(StepKind.Out, 3),     // step out z += 1
                new ExpectedStep(StepKind.Out, 3),     // step out z += 1
                new ExpectedStep(StepKind.Out, 3),     // step out z += 1
                new ExpectedStep(StepKind.Out, 3),     // step out z += 1

                new ExpectedStep(StepKind.Out, 14),     // step out after stepping out to x2(5)
                new ExpectedStep(StepKind.Out, 12),     // step out after hitting breakpoint at return y
                new ExpectedStep(StepKind.Out, 10),     // step out return z + 3
                new ExpectedStep(StepKind.Out, 10),     // step out return z + 3
                new ExpectedStep(StepKind.Out, 10),     // step out return z + 3
                new ExpectedStep(StepKind.Out, 10),     // step out return z + 3
                new ExpectedStep(StepKind.Out, 10),     // step out return z + 3

                new ExpectedStep(StepKind.Resume, 15)     // let the program exit
            );

            if (Version.Version < PythonLanguageVersion.V30) {  // step into print on 3.x runs more Python code
                StepTest(Path.Combine(DebuggerTestPath, "SteppingTest7.py"),
                    new ExpectedStep(StepKind.Over, 1),     // step over def f():
                    new ExpectedStep(StepKind.Over, 6),     // step over def g():
                    new ExpectedStep(StepKind.Over, 10),     // step over def h()
                    new ExpectedStep(StepKind.Into, 13),     // step into f() call
                    new ExpectedStep(StepKind.Into, 2),     // step into print 'abc'
                    new ExpectedStep(StepKind.Into, 3),     // step into print 'def'
                    new ExpectedStep(StepKind.Into, 4),     // step into print 'baz'
                    new ExpectedStep(StepKind.Into, 13),     // step into g()
                    new ExpectedStep(StepKind.Into, 14),     // step into g()
                    new ExpectedStep(StepKind.Into, 7),     // step into dict assign
                    new ExpectedStep(StepKind.Into, 8),     // step into print 'hello'
                    new ExpectedStep(StepKind.Into, 14),     // step into h()
                    new ExpectedStep(StepKind.Into, 15),     // step into h()
                    new ExpectedStep(StepKind.Into, 11),    // step into h() return
                    new ExpectedStep(StepKind.Resume, 15)    // step into h() return
                );
            }

            StepTest(Path.Combine(DebuggerTestPath, "SteppingTest6.py"),
                new ExpectedStep(StepKind.Over, 1),     // step over print 'hello world'
                new ExpectedStep(StepKind.Over, 2),     // step over a = set([i for i in range(256)])
                new ExpectedStep(StepKind.Over, 3),     // step over print a
                new ExpectedStep(StepKind.Resume, 4)    // let the program exit
            );

            StepTest(Path.Combine(DebuggerTestPath, "SteppingTest5.py"),
                new ExpectedStep(StepKind.Over, 1),     // step over def g():...
                new ExpectedStep(StepKind.Over, 4),     // step over def f():...
                new ExpectedStep(StepKind.Into, 8),     // step into f()
                new ExpectedStep(StepKind.Out, 5),      // step out of f() on line "g()"
                new ExpectedStep(StepKind.Resume, 8)    // let the program exit
            );

            StepTest(Path.Combine(DebuggerTestPath, "SteppingTest4.py"),
                new ExpectedStep(StepKind.Over, 1),     // step over def f():...
                new ExpectedStep(StepKind.Into, 5),     // step into f()
                new ExpectedStep(StepKind.Over, 2),     // step over for i in xrange(10):
                new ExpectedStep(StepKind.Over, 3),     // step over for print i
                new ExpectedStep(StepKind.Over, 2),     // step over for i in xrange(10):
                new ExpectedStep(StepKind.Over, 3),     // step over for print i
                new ExpectedStep(StepKind.Over, 2),     // step over for i in xrange(10):
                new ExpectedStep(StepKind.Over, 3),     // step over for print i
                new ExpectedStep(StepKind.Over, 2),     // step over for i in xrange(10):
                new ExpectedStep(StepKind.Resume, 5)    // let the program exit
            );

            StepTest(Path.Combine(DebuggerTestPath, "SteppingTest3.py"),
                new ExpectedStep(StepKind.Over, 1),     // step over def f():...
                new ExpectedStep(StepKind.Over, 5),     // step over f()
                new ExpectedStep(StepKind.Resume, 6)    // let the program exit
            );

            StepTest(Path.Combine(DebuggerTestPath, "SteppingTest3.py"),
                new ExpectedStep(StepKind.Over, 1),     // step over def f():...
                new ExpectedStep(StepKind.Into, 5),     // step into f()
                new ExpectedStep(StepKind.Out, 2),     // step out of f()
                new ExpectedStep(StepKind.Resume, 5)    // let the program exit
            );

            StepTest(Path.Combine(DebuggerTestPath, "SteppingTest2.py"),
                new ExpectedStep(StepKind.Over, 1),     // step over def f():...
                new ExpectedStep(StepKind.Into, 4),     // step into f()
                new ExpectedStep(StepKind.Over, 2),     // step over print 'hi'
                new ExpectedStep(StepKind.Resume, 4)    // let the program exit
            );

            StepTest(Path.Combine(DebuggerTestPath, "SteppingTest2.py"),
                new ExpectedStep(StepKind.Over, 1),     // step over def f():...
                new ExpectedStep(StepKind.Into, 4),     // step into f()
                new ExpectedStep(StepKind.Over, 2),     // step over print 'hi'
                new ExpectedStep(StepKind.Resume, 4)      // let the program exit
            );

            StepTest(Path.Combine(DebuggerTestPath, "SteppingTest.py"),
                new ExpectedStep(StepKind.Over, 1),     // step over print "hello"
                new ExpectedStep(StepKind.Over, 2),     // step over print "goodbye"
                new ExpectedStep(StepKind.Resume, 3)   // let the program exit
            );

        }

        [TestMethod, Priority(1)]
        [TestCategory("10s")]
        public void StepStdLib() {
            // http://pytools.codeplex.com/workitem/504 - test option for stepping into std lib.
            var debugger = new PythonDebugger();

            string fullPath = Path.Combine(DebuggerTestPath, "StepStdLib.py");
            foreach (var steppingStdLib in new[] { false, true }) {
                var process = debugger.CreateProcess(
                    Version.Version,
                    Version.InterpreterPath,
                    "\"" + fullPath + "\"",
                    DebuggerTestPath,
                    "",
                    debugOptions: steppingStdLib ? (PythonDebugOptions.DebugStdLib | PythonDebugOptions.RedirectOutput) : PythonDebugOptions.RedirectOutput);

                PythonThread thread = null;
                process.ThreadCreated += (sender, args) => {
                    thread = args.Thread;
                };

                AutoResetEvent processEvent = new AutoResetEvent(false);

                bool processLoad = false, stepComplete = false;
                PythonBreakpoint bp = null;
                process.ProcessLoaded += (sender, args) => {
                    bp = process.AddBreakPoint(fullPath, 2);
                    bp.Add();

                    processLoad = true;
                    processEvent.Set();
                };

                process.StepComplete += (sender, args) => {
                    stepComplete = true;
                    processEvent.Set();
                };

                bool breakHit = false;
                process.BreakpointHit += (sender, args) => {
                    breakHit = true;
                    bp.Disable();
                    processEvent.Set();
                };

                process.Start();
                try {
                    AssertWaited(processEvent);
                    Assert.IsTrue(processLoad, "process did not load");
                    Assert.IsFalse(stepComplete, "step should not have completed");
                    process.Resume();

                    AssertWaited(processEvent);
                    Assert.IsTrue(breakHit, "breakpoint was not hit");

                    thread.StepInto();
                    AssertWaited(processEvent);
                    Assert.IsTrue(stepComplete, "step was not completed");

                    Debug.WriteLine(thread.Frames[thread.Frames.Count - 1].FileName);

                    if (steppingStdLib) {
                        Assert.IsTrue(thread.Frames[0].FileName.EndsWith("\\os.py"), "did not break in os.py; instead, " + thread.Frames[0].FileName);
                    } else {
                        Assert.IsTrue(thread.Frames[0].FileName.EndsWith("\\StepStdLib.py"), "did not break in StepStdLib.py; instead, " + thread.Frames[0].FileName);
                    }

                    process.Resume();
                } finally {
                    WaitForExit(process);
                }
            }
        }

        [TestMethod, Priority(1)]
        public void StepToEntryPoint() {
            // https://pytools.codeplex.com/workitem/1344
            var debugger = new PythonDebugger();

            PythonThread thread = null;
            AutoResetEvent loaded = new AutoResetEvent(false);
            var process = DebugProcess(debugger, DebuggerTestPath + "SteppingTest.py", (newproc, newthread) => {
                thread = newthread;
                loaded.Set();
            });

            process.Start();
            try {
                AssertWaited(loaded);
                Assert.IsTrue(thread.Frames[0].FileName.EndsWith("SteppingTest.py"), "did not break in SteppingTest.py; instead, " + thread.Frames[0].FileName);
                Assert.AreEqual(1, thread.Frames[0].StartLine);
            } finally {
                TerminateProcess(process);
            }
        }

        #endregion

        #region Breakpoint Tests

        /// <summary>
        /// Sets 2 breakpoints on one line after another, hits the 1st one, then steps onto
        /// the next one.  Makes sure we only break in once.
        /// </summary>
        [TestMethod, Priority(1)]
        public void BreakStepStep() {
            // http://pytools.codeplex.com/workitem/815

            var debugger = new PythonDebugger();
            string fn = Path.Combine(DebuggerTestPath, "StepBreakBreak.py");
            var process = DebugProcess(debugger, fn, (newproc, newthread) => {
                PythonBreakpoint breakPoint = newproc.AddBreakPointByFileExtension(2, fn);
                breakPoint.Add();

                breakPoint = newproc.AddBreakPointByFileExtension(3, fn);
                breakPoint.Add();
            }, cwd: DebuggerTestPath);

            int hitBp = 0;
            process.BreakpointHit += (sender, args) => {
                ++hitBp;
                args.Thread.StepOver();
            };
            bool sentStep = false;
            process.StepComplete += (sender, args) => {
                if (sentStep) {
                    process.Continue();
                } else {
                    args.Thread.StepOver();
                    sentStep = true;
                }
            };
            StartAndWaitForExit(process);
        }

        [TestMethod, Priority(1)]
        public void BreakpointNonMainFileRemoved() {
            // http://pytools.codeplex.com/workitem/638

            new BreakpointTest(this, Path.Combine(DebuggerTestPath, "BreakpointNonMainFileRemoved.py")) {
                WorkingDirectory = DebuggerTestPath,
                BreakFileName = Path.Combine(DebuggerTestPath, "BreakpointNonMainFileRemovedImported.py"),
                Breakpoints = {
                    new Breakpoint(2) { RemoveWhenHit = true }
                },
                ExpectedHits = { 0 },
                IsBindFailureExpected = true
            }.Run();
        }


        [TestMethod, Priority(1)]
        public void BreakpointNonMainThreadMainThreadExited() {
            // http://pytools.codeplex.com/workitem/638

            new BreakpointTest(this, Path.Combine(DebuggerTestPath, "BreakpointMainThreadExited.py")) {
                WorkingDirectory = DebuggerTestPath,
                Breakpoints = { 8 },
                ExpectedHits = { 0, 0, 0, 0, 0 },
                ExpectHitOnMainThread = false
            }.Run();
        }

        [TestMethod, Priority(1)]
        public void TestBreakpointsCollidingFilenames() {
            // http://pytools.codeplex.com/workitem/565

            new BreakpointTest(this, Path.Combine(DebuggerTestPath, "BreakpointFilenames.py")) {
                WorkingDirectory = DebuggerTestPath,
                BreakFileName = Path.Combine(DebuggerTestPath, "B", "module1.py"),
                Breakpoints = { 4 },
                ExpectedHits = { },
                IsBindFailureExpected = true
            }.Run();

        }

        [TestMethod, Priority(1)]
        public void TestBreakpointsRelativePathTopLevel() {
            // http://pytools.codeplex.com/workitem/522

            new BreakpointTest(this, Path.Combine(DebuggerTestPath, "SimpleFilenameBreakpoint.py")) {
                WorkingDirectory = DebuggerTestPath,
                BreakFileName = Path.Combine(DebuggerTestPath, "CompiledCodeFile.py"),
                Breakpoints = { 4, 10 },
                ExpectedHits = { 0, 1 },
                IsBindFailureExpected = true
            }.Run();
        }

        [TestMethod, Priority(1)]
        public void TestBreakpointsRelativePathInPackage() {
            // http://pytools.codeplex.com/workitem/522

            new BreakpointTest(this, Path.Combine(DebuggerTestPath, "BreakpointRelativePathInPackage.py")) {
                WorkingDirectory = DebuggerTestPath,
                BreakFileName = Path.Combine(DebuggerTestPath, "A", "relpath.py"),
                Breakpoints = { 6 },
                ExpectedHits = { 0, 0 },
                InterpreterOptions = Version.IsIronPython ? "-X:Frames" : "",
                IsBindFailureExpected = true
            }.Run();
        }

        [TestMethod, Priority(1)]
        public void TestBreakpointHitOtherThreadStackTrace() {
            // http://pytools.codeplex.com/workitem/483

            var debugger = new PythonDebugger();
            string filename = Path.Combine(DebuggerTestPath, "ThreadJoin.py");
            PythonThread thread = null;
            var process = DebugProcess(debugger, filename, (newproc, newthread) => {
                thread = newthread;
                var bp = newproc.AddBreakPoint(filename, 5);
                bp.Add();
            },
                debugOptions: PythonDebugOptions.WaitOnAbnormalExit | PythonDebugOptions.WaitOnNormalExit
            );

            AutoResetEvent bpHit = new AutoResetEvent(false);

            ExceptionDispatchInfo exc = null;

            process.BreakpointHit += (sender, args) => {
                try {
                    Assert.AreNotEqual(args.Thread, thread, "breakpoint shouldn't be on main thread");

                    foreach (var frame in thread.Frames) {
                        Console.WriteLine(frame.FileName);
                        Console.WriteLine(frame.LineNo);
                    }
                    Assert.IsTrue(thread.Frames.Count > 1, "expected more than one frame");
                    process.Continue();
                } catch (Exception ex) {
                    exc = ExceptionDispatchInfo.Capture(ex);
                } finally {
                    bpHit.Set();
                }
            };

            process.Start();

            try {
                if (!bpHit.WaitOne(10000)) {
                    Assert.Fail("Failed to hit breakpoint");
                }
                if (exc != null) {
                    exc.Throw();
                }
            } finally {
                TerminateProcess(process);
            }
        }

        [TestMethod, Priority(1)]
        public void TestBreakpoints() {
            new BreakpointTest(this, "BreakpointTest.py") {
                Breakpoints = { 1 },
                ExpectedHits = { 0 }
            }.Run();
        }

        [TestMethod, Priority(1)]
        public void TestBreakpoints2() {
            new BreakpointTest(this, "BreakpointTest2.py") {
                Breakpoints = { 3 },
                ExpectedHits = { 0, 0, 0 }
            }.Run();
        }

        [TestMethod, Priority(1)]
        public void TestBreakpoints3() {
            new BreakpointTest(this, "BreakpointTest3.py") {
                Breakpoints = { 1 },
                ExpectedHits = { 0 }
            }.Run();
        }

        [TestMethod, Priority(1)]
        public void TestBreakpointsConditionalWhenTrue() {
            new BreakpointTest(this, "BreakpointTest2.py") {
                Breakpoints = {
                    new Breakpoint(3) {
                        ConditionKind = PythonBreakpointConditionKind.WhenTrue,
                        Condition = "i == 1",
                        OnHit = args => {
                            Assert.AreEqual("1", args.Thread.Frames[0].Locals.Single(er => er.ChildName == "i").StringRepr);
                        }
                    }
                },
                ExpectedHits = { 0 },
            }.Run();
        }

        [TestMethod, Priority(1)]
        public void TestBreakpointsConditionalWhenChanged() {
            var expectedReprs = new Queue<string>(new[] { "0", "2", "4", "6", "8" });

            new BreakpointTest(this, "BreakpointTest5.py") {
                Breakpoints = {
                    new Breakpoint(4) {
                        ConditionKind = PythonBreakpointConditionKind.WhenChanged,
                        Condition = "j",
                        OnHit = args => {
                            Assert.AreEqual(expectedReprs.Dequeue(), args.Thread.Frames[0].Locals.Single(er => er.ChildName == "i").StringRepr);
                        }
                    }
                },
                ExpectedHits = { 0, 0, 0, 0, 0 }
            }.Run();

            Assert.AreEqual(0, expectedReprs.Count);
        }

        [TestMethod, Priority(1)]
        public void TestBreakpointsPassCountEvery() {
            var expectedReprs = new Queue<string>(new[] { "2", "5", "8" });
            var expectedHitCounts = new Queue<int>(new[] { 3, 6, 9 });

            new BreakpointTest(this, "BreakpointTest5.py") {
                Breakpoints = {
                    new Breakpoint(3) {
                        PassCountKind = PythonBreakpointPassCountKind.Every,
                        PassCount = 3,
                        OnHit = args => {
                            Assert.AreEqual(expectedReprs.Dequeue(), args.Thread.Frames[0].Locals.Single(er => er.ChildName == "i").StringRepr);
                            args.Breakpoint.GetHitCountAsync().ContinueWith(t => Assert.AreEqual(expectedHitCounts.Dequeue(), t.Result));
                        }
                    }
                },
                ExpectedHits = { 0, 0, 0 },
            }.Run();

            Assert.AreEqual(0, expectedReprs.Count);
            Assert.AreEqual(0, expectedHitCounts.Count);
        }

        [TestMethod, Priority(1)]
        public void TestBreakpointsPassCountWhenEqual() {
            new BreakpointTest(this, "BreakpointTest5.py") {
                Breakpoints = {
                    new Breakpoint(3) {
                        PassCountKind = PythonBreakpointPassCountKind.WhenEqual,
                        PassCount = 5,
                        OnHit = args => {
                            Assert.AreEqual("4", args.Thread.Frames[0].Locals.Single(er => er.ChildName == "i").StringRepr);
                            args.Breakpoint.GetHitCountAsync().ContinueWith(t => Assert.AreEqual(5, t.Result));
                        }
                    }
                },
                ExpectedHits = { 0 },
            }.Run();
        }

        [TestMethod, Priority(1)]
        public void TestBreakpointsPassCountWhenEqualOrGreater() {
            var expectedReprs = new Queue<string>(new[] { "7", "8", "9" });
            var expectedHitCounts = new Queue<int>(new[] { 8, 9, 10 });

            new BreakpointTest(this, "BreakpointTest5.py") {
                Breakpoints = {
                    new Breakpoint(3) {
                        PassCountKind = PythonBreakpointPassCountKind.WhenEqualOrGreater,
                        PassCount = 8,
                        OnHit = args => {
                            Assert.AreEqual(expectedReprs.Dequeue(), args.Thread.Frames[0].Locals.Single(er => er.ChildName == "i").StringRepr);
                            args.Breakpoint.GetHitCountAsync().ContinueWith(t => Assert.AreEqual(expectedHitCounts.Dequeue(), t.Result));
                        }
                    }
                },
                ExpectedHits = { 0, 0, 0 },
            }.Run();

            Assert.AreEqual(0, expectedReprs.Count);
            Assert.AreEqual(0, expectedHitCounts.Count);
        }

        [TestMethod, Priority(1)]
        public void TestBreakpointsPassCountAndCondition() {
            new BreakpointTest(this, "BreakpointTest5.py") {
                Breakpoints = {
                    new Breakpoint(3) {
                        ConditionKind = PythonBreakpointConditionKind.WhenTrue,
                        Condition = "i % 2 == 0",
                        PassCountKind = PythonBreakpointPassCountKind.WhenEqual,
                        PassCount = 3,
                        OnHit = args => {
                            Assert.AreEqual("4", args.Thread.Frames[0].Locals.Single(er => er.ChildName == "i").StringRepr);
                            args.Breakpoint.GetHitCountAsync().ContinueWith(t => Assert.AreEqual(3, t.Result));
                        }
                    }
                },
                ExpectedHits = { 0 },
            }.Run();
        }

        [TestMethod, Priority(1)]
        public void TestBreakpointRemove() {
            new BreakpointTest(this, "BreakpointTest2.py") {
                Breakpoints = {
                    new Breakpoint(3) { RemoveWhenHit = true }
                },
                ExpectedHits = { 0 }
            }.Run();
        }

        [TestMethod, Priority(1)]
        public void TestBreakpointFailed() {
            var debugger = new PythonDebugger();

            PythonThread thread = null;
            PythonBreakpoint breakPoint = null;
            var process = DebugProcess(debugger, DebuggerTestPath + "BreakpointTest.py", (newproc, newthread) => {
                breakPoint = newproc.AddBreakPoint("doesnotexist.py", 1);
                breakPoint.Add();
                thread = newthread;
            });

            bool bindFailed = false;
            process.BreakpointBindFailed += (sender, args) => {
                bindFailed = true;
                Assert.AreEqual(breakPoint, args.Breakpoint);
            };

            StartAndWaitForExit(process);

            Assert.IsTrue(bindFailed, "Should not have bound the breakpoint");
        }

        #endregion

        #region Call Stack Tests

        [TestMethod, Priority(1)]
        public void TestCallStackFunctionNames() {
            var expectedNames = new[] {
                "InnerClass.InnermostClass.innermost_method in nested_function in OuterClass.outer_method",
                "nested_function in OuterClass.outer_method",
                "OuterClass.outer_method",
                "<module>"
            };

            new BreakpointTest(this, "CallStackTest.py") {
                WaitForExit = false,
                Breakpoints = {
                    new Breakpoint(7) {
                        OnHit = args => {
                            var actualNames = args.Thread.Frames.Select(f => f.GetQualifiedFunctionName());
                            AssertUtil.AreEqual(actualNames, expectedNames);
                        }
                    }
                },
                ExpectedHits = { 0 },
            }.Run();
        }

        #endregion

        #region Exception Tests

        class ExceptionInfo {
            public readonly string TypeName;
            public readonly int LineNumber;

            public ExceptionInfo(string typeName, int lineNumber) {
                TypeName = typeName;
                LineNumber = lineNumber;
            }
        }

        class ExceptionHandlerInfo {
            public readonly int FirstLine;
            public readonly int LastLine;
            public readonly HashSet<string> Expressions;

            public ExceptionHandlerInfo(int firstLine, int lastLine, params string[] expressions) {
                FirstLine = firstLine;
                LastLine = lastLine;
                Expressions = new HashSet<string>(expressions);
            }
        }

        public string PickleModule {
            get {
                if (Version.Version.Is3x()) {
                    return "_pickle";
                }
                return "cPickle";
            }
        }

        public virtual string ComplexExceptions {
            get {
                return "ComplexExceptions.py";
            }
        }

        [TestMethod, Priority(1)]
        [TestCategory("10s"), TestCategory("60s")]
        public void TestExceptions() {
            var debugger = new PythonDebugger();
            for (int i = 0; i < 2; i++) {
                TestException(debugger, Path.Combine(DebuggerTestPath, "SimpleException.py"), i == 0, ExceptionMode.Always, null,
                    new ExceptionInfo("Exception", 3)
                );

                TestException(debugger, DebuggerTestPath + ComplexExceptions, i == 0, ExceptionMode.Always, null,
                    new ExceptionInfo(PickleModule + ".PickleError", 6),
                    new ExceptionInfo("StopIteration", 13),
                    new ExceptionInfo("NameError", 15),
                    new ExceptionInfo("StopIteration", 21),
                    new ExceptionInfo("NameError", 23),
                    new ExceptionInfo("Exception", 29),
                    new ExceptionInfo("Exception", 32)
                );

                TestException(debugger, DebuggerTestPath + ComplexExceptions, i == 0, ExceptionMode.Unhandled, new[] {
                    new KeyValuePair<string, ExceptionMode>(PickleModule + ".PickleError", ExceptionMode.Never)
                });
                TestException(debugger, DebuggerTestPath + ComplexExceptions, i == 0, ExceptionMode.Never, new[] {
                    new KeyValuePair<string, ExceptionMode>(PickleModule + ".PickleError", ExceptionMode.Always),
                    new KeyValuePair<string, ExceptionMode>("StopIteration", ExceptionMode.Unhandled),
                    new KeyValuePair<string, ExceptionMode>("NameError", ExceptionMode.Never),
                    new KeyValuePair<string, ExceptionMode>("Exception", ExceptionMode.Always | ExceptionMode.Unhandled),
                },
                    new ExceptionInfo(PickleModule + ".PickleError", 6),
                    new ExceptionInfo("Exception", 29),
                    new ExceptionInfo("Exception", 32)
                );

                TestException(debugger, DebuggerTestPath + "FinallyExceptions.py", i == 0, ExceptionMode.Unhandled, null,
                    new ExceptionInfo("Exception", 3)
                );

                if (Version.Version.Is2x()) {
                    TestException(debugger, Path.Combine(DebuggerTestPath, "UnicodeException.py"), i == 0, ExceptionMode.Always, null,
                        new ExceptionInfo("Exception", 3)
                    );
                }

                // Only the last exception in each file should be noticed.
                if (Version.Version <= PythonLanguageVersion.V25) {
                    TestException(debugger, Path.Combine(DebuggerTestPath, "UnhandledException1_v25.py"), i == 0, ExceptionMode.Unhandled, null,
                        new ExceptionInfo("Exception", 57)
                    );
                } else if (Version.Version.Is3x()) {
                    TestException(debugger, Path.Combine(DebuggerTestPath, "UnhandledException1_v3x.py"), i == 0, ExceptionMode.Unhandled, null,
                        new ExceptionInfo("Exception", 56)
                    );
                } else {
                    TestException(debugger, Path.Combine(DebuggerTestPath, "UnhandledException1.py"), i == 0, ExceptionMode.Unhandled, null,
                        new ExceptionInfo("Exception", 81)
                    );
                }

                TestException(debugger, Path.Combine(DebuggerTestPath, "UnhandledException2.py"), i == 0, ExceptionMode.Unhandled, null,
                    new ExceptionInfo("Exception", 16)
                );
                TestException(debugger, Path.Combine(DebuggerTestPath, "UnhandledException3.py"), i == 0, ExceptionMode.Unhandled, null,
                    new ExceptionInfo("ValueError", 12)
                );
                TestException(debugger, Path.Combine(DebuggerTestPath, "UnhandledException4.py"), i == 0, ExceptionMode.Unhandled, null,
                    // On IronPython, an unhandled exception will be repeatedly reported as raised as it bubbles up the stack.
                    // Everywhere else, it will only be reported once at the point where it is initially raised. 
                    this is DebuggerTestsIpy ?
                    new[] { new ExceptionInfo("OSError", 17), new ExceptionInfo("OSError", 32), new ExceptionInfo("OSError", 55) } :
                    new[] { new ExceptionInfo("OSError", 17) }
                );
                TestException(debugger, Path.Combine(DebuggerTestPath, "UnhandledException5.py"), i == 0, ExceptionMode.Unhandled, null,
                    new ExceptionInfo("ValueError", 4)
                );
                TestException(debugger, Path.Combine(DebuggerTestPath, "UnhandledException6.py"), i == 0, ExceptionMode.Unhandled, null,
                    new ExceptionInfo("OSError", 12)
                );
            }
        }

        [TestMethod, Priority(1)]
        public void TestExceptionInEgg() {
            var debugger = new PythonDebugger();

            TestException(debugger, DebuggerTestPath + "EGGceptionOnImport.py", true, ExceptionMode.Always, null,
                // We only see the unhandled exception in our script
                new ExceptionInfo("ValueError", 7)
            );
            TestException(debugger, DebuggerTestPath + "EGGceptionOnImport.py", true, ExceptionMode.Unhandled, null);

            // We never see this exception because it is fully handled in the egg
            TestException(debugger, DebuggerTestPath + "EGGceptionOnCall.py", true, ExceptionMode.Always, null);
            TestException(debugger, DebuggerTestPath + "EGGceptionOnCall.py", true, ExceptionMode.Unhandled, null);

            TestException(debugger, DebuggerTestPath + "EGGceptionOnCallback.py", true, ExceptionMode.Always, null,
                new ExceptionInfo("ValueError", 7),
                new ExceptionInfo("TypeError", 10),
                new ExceptionInfo("TypeError", 13)
            );

            TestException(debugger, DebuggerTestPath + "EGGceptionOnCallback.py", true, ExceptionMode.Unhandled, null,
                new ExceptionInfo("TypeError", 13)
            );
        }

        [Flags]
        private enum ExceptionMode {
            Never = 0,
            Always = 1,
            Unhandled = 32
        }

        private void TestException(
            PythonDebugger debugger,
            string filename,
            bool resumeProcess,
            ExceptionMode defaultExceptionMode,
            ICollection<KeyValuePair<string, ExceptionMode>> exceptionModes,
            params ExceptionInfo[] exceptions
        ) {
            TestException(debugger, filename, resumeProcess, defaultExceptionMode, exceptionModes, PythonDebugOptions.RedirectOutput, exceptions);
        }

        private static string TryGetStack(PythonThread thread) {
            try {
                return string.Join(
                    Environment.NewLine,
                    thread.Frames.Select(f => {
                        var fn = f.FileName;
                        if (CommonUtils.IsSubpathOf(TestData.GetPath("TestData"), fn)) {
                            fn = CommonUtils.GetRelativeFilePath(TestData.GetPath(), fn);
                        }
                        return string.Format("    {0} in {1}:{2}", f.FunctionName, fn, f.LineNo);
                    })
                );
            } catch (Exception ex) {
                return "Failed to read stack." + Environment.NewLine + ex.ToString();
            }
        }

        private void TestException(
            PythonDebugger debugger,
            string filename,
            bool resumeProcess,
            ExceptionMode defaultExceptionMode,
            ICollection<KeyValuePair<string, ExceptionMode>> exceptionModes,
            PythonDebugOptions debugOptions,
            params ExceptionInfo[] exceptions
        ) {
            Console.WriteLine();
            Console.WriteLine("Testing {0}", filename);

            bool loaded = false;
            var process = DebugProcess(debugger, filename, (processObj, threadObj) => {
                loaded = true;
                processObj.SetExceptionInfo(
                    (int)defaultExceptionMode,
                    exceptionModes == null ?
                        Enumerable.Empty<KeyValuePair<string, int>>() :
                        exceptionModes.Select(m => new KeyValuePair<string, int>(m.Key, (int)m.Value))
                );
            }, debugOptions: debugOptions);

            var raised = new List<Tuple<string, string>>();
            process.ExceptionRaised += (sender, args) => {
                if (loaded) {
                    raised.Add(Tuple.Create(args.Exception.TypeName, TryGetStack(args.Thread)));
                }
                if (resumeProcess) {
                    process.Resume();
                } else {
                    args.Thread.Resume();
                }
            };

            StartAndWaitForExit(process);

            if (Version.Version == PythonLanguageVersion.V30 && raised.Count > exceptions.Length) {
                // Python 3.0 raises an exception as the process shuts down.
                raised.RemoveAt(raised.Count - 1);
            }

            if (GetType() == typeof(DebuggerTestsIpy) && raised.Count == exceptions.Length + 1) {
                // IronPython over-reports exceptions
                raised.RemoveAt(raised.Count - 1);
            }

            foreach (var t in raised) {
                Console.WriteLine("Received {0} at{1}{2}", t.Item1, Environment.NewLine, t.Item2);
            }
            AssertUtil.AreEqual(
                raised.Select(t => t.Item1),
                exceptions.Select(e => e.TypeName).ToArray()
            );
        }

        /// <summary>
        /// Test cases for http://pytools.codeplex.com/workitem/367
        /// </summary>
        [TestMethod, Priority(1)]
        [TestCategory("10s")]
        public void TestExceptionsSysExitZero() {
            var debugger = new PythonDebugger();

            TestException(
                debugger,
                Path.Combine(DebuggerTestPath, "SysExitNone.py"),
                true, ExceptionMode.Unhandled,
                null,
                PythonDebugOptions.BreakOnSystemExitZero,
                new ExceptionInfo("SystemExit", 1)
            );

            TestException(
                debugger,
                Path.Combine(DebuggerTestPath, "SysExitZeroRaise.py"),
                true, ExceptionMode.Unhandled,
                null,
                PythonDebugOptions.BreakOnSystemExitZero,
                new ExceptionInfo("SystemExit", 1)
            );

            TestException(
                debugger,
                Path.Combine(DebuggerTestPath, "SysExitZero.py"),
                true, ExceptionMode.Unhandled,
                null,
                PythonDebugOptions.BreakOnSystemExitZero,
                new ExceptionInfo("SystemExit", 2)
            );

            TestException(
                debugger,
                Path.Combine(DebuggerTestPath, "SysExitNone.py"),
                true, ExceptionMode.Unhandled,
                null,
                PythonDebugOptions.RedirectOutput
            );

            TestException(
                debugger,
                Path.Combine(DebuggerTestPath, "SysExitZeroRaise.py"),
                true, ExceptionMode.Unhandled,
                null,
                PythonDebugOptions.RedirectOutput
            );

            TestException(
                debugger,
                Path.Combine(DebuggerTestPath, "SysExitZero.py"),
                true, ExceptionMode.Unhandled,
                null,
                PythonDebugOptions.RedirectOutput
            );
        }

        [TestMethod, Priority(1)]
        public void TestExceptionHandlers() {
            var debugger = new PythonDebugger();

            TestGetHandledExceptionRanges(debugger, Path.Combine(DebuggerTestPath, "ExceptionHandlers.py"),
                new ExceptionHandlerInfo(1, 3, "*"),
                new ExceptionHandlerInfo(6, 7, "*"),
                new ExceptionHandlerInfo(9, 13, "*"),

                new ExceptionHandlerInfo(18, 19, "ArithmeticError", "AssertionError", "AttributeError", "BaseException", "BufferError", "BytesWarning", "DeprecationWarning", "EOFError", "EnvironmentError", "Exception", "FloatingPointError", "FutureWarning", "GeneratorExit", "IOError", "ImportError", "ImportWarning", "IndentationError", "IndexError", "KeyError", "KeyboardInterrupt", "LookupError", "MemoryError", "NameError", "NotImplementedError", "OSError", "OverflowError", "PendingDeprecationWarning", "ReferenceError", "RuntimeError", "RuntimeWarning", "StandardError", "StopIteration", "SyntaxError", "SyntaxWarning", "SystemError", "SystemExit", "TabError", "TypeError", "UnboundLocalError", "UnicodeDecodeError", "UnicodeEncodeError", "UnicodeError", "UnicodeTranslateError", "UnicodeWarning", "UserWarning", "ValueError", "Warning", "WindowsError", "ZeroDivisionError"),
                new ExceptionHandlerInfo(69, 70, "ArithmeticError", "AssertionError", "AttributeError", "BaseException", "BufferError", "BytesWarning", "DeprecationWarning", "EOFError", "EnvironmentError", "Exception", "FloatingPointError", "FutureWarning", "GeneratorExit", "IOError", "ImportError", "ImportWarning", "IndentationError", "IndexError", "KeyError", "KeyboardInterrupt", "LookupError", "MemoryError", "NameError", "NotImplementedError", "OSError", "OverflowError", "PendingDeprecationWarning", "ReferenceError", "RuntimeError", "RuntimeWarning", "StandardError", "StopIteration", "SyntaxError", "SyntaxWarning", "SystemError", "SystemExit", "TabError", "TypeError", "UnboundLocalError", "UnicodeDecodeError", "UnicodeEncodeError", "UnicodeError", "UnicodeTranslateError", "UnicodeWarning", "UserWarning", "ValueError", "Warning", "WindowsError", "ZeroDivisionError"),
                new ExceptionHandlerInfo(72, 73, "*"),

                new ExceptionHandlerInfo(125, 126, "struct.error", "socket.error", "os.error"),
                new ExceptionHandlerInfo(130, 131, "struct.error", "socket.error", "os.error"),

                new ExceptionHandlerInfo(133, 143, "ValueError"),
                new ExceptionHandlerInfo(135, 141, "TypeError"),
                new ExceptionHandlerInfo(137, 139, "ValueError"),

                new ExceptionHandlerInfo(146, 148, "ValueError"),
                new ExceptionHandlerInfo(150, 156, "TypeError"),
                new ExceptionHandlerInfo(152, 154, "ValueError"),

                new ExceptionHandlerInfo(159, 160, "Exception"),
                new ExceptionHandlerInfo(162, 163, "Exception"),
                new ExceptionHandlerInfo(165, 166, "ValueError", "TypeError"),
                new ExceptionHandlerInfo(168, 169, "ValueError", "TypeError"),

                new ExceptionHandlerInfo(171, 172, "is_included", "also.included", "this.one.too.despite.having.lots.of.dots")
            );
        }

        private void TestGetHandledExceptionRanges(PythonDebugger debugger, string filename, params ExceptionHandlerInfo[] expected) {
            var process = DebugProcess(debugger, filename, (processObj, threadObj) => { });

            var actual = process.GetHandledExceptionRanges(filename);
            Assert.AreEqual(expected.Length, actual.Count);

            Assert.IsTrue(actual.All(a =>
                expected.SingleOrDefault(e => e.FirstLine == a.Item1 && e.LastLine == a.Item2 && e.Expressions.ContainsExactly(a.Item3)) != null
            ));
        }

        #endregion

        #region Module Load Tests

        [TestMethod, Priority(1)]
        public void TestModuleLoad() {
            var debugger = new PythonDebugger();

            // main file is reported
            TestModuleLoad(debugger, TestData.GetPath(@"TestData\HelloWorld\Program.py"), "Program.py");

            // imports are reported
            TestModuleLoad(debugger, Path.Combine(DebuggerTestPath, "imports_other.py"), "imports_other.py", "is_imported.py");
        }

        private void TestModuleLoad(PythonDebugger debugger, string filename, params string[] expectedModulesLoaded) {
            var process = DebugProcess(debugger, filename);

            List<string> receivedFilenames = new List<string>();
            process.ModuleLoaded += (sender, args) => {
                receivedFilenames.Add(args.Module.Filename);
            };

            StartAndWaitForExit(process);

            Assert.IsTrue(receivedFilenames.Count >= expectedModulesLoaded.Length, "did not receive enough module names");
            var set = new HashSet<string>();
            foreach (var received in receivedFilenames) {
                set.Add(Path.GetFileName(received));
            }

            AssertUtil.ContainsAtLeast(set, expectedModulesLoaded);
        }

        #endregion

        #region Exit Code Tests

        [TestMethod, Priority(1)]
        [TestCategory("10s")]
        public void TestStartup() {
            var debugger = new PythonDebugger();

            // hello world
            TestExitCode(debugger, TestData.GetPath(@"TestData\HelloWorld\Program.py"), 0);

            // test which calls sys.exit(23)
            TestExitCode(debugger, Path.Combine(DebuggerTestPath, "SysExit.py"), 23);

            // test which calls raise Exception()
            TestExitCode(debugger, Path.Combine(DebuggerTestPath, "ExceptionalExit.py"), 1);

            // test which checks __name__ and __file__ to be correct
            TestExitCode(debugger, Path.Combine(DebuggerTestPath, "CheckNameAndFile.py"), 0);
        }

        [TestMethod, Priority(1)]
        [TestCategory("10s")]
        public void TestWindowsStartup() {
            var debugger = new PythonDebugger();

            string pythonwExe = Path.Combine(Path.GetDirectoryName(Version.InterpreterPath), "pythonw.exe");
            if (!File.Exists(pythonwExe)) {
                pythonwExe = Path.Combine(Path.GetDirectoryName(Version.InterpreterPath), "ipyw.exe");
            }

            if (File.Exists(pythonwExe)) {
                // hello world
                TestExitCode(debugger, TestData.GetPath(@"TestData\HelloWorld\Program.py"), 0, pythonExe: pythonwExe);

                // test which calls sys.exit(23)
                TestExitCode(debugger, Path.Combine(DebuggerTestPath, "SysExit.py"), 23, pythonExe: pythonwExe);

                // test which calls raise Exception()
                TestExitCode(debugger, Path.Combine(DebuggerTestPath, "ExceptionalExit.py"), 1, pythonExe: pythonwExe);
            }
        }

        private void TestExitCode(PythonDebugger debugger, string filename, int expectedExitCode, string interpreterOptions = null, string pythonExe = null) {
            var process = DebugProcess(debugger, filename, interpreterOptions: interpreterOptions, pythonExe: pythonExe);

            // Collect these values and assert on them on the main thread
            bool threadCreated = false, threadExited = false;
            bool processExited = false;
            int exitCode = -1000;
            var output = new StringBuilder();
            AutoResetEvent hasExited = new AutoResetEvent(false);
            process.ThreadCreated += (sender, args) => {
                threadCreated = true;
            };
            process.ThreadExited += (sender, args) => {
                threadExited = true;
            };
            process.ProcessExited += (sender, args) => {
                exitCode = args.ExitCode;
                processExited = true;
                hasExited.Set();
            };
            process.ExceptionRaised += (sender, args) => {
                process.Resume();
            };
            process.DebuggerOutput += (sender, args) => {
                if (args.Output != null) {
                    lock (output) {
                        output.AppendLine(args.Output);
                    }
                }
            };

            StartAndWaitForExit(process);
            // Only wait a little while - the process should have already exited
            // by the time we get here, but we may not have received the event
            // yet.
            Assert.IsTrue(hasExited.WaitOne(1000), "ProcessExited event was not raised");

            Console.WriteLine("Output from process:");
            Console.Write(output.ToString());
            Console.WriteLine("=== End of output ===");
            Assert.IsTrue(threadCreated, "Never got notification of thread creation");
            Assert.IsTrue(threadExited, "Process failed to exit");
            Assert.IsTrue(processExited, "Process failed to exit");
            Assert.AreEqual(expectedExitCode, exitCode, String.Format("Unexpected Python process exit code for '{0}'", filename));
        }

        private new PythonProcess DebugProcess(PythonDebugger debugger, string filename, Action<PythonProcess, PythonThread> onLoaded = null, bool resumeOnProcessLoaded = true, string interpreterOptions = null, PythonDebugOptions debugOptions = PythonDebugOptions.RedirectOutput, string cwd = null, string pythonExe = null) {
            string fullPath = Path.GetFullPath(filename);
            string dir = cwd ?? Path.GetFullPath(Path.GetDirectoryName(filename));
            var process = debugger.CreateProcess(Version.Version, pythonExe ?? Version.InterpreterPath, "\"" + fullPath + "\"", dir, "", interpreterOptions, debugOptions);
            process.ProcessLoaded += (sender, args) => {
                if (onLoaded != null) {
                    onLoaded(process, args.Thread);
                }
                if (resumeOnProcessLoaded) {
                    process.Resume();
                }
            };

            return process;
        }

        #endregion

        #region Argument Tests

        [TestMethod, Priority(1)]
        public void TestInterpreterArguments() {
            Version.AssertInstalled();
            var debugger = new PythonDebugger();

            // test which verifies we have no doc string when running w/ -OO
            TestExitCode(debugger, Path.Combine(DebuggerTestPath, "DocString.py"), 0, interpreterOptions: "-OO");
        }

        #endregion

        #region Output Tests

        [TestMethod, Priority(1)]
        public void TestOutputRedirection() {
            var debugger = new PythonDebugger();
            var expectedOutput = new Queue<string>(new[] { "stdout", "stderr" });

            var process = DebugProcess(debugger, Path.Combine(DebuggerTestPath, "Output.py"), (processObj, threadObj) => {
                processObj.DebuggerOutput += (sender, e) => {
                    if (expectedOutput.Count != 0) {
                        Assert.AreEqual(expectedOutput.Dequeue(), e.Output);
                    }
                };
            }, debugOptions: PythonDebugOptions.RedirectOutput);

            try {
                process.Start();
                Thread.Sleep(1000);
            } finally {
                WaitForExit(process);
            }
        }

        [TestMethod, Priority(1)]
        public void Test3xStdoutBuffer() {
            if (Version.Version.Is3x()) {
                var debugger = new PythonDebugger();

                bool gotOutput = false;
                var process = DebugProcess(debugger, Path.Combine(DebuggerTestPath, "StdoutBuffer3x.py"), (processObj, threadObj) => {
                    processObj.DebuggerOutput += (sender, args) => {
                        Assert.IsFalse(gotOutput, "got output more than once");
                        gotOutput = true;
                        Assert.AreEqual("fob", args.Output);
                    };
                }, debugOptions: PythonDebugOptions.RedirectOutput);

                StartAndWaitForExit(process);

                Assert.IsTrue(gotOutput, "failed to get output");
            }
        }

        [TestMethod, Priority(1)]
        public void TestInputFunction() {
            // 845 Python 3.3 Bad argument type for the debugger output wrappers
            // A change to the Python 3.3 implementation of input() now requires
            // that `errors` be set to a valid value on stdout. This test
            // ensures that calls to `input` continue to work.

            var debugger = new PythonDebugger();
            var expectedOutput = "Provide A: fob\n";
            string actualOutput = string.Empty;

            var process = DebugProcess(debugger, Path.Combine(DebuggerTestPath, "InputFunction.py"), (processObj, threadObj) => {
                processObj.DebuggerOutput += (sender, args) => {
                    actualOutput += args.Output;
                };
            }, debugOptions: PythonDebugOptions.RedirectOutput | PythonDebugOptions.RedirectInput);

            try {
                process.Start();
                Thread.Sleep(1000);
                process.SendStringToStdInput("fob\n");
            } finally {
                WaitForExit(process);
            }

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        #endregion
    }

    public abstract class DebuggerTests3x : DebuggerTests {
        public override string EnumChildrenTestName {
            get {
                return "EnumChildTestV3.py";
            }
        }
        public override string ComplexExceptions {
            get {
                return "ComplexExceptionsV3.py";
            }
        }
    }

    [TestClass]
    public class DebuggerTests30 : DebuggerTests3x {
        internal override PythonVersion Version {
            get {
                return PythonPaths.Python30 ?? PythonPaths.Python30_x64;
            }
        }
    }

    [TestClass]
    public class DebuggerTests31 : DebuggerTests3x {
        internal override PythonVersion Version {
            get {
                return PythonPaths.Python31 ?? PythonPaths.Python31_x64;
            }
        }
    }

    [TestClass]
    public class DebuggerTests32 : DebuggerTests3x {
        internal override PythonVersion Version {
            get {
                return PythonPaths.Python32 ?? PythonPaths.Python32_x64;
            }
        }
    }

    [TestClass]
    public class DebuggerTests33 : DebuggerTests3x {
        internal override PythonVersion Version {
            get {
                return PythonPaths.Python33 ?? PythonPaths.Python33_x64;
            }
        }
    }

    [TestClass]
    public class DebuggerTests34 : DebuggerTests3x {
        internal override PythonVersion Version {
            get {
                return PythonPaths.Python34;
            }
        }
    }

    [TestClass]
    public class DebuggerTests34_x64 : DebuggerTests34 {
        internal override PythonVersion Version {
            get {
                return PythonPaths.Python34_x64;
            }
        }
    }

    [TestClass]
    public class DebuggerTests35 : DebuggerTests3x {
        internal override PythonVersion Version {
            get {
                return PythonPaths.Python35;
            }
        }
    }

    [TestClass]
    public class DebuggerTests35_x64 : DebuggerTests35 {
        internal override PythonVersion Version {
            get {
                return PythonPaths.Python35_x64;
            }
        }
    }

    [TestClass]
    public class DebuggerTests25 : DebuggerTests {
        internal override PythonVersion Version {
            get {
                return PythonPaths.Python25 ?? PythonPaths.Python25_x64;
            }
        }
    }

    [TestClass]
    public class DebuggerTests26 : DebuggerTests {
        internal override PythonVersion Version {
            get {
                return PythonPaths.Python26 ?? PythonPaths.Python26_x64;
            }
        }
    }

    [TestClass]
    public class DebuggerTests27 : DebuggerTests {
        internal override PythonVersion Version {
            get {
                return PythonPaths.Python27;
            }
        }
    }

    [TestClass]
    public class DebuggerTests27_x64 : DebuggerTests {
        internal override PythonVersion Version {
            get {
                return PythonPaths.Python27_x64;
            }
        }
    }

    [TestClass]
    public class DebuggerTestsIpy : DebuggerTests {
        internal override PythonVersion Version {
            get {
                return PythonPaths.IronPython27 ?? PythonPaths.IronPython27_x64;
            }
        }

        protected override string UnassignedLocalRepr {
            get { return "None"; }
        }

        protected override string UnassignedLocalType {
            get { return "NoneType"; }
        }

        // IronPython doesn't expose closure variables in frame.f_locals
        public override void LocalClosureVarsTest() {
            var test = new LocalsTest(this, "LocalClosureVarsTest.py", 4) {
                Params = { "z" }
            };
            test.Run();

            test.BreakFileName = "LocalClosureVarsTestImported.py";
            test.LineNo = 6;
            test.Run();
        }

        // IronPython exposes some builtin elements in co_names not in __builtins__
        public override void LocalBuiltinUsageTest() {
            var test = new LocalsTest(this, "LocalBuiltinUsageTest.py", 4) {
                Params = { "start", "end" },
                Locals = { "i", "foreach_enumerator" }
            };
            test.Run();

            test.BreakFileName = "LocalBuiltinUsageTestImported.py";
            test.LineNo = 6;
            test.Params.Add("self");
            test.Run();
        }
    }
}
