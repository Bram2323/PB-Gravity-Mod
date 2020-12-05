using System;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using HarmonyLib;
using Poly.Math;
using PolyPhysics;
using Poly.Physics;
using Poly.Physics.Solver;
using BepInEx;
using BepInEx.Configuration;
using PolyTechFramework;

namespace GravityMod
{
    [BepInPlugin(pluginGuid, pluginName, pluginVerson)]
    [BepInProcess("Poly Bridge 2")]
    [BepInDependency(PolyTechMain.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    public class GravityMain : PolyTechMod
    {

        public const string pluginGuid = "polytech.gravitymod";

        public const string pluginName = "Gravity Mod";

        public const string pluginVerson = "1.2.1";

        public ConfigDefinition modEnableDef = new ConfigDefinition(pluginName, "Enable/Disable Mod");

        //always
        public ConfigDefinition BodiesAffectedDef = new ConfigDefinition(pluginName, "Rigidbodies Affected");
        public ConfigDefinition NodesAffectedDef = new ConfigDefinition(pluginName, "Bridge Pieces Affected");
        public ConfigDefinition gravityModifierDef = new ConfigDefinition(pluginName, "Gravity Modifier");
        public ConfigDefinition gravityTypeDef = new ConfigDefinition(pluginName, "Gravity Type");

        //Only when usin center point
        public ConfigDefinition CenterPointDef = new ConfigDefinition(pluginName, "Center Point");

        //only when using center shape
        public ConfigDefinition ShapeColorDef = new ConfigDefinition(pluginName, "Custom Shape Color");
        public ConfigDefinition IgnoreGravityDef = new ConfigDefinition(pluginName, "Ignore Own Gravity");

        //Only when using center
        public ConfigDefinition DistanceTypeDef = new ConfigDefinition(pluginName, "Gravity Distance Type");
        public ConfigDefinition DistanceNormalDef = new ConfigDefinition(pluginName, "Normal Gravity Distance"); //Only when using number based normal
        public ConfigDefinition DistanceMultiplierDef = new ConfigDefinition(pluginName, "Gravity Distance Smoothness"); //Only when using distance
        public ConfigDefinition NearestCenterDef = new ConfigDefinition(pluginName, "Only Nearest Center");

        //always
        public ConfigDefinition LoadCustomCampaignDef = new ConfigDefinition(pluginName, "Load Custom Layout");
        public ConfigDefinition RotateCameraDef = new ConfigDefinition(pluginName, "Rotate Camera");
        public ConfigDefinition FollowCameraDef = new ConfigDefinition(pluginName, "Follow Camera");
        public ConfigDefinition LerpVelDef = new ConfigDefinition(pluginName, "Camera Move Speed");
        public ConfigDefinition ChangeTargetDef = new ConfigDefinition(pluginName, "Change Target");

        public enum GravityType
        {
            Normal,
            CenterPoint,
            CenterShape,
            CenterShapeStaticPins,
            LineBetweenStaticPins
        }

        public enum DistanceType
        {
            [Description("Not Distance Based")]
            None,
            [Description("Number Based Normal")]
            Number,
            [Description("Mass Based Normal")]
            MassBased
        }

        public ConfigEntry<bool> mEnabled;

        public ConfigEntry<bool> mBodiesAffected;

        public ConfigEntry<bool> mNodesAffected;

        public ConfigEntry<Vector2> mGravityModifier;

        public ConfigEntry<GravityType> mGravityType;

        public ConfigEntry<Vector2> mCenterPoint;

        public ConfigEntry<string> mShapeColor;

        public ConfigEntry<bool> mIgnoreGravity;

        public ConfigEntry<DistanceType> mDistanceType;

        public ConfigEntry<float> mDistanceNormal;

        public ConfigEntry<float> mDistanceMultiplier;

        public ConfigEntry<bool> mNearestCenter;

        public ConfigEntry<bool> mLoadCustomCampaign;

        public ConfigEntry<bool> mRotateCamera;

        public ConfigEntry<bool> mFollowCamera;

        public ConfigEntry<float> mLerpVel;

        public ConfigEntry<KeyboardShortcut> mChangeTarget;

        public Vec2 GravityModifier;

        public GravityType _GravityType;

        public Vec2 CenterPoint;

        public Color ShapeColor;

        public bool IgnoreGravity;

        public DistanceType _DistanceType;

        public float DistanceNormal;

        public float DistanceMultiplier;

        public bool NearestCenter;

        public bool LoadCustomCampaign;

        public bool RotateCamera;

        public bool FollowCamera;

        public float LerpVel;


        public Vec2 ScaledGrav;

        public List<Rigidbody> rigidBodies;

        public List<CustomShape> customShapes;

        public List<Rigidbody> CamTargets = new List<Rigidbody>();
        public int CamIndex;
        public bool IsPressed;


        public Vector3 TargetPos;

        public Quaternion TargetRot;

        public static GravityMain instance;

        void Awake()
        {
            if (instance == null) instance = this;
            IsPressed = false;
            
            Config.Bind(modEnableDef, true, new ConfigDescription("Controls if the mod should be enabled or disabled", null, new ConfigurationManagerAttributes { Order = 2 }));
            mEnabled = (ConfigEntry<bool>)Config[modEnableDef];
            mEnabled.SettingChanged += onEnableDisable;

            Config.Bind(BodiesAffectedDef, true, new ConfigDescription("Controls if cars/shapes should be affected by the mod", null, new ConfigurationManagerAttributes { Order = 1 }));
            mBodiesAffected = (ConfigEntry<bool>)Config[BodiesAffectedDef];

            Config.Bind(NodesAffectedDef, true, new ConfigDescription("Controls if bridge pieces should be affected by the mod", null, new ConfigurationManagerAttributes { Order = 0 }));
            mNodesAffected = (ConfigEntry<bool>)Config[NodesAffectedDef];

            Config.Bind(gravityModifierDef, Vector2.up, new ConfigDescription("How strong gravity is", null, new ConfigurationManagerAttributes { Order = -1 }));
            mGravityModifier = (ConfigEntry<Vector2>)Config[gravityModifierDef];

            Config.Bind(gravityTypeDef, GravityType.Normal, new ConfigDescription("What type of gravity the game uses", null, new ConfigurationManagerAttributes { Order = -2 }));
            mGravityType = (ConfigEntry<GravityType>)Config[gravityTypeDef];

            Config.Bind(CenterPointDef, Vector2.zero, new ConfigDescription("Where the gravity center point is", null, new ConfigurationManagerAttributes { Order = -3 }));
            mCenterPoint = (ConfigEntry<Vector2>)Config[CenterPointDef];

            Config.Bind(ShapeColorDef, "#FFFFFF", new ConfigDescription("Custom shapes with that color get gravity (Leave blank to target every custom shape)", null, new ConfigurationManagerAttributes { Order = -4 }));
            mShapeColor = (ConfigEntry<string>)Config[ShapeColorDef];

            Config.Bind(IgnoreGravityDef, true, new ConfigDescription("Custom shapes will ignore their own gravity", null, new ConfigurationManagerAttributes { Order = -5 }));
            mIgnoreGravity = (ConfigEntry<bool>)Config[IgnoreGravityDef];

            Config.Bind(DistanceTypeDef, DistanceType.None, new ConfigDescription("Gravity will change based on how far a object is from a center", null, new ConfigurationManagerAttributes { Order = -6 }));
            mDistanceType = (ConfigEntry<DistanceType>)Config[DistanceTypeDef];

            Config.Bind(DistanceNormalDef, 5f, new ConfigDescription("The distance where gravity will be normal when using Gravity Distance", null, new ConfigurationManagerAttributes { Order = -7 }));
            mDistanceNormal = (ConfigEntry<float>)Config[DistanceNormalDef];

            Config.Bind(DistanceMultiplierDef, 10f, new ConfigDescription("Controls how much gravity changes using Gravity Distance", null, new ConfigurationManagerAttributes { Order = -8 }));
            mDistanceMultiplier = (ConfigEntry<float>)Config[DistanceMultiplierDef];

            Config.Bind(NearestCenterDef, false, new ConfigDescription("Objects will only have gravity from the nearest center", null, new ConfigurationManagerAttributes { Order = -9 }));
            mNearestCenter = (ConfigEntry<bool>)Config[NearestCenterDef];

            Config.Bind(LoadCustomCampaignDef, false, new ConfigDescription("Loads a custom layout when playing the campaign", null, new ConfigurationManagerAttributes { Order = -10 }));
            mLoadCustomCampaign = (ConfigEntry<bool>)Config[LoadCustomCampaignDef];

            Config.Bind(RotateCameraDef, false, new ConfigDescription("Rotate camera based on an vehicles gravity", null, new ConfigurationManagerAttributes { Order = -11 }));
            mRotateCamera = (ConfigEntry<bool>)Config[RotateCameraDef];

            Config.Bind(FollowCameraDef, false, new ConfigDescription("Changes camera position to a vehicle", null, new ConfigurationManagerAttributes { Order = -12 }));
            mFollowCamera = (ConfigEntry<bool>)Config[FollowCameraDef];

            Config.Bind(LerpVelDef, 10f, new ConfigDescription("How fast the camera moves to a vehicle", null, new ConfigurationManagerAttributes { Order = -13 }));
            mLerpVel = (ConfigEntry<float>)Config[LerpVelDef];

            mChangeTarget = Config.Bind(ChangeTargetDef, new KeyboardShortcut(KeyCode.Tab), new ConfigDescription("What button changes the camera target", null, new ConfigurationManagerAttributes { Order = -14 }));


            Config.SettingChanged += onSettingChanged;
            onSettingChanged(null, null);

            Harmony harmony = new Harmony(pluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            this.isCheat = true;
            this.isEnabled = mEnabled.Value;

            PolyTechMain.registerMod(this);
        }

        public void onEnableDisable(object sender, EventArgs e)
        {
            this.isEnabled = mEnabled.Value;
        }

        public void onSettingChanged(object sender, EventArgs e)
        {
            GravityModifier = new Vec2(mGravityModifier.Value.x * 9.81f, mGravityModifier.Value.y * -9.81f);

            _GravityType = mGravityType.Value;
            CenterPoint = mCenterPoint.Value;
            if (!ColorUtility.TryParseHtmlString(mShapeColor.Value, out ShapeColor)) ShapeColor = Color.black;
            if (mShapeColor.Value == "") ShapeColor.a = 0f;
            IgnoreGravity = mIgnoreGravity.Value;
            
            _DistanceType = mDistanceType.Value;
            DistanceNormal = mDistanceNormal.Value;
            if (mDistanceMultiplier.Value != 0) DistanceMultiplier = mDistanceMultiplier.Value;
            else DistanceMultiplier = 1f;

            NearestCenter = mNearestCenter.Value;

            LoadCustomCampaign = mLoadCustomCampaign.Value;

            RotateCamera = mRotateCamera.Value;
            FollowCamera = mFollowCamera.Value;
            LerpVel = mLerpVel.Value / 100;

            getSettings();
        }


        public override void enableMod()
        {
            this.isEnabled = true;
            mEnabled.Value = true;
        }

        public override void disableMod()
        {
            this.isEnabled = false;
            mEnabled.Value = false;
        }


        public override string getSettings()
        {
            SettingsObj settings = new SettingsObj();

            settings.BodiesAffected = mBodiesAffected.Value;
            settings.NodesAffected = mNodesAffected.Value;
            settings.GravityModifier = mGravityModifier.Value;
            settings.GravityType = _GravityType;

            if (_GravityType == GravityType.CenterPoint) settings.CenterPoint = CenterPoint;
            else if(_GravityType != GravityType.Normal)
            {
                settings.ShapeColor = mShapeColor.Value;
                settings.IgnoreGravity = IgnoreGravity;
            }

            if (_GravityType != GravityType.Normal)
            {
                settings.DistanceType = _DistanceType;
                if (_DistanceType != DistanceType.None)
                {
                    settings.DistanceMultiplier = DistanceMultiplier;
                    if (_DistanceType == DistanceType.Number) settings.DistanceNormal = DistanceNormal;
                }
                settings.NearestCenter = NearestCenter;
            }
            
            return settings.Serialize();
        }

        public override void setSettings(string settingsStr)
        {
            SettingsObj settings = SettingsObj.DeSerialize(settingsStr);

            mBodiesAffected.Value = settings.BodiesAffected;
            mNodesAffected.Value = settings.NodesAffected;
            mGravityModifier.Value = settings.GravityModifier;
            mGravityType.Value = settings.GravityType;

            if (settings.GravityType == GravityType.CenterPoint) mCenterPoint.Value = settings.CenterPoint;
            else if (settings.GravityType != GravityType.Normal)
            {
                mShapeColor.Value = settings.ShapeColor;
                mIgnoreGravity.Value = settings.IgnoreGravity;
            }

            if (settings.GravityType != GravityType.Normal)
            {
                mDistanceType.Value = settings.DistanceType;
                if (settings.DistanceType != DistanceType.None)
                {
                    mDistanceMultiplier.Value = settings.DistanceMultiplier;
                    if (settings.DistanceType == DistanceType.Number) mDistanceNormal.Value = settings.DistanceNormal;
                }
                mNearestCenter.Value = settings.NearestCenter;
            }

            onSettingChanged(null, null);
        }
        

        public Vec2 GetGravity(Vec2 Pos, SolverSettings settings, Rigidbody RB)
        {
            Vec2 scaledGravity = ScaledGrav;

            if (_GravityType == GravityType.Normal) return scaledGravity;
            
            Vec2 ReturnGrav = Vec2.zero;
            List<KeyValuePair<Vec2, float>> Centers = new List<KeyValuePair<Vec2, float>>();

            if (_GravityType == GravityType.CenterPoint)
            {
                Centers.Add(new KeyValuePair<Vec2, float>(CenterPoint, DistanceNormal));
            }
            else
            {
                foreach (CustomShape cs in customShapes)
                {
                    if (IgnoreGravity && RB != null && cs.m_PhysicsBodyIfDynamic == RB) continue;
                    if (cs.m_Color == ShapeColor || ShapeColor.a == 0)
                    {
                        if (_GravityType == GravityType.CenterShape) Centers.Add(new KeyValuePair<Vec2, float>(cs.transform.position, cs.m_Mass / 4));
                        else if (instance._GravityType == GravityType.LineBetweenStaticPins)
                        {
                            if (cs.m_Pins.Count == 2)
                            {
                                Vec2 vec2 = new Vec2(cs.m_Pins[0].transform.position.x, cs.m_Pins[0].transform.position.y);
                                Vec2 vec3 = new Vec2(cs.m_Pins[1].transform.position.x, cs.m_Pins[1].transform.position.y);
                                float num = (vec3.x - vec2.x) * (vec3.x - vec2.x) + (vec3.y - vec2.y) * (vec3.y - vec2.y);
                                if (num == 0f)
                                {
                                    Centers.Add(new KeyValuePair<Vec2, float>(new Vec2(vec2.x + 0f * (vec3.x - vec2.x), vec2.y + 0f * (vec3.y - vec2.y)), cs.m_Mass / 4f));
                                }
                                else
                                {
                                    float num2 = Math.Max(0f, Math.Min(1f, ((Pos.x - vec2.x) * (vec3.x - vec2.x) + (Pos.y - vec2.y) * (vec3.y - vec2.y)) / num));
                                    Centers.Add(new KeyValuePair<Vec2, float>(new Vec2(vec2.x + num2 * (vec3.x - vec2.x), vec2.y + num2 * (vec3.y - vec2.y)), cs.m_Mass / 4f));
                                }
                            }
                            else if (cs.m_Pins.Count == 1)
                            {
                                Centers.Add(new KeyValuePair<Vec2, float>(cs.m_Pins[0].transform.position, cs.m_Mass / 4f));
                            }
                            else
                            {
                                Centers.Add(new KeyValuePair<Vec2, float>(cs.transform.position, cs.m_Mass / 4f));
                            }
                        }
                        else
                        {
                            if (cs.m_Pins.Count == 2) Centers.Add(new KeyValuePair<Vec2, float>(new Vec2((cs.m_Pins[0].transform.position.x + cs.m_Pins[1].transform.position.x) / 2, (cs.m_Pins[0].transform.position.y + cs.m_Pins[1].transform.position.y) / 2), cs.m_Mass / 4));
                            else if (cs.m_Pins.Count == 1) Centers.Add(new KeyValuePair<Vec2, float>(cs.m_Pins[0].transform.position, cs.m_Mass / 4));
                            else Centers.Add(new KeyValuePair<Vec2, float>(cs.transform.position, cs.m_Mass / 4));
                        }
                    }
                }
            }

            if (NearestCenter)
            {
                Centers.Sort((x, y) => Math.Sqrt((x.Key.x - Pos.x) * (x.Key.x - Pos.x) + (x.Key.y - Pos.y) * (x.Key.y - Pos.y)).CompareTo(Math.Sqrt((y.Key.x - Pos.x) * (y.Key.x - Pos.x) + (y.Key.y - Pos.y) * (y.Key.y - Pos.y))));
            }

            for (int i = 0; i < Centers.Count; i++)
            {
                KeyValuePair<Vec2, float> Center = Centers[i];
                float NormalMass = Center.Value;
                float Distance = (float)Math.Sqrt((Center.Key.x - Pos.x) * (Center.Key.x - Pos.x) + (Center.Key.y - Pos.y) * (Center.Key.y - Pos.y));
                float f = Mathf.Atan2(Center.Key.y - Pos.y, Center.Key.x - Pos.x);
                Vec2 grav = new Vec2(scaledGravity.y * Mathf.Cos(f) * -1f, scaledGravity.y * Mathf.Sin(f) * -1f);
                if (_DistanceType != DistanceType.None)
                {
                    grav *= (float)Math.Pow(1f / 2f, (Distance - NormalMass) / DistanceMultiplier);
                }
                if (Center.Key.y - Pos.y == 0 && Center.Key.x - Pos.x == 0) grav = Vec2.zero;
                ReturnGrav += grav;
                
                if (NearestCenter) return ReturnGrav;
            }

            return ReturnGrav;
        }

        private void UpdateCamera(Rigidbody RB, Vec2 Pos, Vec2 Grav, float deltaTime)
        {
            if (CamIndex >= CamTargets.Count) CamIndex = 0;
            if (CamTargets[CamIndex] != RB) return;

            Camera cam = CameraControl.instance.cam;

            if (FollowCamera)
            {
                TargetPos = new Vector3(Pos.x, Pos.y, cam.transform.position.z);
            }
            if (RotateCamera)
            {
                Vector3 rot = cam.transform.rotation.eulerAngles;
                float f = Mathf.Atan2(Grav.y, Grav.x);

                rot.z = f;
                rot *= 180 / Mathf.PI;
                rot.z += 90;

                TargetRot = Quaternion.Euler(rot);
            }
        }

        private void CreateTargetList(List<Rigidbody> bodies)
        {
            CamTargets.Clear();
            CamTargets.Add(null);
            if (bodies == null) return;
            foreach (Rigidbody RB in bodies)
            {
                if (RB.name != "RB Chassis") continue;
                CamTargets.Add(RB);
            }
        }

        private bool CheckForCheating()
        {
            return mEnabled.Value && PolyTechMain.modEnabled.Value;
        }

        [HarmonyPatch(typeof(GameManager), "FixedUpdateManual")]
        private static class patchUpdate
        {
            private static void Postfix()
            {
                if (!instance.CheckForCheating() || GameStateManager.GetState() != GameState.SIM) return;

                Camera cam = CameraControl.instance.cam;

                if (!instance.IsPressed && instance.mChangeTarget.Value.IsDown())
                {
                    instance.CamIndex++;
                    if(instance.CamIndex >= instance.CamTargets.Count)
                    {
                        instance.CamIndex = 0;
                    }
                }
                instance.IsPressed = instance.mChangeTarget.Value.IsDown();

                if (instance.FollowCamera && instance.CamIndex != 0) cam.transform.position = Vector3.Lerp(cam.transform.position, instance.TargetPos, instance.LerpVel);
                if (instance.RotateCamera && instance.CamIndex != 0) cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, instance.TargetRot, instance.LerpVel);
            }
        }

        [HarmonyPatch(typeof(Solver), "Solve")]
        private static class patchSolver
        {
            private static void Prefix(List<Rigidbody> bodies)
            {
                instance.rigidBodies = bodies;
                instance.CreateTargetList(bodies);
                instance.customShapes = CustomShapes.m_Shapes;
            }
        }

        [HarmonyPatch(typeof(Campaign), "LoadLayout")]
        private static class patchCampaign
        {
            private static bool Prefix(ref bool __result, CampaignLevel level)
            {
                if (!instance.CheckForCheating()) return true;
                string path = level.m_Filename;
                if (instance.LoadCustomCampaign) path = "GravityMod\\" + level.m_Filename;

                SandboxLayoutData sandboxLayoutData = SandboxLayout.Load(Campaign.GetLevelsPath(), path);
                if (sandboxLayoutData == null)
                {
                    sandboxLayoutData = SandboxLayout.LoadLegacy(Campaign.GetLevelsPath(), path);
                }
                if (sandboxLayoutData == null)
                {
                    return true;
                }
                string themeStubKey = sandboxLayoutData.m_ThemeStubKey;
                if (string.IsNullOrEmpty(themeStubKey))
                {
                    __result = false;
                    return false;
                }
                BridgeSaveSlots.ClearSlots();
                BridgeSaveSlots.LoadSlots(Path.GetFileNameWithoutExtension(path));
                Sandbox.Clear();
                BridgeSaveSlotData autoSave = BridgeSaveSlots.GetAutoSave();
                bool flag = false;
                Sandbox.Load(themeStubKey, sandboxLayoutData, flag || level.IsTutorial());
                PointsOfView.OnLayoutLoaded();
                if (!flag && Profile.m_AutomatiallyLoadAutoSave && autoSave != null)
                {
                    Bridge.ClearAndLoadBinary(autoSave.m_Bridge);
                    Budget.MaybeApplyForcedBudgets(autoSave.m_UsingUnlimitedBudget, autoSave.m_UsingUnlimitedMaterials);
                }
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Solver), "IntegrateMotions")]
        private static class patchIntegrateMotions
        {
            private static bool Prefix(Motion[] motionsPtr, int numBodyMotions, float deltaTime, SolverSettings settings, bool clipAngles = false)
            {
                if (!instance.CheckForCheating() || !instance.mBodiesAffected.Value) return true;
                
                float oneLess_rigidbodyLinearDrag_PerIntegration = settings.oneLess_rigidbodyLinearDrag_PerIntegration;
                float oneLess_rigidbodyAngularDrag_PerIntegration = settings.oneLess_rigidbodyAngularDrag_PerIntegration;
                for (int i = 0; i < numBodyMotions; i++)
                {
                    Vec2 Pos = new Vec2(instance.rigidBodies[i].t2.position.x, instance.rigidBodies[i].t2.position.y);
                    Vec2 scaledGravity = instance.GetGravity(Pos, settings, instance.rigidBodies[i]);
                    instance.UpdateCamera(instance.rigidBodies[i], Pos, scaledGravity, deltaTime);
                    Motion motion = motionsPtr[i];
                    Vec2 vec = motion.linVel;
                    float num = motion.angVel;
                    if (settings.clipBodyVelocities)
                    {
                        float sqrMagnitude = vec.sqrMagnitude;
                        if (sqrMagnitude > settings.maxLinearVelocityDisplacement_perIntegrationIteration * settings.maxLinearVelocityDisplacement_perIntegrationIteration)
                        {
                            if (sqrMagnitude > 4f * settings.maxLinearVelocityDisplacement_perIntegrationIteration * settings.maxLinearVelocityDisplacement_perIntegrationIteration)
                            {
                                Debug.LogWarning("Linear velocity very high; clamping");
                            }
                            float b = settings.maxLinearVelocityDisplacement_perIntegrationIteration / Mathf.Sqrt(sqrMagnitude);
                            vec *= b;
                        }
                        if (Mathf.Abs(num) > settings.maxAngularVelocity_radPerSec_perIntegrationIteration)
                        {
                            if (Mathf.Abs(num) > 2f * settings.maxAngularVelocity_radPerSec_perIntegrationIteration)
                            {
                                Debug.LogWarning("Angular velocity very high; clamping");
                            }
                            num = Mathf.Clamp(num, -settings.maxAngularVelocity_radPerSec_perIntegrationIteration, settings.maxAngularVelocity_radPerSec_perIntegrationIteration);
                        }
                    }
                    motion.com += vec;
                    motion.linVel = vec * oneLess_rigidbodyLinearDrag_PerIntegration;
                    motion.angle += num;
                    motion.angVel = num * oneLess_rigidbodyAngularDrag_PerIntegration;
                    if (motion.invMass != 0f)
                    {
                        motion.linVel += scaledGravity * deltaTime * deltaTime;
                    }
                    if (clipAngles)
                    {
                        int num2 = (int)(Mathf.Abs(motion.angle) / 12.566371f);
                        if (num2 >= 1)
                        {
                            if (motion.angle < 0f)
                            {
                                motion.angle += (float)num2 * 4f * 3.1415927f;
                            }
                            else
                            {
                                motion.angle -= (float)num2 * 4f * 3.1415927f;
                            }
                        }
                        float num3 = 62.831856f * deltaTime;
                        motion.angVel = Mathf.Clamp(motion.angVel, -num3, num3);
                    }
                    motionsPtr[i] = motion;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(Solver), "IntegrateNodes")]
        private static class patchIntegrateNodes
        {
            private static bool Prefix(SolverNode[] nodesPtr, int numNodes, float deltaTime, SolverSettings settings)
            {
                if (!instance.CheckForCheating() || !instance.mNodesAffected.Value) return true;
                float num = Mathf.Pow(1f - settings.nodeVelocityDrag, deltaTime);
                if (settings.clipNodeVelocities)
                {
                    for (int i = 0; i < numNodes; i++)
                    {
                        ref SolverNode ptr = ref nodesPtr[i];
                        float num4 = ptr.vel.x;
                        float num5 = ptr.vel.y;
                        float num6 = num4 * num4 + num5 * num5;
                        if (num6 > settings.maxLinearVelocityDisplacement_perIntegrationIteration * settings.maxLinearVelocityDisplacement_perIntegrationIteration)
                        {
                            float num7 = settings.maxLinearVelocityDisplacement_perIntegrationIteration / Mathf.Sqrt(num6);
                            num4 *= num7;
                            num5 *= num7;
                        }
                        ref SolverNode ptr2 = ref ptr;
                        ptr2.pos.x = ptr2.pos.x + num4;
                        ref SolverNode ptr3 = ref ptr;
                        ptr3.pos.y = ptr3.pos.y + num5;
                        Vec2 pos = ptr.pos;
                        Vec2 scaledGravity = instance.GetGravity(pos, settings, null);
                        ptr.vel.x = num4 * num + ptr.gravityScale * scaledGravity.x * deltaTime * deltaTime;
                        ptr.vel.y = num5 * num + ptr.gravityScale * scaledGravity.y * deltaTime * deltaTime;
                    }
                }
                else
                {
                    for (int j = 0; j < numNodes; j++)
                    {
                        ref SolverNode ptr4 = ref nodesPtr[j];
                        float x = ptr4.vel.x;
                        float y = ptr4.vel.y;
                        ref SolverNode ptr5 = ref ptr4;
                        ptr5.pos.x = ptr5.pos.x + x;
                        ref SolverNode ptr6 = ref ptr4;
                        ptr6.pos.y = ptr6.pos.y + y;
                        Vec2 pos = ptr4.pos;
                        Vec2 scaledGravity = instance.GetGravity(pos, settings, null);
                        ptr4.vel.x = x * num + ptr4.gravityScale * scaledGravity.x * deltaTime * deltaTime;
                        ptr4.vel.y = y * num + ptr4.gravityScale * scaledGravity.y * deltaTime * deltaTime;
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(SolverSettings), "CacheValuesForFrame")]
        private static class patchSolverSettings
        {
            private static void Postfix(SolverSettings __instance, float timeElapsed)
            {
                if (!instance.CheckForCheating()) return;
                instance.ScaledGrav = Mathf.Clamp01(timeElapsed / (__instance.gravityFadeinDuration + 5.877472E-39f)) * instance.GravityModifier;
            }
        }
    }

    [Serializable]
    public class SettingsObj
    {
        public string Serialize()
        {
            string Data = "";

            Data += BodiesAffected;

            Data += "|" + NodesAffected;

            Data += "|" + GravityModifier.x;
            Data += "|" + GravityModifier.y;

            Data += "|" + GravityType;

            Data += "|" + CenterPoint.x;
            Data += "|" + CenterPoint.y;

            Data += "|" + ShapeColor;

            Data += "|" + IgnoreGravity;

            Data += "|" + DistanceType;

            Data += "|" + DistanceNormal;

            Data += "|" + DistanceMultiplier;

            Data += "|" + NearestCenter;

            return Data;
        }

        public static SettingsObj DeSerialize(string SerializedData)
        {
            string[] DataArray = SerializedData.Split('|');
            SettingsObj settings = new SettingsObj();

            try
            {
                settings.BodiesAffected = DataArray[0] == "True";

                settings.NodesAffected = DataArray[1] == "True";

                settings.GravityModifier.x = float.Parse(DataArray[2]);
                settings.GravityModifier.y = float.Parse(DataArray[3]);

                settings.GravityType = (GravityMain.GravityType)Enum.Parse(typeof(GravityMain.GravityType), DataArray[4]);

                settings.CenterPoint.x = float.Parse(DataArray[5]);
                settings.CenterPoint.y = float.Parse(DataArray[6]);

                settings.ShapeColor = DataArray[7];

                settings.IgnoreGravity = DataArray[8] == "True";

                settings.DistanceType = (GravityMain.DistanceType)Enum.Parse(typeof(GravityMain.DistanceType), DataArray[9]);

                settings.DistanceNormal = float.Parse(DataArray[10]);

                settings.DistanceMultiplier = float.Parse(DataArray[11]);

                settings.NearestCenter = DataArray[12] == "True";
            }
            catch (FormatException)
            {
                Debug.LogError("Could not deserialize settings: " + SerializedData);
            }
            catch (ArgumentOutOfRangeException)
            {
                Debug.LogError("Array out of bounds while deserializing settings: " + SerializedData);
            }

            return settings;
        }

        public bool BodiesAffected = true;

        public bool NodesAffected = true;

        public Vector2 GravityModifier = new Vector2(0, 1);

        public GravityMain.GravityType GravityType = GravityMain.GravityType.Normal;

        public Vector2 CenterPoint = new Vector2(0, 0);

        public string ShapeColor = "";

        public bool IgnoreGravity = true;

        public GravityMain.DistanceType DistanceType = GravityMain.DistanceType.None;

        public float DistanceNormal = 1;

        public float DistanceMultiplier = 1;

        public bool NearestCenter = false;
    }


    /// <summary>
    /// Class that specifies how a setting should be displayed inside the ConfigurationManager settings window.
    /// 
    /// Usage:
    /// This class template has to be copied inside the plugin's project and referenced by its code directly.
    /// make a new instance, assign any fields that you want to override, and pass it as a tag for your setting.
    /// 
    /// If a field is null (default), it will be ignored and won't change how the setting is displayed.
    /// If a field is non-null (you assigned a value to it), it will override default behavior.
    /// </summary>
    /// 
    /// <example> 
    /// Here's an example of overriding order of settings and marking one of the settings as advanced:
    /// <code>
    /// // Override IsAdvanced and Order
    /// Config.AddSetting("X", "1", 1, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 3 }));
    /// // Override only Order, IsAdvanced stays as the default value assigned by ConfigManager
    /// Config.AddSetting("X", "2", 2, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1 }));
    /// Config.AddSetting("X", "3", 3, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 2 }));
    /// </code>
    /// </example>
    /// 
    /// <remarks> 
    /// You can read more and see examples in the readme at https://github.com/BepInEx/BepInEx.ConfigurationManager
    /// You can optionally remove fields that you won't use from this class, it's the same as leaving them null.
    /// </remarks>
#pragma warning disable 0169, 0414, 0649
    internal sealed class ConfigurationManagerAttributes
    {
        /// <summary>
        /// Should the setting be shown as a percentage (only use with value range settings).
        /// </summary>
        public bool? ShowRangeAsPercent;

        /// <summary>
        /// Custom setting editor (OnGUI code that replaces the default editor provided by ConfigurationManager).
        /// See below for a deeper explanation. Using a custom drawer will cause many of the other fields to do nothing.
        /// </summary>
        public System.Action<BepInEx.Configuration.ConfigEntryBase> CustomDrawer;

        /// <summary>
        /// Show this setting in the settings screen at all? If false, don't show.
        /// </summary>
        public bool? Browsable;

        /// <summary>
        /// Category the setting is under. Null to be directly under the plugin.
        /// </summary>
        public string Category;

        /// <summary>
        /// If set, a "Default" button will be shown next to the setting to allow resetting to default.
        /// </summary>
        public object DefaultValue;

        /// <summary>
        /// Force the "Reset" button to not be displayed, even if a valid DefaultValue is available. 
        /// </summary>
        public bool? HideDefaultButton;

        /// <summary>
        /// Force the setting name to not be displayed. Should only be used with a <see cref="CustomDrawer"/> to get more space.
        /// Can be used together with <see cref="HideDefaultButton"/> to gain even more space.
        /// </summary>
        public bool? HideSettingName;

        /// <summary>
        /// Optional description shown when hovering over the setting.
        /// Not recommended, provide the description when creating the setting instead.
        /// </summary>
        public string Description;

        /// <summary>
        /// Name of the setting.
        /// </summary>
        public string DispName;

        /// <summary>
        /// Order of the setting on the settings list relative to other settings in a category.
        /// 0 by default, higher number is higher on the list.
        /// </summary>
        public int? Order;

        /// <summary>
        /// Only show the value, don't allow editing it.
        /// </summary>
        public bool? ReadOnly;

        /// <summary>
        /// If true, don't show the setting by default. User has to turn on showing advanced settings or search for it.
        /// </summary>
        public bool? IsAdvanced;

        /// <summary>
        /// Custom converter from setting type to string for the built-in editor textboxes.
        /// </summary>
        public System.Func<object, string> ObjToStr;

        /// <summary>
        /// Custom converter from string to setting type for the built-in editor textboxes.
        /// </summary>
        public System.Func<string, object> StrToObj;
    }
}
