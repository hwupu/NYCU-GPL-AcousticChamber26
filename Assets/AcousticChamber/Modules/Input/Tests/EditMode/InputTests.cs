using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GPL.Input.Tests
{
    public class BindingTableTests
    {
        [Test]
        public void StopIsOnYByDefault()
        {
            var t = new BindingTable();
            Assert.AreEqual(Control.Y, t.ControlOf(InputActions.Stop));
            Assert.AreEqual("Y (left): Stop", t.Describe().Single());
            Assert.AreEqual(Hand.Left, Controls.HandOf(Control.Y));
        }

        [Test]
        public void StandardActionsHaveTheirDefaultsAndXStaysFree()
        {
            var t = new BindingTable();
            foreach (var a in InputActions.Standard.Keys) t.DeclareStandard(a);
            Assert.AreEqual(Control.A, t.ControlOf(InputActions.Confirm));
            Assert.AreEqual(Control.B, t.ControlOf(InputActions.Back));
            Assert.IsNull(t.ActionOn(Control.X));
            Assert.AreEqual("Y (left): Stop", t.Describe().First(), "globals listed first");
            Assert.AreEqual(9, t.All.Count);
        }

        [Test]
        public void ConflictsAndDuplicatesThrow()
        {
            var t = new BindingTable().DeclareStandard(InputActions.Confirm);
            Assert.Throws<ArgumentException>(() => t.Declare("answer", Control.Y), "Y is reserved for Stop");
            Assert.Throws<ArgumentException>(() => t.Declare("answer", Control.A), "A is taken by confirm");
            Assert.Throws<ArgumentException>(() => t.Declare(InputActions.Confirm, Control.X), "declared twice");
            Assert.Throws<ArgumentException>(() => t.Declare("answer", Control.None));
            Assert.Throws<ArgumentException>(() => t.DeclareStandard("answer"), "not a standard action");
        }

        [Test]
        public void GlobalCanOnlyBeMovedExplicitly()
        {
            var t = new BindingTable();
            t.Declare("answer", Control.X);
            Assert.Throws<ArgumentException>(() => t.RemapGlobal(InputActions.Stop, Control.X), "X is taken");
            Assert.Throws<ArgumentException>(() => t.RemapGlobal("answer", Control.B), "not global");
            t.RemapGlobal(InputActions.Stop, Control.B);
            Assert.AreEqual(Control.B, t.ControlOf(InputActions.Stop));
            t.Declare("other", Control.Y);
            Assert.AreEqual("other", t.ActionOn(Control.Y));
        }
    }

    /// <summary>Scripted controller state for tests.</summary>
    sealed class FakeSource : IControlSource
    {
        public readonly HashSet<Control> DownNow = new HashSet<Control>();
        public readonly Dictionary<Control, float> Values = new Dictionary<Control, float>();
        public bool Down(Control c) => DownNow.Contains(c);
        public bool Held(Control c) => DownNow.Contains(c);
        public bool Up(Control c) => false;
        public float Value(Control c) => Values.TryGetValue(c, out var v) ? v : 0f;
        public Vector2 Stick(Control c) => Vector2.zero;
    }

    public class ActionReaderTests
    {
        [Test]
        public void ReadsActionsThroughTheTable()
        {
            var src = new FakeSource();
            var r = new ActionReader(new BindingTable().DeclareStandard(InputActions.Confirm).DeclareStandard(InputActions.TriggerRight), src);
            src.DownNow.Add(Control.A);
            src.Values[Control.TriggerRight] = 0.7f;
            Assert.IsTrue(r.Down(InputActions.Confirm));
            Assert.AreEqual(0.7f, r.Value(InputActions.TriggerRight), 1e-6);
        }

        [Test]
        public void UndeclaredActionLogsOnceAndReadsFalse()
        {
            var r = new ActionReader(new BindingTable(), new FakeSource());
            LogAssert.Expect(LogType.Error, "[GPL] Input: action 'confirm' is not declared in the binding table.");
            Assert.IsFalse(r.Down(InputActions.Confirm));
            Assert.IsFalse(r.Down(InputActions.Confirm));
            LogAssert.NoUnexpectedReceived();
        }
    }

    public class GlobalActionsTests
    {
        GameObject _go;
        GlobalActions _g;
        FakeSource _src;
        int _stops;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Global Actions");
            _g = _go.AddComponent<GlobalActions>();
            _src = new FakeSource();
            _g.UseSource(_src);
            _stops = 0;
            _g.StopRequested += () => _stops++;
        }

        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(_go);

        [Test]
        public void YRaisesStopOnlyWhileEnabledAndNotBlocked()
        {
            _src.DownNow.Add(Control.Y);
            _g.Poll();
            Assert.AreEqual(0, _stops, "no session: Y does nothing");
            _g.StopEnabled = true;
            _g.Poll();
            Assert.AreEqual(1, _stops);
            _g.Blocked = true;
            _g.Poll();
            Assert.AreEqual(1, _stops, "dialog open: Y ignored");
        }

        [Test]
        public void UseKeepsSourceAndFollowsRemap()
        {
            _g.StopEnabled = true;
            _g.Use(new BindingTable().RemapGlobal(InputActions.Stop, Control.X));
            _src.DownNow.Add(Control.Y);
            _g.Poll();
            Assert.AreEqual(0, _stops);
            _src.DownNow.Add(Control.X);
            _g.Poll();
            Assert.AreEqual(1, _stops);
        }
    }
}
