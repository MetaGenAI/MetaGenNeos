using BaseX;
using FrooxEngine.UIX;
//using OBSWebsocketDotNet;
//using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using metagen;

namespace FrooxEngine
{
    public class MetaGenBotPanelUI : NeosSwapCanvasPanel
    {
        //protected readonly SyncRef<Checkbox> _autoMirror;
        public readonly Sync<bool> _active;
        public readonly SyncRef<Button> _playButton;
        public readonly SyncRef<Button> _recordButton;
        public readonly SyncRef<TextField> _recordIndexField;
        public readonly SyncRef<Checkbox> _animationsCheckbox;
        public readonly SyncRef<Checkbox> _videoCheckbox;
        public readonly SyncRef<Checkbox> _voicesCheckbox;
        public readonly SyncRef<Checkbox> _publicDomainCheckbox;
        public readonly SyncRef<Checkbox> _recordUserCheckbox;
        public readonly SyncRef<Checkbox> _hearingCheckbox;
        public readonly SyncRef<ReferenceField<Slot>> _avatarRefField;
        public readonly SyncTime _recordingStarted;
        public readonly SyncRef<Sync<bool>> record_button_pressed;
        public readonly SyncRef<Sync<bool>> play_button_pressed;
        public readonly SyncRef<Text> _recordingTime;
        public ValueUserOverride<bool> publicDomainOverride;
        public ValueUserOverride<bool> recordUserOverride;
        public bool IsRecording = false;
        public bool IsPlaying = false;
        private NeosSwapCanvasPanel panel;
        public event Action ToggleRecording;
        public event Action TogglePlaying;
        MetaGen mg;

        protected override void OnAttach()
        {
            base.OnAttach();
            mg = this.Slot.GetComponent<BotLogic>().mg;
            float2 float2 = new float2(2300f, 5900f);
            this.CanvasSize = float2 * 1.0f;
            this.PhysicalHeight = this.Slot.Parent.LocalScaleToGlobal(0.3f);
            this.Panel.ShowHeader.Value = false;
            this.Panel.ShowHandle.Value = false;
            Slot holder = this.Slot.Parent.AddSlot("panel holder");
            //holder.Tag = "Developer";
            holder.LocalPosition = new float3(0f, 1.5f, 1f);
            this.Slot.DestroyChildren();
            panel = holder.AttachComponent<NeosSwapCanvasPanel>();
            panel.CanvasSize = float2 * 0.4f;
            panel.Slot.LocalScale = new float3(2.5f, 2.5f, 2.5f);
            panel.PhysicalHeight = panel.Slot.Parent.LocalScaleToGlobal(0.3f);
            panel.Panel.ShowHeader.Value = false;
            panel.Panel.ShowHandle.Value = false;
            record_button_pressed.Target = holder.AttachComponent<ValueField<bool>>().Value;
            play_button_pressed.Target = holder.AttachComponent<ValueField<bool>>().Value;
            this.OpenConnectedPanel();
            //this._container.Target.SetParent(this.World.RootSlot);
            //var t = typeof(NeosPanel);
            //t.GetProperty("_contentSlot").SetValue(engine, compatibilityHash, null);
            //this._panel.Target.
            //this.Slot.SetParent(this.World.RootSlot);
            //this.Slot.LocalPosition = new float3(1.5f, 0.5f, 0f);
            //this.Slot.RemoveComponent(this);
        }

        //private void OpenErrorPage(string error)
        //{
        //    UIBuilder uiBuilder = this.SwapPanel(NeosSwapCanvasPanel.Slide.Left, 0.5f);
        //    uiBuilder.VerticalLayout(4f, 0.0f, new Alignment?());
        //    uiBuilder.Style.PreferredHeight = 32f;
        //    uiBuilder.Style.MinHeight = 32f;
        //    uiBuilder.Style.FlexibleHeight = 100f;
        //    LocaleString text1 = (LocaleString)error;
        //    uiBuilder.Text(in text1, true, new Alignment?(), true, (string)null);
        //    uiBuilder.Style.FlexibleHeight = -1f;
        //    LocaleString text2 = "CameraControl.OBS.ReturnToConnect".AsLocaleKey((string)null, true, (Dictionary<string, IField>)null);
        //    uiBuilder.Button(in text2, new ButtonEventHandler(this.OnReturnToConnect));
        //}

