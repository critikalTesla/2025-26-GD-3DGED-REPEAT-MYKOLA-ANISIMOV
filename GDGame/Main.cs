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

            // Right wall
            CreateZone1StaticBox(
                "Zone1 East Wall",
                new Vector3(roomWidth / 2f, roomHeight / 2f, 0f),
                new Vector3(wallThickness, roomHeight, roomLength),
                Vector3.Zero);

            // Door
            CreateZone1StaticBox(
                "Zone1 Door",
                new Vector3(0f, 1.5f, -4.85f),
                new Vector3(2f, 3f, 0.25f),
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

            // RIGHT WALL

            CreateZone2StaticBox(
                "Zone2 Right Wall",
                new Vector3(
                    centerX + roomWidth / 2f,
                    roomHeight / 2f,
                    0f),
                new Vector3(
                    wallThickness,
                    roomHeight,
                    roomLength));
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

            // RIGHT WALL

            CreateZone3StaticBox(
                "Zone3 Right Wall",
                new Vector3(
                    centerX + roomWidth / 2f,
                    roomHeight / 2f,
                    0f),
                new Vector3(
                    wallThickness,
                    roomHeight,
                    roomLength));

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
                (float)Time.TotalGameTime.TotalSeconds *
                0.4f;

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


            _zone3OrbitCameraObject
                .Transform
                .RotateEulerTo(
                    new Vector3(
                        pitch,
                        yaw,
                        0f));
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
        #region temporary visible collision zones
        //temporary visible collision zones, delete in the future
        InitializeModel(
            new Vector3(
                Zone3CenterX - 3.5f,
                1f,
                2f),
            Vector3.Zero,
            new Vector3(
                2f,
                2f,
                2f),
            Zone1Texture,
            Zone1CubeModel,
            "First Person Camera Zone");


        InitializeModel(
            new Vector3(
                Zone3CenterX,
                1f,
                0f),
            Vector3.Zero,
            new Vector3(
                2f,
                2f,
                2f),
            Zone1Texture,
            Zone1CubeModel,
            "Orbit Camera Zone");


        InitializeModel(
            new Vector3(
                Zone3CenterX + 3.5f,
                1f,
                -2f),
            Vector3.Zero,
            new Vector3(
                2f,
                2f,
                2f),
            Zone1Texture,
            Zone1CubeModel,
            "Cinematic Camera Zone");
            #endregion

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