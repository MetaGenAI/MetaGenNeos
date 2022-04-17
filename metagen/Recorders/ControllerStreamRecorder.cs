using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using FrooxEngine;
using BaseX;
using System.IO;
using FrooxEngine.CommonAvatar;
using FrooxEngine.LogiX;
using FrooxEngine.LogiX.Input;
using FrooxEngine.LogiX.Data;
using Stream = FrooxEngine.Stream;
using RefID = BaseX.RefID;
using Newtonsoft.Json;

namespace metagen
{
    public class ControllerStreamRecorder : UserBinaryDataRecorder, IRecorder
    {
        public Dictionary<RefID, InputDeviceStreamDriver> input_streams = new Dictionary<RefID, InputDeviceStreamDriver>();
        public List<RefID> current_tracked_users = new List<RefID>();
        public bool isRecording = false;
        //public Dictionary<RefID, List<Tuple<string, int>>> property_indices = new Dictionary<RefID, List<Tuple<string, int>>>();
        public Dictionary<RefID, List<InputDeviceMetadata>> inputDeviceMetadatas = new Dictionary<RefID, List<InputDeviceMetadata>>();
        public Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>> primaryStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
        public Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>> secondaryStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
        public Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>> grabStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
        public Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>> menuStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
        public Dictionary<RefID, Tuple<ValueStream<float>, ValueStream<float>>> strengthStreams = new Dictionary<RefID, Tuple<ValueStream<float>, ValueStream<float>>>();
        public Dictionary<RefID, Tuple<ValueStream<float2>, ValueStream<float2>>> axisStreams = new Dictionary<RefID, Tuple<ValueStream<float2>, ValueStream<float2>>>();
        public Dictionary<RefID, Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>> primaryStreamsRefs = new Dictionary<RefID, Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>>();
        public Dictionary<RefID, Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>> secondaryStreamsRefs = new Dictionary<RefID, Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>>();
        public Dictionary<RefID, Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>> grabStreamsRefs = new Dictionary<RefID, Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>>();
        public Dictionary<RefID, Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>> menuStreamsRefs = new Dictionary<RefID, Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>>();
        public Dictionary<RefID, Tuple<SyncRef<ValueStream<float>>, SyncRef<ValueStream<float>>>> strengthStreamsRefs = new Dictionary<RefID, Tuple<SyncRef<ValueStream<float>>, SyncRef<ValueStream<float>>>>();
        public Dictionary<RefID, Tuple<SyncRef<ValueStream<float2>>, SyncRef<ValueStream<float2>>>> axisStreamsRefs = new Dictionary<RefID, Tuple<SyncRef<ValueStream<float2>>, SyncRef<ValueStream<float2>>>>();
        public Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>> primaryBlockedStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
        public Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>> secondaryBlockedStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
        public Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>> laserActiveStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
        public Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>> showLaserToOthersStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
        public Dictionary<RefID, Tuple<ValueStream<float3>, ValueStream<float3>>> laserTargetStreams = new Dictionary<RefID, Tuple<ValueStream<float3>, ValueStream<float3>>>();
        public Dictionary<RefID, Tuple<ValueStream<float>, ValueStream<float>>> grabDistanceStreams = new Dictionary<RefID, Tuple<ValueStream<float>, ValueStream<float>>>();
        public ControllerStreamRecorder(MetaGen component) : base(component)
        {
            metagen_comp = component;
        }