        private void OpenConnectedPanel()
        {
            UIBuilder uiBuilder1 = panel.SwapPanel(NeosSwapCanvasPanel.Slide.None, 0.5f);
            uiBuilder1.VerticalLayout(4f, 0.0f, new Alignment?());
            uiBuilder1.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);
            uiBuilder1.Style.PreferredHeight = 65f;
            uiBuilder1.Style.MinHeight = 32f;
            uiBuilder1.Style.TextAutoSizeMin = 45f;
            uiBuilder1.Style.TextAutoSizeMax = 65f;

            //status text
            //SyncRef<Text> status = this._status;
            //LocaleString localeString1 = (LocaleString)"";
            //ref LocaleString local1 = ref localeString1;
            //Alignment? alignment1 = new Alignment?();
            //Text text1 = uiBuilder1.Text(in local1, true, alignment1, true, (string)null);
            //status.Target = text1;

            //Title
            uiBuilder1.Style.PreferredHeight = 200f;
            Text text4 = uiBuilder1.Text("MetaGen Bot");
            text4.AutoSizeMax.Value = 150f;
            text4.Size.Value = 150f;

            //Description
            uiBuilder1.Style.MinHeight = 350f;
            Text text1 = uiBuilder1.Text("<b>This recording system is currenlty in Beta. Expect bugs</b>. MetaGen is a project to generate a public dataset of VR experiences, for use in scientific research, and development of AI technologies.");
            uiBuilder1.Style.MinHeight = 32f;

            //Recording checkbox
            uiBuilder1.Style.PreferredHeight = 100f;
            uiBuilder1.Style.MinHeight = 100f;
            Checkbox checkbox_record_user = uiBuilder1.Checkbox("Record me (local)",false);
            this._recordUserCheckbox.Target = checkbox_record_user;
            recordUserOverride = uiBuilder1.Current.AttachComponent<ValueUserOverride<bool>>();
            recordUserOverride.Target.Target = checkbox_record_user.State;

            //Data submission checkbox
            uiBuilder1.Style.MinHeight = 350f;
            Text text2 = uiBuilder1.Text("<b>By checking this box you agree to license the recorded data as CC0 (Public domain), as part of the MetaGen Public Dataset (intended for research in AI and other sciences).</b>");
            text2.HorizontalAlign.Value = CodeX.TextHorizontalAlignment.Left;
            uiBuilder1.Style.PreferredHeight = 100f;
            uiBuilder1.Style.MinHeight = 100f;
            Checkbox checkbox_public_domain = uiBuilder1.Checkbox("Public domain",false);
            this._publicDomainCheckbox.Target = checkbox_public_domain;
            publicDomainOverride = uiBuilder1.Current.AttachComponent<ValueUserOverride<bool>>();
            publicDomainOverride.Target.Target = checkbox_public_domain.State;

            //recording time
            uiBuilder1.Style.PreferredHeight = 75f;
            uiBuilder1.Style.MinHeight = 75f;
            SyncRef<Text> recording_time = this._recordingTime;
            LocaleString localeString2 = (LocaleString)"";
            Text text3 = uiBuilder1.Text(localeString2);
            recording_time.Target = text3;

            uiBuilder1.Style.PreferredHeight = 100f;
            uiBuilder1.Style.MinHeight = 100f;

            //animation checkpoint
            Checkbox animCheckbox = uiBuilder1.Checkbox("Generate animation",false);
            this._animationsCheckbox.Target = animCheckbox;

