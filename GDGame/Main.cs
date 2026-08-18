using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using GDEngine.Core;
using GDEngine.Core.Audio;
using GDEngine.Core.Collections;
using GDEngine.Core.Components;
using GDEngine.Core.Components.Controllers.Physics;
using GDEngine.Core.Debug;
using GDEngine.Core.Entities;
using GDEngine.Core.Events;
using GDEngine.Core.Factories;
using GDEngine.Core.Gameplay;
using GDEngine.Core.Impulses;
using GDEngine.Core.Input.Data;
using GDEngine.Core.Input.Devices;
using GDEngine.Core.Managers;
using GDEngine.Core.Orchestration;
using GDEngine.Core.Rendering;
using GDEngine.Core.Rendering.Base;
using GDEngine.Core.Rendering.UI;
using GDEngine.Core.Screen;
using GDEngine.Core.Serialization;
using GDEngine.Core.Services;
using GDEngine.Core.Systems;
using GDEngine.Core.Timing;
using GDEngine.Core.Utilities;
using GDGame.Demos.Components;
using GDGame.Demos.Controllers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.Direct2D1.Effects;
using Color = Microsoft.Xna.Framework.Color;
using GDGame.Zone2;
using GDGame.Zone3; 
using GDGame.Zone4;

namespace GDGame
{
    public class Main : Game
    {
        #region Core Fields (Common to all games)     
        private GraphicsDeviceManager _graphics;
        private ContentDictionary<Texture2D> _textureDictionary;
        private ContentDictionary<Model> _modelDictionary;
        private ContentDictionary<SpriteFont> _fontDictionary;
        private ContentDictionary<SoundEffect> _soundDictionary;
        private ContentDictionary<Effect> _effectsDictionary;
        private bool _disposed = false;
        private Material _matBasicUnlit, _matBasicLit, _matAlphaCutout, _matBasicUnlitGround;
        //private PBRMaterial _matPBR;
        #endregion

        #region Zone 1 Fields


        private const string Zone1CubeModel = "cube";
        private const string Zone1MonkeyModel = "monkey1";
        private const string Zone1TableModel = "table";
        private const string Zone1RampModel = "ramp";

        private const string Zone1Texture = "crate1";



        private GameObject _zone1Button;
        private GameObject _zone1ButtonCap;

        private GameObject _zone1Sphere1;
        private GameObject _zone1Sphere2;

        private RigidBody _zone1Sphere1Body;
        private RigidBody _zone1Sphere2Body;

        private KeyboardState _zone1PreviousKeyboardState;

        private bool _zone1Activated;
        private bool _zone1PlayerNearButton;

        private const float Zone1InteractionDistance = 2.5f;

        #endregion
        #region Zone 2 Fields

        private const float Zone2CenterX = 12f;

        private GameObject _zone2AudioButton;

        private KeyboardState _zone2PreviousKeyboardState;

        private bool _zone2MusicChanged;

        private IDisposable _zone2MusicEventSubscription;


        // Spatial source transforms
        private GameObject _zone2SourceLeft;
        private GameObject _zone2SourceRight;


        // Looping spatial audio instances
        private SoundEffectInstance _zone2LeftInstance;
        private SoundEffectInstance _zone2RightInstance;


        // MonoGame 3D audio objects
        private readonly AudioListener _zone2Listener =
            new AudioListener();

        private readonly AudioEmitter _zone2LeftEmitter =
            new AudioEmitter();

        private readonly AudioEmitter _zone2RightEmitter =
            new AudioEmitter();

        #endregion
        #region Zone 3 Fields

        private const float Zone3CenterX = 24f;

        private GameObject _zone3FirstPersonTrigger;
        private GameObject _zone3OrbitTrigger;
        private GameObject _zone3CinematicTrigger;

        private Camera _zone3FirstPersonCamera;
        private Camera _zone3OrbitCamera;
        private Camera _zone3CinematicCamera;

        private GameObject _zone3OrbitCameraObject;
        private GameObject _zone3CinematicCameraObject;

        private IDisposable _zone3CameraEventSubscription;

        private Zone3CameraMode _zone3CurrentMode =
            Zone3CameraMode.FirstPerson;

        #endregion
        #region Zone 4 Fields

        private const float Zone4CenterX = 36f;

        private GameObject _zone4Button;
        private GameObject _zone4ImpulseObject;

        private KeyboardState _zone4PreviousKeyboardState;

        private bool _zone4StateRequested;
        private bool _zone4Completed;

        private IDisposable _zone4ButtonSubscription;
        private IDisposable _zone4StateSubscription;
        private IDisposable _zone4ImpulseSubscription;
        private IDisposable _zone4GameWonSubscription;

        #endregion
        #region Zone 5 Fields

        private const float Zone5CenterX = 48f;

        // Used for 1-5 teleport edge detection.
        private KeyboardState _zone5PreviousKeyboardState;

        // HUD UI components
        private UIText _zone5CameraPositionText;
        private UIText _zone5VelocityText;
        private UIText _zone5ElapsedTimeText;
        private UIText _zone5FovText;

        // Interactive UI
        private UIButton _zone5ResetButton;
        private UISlider _zone5FovSlider;

        // UI graphics
        private UITexture _zone5FovTrack;
        private UITexture _zone5FovHandle;

        // Prevent repeated teleport in one held key press.
        private int _zone5CurrentZone = 1;

        //UI text overlay
        private UIText _zoneInfoNameText;
        private UIText _zoneInfoSimulationText;
        private UIText _zoneInfoActionText;

        private string _zoneInfoName = "";
        private string _zoneInfoSimulation = "";
        private string _zoneInfoAction = "";

        private KeyboardState _zone5PreviousFovKeyboardState;

        #endregion

        private SceneManager _sceneManager;
        private UIDebugInfo _debugRenderer;

        #region Core Methods (Common to all games)     
        public Main()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            #region Core
            Window.Title = "Zone 1";
            InitializeGraphics(ScreenResolution.R_WXGA_16_10_1280x800);

            //Full-Screen on launch
           //DisplayMode displayMode =
           //     GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

           // _graphics.PreferredBackBufferWidth =
           //     displayMode.Width;

           // _graphics.PreferredBackBufferHeight =
           //     displayMode.Height;

           // _graphics.IsFullScreen = true;

           // _graphics.ApplyChanges();


            InitializeMouse();
            InitializeContext();

            var relativeFilePathAndName = "assets/data/asset_manifest.json";
            LoadAssetsFromJSON(relativeFilePathAndName);
            InitializeEffects();

            // Game component that exists outside scene to manage and swap scenes
            InitializeSceneManager();

            // Create the scene and register it
            InitializeScene();

            // Safe to use _sceneManager.ActiveScene from here on
            InitializeSystems();
            InitializeCameras();
            InitializeCameraManagers();

            int scale = 500;
            InitializeSkyParent();
            InitializeSkyBox(scale);
            //Stop spawning player monkey object
            //InitializePlayer();
            InitializeZone1();
            InitializeZone2();
            InitializeZone3();
            InitializeZone4();
            InitializeZone5();
            #endregion



            // Mouse reticle
            InitializeUI();
          

            // Set pause and show menu
            SetPauseShowMenu();

            // Set the active scene
            _sceneManager.SetActiveScene(AppData.LEVEL_1_NAME);
            _sceneManager.Paused = false;

            _sceneManager.ActiveScene.GetSystem<PhysicsSystem>()?
                                     .SetPaused(false);

            base.Initialize();
        }

        private GameObject CreateZone1StaticBox(
                    string name,
                    Vector3 position,
                    Vector3 size,
                    Vector3 rotationDegrees)
        {
            GameObject gameObject = InitializeModel(
                position,
                rotationDegrees,
                size,
                Zone1Texture,
                Zone1CubeModel,
                name);

            // Create collider already configured with the correct size
            var collider = new BoxCollider(size);

            collider.Center = Vector3.Zero;

            gameObject.AddComponent(collider);

            // Add rigid body AFTER collider
            var rigidBody = new RigidBody();

            rigidBody.BodyType = BodyType.Static;
            rigidBody.UseGravity = false;

            gameObject.AddComponent(rigidBody);

            gameObject.IsStatic = true;

            return gameObject;
        }

        private void InitializeZone1()
        {
            _zone1Activated = false;
            _zone1PlayerNearButton = false;
            _zone1PreviousKeyboardState = Keyboard.GetState();

            InitializeZone1Room();
            InitializeZone1Table();
            InitializeZone1Ramp();
            InitializeZone1Monkeys();
            InitializeZone1Button();
        }
        private void InitializeZone1Floor(
                float roomWidth,
                float roomLength)
                    {
                       //Texture and model
                        GameObject floorVisual =
                            new GameObject("Zone1 Floor Visual");

                        // Create a flat rectangular mesh.
                        MeshFilter floorMesh =
                            MeshFilterFactory.CreateQuadGridTexturedLit(
                                _graphics.GraphicsDevice,
                                1,
                                1,
                                roomWidth,
                                roomLength,
                                4f,
                                4f);

                        floorVisual.AddComponent(floorMesh);

                        MeshRenderer floorRenderer =
                            floorVisual.AddComponent<MeshRenderer>();

                        floorRenderer.Material =
                            _matBasicUnlitGround;

                        floorRenderer.Overrides.MainTexture =
                            _textureDictionary.Get(Zone1Texture);

                        // The generated quad needs to lie horizontally.
                        floorVisual.Transform.RotateEulerBy(
                            new Vector3(
                                MathHelper.ToRadians(-90f),
                                0f,
                                0f));

                        // Slightly above the collider so there is no z-fighting.
                        floorVisual.Transform.TranslateTo(
                            new Vector3(0f, 0.01f, 0f));

                        _sceneManager.ActiveScene.Add(floorVisual);

                        // Phisics part

                        GameObject floorPhysics =
                            new GameObject("Zone1 Floor Physics");

                        floorPhysics.Transform.TranslateTo(
                            new Vector3(0f, -0.25f, 0f));

                        var collider =
                            floorPhysics.AddComponent<BoxCollider>();

                        collider.Size =
                            new Vector3(
                                roomWidth,
                                0.5f,
                                roomLength);

                        collider.Center = Vector3.Zero;

                        collider.IsTrigger = false;

                        var rigidBody =
                            floorPhysics.AddComponent<RigidBody>();

                        rigidBody.BodyType =
                            BodyType.Static;

                        rigidBody.UseGravity =
                            false;

                        floorPhysics.IsStatic = true;

                        _sceneManager.ActiveScene.Add(floorPhysics);
                    }
        private void InitializeZone1Room()
        {
            const float roomWidth = 12f;
            const float roomLength = 10f;
            const float roomHeight = 4f;
            const float wallThickness = 0.2f;

            // Floor
            InitializeZone1Floor(roomWidth, roomLength);

            // North wall
            CreateZone1StaticBox(
                "Zone1 North Wall",
                new Vector3(0f, roomHeight / 2f, -roomLength / 2f),
                new Vector3(roomWidth, roomHeight, wallThickness),
                Vector3.Zero);

            // South wall
            CreateZone1StaticBox(
                "Zone1 South Wall",
                new Vector3(0f, roomHeight / 2f, roomLength / 2f),
                new Vector3(roomWidth, roomHeight, wallThickness),
                Vector3.Zero);

            // Left wall
            CreateZone1StaticBox(
                "Zone1 West Wall",
                new Vector3(-roomWidth / 2f, roomHeight / 2f, 0f),
                new Vector3(wallThickness, roomHeight, roomLength),
                Vector3.Zero);

           
        }
        private void InitializeZone1Table()
        {
            GameObject table = InitializeModel(
                new Vector3(0f, 0f, 0f),
                new Vector3(-90f, 0f, 0f),
                new Vector3(1f, 1f, 1f),
                "TableTexture",
                Zone1TableModel,
                "table");

            var collider = table.AddComponent<BoxCollider>();

            collider.Size = new Vector3(
                5f,
                2f,
                3f);

            collider.Center = new Vector3(
                0f,
                1f,
                0f);

            var rigidBody = table.AddComponent<RigidBody>();

            rigidBody.BodyType = BodyType.Static;
            rigidBody.UseGravity = false;

            table.IsStatic = true;
        }
        private void InitializeZone1Ramp()
        {
            GameObject ramp = InitializeModel(
                new Vector3(0f, 2.6f, 0f),
                new Vector3(-30f, -15f, 0f),
                new Vector3(1f, 1f, 1f),
                Zone1Texture,
                Zone1RampModel,
                "ramp");

            var collider = ramp.AddComponent<BoxCollider>();

            collider.Size = new Vector3(
                1.5f,
                3f,
                4f);


            collider.Center = Vector3.Zero;

            var rigidBody = ramp.AddComponent<RigidBody>();

            rigidBody.BodyType = BodyType.Static;
            rigidBody.UseGravity = false;

            ramp.IsStatic = true;

        
        }
        private GameObject CreateZone1Monkey(
                string name,
                Vector3 position,
                float scale,
                BodyType startingBodyType,
                bool useGravity,
                out RigidBody rigidBody)
        {
            GameObject monkey = InitializeModel(
                position,
                Vector3.Zero,
                Vector3.One * scale,
                "mona lisa",
                Zone1MonkeyModel,
                name);

            var collider = monkey.AddComponent<SphereCollider>();

            collider.Diameter = scale;

            rigidBody = monkey.AddComponent<RigidBody>();

            rigidBody.BodyType = startingBodyType;
            rigidBody.Mass = 1f;
            rigidBody.UseGravity = useGravity;

            rigidBody.LinearDamping = 0.02f;
            rigidBody.AngularDamping = 0.02f;

            rigidBody.LinearVelocity = Vector3.Zero;
            rigidBody.AngularVelocity = Vector3.Zero;

            return monkey;
        }
        private void InitializeZone1Monkeys()
        {
            const float monkeyScale = 0.9f;

            // Monkey 1 - Above the ramp1
            _zone1Sphere1 = CreateZone1Monkey(
                "Zone1 Monkey1",
                new Vector3(0f, 12f, 1.3f),
                monkeyScale,
                BodyType.Dynamic,
                true,
                out _zone1Sphere1Body);

            // Monkey - above the ramp2
            _zone1Sphere2 = CreateZone1Monkey(
                "Zone1 Monkey",
                new Vector3(0f, 9f, 1.3f),
                monkeyScale,
                BodyType.Kinematic,
                false,
                out _zone1Sphere2Body);
        }
        private void InitializeZone1Button()
        {
            _zone1Button = CreateZone1StaticBox(
                "Zone1 Activation Button",
                new Vector3(0f, 0.6f, 2.8f),
                new Vector3(1f, 1.2f, 1.4f),
                Vector3.Zero);

            _zone1ButtonCap = CreateZone1StaticBox(
                "Zone1 Button Cap",
                new Vector3(0f, 1.3f, 2.8f),
                new Vector3(1f, 0.25f, 1f),
                Vector3.Zero);
        }
        private void UpdateZone1Interaction()
        {
            if (_zone1Button == null || _zone1Sphere2Body == null)
                return;

            KeyboardState currentKeyboardState = Keyboard.GetState();

            GameObject player = _sceneManager.ActiveScene.Find(
                gameObject =>
                    gameObject.Name == AppData.CAMERA_NAME_FIRST_PERSON_PARENT);

            if (player == null)
            {
                _zone1PreviousKeyboardState = currentKeyboardState;
                return;
            }

            float distanceToButton = Vector3.Distance(
                player.Transform.Position,
                _zone1Button.Transform.Position);

            _zone1PlayerNearButton =
                distanceToButton <= Zone1InteractionDistance;

            bool ePressedThisFrame =
                currentKeyboardState.IsKeyDown(Keys.E) &&
                _zone1PreviousKeyboardState.IsKeyUp(Keys.E);

            if (_zone1PlayerNearButton &&
                ePressedThisFrame &&
                !_zone1Activated)
            {
                ActivateZone1Puzzle();
            }

            _zone1PreviousKeyboardState = currentKeyboardState;
        }
        private void ActivateZone1Puzzle()
        {
            if (_zone1Activated || _zone1Sphere2Body == null)
                return;

            _zone1Activated = true;

            _zone1Sphere2Body.LinearVelocity = Vector3.Zero;
            _zone1Sphere2Body.AngularVelocity = Vector3.Zero;

            _zone1Sphere2Body.BodyType = BodyType.Dynamic;
            _zone1Sphere2Body.UseGravity = true;

            if (_zone1ButtonCap != null)
            {
                _zone1ButtonCap.Transform.TranslateTo(
                    _zone1ButtonCap.Transform.Position +
                    new Vector3(0f, -0.15f, 0f));
            }

            System.Diagnostics.Debug.WriteLine(
                "Zone 1 activated: Sphere2 released.");
        }