        public void RecordStreams(float deltaT)
        {
            UniLog.Log(current_tracked_users);
            foreach (RefID user_id in current_tracked_users)
            {
                //UniLog.Log("Recording controller streams for " + user_id.ToString());
                //Encode the streams
                UniLog.Log(user_id);
                UniLog.Log(output_writers[user_id]);
                UniLog.Log(primaryStreams[user_id].Item1);
                UniLog.Log(primaryStreams[user_id].Item2);
                UniLog.Log(primaryBlockedStreams[user_id].Item1);
                UniLog.Log(primaryBlockedStreams[user_id].Item2);
                BinaryWriterX writer = output_writers[user_id];

                //WRITE deltaT
                writer.Write(deltaT); //float

                //WRITE primaryStreams
                UniLog.Log("write primaryStreams");
                writer.Write(primaryStreams[user_id].Item1.Value); //Left
                writer.Write(primaryStreams[user_id].Item2.Value); //Right

                //WRITE secondaryStreams
                UniLog.Log("write secondaryStreams");
                writer.Write(secondaryStreams[user_id].Item1.Value); //Left
                writer.Write(secondaryStreams[user_id].Item2.Value); //Right

                //WRITE grabStreams
                writer.Write(grabStreams[user_id].Item1.Value); //Left
                writer.Write(grabStreams[user_id].Item2.Value); //Right

                //WRITE menuStreams
                writer.Write(menuStreams[user_id].Item1.Value); //Left
                writer.Write(menuStreams[user_id].Item2.Value); //Right

                //WRITE strengthStreams
                writer.Write(strengthStreams[user_id].Item1.Value); //Left
                writer.Write(strengthStreams[user_id].Item2.Value); //Right

                //WRITE axisStreams
                writer.Write(axisStreams[user_id].Item1.Value); //Left
                writer.Write(axisStreams[user_id].Item2.Value); //Right

                //WRITE primaryBlockedStreams
                writer.Write(primaryBlockedStreams[user_id].Item1.Value); //Left
                writer.Write(primaryBlockedStreams[user_id].Item2.Value); //Right

                //WRITE secondaryBlockedStreams
                writer.Write(secondaryBlockedStreams[user_id].Item1.Value); //Left
                writer.Write(secondaryBlockedStreams[user_id].Item2.Value); //Right

                //WRITE laserActiveStreams
                writer.Write(laserActiveStreams[user_id].Item1.Value); //Left
                writer.Write(laserActiveStreams[user_id].Item2.Value); //Right

                //WRITE showLaserToOthersStreams
                writer.Write(showLaserToOthersStreams[user_id].Item1.Value); //Left
                writer.Write(showLaserToOthersStreams[user_id].Item2.Value); //Right
                
                //WRITE laserTargetStreams
                writer.Write(laserTargetStreams[user_id].Item1.Value); //Left
                writer.Write(laserTargetStreams[user_id].Item2.Value); //Right

                //WRITE grabDistanceStreams
                writer.Write(grabDistanceStreams[user_id].Item1.Value); //Left
                writer.Write(grabDistanceStreams[user_id].Item2.Value); //Right
            }
        }

