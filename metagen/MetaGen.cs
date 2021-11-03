/* 
 * This is like the Engine of MetaGen, which has the main event loop, and pointers to some of the other subsystems
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using FrooxEngine.UIX;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using BaseX;
using System.Threading;
using System.IO;
using UnityEngine;
using CodeX;
using metagen;
using System.Runtime.InteropServices;
using FrooxEngine.CommonAvatar;
using NeosAnimationToolset;
using metagen.Interactions;


namespace metagen
{
    public class MetaGen : FrooxEngine.Component
    {
        public bool playing = false;
        public OutputState playing_state = OutputState.Stopped;
        public bool recording = false;
        public bool interacting = false;
        public bool record_local_user = false;
        public bool record_only_local_user = true;
        public bool silent_mode = false;
        public bool record_everyone = false;
        public bool admin_mode = false;
        public OutputState recording_state = OutputState.Stopped;
        public OutputState interacting_state = OutputState.Stopped;
        private DateTime utcNow;
        private DateTime recordingBeginTime;
        private DateTime playingBeginTime;
        public DataManager dataManager;

        private MetaInteraction metaInteraction;
        public MetaRecorder metaRecorder;
        public UnityNeos.AudioRecorderNeos hearingRecorder; //separate because it's actually set up in MetaMetaGen

        //TODO: make these configurations more neatly organized (maybe in an external json or something)

        public bool recording_hearing = true;
        public bool play_hearing = false;
        public User recording_hearing_user;

        public bool recording_voice = false;
        public bool play_voice = true;

        public bool recording_vision = false;
        public bool play_vision = false;

        public bool recording_streams = false;
        public bool play_streams = true;
        public UnifiedPayer streamPlayer;
        public bool use_grpc_player = false;
        public bool generate_animation_play = true;
        private GrpcPlayer grpcStreamPlayer;

        public bool recording_faces = false;

        public bool recording_animation = false;

        public bool recording_bvh = true;

        public BotLogic botComponent;

        public MetaDataManager metaDataManager;
        public DataBase dataBase;
        int recording_frame_index = 0;
        int playing_frame_index = 0;
        float MAX_CHUNK_LEN_MIN = 10f;
        public event Action<User> OnUserLeftCallback;
        public event Action<User> OnUserJoinedCallback;

        public bool is_loaded = false;

        private List<User> current_users = new List<User>();

        public Slot config_slot = null;
        public Slot interaction_slot = null;
        public Slot text_interaction_slot = null;
        private Slot users_config_slot = null;
        public DynamicVariableSpace users_config_space = null;
        public DynamicVariableSpace config_space = null;
        public DynamicVariableSpace interaction_space = null;
        public Slot extra_meshes_slot = null;
        public Slot extra_slots_slot = null;
        public Slot extra_fields_slot = null;

        //Metadata refers to the per-recording data about the user
        public Dictionary<User, UserMetadata> userMetaData
        {
            get
            {
                metaDataManager.GetUserMetaData();
                return metaDataManager.userMetaData;
            }
        }
        //the user data refers to the data about the user gotten from the database
        public Dictionary<User, MetaGenUser> users
        {
            get
            {
                metaDataManager.GetUsers();
                return metaDataManager.users;
            }
        }

        //public override void OnUserJoined(User user)
        //{
        //    if (is_loaded && metaDataManager != null)
        //    {
        //        current_users.Add(user);
        //        metaDataManager.GetUsers();
        //        metaDataManager.AddUserMetaData(user);
        //        botComponent.AddOverride(user);
        //        OnUserJoinedCallback.Invoke(user);
        //        UniLog.Log("USER JOINED " + user.UserID);
        //    }
        //}

        //public override void OnUserLeft(User user)
        //{
        //    if (is_loaded && metaDataManager != null)
        //    {
        //        current_users.Remove(user);
        //        metaDataManager.GetUsers();
        //        metaDataManager.RemoveUserMetaData(user);
        //        //botComponent.RemoveOverride(user);
        //        OnUserLeftCallback.Invoke(user);
        //        UniLog.Log("USER LEFT " + user.UserID);
        //    }
        //}

        public float recording_time
        {
            get
            {
                return (float)(recording ? (DateTime.UtcNow - recordingBeginTime).TotalMilliseconds : 0f);
            }
        }
        public float playing_time
        {
            get
            {
                return (float)(playing ? (DateTime.UtcNow - playingBeginTime).TotalMilliseconds : 0f);
            }
        }

        protected override void OnAttach()
        {
            base.OnAttach();
        }
        public void Initialize()
        {
            recording_streams = true;
            recording_faces = true;
            recording_animation = true;
            recording_voice = true;
            //recording_hearing = true;
            recording_vision = false;
            utcNow = DateTime.UtcNow;
            recordingBeginTime = DateTime.UtcNow;
            dataManager = this.Slot.AttachComponent<DataManager>();
            dataManager.metagen_comp = this;
            metaInteraction = new MetaInteraction(this);
            metaRecorder = new MetaRecorder(this);
            streamPlayer = new UnifiedPayer(dataManager, this);
            grpcStreamPlayer = new GrpcPlayer(dataManager, this);
            metaDataManager = new MetaDataManager(this);
            if (!silent_mode)
            {
                SetUpConfigSlot();
            }
            foreach (User user in World.AllUsers)
            {
                current_users.Add(user);
            }
            UpdateLocalUserConfigSlot();
            metaDataManager.GetUserMetaData();
        }
        private void SetUpConfigSlot()
        {
            config_slot = World.RootSlot.FindChild((Slot s) => s.Name == "metagen config");
            if (config_slot == null)
            {
                config_slot = World.RootSlot.AddSlot("metagen config");
            }
            config_space = config_slot.GetComponent<DynamicVariableSpace>();
            if (config_space == null)
            {
                config_space = config_slot.AttachComponent<DynamicVariableSpace>();
            }
            config_space.SpaceName.Value = "metagen config space";
            interaction_slot = config_slot.FindChild((Slot s) => s.Name == "metagen interaction");
            if (interaction_slot == null)
            {
                interaction_slot = config_slot.AddSlot("metagen interaction");
            }
            interaction_space = interaction_slot.GetComponent<DynamicVariableSpace>();
            if (interaction_space == null)
            {
                interaction_space = interaction_slot.AttachComponent<DynamicVariableSpace>();
            }
            interaction_space.SpaceName.Value = "metagen interaction space";

            text_interaction_slot = interaction_slot.FindChild((Slot s) => s.Name == "metagen text interaction");
            if (text_interaction_slot == null)
            {
                text_interaction_slot = interaction_slot.AddSlot("metagen text interaction");
                text_interaction_slot.CreateVariable<Sync<string>>("output field", null);
                text_interaction_slot.CreateVariable<Sync<string>>("input field", null);
            }

            extra_meshes_slot = config_slot.FindChild((Slot s) => s.Name == "metagen extra meshes");
            if (extra_meshes_slot == null) extra_meshes_slot = config_slot.AddSlot("metagen extra meshes");
            extra_meshes_slot.ChildAdded -= Extra_meshes_slot_ChildAdded;
            extra_meshes_slot.ChildAdded += Extra_meshes_slot_ChildAdded;
            extra_slots_slot = config_slot.FindChild((Slot s) => s.Name == "metagen extra slots");
            if (extra_slots_slot == null) extra_slots_slot = config_slot.AddSlot("metagen extra slots");
            extra_slots_slot.ChildAdded -= Extra_slots_slot_ChildAdded;
            extra_slots_slot.ChildAdded += Extra_slots_slot_ChildAdded;
            extra_fields_slot = config_slot.FindChild((Slot s) => s.Name == "metagen extra fields");
            if (extra_fields_slot == null) extra_fields_slot = config_slot.AddSlot("metagen extra fields");
            extra_fields_slot.ChildAdded -= Extra_fields_slot_ChildAdded;
            extra_fields_slot.ChildAdded += Extra_fields_slot_ChildAdded;
            users_config_slot = config_slot.FindChild((Slot s) => s.Name == "metagen users config");
            if (users_config_slot == null)
            {
                users_config_slot = config_slot.AddSlot("metagen users config");
                users_config_space = users_config_slot.AttachComponent<DynamicVariableSpace>();
            }
            users_config_space = users_config_slot.GetComponent<DynamicVariableSpace>();
            users_config_space.SpaceName.Value = "metagen users config space";
            if (users_config_space == null)
            {
                users_config_space = users_config_slot.AttachComponent<DynamicVariableSpace>();
                users_config_space.SpaceName.Value = "metagen users config space";
            }
        }
        //protected override void OnDispose()
        //{
        //    base.OnDispose();
        //    StopRecording();
        //}
        protected override void OnCommonUpdate()
        {
            base.OnCommonUpdate();
            World currentWorld = this.World;

            foreach (User user in World.AllUsers)
            {
                if (!current_users.Contains(user))
                {
                    OnUserJoined(user);
                }
            }

            foreach (User user in current_users)
            {
                if (!World.AllUsers.Contains(user))
                {
                    OnUserLeft(user);
                }
            }

            //UniLog.Log("HI from " + currentWorld.CorrespondingWorldId);

//Start / Stop recording
#if NOHL
            //if (this.InputInterface.GetKeyDown(Key.R))
            //{
            //    ToggleRecording();
            //}
#endif

            //Start a new chunk, if we have been recording for 30 minutes, or start a new section, if a new user has left or joined
            if (recording && ((recording_time > MAX_CHUNK_LEN_MIN * 60 * 1000) || dataManager.ShouldStartNewSection()))
            {
                StopRecording();
                StartRecording();
            }

            //TODO: controller stream playback?

//Start / Stop playing
#if NOHL
            //if (this.InputInterface.GetKeyDown(Key.P))
            //{
            //    TogglePlaying();
            //}
#endif

            //TODO: record haptics, and biometric data via some standard dynamic variables and things?
            //TODO: Save controller streams

            //RECORD ONE FRAME
            //Record one frame of video and streams (audio is handled on the audioRecorder itself via a function tied to the audio system of Neos)
            //We condition on deltaT to be as close to 30fps as possible
            float deltaT = (float)(DateTime.UtcNow - utcNow).TotalMilliseconds;
            int new_frame = (int)Math.Floor(recording_time / 33.33333f);
            if (new_frame > recording_frame_index)
            {
                if (recording)
                {
                    metaRecorder.RecordFrame(deltaT);
                }
                if (interacting)
                {
                    metaInteraction.InteractionStep(deltaT);
                }
                recording_frame_index += 1;
                utcNow = DateTime.UtcNow;
            }

            //playback
            new_frame = (int)Math.Floor(playing_time / 33.33333f);
            if (new_frame > playing_frame_index)
            {
                if (playing)
                {
                    //UniLog.Log("playing streams");
                    if (use_grpc_player)
                    {
                        grpcStreamPlayer.PlayStreams();
                    } else
                    {
                        streamPlayer.PlayStreams();
                    }
                }
                playing_frame_index += 1;
            }
#if NOHL
            if (World == Engine.Current.WorldManager.FocusedWorld) {
                Slot slot = recording_hearing_user.Root.HeadSlot;
                hearingRecorder.earSlot.GlobalPosition = slot.GlobalPosition;
                hearingRecorder.earSlot.GlobalRotation = slot.GlobalRotation;
                }
#endif
        }

        private void UpdateLocalUserConfigSlot()
        {
            foreach(User user in World.AllUsers)
            {
                string user_id = user.UserID;
                bool value = false;
                if (!users_config_space.TryReadValue<bool>(user_id, out value))
                {
                    DynamicValueVariable<bool> new_val = users_config_space.Slot.AttachComponent<DynamicValueVariable<bool>>();
                    new_val.VariableName.Value = user_id.Substring(2).Replace("-", " ");
                    if (user == World.LocalUser && record_local_user)
                        new_val.Value.Value = true;
                    else
                        new_val.Value.Value = false;
                    users_config_space.RegisterDynamicValue<bool>(user_id.Substring(2).Replace("-", " "), new_val);
                }
            }
        }
        protected override void OnAudioUpdate()
        {
            try
            {
                //base.OnAudioUpdate();
                if (recording && metaRecorder?.voiceRecorder == null ? false : metaRecorder.voiceRecorder.isRecording)
                {
                    //UniLog.Log("recording voice");
                    metaRecorder.voiceRecorder.RecordAudio();
                }
                if (interacting && metaInteraction?.voiceInteraction == null ? false : metaInteraction.voiceInteraction.isInteracting)
                {
                    metaInteraction.voiceInteraction.InteractAudio();
                }
            } catch (Exception e)
            {
                //UniLog.Log("Hecc, exception in metagen.OnAudioUpdate:" + e.Message);
            }

        }
        public void StartInteracting()
        {
            UniLog.Log("Start interacting");
            SetUpConfigSlot();
            interacting = true;
            //TODO: need to add these if we are gonna have interaction with a certain fps?
            interacting_state = OutputState.Starting;
            //recording_frame_index = 0;
            //Set the recordings time to now
            //utcNow = DateTime.UtcNow;
            //recordingBeginTime = DateTime.UtcNow;
            if (!metaInteraction.isInteracting)
            {
                metaInteraction.StartInteracting();
            }
            interacting_state = OutputState.Started;
        }
        public void StopInteracting()
        {
            UniLog.Log("Stop interacting");
            interacting = false;
            interacting_state = OutputState.Stopping;
            if (metaInteraction.isInteracting)
            {
                metaInteraction.StopInteracting();
            }
            interacting_state = OutputState.Stopped;
        }

        public void StartRecording()
        {
            UniLog.Log("Start recording");
            SetUpConfigSlot();
            if (!dataManager.have_started_recording_session)
                dataManager.StartRecordingSession();
            if (!recording)
                dataManager.StartSection();
            recording = true;
            recording_state = OutputState.Starting;
            recording_frame_index = 0;
            //Set the recordings time to now
            utcNow = DateTime.UtcNow;
            recordingBeginTime = DateTime.UtcNow;
            if (!metaRecorder.isRecording)
            {
                metaRecorder.StartRecording();
            }
            
            recording_state = OutputState.Started;
        }
        public void StopRecording()
        {
            UniLog.Log("Stop recording");
            metaDataManager.WriteUserMetaData();

            if (recording)
            {
                foreach (var item in userMetaData)
                {
                    User user = item.Key;
                    UserMetadata metadata = item.Value;
                    if (metadata.isRecording)
                        dataBase.UpdateRecordedTime(user.UserID, recording_time/1000, metadata.isPublic); //in seconds
                }
            }

            recording = false;
            recording_state = OutputState.Stopping;

            if (metaRecorder.isRecording)
            {
                metaRecorder.StopRecording();
            }
        }

        public void ToggleRecording()
        {
            //UniLog.Log("Start/Stop recording");
            if (recording) StopRecording();
            else StartRecording();
        }
        public void StartPlaying(int recording_index = 0, Slot avatar_template = null)
        {
            UniLog.Log("Start playing");
            SetUpConfigSlot();
            playing = true;
            playing_state = OutputState.Started;
            playingBeginTime = DateTime.UtcNow;
            playing_frame_index = 0;
            if (use_grpc_player)
            {
                grpcStreamPlayer.generateAnimation = generate_animation_play;
                grpcStreamPlayer.generateBvh = recording_bvh;
                if (!grpcStreamPlayer.isPlaying)
                    grpcStreamPlayer.StartPlaying(avatar_template);
            } else
            {
                streamPlayer.generateAnimation = generate_animation_play;
                streamPlayer.generateBvh = recording_bvh;
                if (!streamPlayer.isPlaying)
                    streamPlayer.StartPlaying(recording_index, avatar_template);
            }
        }
        public void StopPlaying()
        {
            UniLog.Log("Stop playing");
            playing = false;
            if (grpcStreamPlayer.isPlaying)
                grpcStreamPlayer.StopPlaying();
            if (streamPlayer.isPlaying)
                streamPlayer.StopPlaying();
            playing_state = OutputState.Stopped;
        }
        public void TogglePlaying()
        {
            if (playing)
            {
                StopPlaying();
            } else
            {
                StartPlaying();
            }
        }
        private void Extra_meshes_slot_ChildAdded(Slot slot, Slot child)
        {
            if (child.GetComponent<ReferenceField<Slot>>() == null)
                child.AttachComponent<ReferenceField<Slot>>();
        }
        private void Extra_fields_slot_ChildAdded(Slot slot, Slot child)
        {
            if (child.GetComponent<ReferenceField<IField>>() == null)
                child.AttachComponent<ReferenceField<IField>>();
        }
        private void Extra_slots_slot_ChildAdded(Slot slot, Slot child)
        {
            if (child.GetComponent<ReferenceField<Slot>>() == null)
                child.AttachComponent<ReferenceField<Slot>>();
        }


        protected override void OnDestroy()
        {
            base.OnDestroy();
            botComponent.Slot.Destroy();
        }

    }
}