        private void InitializeZone2()
        {
            _zone2PreviousKeyboardState =
                Keyboard.GetState();

            _zone2MusicChanged = false;

            InitializeZone2Room();
            InitializeZone2AudioSources();
            InitializeZone2Button();
            InitializeZone2Events();

            StartZone2Music();
        }
        private void InitializeZone2Room()
        {
            const float roomWidth = 12f;
            const float roomLength = 10f;
            const float roomHeight = 4f;
            const float wallThickness = 0.2f;

            float centerX = Zone2CenterX;

            // Floor

            GameObject floor =
                new GameObject("Zone2 Floor Physics");

            floor.Transform.TranslateTo(
                new Vector3(
                    centerX,
                    -0.25f,
                    0f));

            var floorCollider =
                floor.AddComponent<BoxCollider>();

            floorCollider.Size =
                new Vector3(
                    roomWidth,
                    0.5f,
                    roomLength);

            floorCollider.Center =
                Vector3.Zero;

            floorCollider.IsTrigger =
                false;

            var floorBody =
                floor.AddComponent<RigidBody>();

            floorBody.BodyType =
                BodyType.Static;

            floorBody.UseGravity =
                false;

            floor.IsStatic = true;

            _sceneManager.ActiveScene.Add(floor);

            // VISUAL FLOOR

            GameObject floorVisual =
                new GameObject("Zone2 Floor Visual");

            MeshFilter floorMesh =
                MeshFilterFactory.CreateQuadGridTexturedLit(
                    _graphics.GraphicsDevice,
                    1,
                    1,
                    roomWidth,
                    roomLength,
                    4f,
                    4f);

            floorVisual.AddComponent(floorMesh);

            MeshRenderer floorRenderer =
                floorVisual.AddComponent<MeshRenderer>();

            floorRenderer.Material =
                _matBasicUnlitGround;

            floorRenderer.Overrides.MainTexture =
                _textureDictionary.Get(Zone1Texture);

            floorVisual.Transform.RotateEulerBy(
                new Vector3(
                    MathHelper.ToRadians(-90f),
                    0f,
                    0f));

            floorVisual.Transform.TranslateTo(
                new Vector3(
                    centerX,
                    0.01f,
                    0f));

            _sceneManager.ActiveScene.Add(floorVisual);

            // FRONT WALL

            CreateZone2StaticBox(
                "Zone2 Front Wall",
                new Vector3(
                    centerX,
                    roomHeight / 2f,
                    roomLength / 2f),
                new Vector3(
                    roomWidth,
                    roomHeight,
                    wallThickness));

            // BACK WALL
            CreateZone2StaticBox(
                "Zone2 Back Wall",
                new Vector3(
                    centerX,
                    roomHeight / 2f,
                    -roomLength / 2f),
                new Vector3(
                    roomWidth,
                    roomHeight,
                    wallThickness));

          
        }
        private GameObject CreateZone2StaticBox(
                            string name,
                            Vector3 position,
                            Vector3 size)
        {
            GameObject gameObject =
                InitializeModel(
                    position,
                    Vector3.Zero,
                    size,
                    Zone1Texture,
                    Zone1CubeModel,
                    name);

            var collider =
                gameObject.AddComponent<BoxCollider>();

            collider.Size = size;
            collider.Center = Vector3.Zero;

            var rigidBody =
                gameObject.AddComponent<RigidBody>();

            rigidBody.BodyType =
                BodyType.Static;

            rigidBody.UseGravity =
                false;

            gameObject.IsStatic = true;

            return gameObject;
        }
        private void InitializeZone2AudioSources()
        {
            // LEFT AUDIO SOURCE

            _zone2SourceLeft =
                InitializeModel(
                    new Vector3(
                        Zone2CenterX - 4f,
                        5f,
                        -2f),
                    Vector3.Zero,
                    Vector3.One,
                    "mona lisa",
                    Zone1MonkeyModel,
                    "Zone2 Spatial Source Left");

            // RIGHT AUDIO SOURCE

            _zone2SourceRight =
                InitializeModel(
                    new Vector3(
                        Zone2CenterX + 4f,
                        5f,
                        -2f),
                    Vector3.Zero,
                    Vector3.One,
                    "mona lisa",
                    Zone1MonkeyModel,
                    "Zone2 Spatial Source Right");

            // SOUND INSTANCES

            SoundEffect leftSound =
                _soundDictionary.Get(
                    "zone2_spatial_left");

            SoundEffect rightSound =
                _soundDictionary.Get(
                    "zone2_spatial_right");


            _zone2LeftInstance =
                leftSound.CreateInstance();

            _zone2RightInstance =
                rightSound.CreateInstance();


            _zone2LeftInstance.IsLooped = true;
            _zone2RightInstance.IsLooped = true;


            _zone2LeftInstance.Volume = 0.7f;
            _zone2RightInstance.Volume = 0.7f;


            _zone2LeftInstance.Play();
            _zone2RightInstance.Play();
        }
        private void InitializeZone2Button()
        {
            _zone2AudioButton =
                CreateZone2StaticBox(
                    "Zone2 Audio Button",
                    new Vector3(
                        Zone2CenterX,
                        0.6f,
                        2.5f),
                    new Vector3(
                        1.2f,
                        1.2f,
                        1.2f));
        }
        private void InitializeZone2Events()
        {
            _zone2MusicEventSubscription =
                EngineContext.Instance.Events
                    .Subscribe<Zone2MusicSwitchEvent>(
                        OnZone2MusicSwitch);
        }
        private void OnZone2MusicSwitch(
                     Zone2MusicSwitchEvent evt)
        {
            if (_zone2MusicChanged)
                return;

            _zone2MusicChanged = true;


            // Publish through the EventBus.
            //
            // AudioSystem is already subscribed to
            // PlayMusicEvent.

            EngineContext.Instance.Events.Publish(
                new PlayMusicEvent(
                    "zone2_music_active",
                    0.6f,
                    1.5f));
        }
        private void StartZone2Music()
        {
            EngineContext.Instance.Events.Publish(
                new PlayMusicEvent(
                    "zone2_music_calm",
                    0.5f,
                    0f));
        }
        private void UpdateZone2SpatialAudio()
        {
            if (_zone2LeftInstance == null ||
                _zone2RightInstance == null)
                return;

            if (_sceneManager.ActiveScene.ActiveCamera == null)
                return;

            // LISTENER = PLAYER

            Transform cameraTransform =
                _sceneManager.ActiveScene
                    .ActiveCamera
                    .Transform;

            _zone2Listener.Position =
                cameraTransform.Position;

            _zone2Listener.Forward =
                cameraTransform.Forward;

            _zone2Listener.Up =
                cameraTransform.Up;

            _zone2Listener.Velocity =
                Vector3.Zero;

            // LEFT SOURCE

            _zone2LeftEmitter.Position =
                _zone2SourceLeft.Transform.Position;

            _zone2LeftEmitter.Forward =
                _zone2SourceLeft.Transform.Forward;

            _zone2LeftEmitter.Up =
                _zone2SourceLeft.Transform.Up;

            _zone2LeftEmitter.Velocity =
                Vector3.Zero;

            // RIGHT SOURCE

            _zone2RightEmitter.Position =
                _zone2SourceRight.Transform.Position;

            _zone2RightEmitter.Forward =
                _zone2SourceRight.Transform.Forward;

            _zone2RightEmitter.Up =
                _zone2SourceRight.Transform.Up;

            _zone2RightEmitter.Velocity =
                Vector3.Zero;


            // Recalculate 3D panning + attenuation
            // EVERY FRAME.

            _zone2LeftInstance.Apply3D(
                _zone2Listener,
                _zone2LeftEmitter);

            _zone2RightInstance.Apply3D(
                _zone2Listener,
                _zone2RightEmitter);
        }
        private void UpdateZone2Interaction()
        {
            if (_zone2AudioButton == null)
                return;


            KeyboardState currentKeyboardState =
                Keyboard.GetState();


            GameObject player =
                _sceneManager.ActiveScene.Find(
                    gameObject =>
                        gameObject.Name ==
                        AppData.CAMERA_NAME_FIRST_PERSON_PARENT);


            if (player == null)
            {
                _zone2PreviousKeyboardState =
                    currentKeyboardState;

                return;
            }


            float distance =
                Vector3.Distance(
                    player.Transform.Position,
                    _zone2AudioButton.Transform.Position);


            bool ePressed =
                currentKeyboardState.IsKeyDown(Keys.E) &&
                _zone2PreviousKeyboardState.IsKeyUp(Keys.E);


            if (distance <= 2.5f &&
                ePressed)
            {
                // SFX via EventBus

                EngineContext.Instance.Events.Publish(
                    new PlaySfxEvent(
                        "SFX_UI_Click_Designed_Pop_Generic_1",
                        1f,
                        false));

                // Named scene event

                EngineContext.Instance.Events.Publish(
                    new Zone2MusicSwitchEvent());
            }


            _zone2PreviousKeyboardState =
                currentKeyboardState;
        }