            //video checkpoint
            Checkbox videoCheckbox = uiBuilder1.Checkbox("Record vision",true);
            this._videoCheckbox.Target = videoCheckbox;

            //record button
            uiBuilder1.Style.PreferredHeight = 120f;
            uiBuilder1.Style.MinHeight = 120f;
            SyncRef<Button> recordButton = this._recordButton;
            Button button1 = uiBuilder1.Button("");
            recordButton.Target = button1;
            ButtonValueSet<bool> comp1 = button1.Slot.AttachComponent<ButtonValueSet<bool>>();
            comp1.SetValue.Value = true;
            comp1.TargetValue.Target = record_button_pressed.Target;

            //Text for debug play section
            uiBuilder1.Style.PreferredHeight = 200f;
            uiBuilder1.Style.MinHeight = 100f;
            Text text5 = uiBuilder1.Text("Debug play");
            text4.AutoSizeMax.Value = 130f;
            text4.Size.Value = 130f;
            uiBuilder1.Style.MinHeight = 100f;
            uiBuilder1.Style.PreferredHeight = 100f;

            //Recording index
            uiBuilder1.Style.PreferredHeight = 75f;
            uiBuilder1.Style.MinHeight = 75f;
            Text text6 = uiBuilder1.Text("Recording index:");
            TextField field1 = uiBuilder1.TextField("0");
            this._recordIndexField.Target = field1;

            uiBuilder1.Style.MinHeight = 100f;
            uiBuilder1.Style.PreferredHeight = 100f;

            //Voices checkpoint
            Checkbox checkbox1 = uiBuilder1.Checkbox("Voices",true);
            this._voicesCheckbox.Target = checkbox1;

            //Hearing checkpoint
            Checkbox checkbox2 = uiBuilder1.Checkbox("Hearing", false);
            this._hearingCheckbox.Target = checkbox2;

            //Avatar ref
            uiBuilder1.Style.PreferredHeight = 75f;
            uiBuilder1.Style.MinHeight = 75f;
            Text text7 = uiBuilder1.Text("Avatar slot:");
            uiBuilder1.Next("Root");
            ReferenceField<Slot> refField = uiBuilder1.Current.AttachComponent<ReferenceField<Slot>>();
            _avatarRefField.Target = refField;
            RefEditor avatarRefEditor = uiBuilder1.Current.AttachComponent<RefEditor>();
            avatarRefEditor.Setup(refField.Reference);

            uiBuilder1.Style.MinHeight = 100f;
            uiBuilder1.Style.PreferredHeight = 100f;

            //play button
            SyncRef<Button> streamButton = this._playButton;
            Button button2 = uiBuilder1.Button("");
            streamButton.Target = button2;
            ButtonValueSet<bool> comp2 = button2.Slot.AttachComponent<ButtonValueSet<bool>>();
            comp2.SetValue.Value = true;
            comp2.TargetValue.Target = play_button_pressed.Target;

            //SyncRef<Checkbox> autoMirror = this._autoMirror;
            //LocaleString localeString8 = "CameraControl.OBS.AutoMirror".AsLocaleKey((string)null, true, (Dictionary<string, IField>)null);
            //Checkbox checkbox = uiBuilder1.Checkbox(localeString8, true, true, 4f);
            //autoMirror.Target = checkbox;
            //this._autoMirror.Target.State.SyncWithSetting<bool>("InteractiveCamera.AutoMirror", SettingSync.LocalChange.UpdateSetting);
            //this._active.Value = true;

        }
        protected override void OnCommonUpdate()
        {
            base.OnCommonUpdate();

            //Check control variables from panel UI
            if (record_button_pressed != null & record_button_pressed.Target.Value)
            {
                record_button_pressed.Target.Value = false;
                ToggleRecording?.Invoke();
            }
            if (play_button_pressed != null & play_button_pressed.Target.Value)
            {
                play_button_pressed.Target.Value = false;
                TogglePlaying?.Invoke();
            }
        }
    }
}