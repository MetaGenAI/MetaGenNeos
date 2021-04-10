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
        public readonly SyncRef<Button> _swapUIButton;
        public readonly SyncRef<TextField> _recordIndexField;
        public readonly SyncRef<Checkbox> _animationsCheckbox;
        public readonly SyncRef<Checkbox> _generateBvhCheckbox;
        public readonly SyncRef<Checkbox> _videoCheckbox;
        public readonly SyncRef<Checkbox> _voicesCheckbox;
        public readonly SyncRef<Checkbox> _publicDomainCheckbox;
        public readonly SyncRef<Checkbox> _recordUserCheckbox;
        public readonly SyncRef<Checkbox> _hearingCheckbox;
        public readonly SyncRef<Checkbox> _externalSourceCheckbox;
        public readonly SyncRef<ReferenceField<Slot>> _avatarRefField;
        public readonly SyncRef<ReferenceField<Slot>> _uiTemplateRefField;
        public readonly SyncTime _recordingStarted;
        public readonly SyncRef<Sync<bool>> record_button_pressed;
        public readonly SyncRef<Sync<bool>> play_button_pressed;
        public readonly SyncRef<Sync<bool>> swapUI_button_pressed;
        public readonly SyncRef<Text> _recordingTime;
        public ValueUserOverride<bool> publicDomainOverride;
        public ValueUserOverride<bool> recordUserOverride;
        public bool IsRecording = false;
        public bool IsPlaying = false;
        private NeosSwapCanvasPanel panel;
        public event Action ToggleRecording;
        public event Action TogglePlaying;
        public event Action SwapUI;
        MetaGen mg;
        public string ui_template;
        public readonly SyncRef<ReferenceField<Slot>> _UITemplateField;
        public Slot UISlot = null;
        public Slot panelSlot = null;

        protected override void OnAttach()
        {
            base.OnAttach();
            mg = this.Slot.GetComponent<BotLogic>().mg;
            float2 float2 = new float2(2300f, 7600f);
            this.CanvasSize = float2 * 1.0f;
            this.PhysicalHeight = this.Slot.Parent.LocalScaleToGlobal(0.3f);
            this.Panel.ShowHeader.Value = false;
            this.Panel.ShowHandle.Value = false;
            Slot holder = this.Slot.Parent.AddSlot("panel holder");
            panelSlot = holder;
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
            swapUI_button_pressed.Target = holder.AttachComponent<ValueField<bool>>().Value;
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

        public void LinkUISlot()
        {
            //Slot slot = UISlot.Duplicate();
            Slot slot = UISlot ?? _uiTemplateRefField.Target.Slot;
            DynamicVariableSpace space = slot.FindSpace("UIVariables");
            if (space == null)
            {
                return;
            }

            //Recording checkbox
            Checkbox recording_checkbox;
            space.TryReadValue<Checkbox>("recording_checkbox", out recording_checkbox);
            this._recordUserCheckbox.Target = recording_checkbox;

            //Data submission checkbox
            Checkbox public_checkbox;
            space.TryReadValue<Checkbox>("public_checkbox", out public_checkbox);
            this._publicDomainCheckbox.Target = public_checkbox;

            //Recording time
            Text recording_time_text;
            space.TryReadValue<Text>("recording_time_text", out recording_time_text);
            this._recordingTime.Target = recording_time_text;

            //Animation checkbox
            Checkbox animation_checkbox;
            space.TryReadValue<Checkbox>("animation_checkbox", out animation_checkbox);
            this._animationsCheckbox.Target = animation_checkbox;

            //Generate Bvh checkbox
            Checkbox generate_bvh_checkbox;
            space.TryReadValue<Checkbox>("generate_bvh_checkbox", out generate_bvh_checkbox);
            this._generateBvhCheckbox.Target = generate_bvh_checkbox;

            //Video checkbox
            Checkbox video_checkbox;
            space.TryReadValue<Checkbox>("video_checkbox", out video_checkbox);
            this._videoCheckbox.Target = video_checkbox;

            //Record button
            Button record_button;
            space.TryReadValue<Button>("record_button", out record_button);
            this._recordButton.Target = record_button;

            //Recording index
            TextField recording_index;
            space.TryReadValue<TextField>("recording_index", out recording_index);
            this._recordIndexField.Target = recording_index;

            //Voices checkbox
            Checkbox voices_checkbox;
            space.TryReadValue<Checkbox>("voices_checkbox", out voices_checkbox);
            this._voicesCheckbox.Target = voices_checkbox;

            //Hearing checkbox
            Checkbox hearing_checkbox;
            space.TryReadValue<Checkbox>("hearing_checkbox", out hearing_checkbox);
            this._hearingCheckbox.Target = hearing_checkbox;

            //External source checkbox
            Checkbox external_source_checkbox;
            space.TryReadValue<Checkbox>("external_source_checkbox", out external_source_checkbox);
            this._externalSourceCheckbox.Target = external_source_checkbox;

            //Avatar ref
            ReferenceField<Slot> avatar_ref_field;
            space.TryReadValue<ReferenceField<Slot>>("avatar_ref_field", out avatar_ref_field);
            this._avatarRefField.Target = avatar_ref_field;

            //Play button
            Button play_button;
            space.TryReadValue<Button>("play_button", out play_button);
            this._playButton.Target = play_button;
        }

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
            if (!mg.admin_mode)
            {
                recordUserOverride = uiBuilder1.Current.AttachComponent<ValueUserOverride<bool>>();
                recordUserOverride.CreateOverrideOnWrite.Value = true;
                recordUserOverride.Target.Target = checkbox_record_user.State;
            }

            //Data submission checkbox
            uiBuilder1.Style.MinHeight = 350f;
            Text text2 = uiBuilder1.Text("<b>By checking this box you agree to license the recorded data as CC0 (Public domain), as part of the MetaGen Public Dataset (intended for research in AI and other sciences).</b>");
            text2.HorizontalAlign.Value = CodeX.TextHorizontalAlignment.Left;
            uiBuilder1.Style.PreferredHeight = 100f;
            uiBuilder1.Style.MinHeight = 100f;
            Checkbox checkbox_public_domain = uiBuilder1.Checkbox("Public domain",false);
            this._publicDomainCheckbox.Target = checkbox_public_domain;
            if (!mg.admin_mode)
            {
                publicDomainOverride = uiBuilder1.Current.AttachComponent<ValueUserOverride<bool>>();
                publicDomainOverride.Target.Target = checkbox_public_domain.State;
            }

            //recording time
            uiBuilder1.Style.PreferredHeight = 75f;
            uiBuilder1.Style.MinHeight = 75f;
            SyncRef<Text> recording_time = this._recordingTime;
            LocaleString localeString2 = (LocaleString)"";
            Text text3 = uiBuilder1.Text(localeString2);
            recording_time.Target = text3;

            uiBuilder1.Style.PreferredHeight = 100f;
            uiBuilder1.Style.MinHeight = 100f;

            //animation checkbox
            Checkbox animCheckbox = uiBuilder1.Checkbox("Generate animation",false);
            this._animationsCheckbox.Target = animCheckbox;

            //Generate bvh checkpoint
            Checkbox checkbox5 = uiBuilder1.Checkbox("Generate bvh", false);
            this._generateBvhCheckbox.Target = checkbox5;


            //video checkbox
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

            //External source checkpoint
            Checkbox checkbox3 = uiBuilder1.Checkbox("External source", false);
            this._externalSourceCheckbox.Target = checkbox3;

            //Avatar ref
            uiBuilder1.Style.PreferredHeight = 75f;
            uiBuilder1.Style.MinHeight = 75f;
            Text text7 = uiBuilder1.Text("Avatar slot:");
            uiBuilder1.Next("Root");
            ReferenceField<Slot> refField = uiBuilder1.Current.AttachComponent<ReferenceField<Slot>>();
            this._avatarRefField.Target = refField;
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

            //UI slot ref
            uiBuilder1.Style.PreferredHeight = 75f;
            uiBuilder1.Style.MinHeight = 75f;
            Text text8 = uiBuilder1.Text("UI slot:");
            uiBuilder1.Next("Root");
            ReferenceField<Slot> refField2 = uiBuilder1.Current.AttachComponent<ReferenceField<Slot>>();
            this._uiTemplateRefField.Target = refField2;
            RefEditor uiTemplateRefEditor = uiBuilder1.Current.AttachComponent<RefEditor>();
            uiTemplateRefEditor.Setup(refField2.Reference);

            uiBuilder1.Style.MinHeight = 100f;
            uiBuilder1.Style.PreferredHeight = 100f;

            //swapUI button
            Button button3 = uiBuilder1.Button("");
            this._swapUIButton.Target = button3;
            ButtonValueSet<bool> comp3 = button2.Slot.AttachComponent<ButtonValueSet<bool>>();
            comp3.SetValue.Value = true;
            comp3.TargetValue.Target = swapUI_button_pressed.Target;

        }
        protected override void OnCommonUpdate()
        {
            base.OnCommonUpdate();

            //Check control variables from panel UI
            if (record_button_pressed != null && record_button_pressed.Target.Value)
            {
                record_button_pressed.Target.Value = false;
                ToggleRecording?.Invoke();
            }
            if (play_button_pressed != null && play_button_pressed.Target.Value)
            {
                play_button_pressed.Target.Value = false;
                TogglePlaying?.Invoke();
            }
            if (swapUI_button_pressed != null && swapUI_button_pressed.Target.Value)
            {
                swapUI_button_pressed.Target.Value = false;
                SwapUI?.Invoke();
            }
        }
    }
}