        private void InitializeZone3()
        {
            InitializeZone3Room();

            InitializeZone3Cameras();

            InitializeZone3CameraTriggers();

            InitializeZone3TriggerVisuals();

            InitializeZone3Events();
        }
        private void InitializeZone3Room()
        {
            const float roomWidth = 12f;
            const float roomLength = 10f;
            const float roomHeight = 4f;
            const float wallThickness = 0.2f;

            float centerX = Zone3CenterX;

            // FLOOR PHYSICS

            GameObject floorPhysics =
                new GameObject("Zone3 Floor Physics");

            floorPhysics.Transform.TranslateTo(
                new Vector3(
                    centerX,
                    -0.25f,
                    0f));

            var floorCollider =
                floorPhysics.AddComponent<BoxCollider>();

            floorCollider.Size =
                new Vector3(
                    roomWidth,
                    0.5f,
                    roomLength);

            floorCollider.Center =
                Vector3.Zero;

            floorCollider.IsTrigger =
                false;

            var floorBody =
                floorPhysics.AddComponent<RigidBody>();

            floorBody.BodyType =
                BodyType.Static;

            floorBody.UseGravity =
                false;

            floorPhysics.IsStatic = true;

            _sceneManager.ActiveScene.Add(
                floorPhysics);

            // FLOOR VISUAL

            GameObject floorVisual =
                new GameObject("Zone3 Floor Visual");

            MeshFilter floorMesh =
                MeshFilterFactory.CreateQuadGridTexturedLit(
                    _graphics.GraphicsDevice,
                    1,
                    1,
                    roomWidth,
                    roomLength,
                    4f,
                    4f);

            floorVisual.AddComponent(
                floorMesh);

            MeshRenderer floorRenderer =
                floorVisual.AddComponent<MeshRenderer>();

            floorRenderer.Material =
                _matBasicUnlitGround;

            floorRenderer.Overrides.MainTexture =
                _textureDictionary.Get(
                    Zone1Texture);

            floorVisual.Transform.RotateEulerBy(
                new Vector3(
                    MathHelper.ToRadians(-90f),
                    0f,
                    0f));

            floorVisual.Transform.TranslateTo(
                new Vector3(
                    centerX,
                    0.01f,
                    0f));

            _sceneManager.ActiveScene.Add(
                floorVisual);

            // FRONT WALL

            CreateZone3StaticBox(
                "Zone3 Front Wall",
                new Vector3(
                    centerX,
                    roomHeight / 2f,
                    roomLength / 2f),
                new Vector3(
                    roomWidth,
                    roomHeight,
                    wallThickness));

            // BACK WALL

            CreateZone3StaticBox(
                "Zone3 Back Wall",
                new Vector3(
                    centerX,
                    roomHeight / 2f,
                    -roomLength / 2f),
                new Vector3(
                    roomWidth,
                    roomHeight,
                    wallThickness));

            // No full left wall because Zone 2 connects here.
        }
        private GameObject CreateZone3StaticBox(
                    string name,
                    Vector3 position,
                    Vector3 size)
        {
            GameObject gameObject =
                InitializeModel(
                    position,
                    Vector3.Zero,
                    size,
                    Zone1Texture,
                    Zone1CubeModel,
                    name);

            var collider =
                gameObject.AddComponent<BoxCollider>();

            collider.Size =
                size;

            collider.Center =
                Vector3.Zero;

            var rigidBody =
                gameObject.AddComponent<RigidBody>();

            rigidBody.BodyType =
                BodyType.Static;

            rigidBody.UseGravity =
                false;

            gameObject.IsStatic =
                true;

            return gameObject;
        }
        private void InitializeZone3Cameras()
        {
            Scene scene =
                _sceneManager.ActiveScene;

            // 1. EXISTING FIRST-PERSON CAMERA

            GameObject fpsCameraObject =
                scene.Find(
                    gameObject =>
                        gameObject.Name ==
                        AppData.CAMERA_NAME_FIRST_PERSON);

            if (fpsCameraObject != null)
            {
                _zone3FirstPersonCamera =
                    fpsCameraObject.GetComponent<Camera>();
            }

            // 2. ORBIT CAMERA

            _zone3OrbitCameraObject =
                new GameObject(
                    "Zone3 Orbit Camera");

            _zone3OrbitCameraObject
                .Transform
                .TranslateTo(
                    new Vector3(
                        Zone3CenterX,
                        6f,
                        8f));

            _zone3OrbitCamera =
                _zone3OrbitCameraObject
                    .AddComponent<Camera>();

            _zone3OrbitCamera.FieldOfView =
                MathHelper.ToRadians(70f);

            scene.Add(
                _zone3OrbitCameraObject);

            // 3. CINEMATIC CAMERA

            _zone3CinematicCameraObject =
                new GameObject(
                    "Zone3 Cinematic Camera");

            _zone3CinematicCameraObject
                .Transform
                .TranslateTo(
                    new Vector3(
                        Zone3CenterX - 5f,
                        3.5f,
                        -3.5f));

            _zone3CinematicCameraObject
                .Transform
                .RotateEulerBy(
                    new Vector3(
                        MathHelper.ToRadians(-10f),
                        MathHelper.ToRadians(45f),
                        0f));

            _zone3CinematicCamera =
                _zone3CinematicCameraObject
                    .AddComponent<Camera>();

            _zone3CinematicCamera.FieldOfView =
                MathHelper.ToRadians(60f);

            scene.Add(
                _zone3CinematicCameraObject);
        }
        private void UpdateZone3OrbitCamera()
        {
            if (_zone3CurrentMode !=
                Zone3CameraMode.Orbit)
                return;

            if (_zone3OrbitCameraObject == null)
                return;

            GameObject player =
                _sceneManager.ActiveScene.Find(
                    gameObject =>
                        gameObject.Name ==
                        AppData.CAMERA_NAME_FIRST_PERSON_PARENT);

            if (player == null)
                return;


            Vector3 playerPosition =
                player.Transform.Position;


            // Fixed orbit angle for demonstration.
            float radius = 6f;

            float angle =
                        Time.TimeSinceStartupSecs * 0.4f;

            Vector3 cameraPosition =
                new Vector3(
                    playerPosition.X +
                        MathF.Cos(angle) * radius,
                    playerPosition.Y + 4f,
                    playerPosition.Z +
                        MathF.Sin(angle) * radius);


            _zone3OrbitCameraObject
                .Transform
                .TranslateTo(
                    cameraPosition);


            // Point toward the player.
            Vector3 direction =
                playerPosition -
                cameraPosition;

            direction.Normalize();

            float yaw =
                MathF.Atan2(
                    direction.X,
                    -direction.Z);

            float pitch =
                MathF.Asin(
                    direction.Y);


            Quaternion cameraRotation =
                        Quaternion.CreateFromYawPitchRoll(
                            yaw,
                            pitch,
                            0f);

            _zone3OrbitCameraObject
                .Transform
                .RotateToWorld(cameraRotation);
        }
        private void InitializeZone3CameraTriggers()
        {
            _zone3FirstPersonTrigger =
                CreateZone3CameraTrigger(
                    "Zone3 First Person Trigger",
                    new Vector3(
                        Zone3CenterX - 3.5f,
                        1f,
                        2f),
                    Zone3CameraMode.FirstPerson);


            _zone3OrbitTrigger =
                CreateZone3CameraTrigger(
                    "Zone3 Orbit Trigger",
                    new Vector3(
                        Zone3CenterX,
                        1f,
                        0f),
                    Zone3CameraMode.Orbit);


            _zone3CinematicTrigger =
                CreateZone3CameraTrigger(
                    "Zone3 Cinematic Trigger",
                    new Vector3(
                        Zone3CenterX + 3.5f,
                        1f,
                        -2f),
                    Zone3CameraMode.Cinematic);
        }
        private GameObject CreateZone3CameraTrigger(
                string name,
                Vector3 position,
                Zone3CameraMode mode)
        {
            GameObject trigger =
                new GameObject(name);

            trigger.Transform.TranslateTo(
                position);


            var collider =
                trigger.AddComponent<BoxCollider>();

            collider.Size =
                new Vector3(
                    2f,
                    2f,
                    2f);

            collider.Center =
                Vector3.Zero;

            collider.IsTrigger =
                true;


            var rigidBody =
                trigger.AddComponent<RigidBody>();

            rigidBody.BodyType =
                BodyType.Static;

            rigidBody.UseGravity =
                false;

            trigger.IsStatic =
                true;


            // Attach component that publishes event.
            var controller =
                trigger.AddComponent<
                    Zone3CameraTriggerController>();

            controller.Mode =
                mode;


            _sceneManager.ActiveScene.Add(
                trigger);

            return trigger;
        }
        private void InitializeZone3Events()
        {
            _zone3CameraEventSubscription =
                EngineContext.Instance.Events
                    .Subscribe<Zone3CameraTriggerEvent>(
                        OnZone3CameraTrigger);
        }
        private void OnZone3CameraTrigger(
                Zone3CameraTriggerEvent evt)
        {
            SwitchZone3Camera(
                evt.Mode);
        }
        private void SwitchZone3Camera(
                Zone3CameraMode mode)
        {
            Scene scene =
                _sceneManager.ActiveScene;

            _zone3CurrentMode =
                mode;


            switch (mode)
            {
                case Zone3CameraMode.FirstPerson:

                    if (_zone3FirstPersonCamera != null)
                    {
                        scene.ActiveCamera =
                            _zone3FirstPersonCamera;
                    }

                    break;


                case Zone3CameraMode.Orbit:

                    if (_zone3OrbitCamera != null)
                    {
                        scene.ActiveCamera =
                            _zone3OrbitCamera;
                    }

                    break;


                case Zone3CameraMode.Cinematic:

                    if (_zone3CinematicCamera != null)
                    {
                        scene.ActiveCamera =
                            _zone3CinematicCamera;
                    }

                    break;
            }


            System.Diagnostics.Debug.WriteLine(
                $"Zone 3 Camera Mode: {mode}");
        }
        private void InitializeZone3TriggerVisuals()
        {
            InitializeModel(
                new Vector3(
                    Zone3CenterX - 3.5f,
                    1f,
                    2f),
                Vector3.Zero,
                new Vector3(2f),
                Zone1Texture,
                Zone1CubeModel,
                "First Person Camera Zone");

            InitializeModel(
                new Vector3(
                    Zone3CenterX,
                    1f,
                    0f),
                Vector3.Zero,
                new Vector3(2f),
                Zone1Texture,
                Zone1CubeModel,
                "Orbit Camera Zone");

            InitializeModel(
                new Vector3(
                    Zone3CenterX + 3.5f,
                    1f,
                    -2f),
                Vector3.Zero,
                new Vector3(2f),
                Zone1Texture,
                Zone1CubeModel,
                "Cinematic Camera Zone");
        }
        private void InitializeZone4()
        {
            _zone4PreviousKeyboardState =
                Keyboard.GetState();

            _zone4StateRequested = false;
            _zone4Completed = false;

            InitializeZone4Room();

            InitializeZone4Button();

            InitializeZone4ImpulseObject();

            InitializeZone4EventSubscriptions();

            InitializeZone4ImpulseSubscription();

            InitializeZone4GameState();
        }
        private void InitializeZone4Room()
        {
            const float roomWidth = 12f;
            const float roomLength = 10f;
            const float roomHeight = 4f;
            const float wallThickness = 0.2f;

            float centerX = Zone4CenterX;


            // ============================
            // FLOOR PHYSICS
            // ============================

            GameObject floorPhysics =
                new GameObject("Zone4 Floor Physics");

            floorPhysics.Transform.TranslateTo(
                new Vector3(
                    centerX,
                    -0.25f,
                    0f));

            var floorCollider =
                floorPhysics.AddComponent<BoxCollider>();

            floorCollider.Size =
                new Vector3(
                    roomWidth,
                    0.5f,
                    roomLength);

            floorCollider.Center =
                Vector3.Zero;

            floorCollider.IsTrigger =
                false;

            var floorBody =
                floorPhysics.AddComponent<RigidBody>();

            floorBody.BodyType =
                BodyType.Static;

            floorBody.UseGravity =
                false;

            floorPhysics.IsStatic = true;

            _sceneManager.ActiveScene.Add(
                floorPhysics);


            // ============================
            // FLOOR VISUAL
            // ============================

            GameObject floorVisual =
                new GameObject("Zone4 Floor Visual");

            MeshFilter floorMesh =
                MeshFilterFactory.CreateQuadGridTexturedLit(
                    _graphics.GraphicsDevice,
                    1,
                    1,
                    roomWidth,
                    roomLength,
                    4f,
                    4f);

            floorVisual.AddComponent(
                floorMesh);

            MeshRenderer floorRenderer =
                floorVisual.AddComponent<MeshRenderer>();

            floorRenderer.Material =
                _matBasicUnlitGround;

            floorRenderer.Overrides.MainTexture =
                _textureDictionary.Get(
                    Zone1Texture);

            floorVisual.Transform.RotateEulerBy(
                new Vector3(
                    MathHelper.ToRadians(-90f),
                    0f,
                    0f));

            floorVisual.Transform.TranslateTo(
                new Vector3(
                    centerX,
                    0.01f,
                    0f));

            _sceneManager.ActiveScene.Add(
                floorVisual);


            // ============================
            // FRONT WALL
            // ============================

            CreateZone4StaticBox(
                "Zone4 Front Wall",
                new Vector3(
                    centerX,
                    roomHeight / 2f,
                    roomLength / 2f),
                new Vector3(
                    roomWidth,
                    roomHeight,
                    wallThickness));


            // ============================
            // BACK WALL
            // ============================

            CreateZone4StaticBox(
                "Zone4 Back Wall",
                new Vector3(
                    centerX,
                    roomHeight / 2f,
                    -roomLength / 2f),
                new Vector3(
                    roomWidth,
                    roomHeight,
                    wallThickness));


            // No left wall:
            // Zone 3 connects here.

            // No right collider:
            // Zone 5 will connect here.
        }
        private GameObject CreateZone4StaticBox(
                string name,
                Vector3 position,
                Vector3 size)
                    {
                        GameObject gameObject =
                            InitializeModel(
                                position,
                                Vector3.Zero,
                                size,
                                Zone1Texture,
                                Zone1CubeModel,
                                name);

                        var collider =
                            gameObject.AddComponent<BoxCollider>();

                        collider.Size =
                            size;

                        collider.Center =
                            Vector3.Zero;

                        var rigidBody =
                            gameObject.AddComponent<RigidBody>();

                        rigidBody.BodyType =
                            BodyType.Static;

                        rigidBody.UseGravity =
                            false;

                        gameObject.IsStatic =
                            true;

                        return gameObject;
                    }
        private void InitializeZone4ImpulseObject()
        {
            _zone4ImpulseObject =
                InitializeModel(
                    new Vector3(
                        Zone4CenterX,
                        1.5f,
                        -1.5f),
                    Vector3.Zero,
                    new Vector3(
                        1.5f,
                        1.5f,
                        1.5f),
                    "mona lisa",
                    Zone1CubeModel,
                    "Zone4 Impulse Display");
        }
        private void InitializeZone4EventSubscriptions()
        {
            var bus =
                EngineContext.Instance.Events;

            _zone4ButtonSubscription =
                bus.On<Zone4ButtonPressedEvent>()
                    .WithPriorityPreset(
                        EventPriority.Gameplay)
                    .Do(evt =>
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Zone4 EVENT 1 received: {evt.ButtonName}");

                        EngineContext.Instance.Impulses.Publish(
                            new Zone4PulseImpulse(
                                1.0f));

                        bus.Publish(
                            new Zone4StateRequestEvent(
                                "Player activated Zone 4 button"));
                    });

