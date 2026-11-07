using System;
using System.Collections;
using GPL.Core;
using GPL.Homing;
using GPL.Logging;
using GPL.Params;
using GPL.Session;
using GPL.UI;
using UnityEngine;

namespace GPL.Protocol
{
    /// <summary>
    /// Runs the stock onboarding stages (Architecture section 8; names and plan entries in <see cref="OnboardingStages"/>):
    /// handover, language, recenter slide, instruction slides, final check at the dot with the <c>volume_check</c> row.
    /// Wires the cross-links (Back on the recenter slide reopens the language choice when that stage is on; Back on
    /// instruction slide 1 reopens the recenter slide), resumes the slides at the stored index and writes the stage rows
    /// (<c>language</c>, <c>handover</c>, <c>homing</c>, <c>slide</c>, <c>volume_check</c>). Fill the properties, then call
    /// <see cref="TryRun"/> for every <c>StageBegin</c> step.
    /// </summary>
    public sealed class Onboarding
    {
        public ProtocolScreens Screens { get; }
        public SessionRecord Record { get; }
        public EventLog Log { get; }
        public ParameterSet Values { get; }

        /// <summary>Writes the session file (Debug runs: nothing).</summary>
        public Action Save;

        public OVRCameraRig Rig;
        public FixationCue Cue;
        public RecenterListener Listener;
        /// <summary>The instruction slides; null or empty = the stage shows nothing.</summary>
        public SlideSet Instructions;
        /// <summary>Title and illustration of the recenter slide; null = the stock text.</summary>
        public SlideSet.Slide RecenterSlide;

        /// <summary>Reads the headset volume step, -1 when it cannot be read (the ready stage's <c>volume_check</c>).</summary>
        public Func<int> ReadVolume;
        /// <summary>The chamber's target volume step; -1 = none.</summary>
        public int VolumeTarget = -1;

        /// <summary>True while the recenter slide is up: its Meta recenter is logged as the stage's <c>homing</c> row, not as a <c>recenter</c> row.</summary>
        public bool InRecenterStage { get; private set; }

        public Onboarding(ProtocolScreens screens, SessionRecord record, EventLog log, ParameterSet values)
        {
            Screens = screens ?? throw new ArgumentNullException(nameof(screens));
            Record = record ?? throw new ArgumentNullException(nameof(record));
            Log = log ?? throw new ArgumentNullException(nameof(log));
            Values = values;
        }

        bool On(string stage) => OnboardingStages.On(Values, stage);

        /// <summary>The stage's coroutine when <paramref name="step"/> begins an onboarding stage, else null.</summary>
        public IEnumerator TryRun(ProtocolStep step)
        {
            if (step == null || step.Kind != StepKind.StageBegin) return null;
            switch (step.Stage.Name)
            {
                case OnboardingStages.Handover: return Handover();
                case OnboardingStages.Language: return ChooseLanguage();
                case OnboardingStages.Recenter: return Recenter();
                case OnboardingStages.Instructions: return InstructionSlides(step.Resumed);
                case OnboardingStages.Ready: return Ready();
                default: return null;
            }
        }

        IEnumerator Handover()
        {
            yield return Screens.Handover();
            Log.Write(LogEvents.Handover, "experimenter pressed Continue");
        }

        /// <summary>The language choice (stage <c>language</c>, and Back on the recenter slide): stored in the record, logged.</summary>
        public IEnumerator ChooseLanguage() =>
            Screens.Language(l =>
            {
                Record.Language = Loc.CodeOf(l);
                Save?.Invoke();
                Log.Write(LogEvents.Language, Loc.CodeOf(l));
            });

        /// <summary>
        /// The recenter slide (stage <c>recenter</c>, and Back from instruction slide 1): Back "← Language" when the language
        /// stage is on, Next named after instruction slide 1 when there is one. Logs the slide and the recenter (<c>homing</c>).
        /// </summary>
        public IEnumerator Recenter()
        {
            SlidePage first = On(OnboardingStages.Instructions) && SlideSet.CountOf(Instructions) > 0 ? SlidePage.From(Instructions.slides[0]) : null;
            InRecenterStage = true;
            try
            {
                yield return Screens.MetaRecenter(Listener, r => Log.Write(LogEvents.Homing, r.Note()), RecenterSlide,
                    On(OnboardingStages.Language) ? (Func<IEnumerator>)ChooseLanguage : null,
                    (deck, page, how) => Log.Write(LogEvents.Slide, deck.Note(page.Id, how)),
                    l => ProtocolScreens.ButtonName(first, l) ?? "");
            }
            finally { InRecenterStage = false; }
        }

        IEnumerator InstructionSlides(bool resumed)
        {
            string stage = OnboardingStages.Instructions;
            int count = SlideSet.CountOf(Instructions);
            int start = resumed ? SlideDeck.Restore(Record, stage, count) : 0;
            // slide 1 goes back to the recenter slide (when that stage is on), which comes back here with Next
            bool recenterOn = On(OnboardingStages.Recenter);
            yield return Screens.Slides(Instructions, start, resumed, (deck, slide, how) =>
            {
                SlideDeck.Store(Record, stage, deck.Index);
                Save?.Invoke();
                Log.Write(LogEvents.Slide, deck.Note(slide.Id, how));
            },
            recenterOn ? (Func<IEnumerator>)Recenter : null,
            recenterOn ? l => ProtocolScreens.ButtonName(ProtocolScreens.RecenterPage(RecenterSlide), l) : (Func<UiLanguage, string>)null);
        }

        /// <summary>The final check: head gate at the dot (<c>homing</c>), then the headset volume against the target (<c>volume_check</c>).</summary>
        IEnumerator Ready()
        {
            HomingResult gate = null;
            yield return Screens.DotCheck(Rig, Cue, HomingParams.ReadGate(Values), r => gate = r);
            Log.Write(LogEvents.Homing, gate.Note());

            int volume = ReadVolume != null ? ReadVolume() : -1;
            string note = (VolumeTarget >= 0 ? "target=" + VolumeTarget : "no target") +
                          (volume >= 0 ? (VolumeTarget >= 0 ? " match=" + (volume == VolumeTarget ? "1" : "0") : "") : " not readable");
            LogValues values = null;
            if (Log.Schema.Has(OnboardingStages.VolumeColumn))
                values = new LogValues().Set(OnboardingStages.VolumeColumn, volume >= 0 ? Fmt.I(volume) : "");
            else if (volume >= 0) note += " step=" + volume;
            Log.Write(LogEvents.VolumeCheck, values, note);
        }
    }
}