        public void StartRecording()
        {
            source_type = Source.FILE;
            //NEED to run this world-sycnrhonously
            World currentWorld = metagen_comp.World;
            int currentTotalUpdates = currentWorld.TotalUpdates;
            Slot logix_slot = metagen_comp.World.RootSlot.AddSlot("temporary logix slot");
            bool added_logix = false;
            currentWorld.RunSynchronously(() =>
            {
                foreach (var userItem in metagen_comp.userMetaData)
                {
                    User user = userItem.Key;
                    UserMetadata metadata = userItem.Value;
                    if (!metadata.isRecording || (metagen_comp.LocalUser == user && !metagen_comp.record_local_user)) continue;
                    RefID user_id = user.ReferenceID;

                    current_tracked_users.Add(user_id);

                    ReferenceRegister<User> userRegister = logix_slot.AttachComponent<ReferenceRegister<User>>();
                    userRegister.Target.Target = user;
                    StandardController standardControllerLeft = logix_slot.AttachComponent<FrooxEngine.LogiX.Input.StandardController>();
                    EnumInput<Chirality> nodeEnum = logix_slot.AttachComponent<EnumInput<Chirality>>();
                    nodeEnum.Value.Value = Chirality.Left;
                    standardControllerLeft.User.Target = userRegister;
                    standardControllerLeft.Node.Target = nodeEnum;
                    SyncRef<ValueStream<bool>> _primaryStreamLeft = (SyncRef<ValueStream<bool>>) typeof(StandardController).GetField("_primaryStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(standardControllerLeft);
                    SyncRef<ValueStream<bool>> _secondaryStreamLeft = (SyncRef<ValueStream<bool>>) typeof(StandardController).GetField("_secondaryStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(standardControllerLeft);
                    SyncRef<ValueStream<bool>> _grabStreamLeft = (SyncRef<ValueStream<bool>>) typeof(StandardController).GetField("_grabStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(standardControllerLeft);
                    SyncRef<ValueStream<bool>> _menuStreamLeft = (SyncRef<ValueStream<bool>>) typeof(StandardController).GetField("_menuStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(standardControllerLeft);
                    SyncRef<ValueStream<float>> _strengthStreamLeft = (SyncRef<ValueStream<float>>) typeof(StandardController).GetField("_strengthStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(standardControllerLeft);
                    SyncRef<ValueStream<float2>> _axisStreamLeft = (SyncRef<ValueStream<float2>>) typeof(StandardController).GetField("_axisStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(standardControllerLeft);

                    StandardController standardControllerRight = logix_slot.AttachComponent<FrooxEngine.LogiX.Input.StandardController>();
                    EnumInput<Chirality> nodeEnum2 = logix_slot.AttachComponent<EnumInput<Chirality>>();
                    nodeEnum2.Value.Value = Chirality.Right;
                    standardControllerRight.User.Target = userRegister;
                    standardControllerRight.Node.Target = nodeEnum2;
                    SyncRef<ValueStream<bool>> _primaryStreamRight = (SyncRef<ValueStream<bool>>) typeof(StandardController).GetField("_primaryStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(standardControllerRight);
                    SyncRef<ValueStream<bool>> _secondaryStreamRight = (SyncRef<ValueStream<bool>>) typeof(StandardController).GetField("_secondaryStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(standardControllerRight);
                    SyncRef<ValueStream<bool>> _grabStreamRight = (SyncRef<ValueStream<bool>>) typeof(StandardController).GetField("_grabStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(standardControllerRight);
                    SyncRef<ValueStream<bool>> _menuStreamRight = (SyncRef<ValueStream<bool>>) typeof(StandardController).GetField("_menuStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(standardControllerRight);
                    SyncRef<ValueStream<float>> _strengthStreamRight = (SyncRef<ValueStream<float>>) typeof(StandardController).GetField("_strengthStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(standardControllerRight);
                    SyncRef<ValueStream<float2>> _axisStreamRight = (SyncRef<ValueStream<float2>>) typeof(StandardController).GetField("_axisStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(standardControllerRight);

                    primaryStreamsRefs[user_id] = new Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>(_primaryStreamLeft, _primaryStreamRight);
                    secondaryStreamsRefs[user_id] = new Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>(_secondaryStreamLeft, _secondaryStreamRight);
                    grabStreamsRefs[user_id] = new Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>(_grabStreamLeft, _grabStreamRight);
                    menuStreamsRefs[user_id] = new Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>(_menuStreamLeft, _menuStreamRight);
                    strengthStreamsRefs[user_id] = new Tuple<SyncRef<ValueStream<float>>, SyncRef<ValueStream<float>>>(_strengthStreamLeft, _strengthStreamRight);
                    axisStreamsRefs[user_id] = new Tuple<SyncRef<ValueStream<float2>>, SyncRef<ValueStream<float2>>>(_axisStreamLeft, _axisStreamRight);
                }
                added_logix = true;
                UniLog.Log("Added logix");
            });
            metagen_comp.StartTask(async () =>
            {
                Task task = Task.Run(() =>
                {
                    bool all_streams_not_null = false;
                    List<RefID> good_tracking_users = new List<RefID>();

                    //UniLog.Log("HO");
                    while (!all_streams_not_null & currentWorld.TotalUpdates <= currentTotalUpdates + 60)
                    {
                        if (!added_logix) continue;
                        //UniLog.Log("HI");
                        bool all_user_streams_not_null = true;
                        all_streams_not_null = true;
                        foreach (RefID user_id in current_tracked_users)
                        {
                            //HMM: why does using .Target here rather than .RawTarget give a NullReferenceException??
                            bool primary_streams_not_null = (primaryStreamsRefs[user_id].Item1.RawTarget != null) & (primaryStreamsRefs[user_id].Item2.RawTarget != null);
                            bool secondary_streams_not_null = (secondaryStreamsRefs[user_id].Item1.RawTarget != null) & (secondaryStreamsRefs[user_id].Item2.RawTarget != null);
                            bool grab_streams_not_null = (grabStreamsRefs[user_id].Item1.RawTarget != null) & (grabStreamsRefs[user_id].Item2.RawTarget != null);
                            bool menu_streams_not_null = (menuStreamsRefs[user_id].Item1.RawTarget != null) & (menuStreamsRefs[user_id].Item2.RawTarget != null);
                            bool strength_streams_not_null = (strengthStreamsRefs[user_id].Item1.RawTarget != null) & (strengthStreamsRefs[user_id].Item2.RawTarget != null);
                            bool axis_streams_not_null = (axisStreamsRefs[user_id].Item1.RawTarget != null) & (axisStreamsRefs[user_id].Item2.RawTarget != null);

                            all_user_streams_not_null = primary_streams_not_null & secondary_streams_not_null & grab_streams_not_null & menu_streams_not_null & strength_streams_not_null & axis_streams_not_null;

                            if (all_user_streams_not_null)
                            {
                                if (!good_tracking_users.Contains(user_id))
                                {
                                    good_tracking_users.Add(user_id);
                                    UniLog.Log("Added user "+user_id.ToString());
                                }
                            }

                            all_streams_not_null &= all_user_streams_not_null;
                        }
                    }

                    current_tracked_users = good_tracking_users;

                    //Get CommonToolStreamDriver
                    List<RefID> good_tracking_users2 = new List<RefID>();
                    foreach (RefID user_id in current_tracked_users)
                    {
                        User user = currentWorld.GetUser(user_id);
                        List<CommonToolStreamDriver> commonToolStreamDrivers = user.Root.Slot.GetComponents<CommonToolStreamDriver>();
                        ValueStream<bool> primaryBlockedStreamLeft = null;
                        ValueStream<bool> secondaryBlockedStreamLeft = null;
                        ValueStream<bool> laserActiveStreamLeft = null;
                        ValueStream<bool> showLaserToOthersStreamLeft = null;
                        ValueStream<float3> laserTargetStreamLeft = null;
                        ValueStream<float> grabDistanceStreamLeft = null;
                        ValueStream<bool> primaryBlockedStreamRight = null;
                        ValueStream<bool> secondaryBlockedStreamRight = null;
                        ValueStream<bool> laserActiveStreamRight = null;
                        ValueStream<bool> showLaserToOthersStreamRight = null;
                        ValueStream<float3> laserTargetStreamRight = null;
                        ValueStream<float> grabDistanceStreamRight = null;
                        foreach(CommonToolStreamDriver driver in commonToolStreamDrivers)
                        {
                            if (driver.Side.Value == Chirality.Left)
                            {
                                primaryBlockedStreamLeft = driver.PrimaryBlockedStream.Target;
                                secondaryBlockedStreamLeft = driver.SecondaryBlockedStream.Target;
                                laserActiveStreamLeft = driver.LaserActiveStream.Target;
                                showLaserToOthersStreamLeft = driver.ShowLaserToOthersStream.Target;
                                laserTargetStreamLeft = driver.LaserTargetStream.Target;
                                grabDistanceStreamLeft = driver.GrabDistanceStream.Target;
                            }
                            else if (driver.Side.Value == Chirality.Right)
                            {
                                primaryBlockedStreamRight = driver.PrimaryBlockedStream.Target;
                                secondaryBlockedStreamRight = driver.SecondaryBlockedStream.Target;
                                laserActiveStreamRight = driver.LaserActiveStream.Target;
                                showLaserToOthersStreamRight = driver.ShowLaserToOthersStream.Target;
                                laserTargetStreamRight = driver.LaserTargetStream.Target;
                                grabDistanceStreamRight = driver.GrabDistanceStream.Target;
                            }
                        }
                        bool all_common_tool_streams_not_null = primaryBlockedStreamLeft != null & primaryBlockedStreamRight != null
                                                                & secondaryBlockedStreamLeft != null & secondaryBlockedStreamRight != null
                                                                & laserActiveStreamLeft != null & laserActiveStreamRight != null
                                                                & showLaserToOthersStreamLeft != null & showLaserToOthersStreamRight != null
                                                                & laserTargetStreamLeft != null & laserActiveStreamRight != null
                                                                & grabDistanceStreamLeft != null & grabDistanceStreamRight != null;
                        if (all_common_tool_streams_not_null)
                        {
                            good_tracking_users2.Add(user_id);
                            primaryBlockedStreams[user_id] = new Tuple<ValueStream<bool>, ValueStream<bool>>(primaryBlockedStreamLeft, primaryBlockedStreamRight);
                            secondaryBlockedStreams[user_id] = new Tuple<ValueStream<bool>, ValueStream<bool>>(secondaryBlockedStreamLeft, secondaryBlockedStreamRight);
                            laserActiveStreams[user_id] = new Tuple<ValueStream<bool>, ValueStream<bool>>(laserActiveStreamLeft, laserActiveStreamRight);
                            showLaserToOthersStreams[user_id] = new Tuple<ValueStream<bool>, ValueStream<bool>>(showLaserToOthersStreamLeft, showLaserToOthersStreamRight);
                            laserTargetStreams[user_id] = new Tuple<ValueStream<float3>, ValueStream<float3>>(laserTargetStreamLeft, laserTargetStreamRight);
                            grabDistanceStreams[user_id] = new Tuple<ValueStream<float>, ValueStream<float>>(grabDistanceStreamLeft, grabDistanceStreamRight);
                        }
                    }
                    current_tracked_users = good_tracking_users2;
                    foreach (RefID user_id in current_tracked_users)
                    {
                        primaryStreams[user_id] = new Tuple<ValueStream<bool>, ValueStream<bool>>(primaryStreamsRefs[user_id].Item1.RawTarget, primaryStreamsRefs[user_id].Item2.RawTarget);
                        secondaryStreams[user_id] = new Tuple<ValueStream<bool>, ValueStream<bool>>(secondaryStreamsRefs[user_id].Item1.RawTarget, secondaryStreamsRefs[user_id].Item2.RawTarget);
                        grabStreams[user_id] = new Tuple<ValueStream<bool>, ValueStream<bool>>(grabStreamsRefs[user_id].Item1.RawTarget, grabStreamsRefs[user_id].Item2.RawTarget);
                        menuStreams[user_id] = new Tuple<ValueStream<bool>, ValueStream<bool>>(menuStreamsRefs[user_id].Item1.RawTarget, menuStreamsRefs[user_id].Item2.RawTarget);
                        strengthStreams[user_id] = new Tuple<ValueStream<float>, ValueStream<float>>(strengthStreamsRefs[user_id].Item1.RawTarget, strengthStreamsRefs[user_id].Item2.RawTarget);
                        axisStreams[user_id] = new Tuple<ValueStream<float2>, ValueStream<float2>>(axisStreamsRefs[user_id].Item1.RawTarget, axisStreamsRefs[user_id].Item2.RawTarget);
                        RegisterUserStream(user_id, "controller_streams");
                    }
                    //Destroy LogiX nodes
                    currentWorld.RunSynchronously(() =>
                    {
                        logix_slot.Destroy();
                    });

                    isRecording = true;
                        
                    });

                //await CancelAfterAsync(ct=>task, TimeSpan.FromSeconds(30), CancellationToken.None);
                await task;

            });


        }
        public void StopRecording()
        {
            UnregisterUserStreams();
            current_tracked_users = new List<RefID>();
            input_streams = new Dictionary<RefID, InputDeviceStreamDriver>();
            primaryStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
            secondaryStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
            grabStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
            menuStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
            strengthStreams = new Dictionary<RefID, Tuple<ValueStream<float>, ValueStream<float>>>();
            axisStreams = new Dictionary<RefID, Tuple<ValueStream<float2>, ValueStream<float2>>>();
            primaryStreamsRefs = new Dictionary<RefID, Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>>();
            secondaryStreamsRefs = new Dictionary<RefID, Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>>();
            grabStreamsRefs = new Dictionary<RefID, Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>>();
            menuStreamsRefs = new Dictionary<RefID, Tuple<SyncRef<ValueStream<bool>>, SyncRef<ValueStream<bool>>>>();
            strengthStreamsRefs = new Dictionary<RefID, Tuple<SyncRef<ValueStream<float>>, SyncRef<ValueStream<float>>>>();
            axisStreamsRefs = new Dictionary<RefID, Tuple<SyncRef<ValueStream<float2>>, SyncRef<ValueStream<float2>>>>();
            primaryBlockedStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
            secondaryBlockedStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
            laserActiveStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
            showLaserToOthersStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
            laserTargetStreams = new Dictionary<RefID, Tuple<ValueStream<float3>, ValueStream<float3>>>();
            grabDistanceStreams = new Dictionary<RefID, Tuple<ValueStream<float>, ValueStream<float>>>();
            isRecording = false;
        }
        public void WaitForFinish()
        {

        }

        async Task CancelAfterAsync(
        Func<CancellationToken, Task> startTask,
        TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var timeoutCancellation = new CancellationTokenSource())
            using (var combinedCancellation = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token))
            {
                Task originalTask = startTask(combinedCancellation.Token);
                Task delayTask = Task.Delay(timeout, timeoutCancellation.Token);
                Task completedTask = await Task.WhenAny(originalTask, delayTask);
                // Cancel timeout to stop either task:
                // - Either the original task completed, so we need to cancel the delay task.
                // - Or the timeout expired, so we need to cancel the original task.
                // Canceling will not affect a task, that is already completed.
                timeoutCancellation.Cancel();
                if (completedTask == originalTask)
                {
                    // original task completed
                    await originalTask;
                }
                else
                {
                    // timeout
                    throw new TimeoutException();
                }
            }
        }
    }
    //public enum ControllerInput
    //{
    //    Primary,
    //    Secondary,
    //    Grab,
    //    Menu,
    //    Strength,
    //    Axis
    //}
}