            _zone4StateSubscription =
                bus.On<Zone4StateRequestEvent>()
                    .WithPriorityPreset(
                        EventPriority.UI)
                    .Do(evt =>
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Zone4 EVENT 2 received: {evt.Reason}");

                        _zone4StateRequested = true;

                        bus.Publish(
                            new PlaySfxEvent(
                                "SFX_UI_Click_Designed_Pop_Generic_1",
                                1f,
                                false));
                    });
        }
        private void InitializeZone4ImpulseSubscription()
        {
            var impulseBus =
                EngineContext.Instance.Impulses;

            _zone4ImpulseSubscription =
            impulseBus
                .On<Zone4PulseImpulse>()
                .WithPriority(
                    ImpulsePriority.Gameplay)
                .Do(impulse =>
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Zone4 IMPULSE received. Strength={impulse.Strength}");

                    if (_zone4ImpulseObject == null)
                        return;

                    Vector3 current =
                        _zone4ImpulseObject.Transform.Position;

                    _zone4ImpulseObject.Transform.TranslateTo(
                        current +
                        Vector3.Up *
                        impulse.Strength);
                });
        }
        private void UpdateZone4Interaction()
        {
            if (_zone4Button == null)
                return;

            KeyboardState keyboard =
                Keyboard.GetState();


            GameObject player =
                _sceneManager.ActiveScene.Find(
                    gameObject =>
                        gameObject.Name ==
                        AppData.CAMERA_NAME_FIRST_PERSON_PARENT);


            if (player == null)
            {
                _zone4PreviousKeyboardState =
                    keyboard;

                return;
            }


            float distance =
                Vector3.Distance(
                    player.Transform.Position,
                    _zone4Button.Transform.Position);


            bool ePressed =
                keyboard.IsKeyDown(Keys.E) &&
                _zone4PreviousKeyboardState.IsKeyUp(Keys.E);


            if (distance <= 2.5f &&
                ePressed &&
                !_zone4Completed)
            {
                EngineContext.Instance.Events.Publish(
                    new Zone4ButtonPressedEvent(
                        "Zone4 Event Button"));
            }


            _zone4PreviousKeyboardState =
                keyboard;
        }
        private void InitializeZone4Button()
        {
            _zone4Button =
                InitializeModel(
                    new Vector3(
                        Zone4CenterX,
                        0.6f,
                        2.5f),
                    Vector3.Zero,
                    new Vector3(
                        1.2f,
                        1.2f,
                        1.2f),
                    Zone1Texture,
                    Zone1CubeModel,
                    "Zone4 Event Button");

            var collider =
                _zone4Button.AddComponent<BoxCollider>();

            collider.Size =
                new Vector3(
                    1.2f,
                    1.2f,
                    1.2f);

            collider.Center =
                Vector3.Zero;

            var rigidBody =
                _zone4Button.AddComponent<RigidBody>();

            rigidBody.BodyType =
                BodyType.Static;

            rigidBody.UseGravity =
                false;

            _zone4Button.IsStatic = true;
        }
        private void InitializeZone4GameState()
        {
            GameStateSystem gameStateSystem =
                _sceneManager.ActiveScene
                    .GetSystem<GameStateSystem>();

            if (gameStateSystem == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Zone4 ERROR: GameStateSystem not found.");

                return;
            }

            gameStateSystem.Reset();

            var winCondition =
                new Zone4StateCondition(
                    "Zone4 event chain completed",
                    () => _zone4StateRequested);

            gameStateSystem.ConfigureConditions(
                winCondition,
                null);

            _zone4GameWonSubscription =
                EngineContext.Instance.Events
                    .On<GameWonEvent>()
                    .WithPriorityPreset(
                        EventPriority.UI)
                    .Do(evt =>
                    {
                        _zone4Completed = true;

                        System.Diagnostics.Debug.WriteLine(
                            "ZONE 4 GAME STATE = WON");

                        EngineContext.Instance.Events.Publish(
                            new PlaySfxEvent(
                                "SFX_UI_Click_Designed_Pop_Generic_1",
                                1f,
                                false));

                        if (_zone4ImpulseObject != null)
                        {
                            _zone4ImpulseObject.Transform.ScaleTo(
                                new Vector3(
                                    3f,
                                    3f,
                                    3f));
                        }
                    });
        }
        private void InitializeZone5()
        {
            _zone5PreviousKeyboardState =
                Keyboard.GetState();

            InitializeZone5Room();

            InitializeZone5HUD();

            _zone5PreviousFovKeyboardState =
                        Keyboard.GetState();
        }
        private void InitializeZone5Room()
        {
            const float roomWidth = 12f;
            const float roomLength = 10f;
            const float roomHeight = 4f;
            const float wallThickness = 0.2f;

            float centerX = Zone5CenterX;


            // =====================================
            // FLOOR PHYSICS
            // =====================================

            GameObject floorPhysics =
                new GameObject("Zone5 Floor Physics");

            floorPhysics.Transform.TranslateTo(
                new Vector3(
                    centerX,
                    -0.25f,
                    0f));

            var floorCollider =
                floorPhysics.AddComponent<BoxCollider>();

            floorCollider.Size =
                new Vector3(
                    roomWidth,
                    0.5f,
                    roomLength);

            floorCollider.Center =
                Vector3.Zero;

            floorCollider.IsTrigger =
                false;

            var floorRigidBody =
                floorPhysics.AddComponent<RigidBody>();

            floorRigidBody.BodyType =
                BodyType.Static;

            floorRigidBody.UseGravity =
                false;

            floorPhysics.IsStatic = true;

            _sceneManager.ActiveScene.Add(
                floorPhysics);


            // =====================================
            // FLOOR VISUAL
            // =====================================

            GameObject floorVisual =
                new GameObject(
                    "Zone5 Floor Visual");

            MeshFilter floorMesh =
                MeshFilterFactory.CreateQuadGridTexturedLit(
                    _graphics.GraphicsDevice,
                    1,
                    1,
                    roomWidth,
                    roomLength,
                    4f,
                    4f);

            floorVisual.AddComponent(
                floorMesh);

            MeshRenderer floorRenderer =
                floorVisual.AddComponent<MeshRenderer>();

            floorRenderer.Material =
                _matBasicUnlitGround;

            floorRenderer.Overrides.MainTexture =
                _textureDictionary.Get(
                    Zone1Texture);

            floorVisual.Transform.RotateEulerBy(
                new Vector3(
                    MathHelper.ToRadians(-90f),
                    0f,
                    0f));

            floorVisual.Transform.TranslateTo(
                new Vector3(
                    centerX,
                    0.01f,
                    0f));

            _sceneManager.ActiveScene.Add(
                floorVisual);


            // =====================================
            // FRONT WALL
            // =====================================

            CreateZone5StaticBox(
                "Zone5 Front Wall",
                new Vector3(
                    centerX,
                    roomHeight / 2f,
                    roomLength / 2f),
                new Vector3(
                    roomWidth,
                    roomHeight,
                    wallThickness));


            // =====================================
            // BACK WALL
            // =====================================

            CreateZone5StaticBox(
                "Zone5 Back Wall",
                new Vector3(
                    centerX,
                    roomHeight / 2f,
                    -roomLength / 2f),
                new Vector3(
                    roomWidth,
                    roomHeight,
                    wallThickness));


            // =====================================
            // RIGHT WALL
            // Zone 5 is the final room.
            // =====================================

            CreateZone5StaticBox(
                "Zone5 Right Wall",
                new Vector3(
                    centerX + roomWidth / 2f,
                    roomHeight / 2f,
                    0f),
                new Vector3(
                    wallThickness,
                    roomHeight,
                    roomLength));

            // NO left wall.
            // Zone 4 connects directly into Zone 5.
        }
        private GameObject CreateZone5StaticBox(
                string name,
                Vector3 position,
                Vector3 size)
        {
            GameObject gameObject =
                InitializeModel(
                    position,
                    Vector3.Zero,
                    size,
                    Zone1Texture,
                    Zone1CubeModel,
                    name);

            var collider =
                gameObject.AddComponent<BoxCollider>();

            collider.Size = size;
            collider.Center = Vector3.Zero;

            var rigidBody =
                gameObject.AddComponent<RigidBody>();

            rigidBody.BodyType =
                BodyType.Static;

            rigidBody.UseGravity =
                false;

            gameObject.IsStatic = true;

            return gameObject;
        }
        private void InitializeZone5HUD()
        {
            InitializeZone5TeleportButtons();

            InitializeZone5LiveStats();

            InitializeZone5UIButton();

            InitializeZone5FovSlider();

            InitializeZoneInformationHUD();
        }
        private void InitializeZone5TeleportButtons()
        {
            float buttonWidth = 220f;
            float buttonHeight = 42f;
            float gap = 10f;

            float totalWidth =
                buttonWidth * 5f +
                gap * 4f;

            float startX =
                (_graphics.PreferredBackBufferWidth -
                 totalWidth) * 0.5f;

            float y = 15f;

            CreateZone5TeleportUIButton(
                "1 - PHYSICS",
                1,
                new Vector2(startX, y),
                new Vector2(buttonWidth, buttonHeight));

            CreateZone5TeleportUIButton(
                "2 - AUDIO",
                2,
                new Vector2(
                    startX + (buttonWidth + gap),
                    y),
                new Vector2(buttonWidth, buttonHeight));

            CreateZone5TeleportUIButton(
                "3 - CAMERA",
                3,
                new Vector2(
                    startX + (buttonWidth + gap) * 2f,
                    y),
                new Vector2(buttonWidth, buttonHeight));

            CreateZone5TeleportUIButton(
                "4 - EVENTS",
                4,
                new Vector2(
                    startX + (buttonWidth + gap) * 3f,
                    y),
                new Vector2(buttonWidth, buttonHeight));

            CreateZone5TeleportUIButton(
                "5 - MAIN MENU",
                5,
                new Vector2(
                    startX + (buttonWidth + gap) * 4f,
                    y),
                new Vector2(buttonWidth, buttonHeight));
        }
        private void CreateZone5TeleportUIButton(
                    string label,
                    int zoneNumber,
                    Vector2 position,
                    Vector2 size)
        {
            GameObject buttonGO =
                new GameObject(
                    $"Zone5 UI Button {zoneNumber}");

            // =========================
            // IMAGE/BACKGROUND
            // =========================

            UITexture background =
                buttonGO.AddComponent<UITexture>();

            background.Texture =
                _textureDictionary.Get(
                    "button_rectangle_10");

            background.Position =
                position;

            background.Size =
                size;

            background.Tint =
                new Color(
                    255,
                    255,
                    255,
                    220);

            background.LayerDepth =
                UILayer.HUD;


            // =========================
            // ACTUAL ENGINE UI BUTTON
            // =========================

            UIButton button =
                buttonGO.AddComponent<UIButton>();

            button.Position =
                position;

            button.Size =
                size;

            button.TargetGraphic =
                background;

            button.Interactable =
                true;

            button.Clicked += () =>
            {
                TeleportPlayerToZone(
                    zoneNumber);
            };


            // =========================
            // TEXT
            // =========================

            UIText text =
                buttonGO.AddComponent<UIText>();

            text.Font =
                _fontDictionary.Get(
                    "menufont");

            text.TextProvider =
                () => label;

            text.PositionProvider =
                () =>
                    position +
                    size * 0.5f;

            text.Anchor =
                TextAnchor.Center;

            text.FallbackColor =
                Color.White;

            text.UniformScale =
                0.4f;

            text.LayerDepth =
                UILayer.HUD;

            _sceneManager.ActiveScene.Add(
                buttonGO);
        }
        private void InitializeZone5LiveStats()
        {
            SpriteFont font =
                _fontDictionary.Get(
                    "menufont");

            // =========================
            // CAMERA POSITION
            // =========================

            GameObject cameraTextGO =
                new GameObject(
                    "Zone5 HUD Camera Position");

            _zone5CameraPositionText =
                cameraTextGO.AddComponent<UIText>();

            _zone5CameraPositionText.Font =
                font;

            _zone5CameraPositionText.TextProvider =
                () =>
                {
                    Camera camera =
                        _sceneManager
                            .ActiveScene
                            .ActiveCamera;

                    if (camera == null)
                        return "Camera: unavailable";

                    Vector3 p =
                        camera.Transform.Position;

                    return
                        $"Camera: X={p.X:F1}  Y={p.Y:F1}  Z={p.Z:F1}";
                };

            _zone5CameraPositionText.PositionProvider =
                () => new Vector2(
                    25f,
                    90f);

            _zone5CameraPositionText.FallbackColor =
                Color.White;

            _zone5CameraPositionText.UniformScale =
                0.4f;

            _sceneManager.ActiveScene.Add(
                cameraTextGO);


            // =========================
            // PLAYER VELOCITY
            // =========================

            GameObject velocityTextGO =
                new GameObject(
                    "Zone5 HUD Player Velocity");

            _zone5VelocityText =
                velocityTextGO.AddComponent<UIText>();

            _zone5VelocityText.Font =
                font;

            _zone5VelocityText.TextProvider =
                () =>
                {
                    GameObject player =
                        GetFirstPersonPlayer();

                    if (player == null)
                        return "Velocity: player unavailable";

                    RigidBody rb =
                        player.GetComponent<RigidBody>();

                    if (rb == null)
                        return "Velocity: rigid body unavailable";

                    Vector3 v =
                        rb.LinearVelocity;

                    return
                        $"Velocity: X={v.X:F2}  Y={v.Y:F2}  Z={v.Z:F2}";
                };

            _zone5VelocityText.PositionProvider =
                () => new Vector2(
                    25f,
                    120f);

            _zone5VelocityText.FallbackColor =
                Color.White;

            _zone5VelocityText.UniformScale =
                0.4f;

            _sceneManager.ActiveScene.Add(
                velocityTextGO);


            // =========================
            // ELAPSED TIME
            // =========================

            GameObject timeTextGO =
                new GameObject(
                    "Zone5 HUD Elapsed Time");

            _zone5ElapsedTimeText =
                timeTextGO.AddComponent<UIText>();

            _zone5ElapsedTimeText.Font =
                font;

            _zone5ElapsedTimeText.TextProvider =
                () =>
                    $"Elapsed Time: {Time.TimeSinceStartupSecs:F1} s";

            _zone5ElapsedTimeText.PositionProvider =
                () => new Vector2(
                    25f,
                    150f);

            _zone5ElapsedTimeText.FallbackColor =
                Color.White;

            _zone5ElapsedTimeText.UniformScale =
                0.4f;

            _sceneManager.ActiveScene.Add(
                timeTextGO);
        }
        private void InitializeZone5UIButton()
        {
            Vector2 position =
                new Vector2(
                    25f,
                    210f);

            Vector2 size =
                new Vector2(
                    250f,
                    48f);

            GameObject buttonGO =
                new GameObject(
                    "Zone5 Return Button");

            UITexture background =
                buttonGO.AddComponent<UITexture>();

            background.Texture =
                _textureDictionary.Get(
                    "button_rectangle_10");

            background.Position =
                position;

            background.Size =
                size;

            background.Tint =
                Color.White;


            _zone5ResetButton =
                buttonGO.AddComponent<UIButton>();

            _zone5ResetButton.Position =
                position;

            _zone5ResetButton.Size =
                size;

            _zone5ResetButton.TargetGraphic =
                background;

            _zone5ResetButton.Interactable =
                true;

            _zone5ResetButton.Clicked += () =>
            {
                TeleportPlayerToZone(5);

                EngineContext.Instance.Events.Publish(
                    new PlaySfxEvent(
                        "SFX_UI_Click_Designed_Pop_Generic_1",
                        1f,
                        false));
            };


            UIText label =
                buttonGO.AddComponent<UIText>();

            label.Font =
                _fontDictionary.Get(
                    "menufont");

            label.TextProvider =
                () => "RETURN TO ZONE 5";

            label.PositionProvider =
                () =>
                    position +
                    size * 0.5f;

            label.Anchor =
                TextAnchor.Center;

            label.FallbackColor =
                Color.White;

            label.UniformScale =
                0.7f;

            _sceneManager.ActiveScene.Add(
                buttonGO);
        }
        private void InitializeZone5FovSlider()
        {
            Vector2 trackPosition =
                new Vector2(
                    25f,
                    320f);

            Vector2 trackSize =
                new Vector2(
                    350f,
                    24f);


            // =========================
            // LABEL
            // =========================

            GameObject labelGO =
                new GameObject(
                    "Zone5 FOV Label");

            _zone5FovText =
                labelGO.AddComponent<UIText>();

            _zone5FovText.Font =
                _fontDictionary.Get(
                    "menufont");

            _zone5FovText.TextProvider =
                        () =>
                            $"Camera FOV: {_zone5FovSlider?.Value:F0} degrees   [Z -] [X +]";

            _zone5FovText.PositionProvider =
                () => new Vector2(
                    25f,
                    285f);

            _zone5FovText.FallbackColor =
                Color.White;

            _zone5FovText.UniformScale =
                0.4f;

            _sceneManager.ActiveScene.Add(
                labelGO);


            // =========================
            // SLIDER TRACK
            // =========================

            GameObject sliderGO =
                new GameObject(
                    "Zone5 FOV Slider");

            _zone5FovTrack =
                sliderGO.AddComponent<UITexture>();

            _zone5FovTrack.Texture =
                _textureDictionary.Get(
                    "white_1x1");

            _zone5FovTrack.Position =
                trackPosition;

            _zone5FovTrack.Size =
                trackSize;

            _zone5FovTrack.Tint =
                new Color(
                    160,
                    160,
                    160,
                    255);


            // =========================
            // HANDLE
            // =========================

            GameObject handleGO =
                new GameObject(
                    "Zone5 FOV Slider Handle");

            _zone5FovHandle =
                handleGO.AddComponent<UITexture>();

            _zone5FovHandle.Texture =
                _textureDictionary.Get(
                    "Free Flat Toggle Thumb Centre Icon");

            _zone5FovHandle.Size =
                new Vector2(
                    28f,
                    38f);

            _sceneManager.ActiveScene.Add(
                handleGO);


            // =========================
            // SLIDER COMPONENT
            // =========================

            _zone5FovSlider =
                sliderGO.AddComponent<UISlider>();

            _zone5FovSlider.Position =
                trackPosition;

            _zone5FovSlider.Size =
                trackSize;

            _zone5FovSlider.TargetGraphic =
                _zone5FovTrack;

            _zone5FovSlider.HandleGraphic =
                _zone5FovHandle;

            _zone5FovSlider.MinValue =
                50f;

            _zone5FovSlider.MaxValue =
                110f;

            _zone5FovSlider.Value =
                80f;

            _zone5FovSlider.WholeNumbers =
                true;

            _zone5FovSlider.Interactable =
                true;

            _zone5FovSlider.ValueChanged +=
                value =>
                {
                    Camera camera =
                        _sceneManager
                            .ActiveScene
                            .ActiveCamera;

                    if (camera == null)
                        return;

                    camera.FieldOfView =
                        MathHelper.ToRadians(
                            value);
                };

            _sceneManager.ActiveScene.Add(
                sliderGO);
        }
        private GameObject GetFirstPersonPlayer()
        {
            return _sceneManager
                .ActiveScene
                .Find(
                    gameObject =>
                        gameObject.Name ==
                        AppData.CAMERA_NAME_FIRST_PERSON_PARENT);
        }
        private void TeleportPlayerToZone(
                    int zoneNumber)
        {
            GameObject player =
                GetFirstPersonPlayer();

            if (player == null)
                return;

            float centerX;

            switch (zoneNumber)
            {
                case 1:
                    centerX = 0f;
                    break;

                case 2:
                    centerX = Zone2CenterX;
                    break;

                case 3:
                    centerX = Zone3CenterX;
                    break;

                case 4:
                    centerX = 36f;
                    break;

                case 5:
                    centerX = Zone5CenterX;
                    break;

                default:
                    return;
            }

            Vector3 spawnPosition =
                new Vector3(
                    centerX,
                    1.5f,
                    4f);


            RigidBody rb =
                player.GetComponent<RigidBody>();

            if (rb != null)
            {
                // Stop old movement.
                rb.LinearVelocity =
                    Vector3.Zero;

                rb.AngularVelocity =
                    Vector3.Zero;

                // Temporarily allow Transform -> Physics sync.
                rb.BodyType =
                    BodyType.Kinematic;

                player.Transform.TranslateTo(
                    spawnPosition);

                // Return player to normal FPS physics.
                rb.BodyType =
                    BodyType.Dynamic;

                rb.FreezeRotation =
                    true;

                rb.LinearVelocity =
                    Vector3.Zero;

                rb.AngularVelocity =
                    Vector3.Zero;
            }
            else
            {
                player.Transform.TranslateTo(
                    spawnPosition);
            }

            // Always restore FPS camera when teleporting.
            if (_zone3FirstPersonCamera != null)
            {
                _sceneManager
                    .ActiveScene
                    .ActiveCamera =
                    _zone3FirstPersonCamera;
            }

            _zone3CurrentMode =
                Zone3CameraMode.FirstPerson;

            _zone5CurrentZone =
                zoneNumber;

            UpdateZoneInformationHUD(
                zoneNumber);

            System.Diagnostics.Debug.WriteLine(
                $"Teleported to Zone {zoneNumber}: {spawnPosition}");
        }
        private void UpdateZone5TeleportKeys()
        {
            KeyboardState keyboard =
                Keyboard.GetState();

            if (keyboard.IsKeyDown(Keys.D1) &&
                _zone5PreviousKeyboardState.IsKeyUp(Keys.D1))
            {
                TeleportPlayerToZone(1);
            }

            if (keyboard.IsKeyDown(Keys.D2) &&
                _zone5PreviousKeyboardState.IsKeyUp(Keys.D2))
            {
                TeleportPlayerToZone(2);
            }

            if (keyboard.IsKeyDown(Keys.D3) &&
                _zone5PreviousKeyboardState.IsKeyUp(Keys.D3))
            {
                TeleportPlayerToZone(3);
            }

            if (keyboard.IsKeyDown(Keys.D4) &&
                _zone5PreviousKeyboardState.IsKeyUp(Keys.D4))
            {
                TeleportPlayerToZone(4);
            }

            if (keyboard.IsKeyDown(Keys.D5) &&
                _zone5PreviousKeyboardState.IsKeyUp(Keys.D5))
            {
                TeleportPlayerToZone(5);
            }

            _zone5PreviousKeyboardState =
                keyboard;
        }
        private void UpdateZone5UIMode()
        {
            GameObject player =
                GetFirstPersonPlayer();

            if (player == null)
                return;

            float minX =
                Zone5CenterX - 6f;

            float maxX =
                Zone5CenterX + 6f;

            bool insideZone5 =
                player.Transform.Position.X >= minX &&
                player.Transform.Position.X <= maxX;

            IsMouseVisible =
                insideZone5;
        }
        private void InitializeZoneInformationHUD()
        {
            SpriteFont font =
                _fontDictionary.Get("menufont");

            // 1. ZONE NAME

            GameObject zoneNameGO =
                new GameObject("HUD Current Zone Name");

            _zoneInfoNameText =
                zoneNameGO.AddComponent<UIText>();

            _zoneInfoNameText.Font =
                font;

            _zoneInfoNameText.TextProvider =
                () => _zoneInfoName;

            _zoneInfoNameText.PositionProvider =
                () =>
                {
                    Viewport viewport =
                        _graphics.GraphicsDevice.Viewport;

                    return new Vector2(
                        viewport.Width / 2f,
                        viewport.Height - 115f);
                };

            _zoneInfoNameText.Anchor =
                TextAnchor.Center;

            _zoneInfoNameText.FallbackColor =
                Color.White;

            _zoneInfoNameText.UniformScale =
                0.3f;

            _sceneManager.ActiveScene.Add(
                zoneNameGO);


            // =====================================================
            // 2. WHAT THE ZONE DEMONSTRATES
            // =====================================================

            GameObject simulationGO =
                new GameObject("HUD Zone Simulation Description");

            _zoneInfoSimulationText =
                simulationGO.AddComponent<UIText>();

            _zoneInfoSimulationText.Font =
                font;

            _zoneInfoSimulationText.TextProvider =
                () => _zoneInfoSimulation;

            _zoneInfoSimulationText.PositionProvider =
                () =>
                {
                    Viewport viewport =
                        _graphics.GraphicsDevice.Viewport;

                    return new Vector2(
                        viewport.Width / 2f,
                        viewport.Height - 80f);
                };

            _zoneInfoSimulationText.Anchor =
                TextAnchor.Center;

            _zoneInfoSimulationText.FallbackColor =
                Color.White;

            _zoneInfoSimulationText.UniformScale =
                0.3f;

            _sceneManager.ActiveScene.Add(
                simulationGO);


            // =====================================================
            // 3. PLAYER INSTRUCTION
            // =====================================================

            GameObject actionGO =
                new GameObject("HUD Zone Player Instruction");

            _zoneInfoActionText =
                actionGO.AddComponent<UIText>();

            _zoneInfoActionText.Font =
                font;

            _zoneInfoActionText.TextProvider =
                () => _zoneInfoAction;

            _zoneInfoActionText.PositionProvider =
                () =>
                {
                    Viewport viewport =
                        _graphics.GraphicsDevice.Viewport;

                    return new Vector2(
                        viewport.Width / 2f,
                        viewport.Height - 45f);
                };

            _zoneInfoActionText.Anchor =
                TextAnchor.Center;

            _zoneInfoActionText.FallbackColor =
                Color.White;

            _zoneInfoActionText.UniformScale =
                0.3f;

            _sceneManager.ActiveScene.Add(
                actionGO);


            // Game normally begins in Zone 1.
            UpdateZoneInformationHUD(1);
        }
        private void UpdateZoneInformationHUD(
                    int zoneNumber)
                    {
                        switch (zoneNumber)
                        {
                            // =================================================
                            // ZONE 1
                            // =================================================

                            case 1:

                                _zoneInfoName =
                                    "ZONE 1 - PHYSICS";

                                _zoneInfoSimulation =
                                    "Simulation: RigidBody physics, gravity, colliders and object-to-object collision.";

                                _zoneInfoAction =
                                    "Action: Approach the button and press E to release Monkey 2 onto the ramp.";

                                break;


                            // =================================================
                            // ZONE 2
                            // =================================================

                            case 2:

                                _zoneInfoName =
                                    "ZONE 2 - AUDIO";

                                _zoneInfoSimulation =
                                    "Simulation: 3D spatial audio, positional attenuation, music switching and EventBus SFX.";

                                _zoneInfoAction =
                                    "Action: Move between both sound sources, then approach the audio button and press E.";

                                break;


                            // =================================================
                            // ZONE 3
                            // =================================================

                            case 3:

                                _zoneInfoName =
                                    "ZONE 3 - CAMERA SYSTEM";

                                _zoneInfoSimulation =
                                    "Simulation: First-person, orbit and cinematic camera modes.";

                                _zoneInfoAction =
                                    "Action: Walk through the camera trigger volumes to switch between camera modes.";

                                break;


                            // =================================================
                            // ZONE 4
                            // =================================================

                            case 4:

                                _zoneInfoName =
                                    "ZONE 4 - EVENTS AND GAME STATE";

                                _zoneInfoSimulation =
                                    "Simulation: EventBus, ImpulseBus, event priorities and GameStateSystem.";

                                _zoneInfoAction =
                                    "Action: Approach the event button and press E to trigger the event and impulse chain.";

                                break;


                            // =================================================
                            // ZONE 5
                            // =================================================

                            case 5:

                                _zoneInfoName =
                                    "ZONE 5 - MAIN MENU AND UI";

                                _zoneInfoSimulation =
                                    "Simulation: Live HUD, UIButton, UISlider and zone teleportation.";

                                _zoneInfoAction =
                                    "Action: Press 1-5 or use the UI buttons to teleport. Drag the FOV slider to change the camera.";

                                break;


                            default:

                                _zoneInfoName =
                                    "UNKNOWN ZONE";

                                _zoneInfoSimulation =
                                    "";

                                _zoneInfoAction =
                                    "";

                                break;
                        }
                    }
        private void UpdateZone5FovKeys()
        {
            if (_zone5FovSlider == null)
                return;

            KeyboardState keyboard =
                Keyboard.GetState();

            bool zPressed =
                keyboard.IsKeyDown(Keys.Z) &&
                _zone5PreviousFovKeyboardState.IsKeyUp(Keys.Z);

            bool xPressed =
                keyboard.IsKeyDown(Keys.X) &&
                _zone5PreviousFovKeyboardState.IsKeyUp(Keys.X);

            const float fovStep = 5f;

            // Z = decrease FOV
            if (zPressed)
            {
                _zone5FovSlider.Value =
                    MathHelper.Clamp(
                        _zone5FovSlider.Value - fovStep,
                        _zone5FovSlider.MinValue,
                        _zone5FovSlider.MaxValue);
            }

            // X = increase FOV
            if (xPressed)
            {
                _zone5FovSlider.Value =
                    MathHelper.Clamp(
                        _zone5FovSlider.Value + fovStep,
                        _zone5FovSlider.MinValue,
                        _zone5FovSlider.MaxValue);
            }

            _zone5PreviousFovKeyboardState =
                keyboard;
        }





        private void SetPauseShowMenu()
        {
            _sceneManager.EventBus = EngineContext.Instance.Events;

            // Start the game unpaused
            _sceneManager.Paused = false;

            EngineContext.Instance.Events.Subscribe<GamePauseChangedEvent>(e =>
            {
                bool paused = e.IsPaused;

                _sceneManager.ActiveScene
                    .GetSystem<PhysicsSystem>()?
                    .SetPaused(paused);

                _sceneManager.ActiveScene
                    .GetSystem<PhysicsDebugSystem>()?
                    .SetPaused(paused);

                _sceneManager.ActiveScene
                    .GetSystem<GameStateSystem>()?
                    .SetPaused(paused);
            });
        }

        private void InitializeSceneManager()
        {
            _sceneManager = new SceneManager(this);
            Components.Add(_sceneManager);
        }

        private void InitializeCameraManagers()
        {
            //inside scene
            var go = new GameObject("Camera Manager");
            go.AddComponent<CameraEventListener>();
            _sceneManager.ActiveScene.Add(go);
        }    

        private void InitializePlayer()
        {
            GameObject player = InitializeModel(new Vector3(0, 5, 10),
                new Vector3(0, 0, 0),
                2 * Vector3.One, "crate1", "monkey1", AppData.PLAYER_NAME);

            var simpleDriveController = new SimpleDriveController();
            player.AddComponent(simpleDriveController);

            // Listen for damage events on the player
            player.AddComponent<DamageEventListener>();

            // Adds an inventory to the player
            player.AddComponent<InventoryComponent>();
        }

        private void InitializePIPCamera(Vector3 position,
      Viewport viewport, int depth, int index = 0)
        {
            var pipCameraGO = new GameObject("PIP camera");
            pipCameraGO.Transform.TranslateTo(position);
            pipCameraGO.Transform.RotateEulerBy(new Vector3(0, MathHelper.ToRadians(-90), 0));

            //if (index == 0)
            //{
            //    pipCameraGO.AddComponent<KeyboardWASDController>();
            //    pipCameraGO.AddComponent<MouseYawPitchController>();
            //}

            var camera = pipCameraGO.AddComponent<Camera>();
            camera.StackRole = Camera.StackType.Overlay;
            camera.ClearFlags = Camera.ClearFlagsType.DepthOnly;
            camera.Depth = depth; //-100

            camera.Viewport = viewport; // new Viewport(0, 0, 400, 300);

            _sceneManager.ActiveScene.Add(pipCameraGO);
        }

        private void InitializeGraphics(Integer2 resolution)
        {
            // Enable per-monitor DPI awareness so the window/UI scales crisply on multi-monitor setups with different DPIs (avoids blurriness when moving between screens).
            System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.PerMonitorV2);

            // Set preferred resolution
            ScreenResolution.SetResolution(_graphics, resolution);

            // Center on primary display (set to index of the preferred monitor)
            WindowUtility.CenterOnMonitor(this, 1);
        }

        private void InitializeMouse()
        {
            Mouse.SetPosition(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);
        }

        private void InitializeContext()
        {
            EngineContext.Initialize(GraphicsDevice, Content);
        }

        /// <summary>
        /// New asset loading from JSON using AssetEntry and ContentDictionary::LoadFromManifest
        /// </summary>
        /// <param name="relativeFilePathAndName"></param>
        /// <see cref="AssetEntry"/>
        /// <see cref="ContentDictionary{T}"/>
        private void LoadAssetsFromJSON(string relativeFilePathAndName)
        {
            // Make dictionaries to store assets
            _textureDictionary = new ContentDictionary<Texture2D>();
            _modelDictionary = new ContentDictionary<Model>();
            _fontDictionary = new ContentDictionary<SpriteFont>();
            _soundDictionary = new ContentDictionary<SoundEffect>();
            _effectsDictionary = new ContentDictionary<Effect>();
            //TODO - Add dictionary loading for other assets - song, other?

            var manifests = JSONSerializationUtility.LoadData<AssetManifest>(Content, relativeFilePathAndName); // single or array
            if (manifests.Count > 0)
            {
                foreach (var m in manifests)
                {
                    _modelDictionary.LoadFromManifest(m.Models, e => e.Name, e => e.ContentPath, overwrite: true);
                    _textureDictionary.LoadFromManifest(m.Textures, e => e.Name, e => e.ContentPath, overwrite: true);
                    _fontDictionary.LoadFromManifest(m.Fonts, e => e.Name, e => e.ContentPath, overwrite: true);
                    _soundDictionary.LoadFromManifest(m.Sounds, e => e.Name, e => e.ContentPath, overwrite: true);
                    _effectsDictionary.LoadFromManifest(m.Effects, e => e.Name, e => e.ContentPath, overwrite: true);
                    //TODO - Add dictionary loading for other assets - song, other?
                }
            }
        }

        private void InitializeEffects()
        {
            #region Unlit Textured BasicEffect 
            var unlitBasicEffect = new BasicEffect(_graphics.GraphicsDevice)
            {
                TextureEnabled = true,
                LightingEnabled = false,
                VertexColorEnabled = false
            };
   
            _matBasicUnlit = new Material(unlitBasicEffect);
            _matBasicUnlit.StateBlock = RenderStates.Opaque3D();      // depth on, cull CCW
            _matBasicUnlit.SamplerState = SamplerState.LinearClamp;   // helps avoid texture seams on sky

            //ground texture where UVs above [0,0]-[1,1]
            _matBasicUnlitGround = new Material(unlitBasicEffect.Clone());
            _matBasicUnlitGround.StateBlock = RenderStates.Opaque3D();      // depth on, cull CCW
            _matBasicUnlitGround.SamplerState = SamplerState.AnisotropicWrap;   // wrap texture based on UV values

            #endregion

            #region Lit Textured BasicEffect 
            var litBasicEffect = new BasicEffect(_graphics.GraphicsDevice)
            {
                TextureEnabled = true,
                LightingEnabled = true,
                PreferPerPixelLighting = true,
                VertexColorEnabled = false
            };
            litBasicEffect.EnableDefaultLighting();
            //litBasicEffect.AmbientLightColor = Color.Red.ToVector3();
            //litBasicEffect.EmissiveColor = Color.Green.ToVector3();
            //litBasicEffect.FogEnabled = true;
            //litBasicEffect.FogColor = Color.LightGray.ToVector3();
            //litBasicEffect.FogStart = 1;
            //litBasicEffect.FogEnd = 100;
            //litBasicEffect.SpecularPower = 8;  //int, power of 2, 1, 2, 4, 8
            //litBasicEffect.SpecularColor = Color.Yellow.ToVector3();
            _matBasicLit = new Material(litBasicEffect);  
            _matBasicLit.StateBlock = RenderStates.Opaque3D();

            #endregion

            #region Alpha-test for foliage/billboards
            var alphaFx = new AlphaTestEffect(GraphicsDevice)
            {
                VertexColorEnabled = false
            };
            _matAlphaCutout = new Material(alphaFx);

            // Depth test/write on; no blending (cutout happens in the effect). 
            // Make it two-sided so the quad is visible from both sides.
            _matAlphaCutout.StateBlock = RenderStates.Cutout3D()
                .WithRaster(new RasterizerState { CullMode = CullMode.None });

            // Clamp avoids edge bleeding from transparent borders.
            // (Use LinearWrap if the foliage textures tile.)
            _matAlphaCutout.SamplerState = SamplerState.LinearClamp;

            #endregion

            //#region Lit PBR Effect
            //// Load effect file
            //Effect pbrEffect = _effectsDictionary.Get("pbr_effect");

            //// Create a PBR material
            //_matPBR = new PBRMaterial(pbrEffect, ownsEffect: false);
            //#endregion
        }

        private void InitializeScene()
        {
            // Make a scene that will store all drawn objects and systems for that level
            var scene = new Scene(EngineContext.Instance, "outdoors - level 1");

            // Add each new scene into the manager
            _sceneManager.AddScene(AppData.LEVEL_1_NAME, scene);

            // Set the active scene before anything that uses ActiveScene
            _sceneManager.SetActiveScene(AppData.LEVEL_1_NAME);
        }

        private void InitializeSystems()
        {
            InitializePhysicsSystem();
            InitializePhysicsDebugSystem(true);
            InitializeEventSystem();  //propagate events  
            InitializeInputSystem();  //input
            InitializeCameraAndRenderSystems(); //update cameras, draw renderable game objects, draw ui and menu
            InitializeAudioSystem();
            InitializeOrchestrationSystem(false); //show debugger
            InitializeImpulseSystem();    //camera shake, audio duck volumes etc
            InitializeUIEventSystem();
            InitializeGameStateSystem();   //manage and track game state
                                           //  InitializeNavMeshSystem();

            InitializeDebugInfo(true);
        }

        private void InitializeDebugInfo(bool showDebug)
        {
            if (showDebug)
            {
                GameObject debugGO = new GameObject("Perf Stats");
                _debugRenderer = debugGO.AddComponent<UIDebugInfo>();

                _debugRenderer.Font = _fontDictionary.Get("perf_stats_font");
                _debugRenderer.ScreenCorner = ScreenCorner.TopLeft;
                _debugRenderer.Margin = new Vector2(10f, 10f);

                var perfProvider = new PerformanceDebugInfoProvider
                {
                    Profile = DisplayProfile.Profiling,
                    ShowMemoryStats = true
                };

                //add memory related info
                _debugRenderer.Providers.Add(perfProvider);

                //add scene related info
                _debugRenderer.Providers.Add(_sceneManager);

                _sceneManager.ActiveScene.Add(debugGO);
            }
        }

        private void InitializeNavMeshSystem()
        {
            var scene = _sceneManager.ActiveScene;

            // Core navmesh system (implements INavigationService)
            var navMeshSystem = scene.AddSystem(new NavMeshSystem());

            // Debug overlay (F2 toggle)
            scene.Add(new NavMeshDebugSystem());
        }

        private void InitializeGameStateSystem()
        {
            // Add game state system
            _sceneManager.ActiveScene.AddSystem(new GameStateSystem());
        }

        private void InitializeUIEventSystem()
        {
            _sceneManager.ActiveScene.AddSystem(new UIEventSystem());
        }

        private void InitializeImpulseSystem()
        {
            _sceneManager.ActiveScene.Add(new ImpulseSystem(EngineContext.Instance.Impulses));
        }

        private void InitializeOrchestrationSystem(bool debugEnabled)
        {
            var orchestrationSystem = new OrchestrationSystem();
            orchestrationSystem.Configure(options =>
            {
                options.Time = Orchestrator.OrchestrationTime.Unscaled;
                options.LocalScale = 1;
                options.Paused = false;
            });
            _sceneManager.ActiveScene.Add(orchestrationSystem);

            // Debugger
            if (debugEnabled)
            {
                GameObject debugGO = new GameObject("Perf Stats");
                var _debugRenderer = debugGO.AddComponent<UIDebugInfo>();

                _debugRenderer.Font = _fontDictionary.Get("perf_stats_font");
                _debugRenderer.ScreenCorner = ScreenCorner.TopLeft;
                _debugRenderer.Margin = new Vector2(10f, 10f);

                // Register orchestration as a debug provider
                if (orchestrationSystem != null)
                    _debugRenderer.Providers.Add(orchestrationSystem);

                var perfProvider = new PerformanceDebugInfoProvider
                {
                    Profile = DisplayProfile.Profiling,
                    ShowMemoryStats = true
                };

                _debugRenderer.Providers.Add(perfProvider);

                _sceneManager.ActiveScene.Add(debugGO);
            }

        }

        private void InitializeAudioSystem()
        {
            _sceneManager.ActiveScene.Add(new AudioSystem(_soundDictionary));
        }

        private void InitializePhysicsDebugSystem(bool isEnabled)
        {
            if (isEnabled)
            {
                var physicsDebugRenderer = _sceneManager.ActiveScene.AddSystem(new PhysicsDebugSystem());

                // Toggle debug rendering on/off
                physicsDebugRenderer.Enabled = isEnabled; // or false to hide

                // Optional: Customize colors
                physicsDebugRenderer.StaticColor = Color.Green;      // Immovable objects
                physicsDebugRenderer.KinematicColor = Color.Blue;    // Animated objects
                physicsDebugRenderer.DynamicColor = Color.Yellow;    // Physics-driven objects
                physicsDebugRenderer.TriggerColor = Color.Red;       // Trigger volumes

            }

        }

        private void InitializePhysicsSystem()
        {
            // 1. add physics
            var physicsSystem = _sceneManager.ActiveScene.AddSystem(new PhysicsSystem());
            physicsSystem.Gravity = AppData.GRAVITY;
        }

        private void InitializeEventSystem()
        {
            _sceneManager.ActiveScene.Add(new EventSystem(EngineContext.Instance.Events));
        }

        private void InitializeCameraAndRenderSystems()
        {
            //manages camera
            var cameraSystem = new CameraSystem(_graphics.GraphicsDevice, -100);
            _sceneManager.ActiveScene.Add(cameraSystem);

            //3d
            var renderSystem = new RenderSystem(-100);
            _sceneManager.ActiveScene.Add(renderSystem);

            //2d
            var uiRenderSystem = new UIRenderSystem(-100);
            _sceneManager.ActiveScene.Add(uiRenderSystem); // draws in PostRender after RenderingSystem (order = -100)
        }

        private void InitializeInputSystem()
        {
            //set mouse, keyboard binding keys (e.g. WASD)
            var bindings = InputBindings.Default;
            // optional tuning
            bindings.MouseSensitivity = 0.12f;  // mouse look scale
            bindings.DebounceMs = 60;           // key/mouse debounce in ms
            bindings.EnableKeyRepeat = true;    // hold-to-repeat
            bindings.KeyRepeatMs = 300;         // repeat rate in ms

            // Create the input system 
            var inputSystem = new InputSystem();

            // Register all the devices, you don't have to, but its for the demo
            inputSystem.Add(new GDKeyboardInput(bindings));
            inputSystem.Add(new GDMouseInput(bindings));
            inputSystem.Add(new GDGamepadInput(PlayerIndex.One, "Gamepad P1"));

            _sceneManager.ActiveScene.Add(inputSystem);
        }

        private void InitializeCameras()
        {
            Scene scene = _sceneManager.ActiveScene;

            GameObject cameraGO = null;
            Camera camera = null;
           
            #region Static birds-eye camera
            cameraGO = new GameObject(AppData.CAMERA_NAME_STATIC_BIRDS_EYE);
            camera = cameraGO.AddComponent<Camera>();
            camera.FieldOfView = MathHelper.ToRadians(80);
            //ISRoT
            cameraGO.Transform.RotateEulerBy(new Vector3(MathHelper.ToRadians(-90), 0, 0));
            cameraGO.Transform.TranslateTo(Vector3.UnitY * 50);
            scene.Add(cameraGO);
            #endregion

            #region Third-person camera
            cameraGO = new GameObject(AppData.CAMERA_NAME_THIRD_PERSON);
            camera = cameraGO.AddComponent<Camera>();

            var thirdPersonController = new ThirdPersonController();
            thirdPersonController.TargetName = AppData.PLAYER_NAME;
            thirdPersonController.ShoulderOffset = 0;
            thirdPersonController.FollowDistance = 50;
            thirdPersonController.RotationDamping = 20;
            cameraGO.AddComponent(thirdPersonController);
            scene.Add(cameraGO);
            #endregion

            #region First-person capsule + camera (parent/child)

            // PARENT: physics + movement (feet at y = 0 here)
            var parentGO = new GameObject(AppData.CAMERA_NAME_FIRST_PERSON_PARENT);
            parentGO.Layer = LayerMask.IgnoreRaycast;
            parentGO.Transform.TranslateTo(new Vector3(0f, 1.5f, 4f)); //spawn point inside zone1

            // Capsule + rigidbody controller (kept upright internally)
            var fpsController = parentGO.AddComponent<FirstPersonCapsuleController>();
            fpsController.MoveSpeed = 8.0f;
            fpsController.Acceleration = 50.0f;
            fpsController.GroundFriction = 10.0f;
            fpsController.JumpImpulse = 7.0f;
            fpsController.CapsuleRadius = 0.5f;
            fpsController.CapsuleHeight = 1.8f;
            fpsController.GroundCheckDistance = 0.25f;

            // camera that can pitch + yaw without affecting the collider
            cameraGO = new GameObject(AppData.CAMERA_NAME_FIRST_PERSON);
            cameraGO.Transform.SetParent(parentGO.Transform);

            // Local offset from feet → eye height
            cameraGO.Transform.TranslateTo(new Vector3(0, 1f, 0));
            camera = cameraGO.AddComponent<Camera>();
            camera.FieldOfView = MathHelper.ToRadians(80.0f);
            var mouseLook = cameraGO.AddComponent<MouseYawPitchController>();
   
            // Add both objects to the scene so their components are updated
            scene.Add(parentGO);
            scene.Add(cameraGO);

            // Make this the active camera
            scene.ActiveCamera = camera;
            #endregion

            #region Curve camera
            cameraGO = new GameObject(AppData.CAMERA_NAME_INTRO_CURVE);
            cameraGO.Transform.RotateEulerBy(new Vector3(MathHelper.ToRadians(-90), 0, 0));
            camera = cameraGO.AddComponent<Camera>();
            camera.FieldOfView = MathHelper.ToRadians(80);

            var curveController = cameraGO.AddComponent<CurveController>();
            curveController.PositionCurve = BuildCameraPositionCurve(CurveLoopType.Oscillate);
            curveController.TargetCurve = BuildCameraTargetCurve(CurveLoopType.Constant);
            curveController.Duration = 10;
            scene.Add(cameraGO);
            #endregion

            //replace with new SetActiveCamera that searches by string
            scene.SetActiveCamera(AppData.CAMERA_NAME_FIRST_PERSON);
        }

        private AnimationCurve3D BuildCameraPositionCurve(CurveLoopType curveLoopType)
        {
            var curve = new AnimationCurve3D(curveLoopType);

            // start
            curve.AddKey(new Vector3(-20, 10, 40), 0);

            // moving inward, slight rise
            curve.AddKey(new Vector3(-10, 10, 30), 0.25f);

            // closest to origin (single “turn”)
            curve.AddKey(new Vector3(0, 10, 30), 0.5f);

            // heading back out
            curve.AddKey(new Vector3(10, 10, 40), 0.75f);

            // end
            curve.AddKey(new Vector3(20, 10, 40), 1);

            return curve;
        }

        private AnimationCurve3D BuildCameraTargetCurve(CurveLoopType curveLoopType)
        {
            var curve = new AnimationCurve3D(curveLoopType);

            // All points “in or around” origin, y ≈ 5 so we look slightly down from y=10–12.
            curve.AddKey(new Vector3(-5,0,0), 0);
            curve.AddKey(new Vector3(5,0,0), 1);

            return curve;
        }


        /// <summary>
        /// Add parent root at origin to rotate the sky
        /// </summary>
        private void InitializeSkyParent()
        {
            var _skyParent = new GameObject("SkyParent");
            var rot = _skyParent.AddComponent<RotationController>();

            // Turntable spin around local +Y
            rot._rotationAxisNormalized = Vector3.Up;

            // Dramatised fast drift at 2 deg/sec. 
            rot._rotationSpeedInRadiansPerSecond = MathHelper.ToRadians(2f);
            _sceneManager.ActiveScene.Add(_skyParent);
        }

        private void InitializeSkyBox(int scale = 500)
        {
            Scene scene = _sceneManager.ActiveScene;
            GameObject gameObject = null;
            MeshFilter meshFilter = null;
            MeshRenderer meshRenderer = null;

            // Find the sky parent object to attach sky to so sky rotates
            GameObject skyParent = scene.Find((GameObject go) => go.Name.Equals("SkyParent"));

            // back
            gameObject = new GameObject("back");
            gameObject.Transform.ScaleTo(new Vector3(scale, scale, 1));
            gameObject.Transform.TranslateTo(new Vector3(0, 0, -scale / 2));
            meshFilter = MeshFilterFactory.CreateQuadTexturedLit(_graphics.GraphicsDevice);
            gameObject.AddComponent(meshFilter);
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.Material = _matBasicUnlit;
            meshRenderer.Overrides.MainTexture = _textureDictionary.Get("skybox_back");
            scene.Add(gameObject);

            //set parent to allow rotation
            gameObject.Transform.SetParent(skyParent.Transform);

            // left
            gameObject = new GameObject("left");
            gameObject.Transform.ScaleTo(new Vector3(scale, scale, 1));
            gameObject.Transform.RotateEulerBy(new Vector3(0, MathHelper.ToRadians(90), 0), true);
            gameObject.Transform.TranslateTo(new Vector3(-scale / 2, 0, 0));
            meshFilter = MeshFilterFactory.CreateQuadTexturedLit(_graphics.GraphicsDevice);
            gameObject.AddComponent(meshFilter);
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.Material = _matBasicUnlit;
            meshRenderer.Overrides.MainTexture = _textureDictionary.Get("skybox_left");
            scene.Add(gameObject);

            //set parent to allow rotation
            gameObject.Transform.SetParent(skyParent.Transform);


            // right
            gameObject = new GameObject("right");
            gameObject.Transform.ScaleTo(new Vector3(scale, scale, 1));
            gameObject.Transform.RotateEulerBy(new Vector3(0, MathHelper.ToRadians(-90), 0), true);
            gameObject.Transform.TranslateTo(new Vector3(scale / 2, 0, 0));
            meshFilter = MeshFilterFactory.CreateQuadTexturedLit(_graphics.GraphicsDevice);
            gameObject.AddComponent(meshFilter);
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.Material = _matBasicUnlit;
            meshRenderer.Overrides.MainTexture = _textureDictionary.Get("skybox_right");
            scene.Add(gameObject);

            //set parent to allow rotation
            gameObject.Transform.SetParent(skyParent.Transform);

            // front
            gameObject = new GameObject("front");
            gameObject.Transform.ScaleTo(new Vector3(scale, scale, 1));
            gameObject.Transform.RotateEulerBy(new Vector3(0, MathHelper.ToRadians(180), 0), true);
            gameObject.Transform.TranslateTo(new Vector3(0, 0, scale / 2));
            meshFilter = MeshFilterFactory.CreateQuadTexturedLit(_graphics.GraphicsDevice);
            gameObject.AddComponent(meshFilter);
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.Material = _matBasicUnlit;
            meshRenderer.Overrides.MainTexture = _textureDictionary.Get("skybox_front");
            scene.Add(gameObject);

            //set parent to allow rotation
            gameObject.Transform.SetParent(skyParent.Transform);

            // sky (top)
            gameObject = new GameObject("sky");
            gameObject.Transform.ScaleTo(new Vector3(scale, scale, 1));
            gameObject.Transform.RotateEulerBy(new Vector3(MathHelper.ToRadians(90), 0, MathHelper.ToRadians(90)), true);
            gameObject.Transform.TranslateTo(new Vector3(0, scale / 2, 0));
            meshFilter = MeshFilterFactory.CreateQuadTexturedLit(_graphics.GraphicsDevice);
            gameObject.AddComponent(meshFilter);
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.Material = _matBasicUnlit;
            meshRenderer.Overrides.MainTexture = _textureDictionary.Get("skybox_sky");
            scene.Add(gameObject);

            //set parent to allow rotation
            gameObject.Transform.SetParent(skyParent.Transform);

        }

        private void InitializeUI()
        {
            InitializeUIReticleRenderer();
        }

        private void InitializeUIReticleRenderer()
        {
            var uiReticleGO = new GameObject("HUD");

            var reticleAtlas = _textureDictionary.Get("Crosshair_21");
            var uiFont = _fontDictionary.Get("mouse_reticle_font");

            // Reticle (cursor): always on top
            var reticle = new UIReticle(reticleAtlas);
            reticle.Origin = reticleAtlas.GetCenter();
            reticle.SourceRectangle = null;
            reticle.Scale = new Vector2(0.1f, 0.1f);
            reticle.RotationSpeedDegPerSec = 55;
            reticle.LayerDepth = UILayer.Cursor;
            uiReticleGO.AddComponent(reticle);

            var textRenderer = uiReticleGO.AddComponent<UIText>();
            textRenderer.Font = uiFont;
            textRenderer.Offset = new Vector2(0, 30);  // Position text below reticle
            textRenderer.Color = Color.White;
            textRenderer.PositionProvider = () => _graphics.GraphicsDevice.Viewport.GetCenter();
            textRenderer.Anchor = TextAnchor.Center;

            var picker = uiReticleGO.AddComponent<UIPickerInfo>();
            picker.HitMask = LayerMask.All;
            picker.MaxDistance = 500f;
            picker.HitTriggers = false;

            // Optional custom formatting
            picker.Formatter = hit =>
            {
                var go = hit.Body?.GameObject;
                if (go == null)
                    return string.Empty;

                return $"{go.Name}  d={hit.Distance:F1}";
            };

            _sceneManager.ActiveScene.Add(uiReticleGO);

            // Hide mouse since reticle will take its place
            IsMouseVisible = false;
        }

        /// <summary>
        /// Adds a single-part FBX model into the scene.
        /// </summary>
        private GameObject InitializeModel(Vector3 position,
            Vector3 eulerRotationDegrees, Vector3 scale,
            string textureName, string modelName, string objectName)
        {
            GameObject gameObject = null;

            gameObject = new GameObject(objectName);
            gameObject.Transform.TranslateTo(position);
            gameObject.Transform.RotateEulerBy(eulerRotationDegrees * MathHelper.Pi / 180f);
            gameObject.Transform.ScaleTo(scale);

          //  gameObject.Layer = LayerMask.NPC | LayerMask.Collectables;

            var model = _modelDictionary.Get(modelName);
            var texture = _textureDictionary.Get(textureName);
            var meshFilter = MeshFilterFactory.CreateFromModel(model, _graphics.GraphicsDevice, 0, 0);
            gameObject.AddComponent(meshFilter);

            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.Material = _matBasicLit;
            meshRenderer.Overrides.MainTexture = texture;

            _sceneManager.ActiveScene.Add(gameObject);

            return gameObject;
        }
        protected override void Update(GameTime gameTime)
        {
            Time.Update(gameTime);

            UpdateZone1Interaction();

            UpdateZone2SpatialAudio();
            UpdateZone2Interaction();
            UpdateZone3OrbitCamera();
            UpdateZone4Interaction();
            UpdateZone5TeleportKeys();
            UpdateZone5FovKeys();
            UpdateZone5UIMode();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.CornflowerBlue);

            base.Draw(gameTime);
        }

        /// <summary>
        /// Override Dispose to clean up engine resources.
        /// MonoGame's Game class already implements IDisposable, so we override its Dispose method.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                base.Dispose(disposing);
                return;
            }

            if (disposing)
            {
                System.Diagnostics.Debug.WriteLine("Disposing Main...");

                // 1. Dispose Materials (which may own Effects)
                System.Diagnostics.Debug.WriteLine("Disposing Materials");
                _matBasicUnlit?.Dispose();
                _matBasicUnlit = null;

                _matBasicLit?.Dispose();
                _matBasicLit = null;

                _matAlphaCutout?.Dispose();
                _matAlphaCutout = null;

                // 2. Clear cached MeshFilters in factory registry
                System.Diagnostics.Debug.WriteLine("Clearing MeshFilter Registry");
                MeshFilterFactory.ClearRegistry();

                // 3. Dispose content dictionaries (now they implement IDisposable!)
                System.Diagnostics.Debug.WriteLine("Disposing Content Dictionaries");
                _textureDictionary?.Dispose();
                _textureDictionary = null;

                _modelDictionary?.Dispose();
                _modelDictionary = null;

                _fontDictionary?.Dispose();
                _fontDictionary = null;

                _zone2LeftInstance?.Stop();
                _zone2LeftInstance?.Dispose();
                _zone2LeftInstance = null;

                _zone2RightInstance?.Stop();
                _zone2RightInstance?.Dispose();
                _zone2RightInstance = null;

                _zone2MusicEventSubscription?.Dispose();
                _zone2MusicEventSubscription = null;

                _zone3CameraEventSubscription?.Dispose();
                _zone3CameraEventSubscription = null;

                _zone4ButtonSubscription?.Dispose();
                _zone4ButtonSubscription = null;

                _zone4StateSubscription?.Dispose();
                _zone4StateSubscription = null;

                _zone4ImpulseSubscription?.Dispose();
                _zone4ImpulseSubscription = null;

                _zone4GameWonSubscription?.Dispose();
                _zone4GameWonSubscription = null;

                // 4. Dispose EngineContext (which owns SpriteBatch and Content)
                System.Diagnostics.Debug.WriteLine("Disposing EngineContext");
                EngineContext.Instance?.Dispose();

                // 5. Clear references to help GC
                System.Diagnostics.Debug.WriteLine("Clearing References");

                System.Diagnostics.Debug.WriteLine("Main disposal complete");

            }

            _disposed = true;

            // Always call base.Dispose
            base.Dispose(disposing);
        }

        #endregion
    }
}