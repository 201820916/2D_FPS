using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using GungeonFPS.Core;
using GungeonFPS.Dungeon;
using GungeonFPS.Enemies;
using GungeonFPS.Player;
using GungeonFPS.UI;
using GungeonFPS.Weapons;

namespace GungeonFPS.Editor
{
    public static class GungeonPrototypeBuilder
    {
        private const string RootPath = "Assets/GungeonFPS";
        private const string ArtPath = RootPath + "/Art";
        private const string DataPath = RootPath + "/Data";
        private const string PrefabPath = RootPath + "/Prefabs";
        private const string ScenePath = RootPath + "/Scenes/GungeonPrototype.unity";
        private const string ExternalPath = "Assets/ExternalAssets";

        [MenuItem("Tools/GungeonFPS/Build Prototype Scene")]
        public static void BuildPrototype()
        {
            EnsureFolders();
            EnsureExternalAssetsFolder();
            EnsureTagExists("Enemy");
            CleanupOldWeapons();

            // 1. Load external Font & Crosshair
            Font hudFont = LoadExternalFont();
            Sprite crosshairSp = LoadExternalCrosshair();

            // 2. Generate clean, eye-friendly dark slate grid textures & neon sprites
            Texture2D floorTex = SaveTexture("neon_floor_grid.png", CreateNeonGridTexture(new Color(0.11f, 0.12f, 0.15f), new Color(0.20f, 0.23f, 0.29f)), false);
            Texture2D wallTex = SaveTexture("neon_wall_grid.png", CreateNeonGridTexture(new Color(0.15f, 0.16f, 0.20f), new Color(0.26f, 0.29f, 0.36f)), false);

            Sprite playerBulletSp = SaveSprite("bullet_player.png", CreateCircleTexture(16, new Color(0.15f, 0.9f, 1f), Color.white), 16);
            Sprite enemyBulletSp = SaveSprite("bullet_enemy.png", CreateCircleTexture(16, new Color(1f, 0.15f, 0.35f), new Color(1f, 0.85f, 0.85f)), 16);
            Sprite chaserSp = SaveSprite("enemy_drone.png", CreateNeonDroneTexture(new Color(0.14f, 0.16f, 0.22f), new Color(1f, 0.25f, 0.2f)), 16);
            Sprite spreadSp = SaveSprite("enemy_turret.png", CreateNeonTurretTexture(new Color(0.16f, 0.12f, 0.24f), new Color(1f, 0.15f, 0.65f)), 16);

            Sprite neonShotgunSp = SaveSprite("gun_neon_shotgun.png", CreateNeonShotgunTexture(), 32);
            Sprite muzzleSp = SaveSprite("muzzle_flash.png", CreateMuzzleFlashTexture(new Color(0.2f, 0.95f, 1f)), 16);
            Sprite hitmarkerSp = SaveSprite("hitmarker.png", CreateHitmarkerTexture(), 16);
            Sprite heartSp = SaveSprite("pickup_heart.png", CreateCircleTexture(16, new Color(0.95f, 0.2f, 0.45f), new Color(1f, 0.8f, 0.85f)), 16);
            Sprite blankSp = SaveSprite("pickup_blank.png", CreateCircleTexture(16, new Color(0.2f, 0.9f, 1f), Color.white), 16);

            // 3. Main Weapons (1 equipped by default + 2 chest loot weapons)
            WeaponData neonShotgun = CreateOrUpdateWeapon("Weapon_NeonShotgun", w =>
            {
                w.weaponName = "PULSAR-8 SHOTGUN";
                w.hudSprite = neonShotgunSp;
                w.projectileColor = new Color(0.25f, 0.95f, 1f);
                w.fireRate = 2.1f;
                w.pelletsPerShot = 8;
                w.spreadAngle = 6.5f;
                w.damage = 15f;
                w.projectileSpeed = 42f;
                w.projectileScale = 0.34f;
                w.knockbackForce = 10f;
                w.maxBounces = 0;
                w.magazineSize = 6;
                w.maxTotalAmmo = -1;
                w.reloadTime = 0.75f;
                w.automatic = false;
                w.recoilKickPixels = 48f;
                w.screenShake = 0.10f;
            });

            WeaponData ricochetRifle = CreateOrUpdateWeapon("Weapon_RicochetRifle", w =>
            {
                w.weaponName = "RICOCHET RIFLE";
                w.hudSprite = neonShotgunSp;
                w.projectileColor = new Color(0.2f, 1f, 0.55f);
                w.fireRate = 4.5f;
                w.pelletsPerShot = 1;
                w.spreadAngle = 0.8f;
                w.damage = 24f;
                w.projectileSpeed = 36f;
                w.projectileScale = 0.42f;
                w.knockbackForce = 5f;
                w.maxBounces = 3;
                w.magazineSize = 16;
                w.maxTotalAmmo = -1;
                w.reloadTime = 0.8f;
                w.automatic = true;
                w.recoilKickPixels = 28f;
                w.screenShake = 0.045f;
            });

            WeaponData hyperSmg = CreateOrUpdateWeapon("Weapon_HyperSMG", w =>
            {
                w.weaponName = "HYPER PULSE SMG";
                w.hudSprite = neonShotgunSp;
                w.projectileColor = new Color(1f, 0.3f, 0.75f);
                w.fireRate = 11.5f;
                w.pelletsPerShot = 1;
                w.spreadAngle = 3.2f;
                w.damage = 10f;
                w.projectileSpeed = 46f;
                w.projectileScale = 0.26f;
                w.knockbackForce = 2.5f;
                w.maxBounces = 0;
                w.magazineSize = 36;
                w.maxTotalAmmo = -1;
                w.reloadTime = 0.9f;
                w.automatic = true;
                w.recoilKickPixels = 16f;
                w.screenShake = 0.025f;
            });

            // 3b. Secondary Weapons (1 equipped by default + 1 chest loot)
            SecondaryWeaponData neonBlade = CreateOrUpdateSecondary("Secondary_NeonBlade", s =>
            {
                s.weaponName = "NEON BLADE";
                s.secondaryType = SecondaryWeaponType.EnergyBlade;
                s.accentColor = new Color(0.25f, 0.95f, 1f);
                s.cooldown = 0.55f;
                s.damage = 34f;
                s.range = 3.4f;
                s.knockback = 14f;
                s.bulletClearRadius = 2.8f;
            });

            SecondaryWeaponData aegisShield = CreateOrUpdateSecondary("Secondary_AegisShield", s =>
            {
                s.weaponName = "AEGIS SHIELD";
                s.secondaryType = SecondaryWeaponType.AegisShield;
                s.accentColor = new Color(0.3f, 0.8f, 1f);
                s.maxEnergy = 100f;
                s.drainPerSecond = 30f;
                s.regenPerSecond = 24f;
            });

            // 4. Create Prefabs
            GameObject playerBulletPrefab = CreateBulletPrefab("PlayerBullet", playerBulletSp, typeof(PlayerProjectile));
            GameObject enemyBulletPrefab = CreateBulletPrefab("EnemyBullet", enemyBulletSp, typeof(EnemyBullet));

            GameObject chaserPrefab = CreateEnemyPrefab("Enemy_Chaser", chaserSp, EnemyPattern.SingleChaser, enemyBulletPrefab, 60f, 1.5f, 8.5f, 1);
            GameObject spreadPrefab = CreateEnemyPrefab("Enemy_Spread", spreadSp, EnemyPattern.SpreadTurret, enemyBulletPrefab, 90f, 2.0f, 7.5f, 5);

            GameObject weaponPickupPrefab = CreateWeaponPickupPrefab();
            GameObject chestPrefab = CreateChestPrefab(
                weaponPickupPrefab,
                new[] { ricochetRifle, hyperSmg, neonShotgun },
                new[] { aegisShield, neonBlade });

            // 5. Build Scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Clean neutral lighting & subtle Bloom (no eye-straining blue fog)
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.46f, 0.50f);
            RenderSettings.fog = false;

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.98f, 0.95f);
            lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            CreateNeonVolume();

            // Managers
            var systems = new GameObject("GameSystems");
            systems.AddComponent<ProjectilePool>();
            systems.AddComponent<RetroAudio>();
            systems.AddComponent<VfxSpawner>();

            // Mini-Dungeon (3 Connected Rooms along +Z)
            BuildMiniDungeon(floorTex, wallTex, chaserPrefab, spreadPrefab, chestPrefab);

            // HUD & Player (1 Main + 1 Secondary)
            CreateHUDAndPlayer(
                playerBulletPrefab,
                new[] { neonShotgun },
                neonBlade,
                muzzleSp,
                hitmarkerSp,
                crosshairSp,
                hudFont);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[GungeonPrototypeBuilder] Neon-SF Prototype scene built at: " + ScenePath);
        }

        private static void EnsureExternalAssetsFolder()
        {
            if (AssetDatabase.IsValidFolder("Assets/outland") && !AssetDatabase.IsValidFolder(ExternalPath))
            {
                string err = AssetDatabase.MoveAsset("Assets/outland", ExternalPath);
                if (!string.IsNullOrEmpty(err))
                    Debug.LogWarning("[GungeonPrototypeBuilder] Could not rename Assets/outland: " + err);
            }
        }

        private static void CleanupOldWeapons()
        {
            string[] legacy = { DataPath + "/Weapon_Pistol.asset", DataPath + "/Weapon_BounceGun.asset", DataPath + "/Weapon_Shotgun.asset" };
            foreach (string path in legacy)
            {
                if (File.Exists(path))
                    AssetDatabase.DeleteAsset(path);
            }
        }

        private static Font LoadExternalFont()
        {
            string[] candidates =
            {
                ExternalPath + "/Orbitron/static/Orbitron-Bold.ttf",
                ExternalPath + "/Orbitron/static/Orbitron-Regular.ttf",
                "Assets/outland/Orbitron/static/Orbitron-Bold.ttf"
            };
            foreach (string path in candidates)
            {
                Font f = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (f != null) return f;
            }
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Sprite LoadExternalCrosshair()
        {
            string[] candidates =
            {
                ExternalPath + "/kenney_crosshairPack/PNG/Outline/crosshair061.png",
                ExternalPath + "/kenney_crosshairPack/PNG/Outline/crosshair008.png",
                ExternalPath + "/kenney_crosshairPack/PNG/Outline/crosshair001.png",
                "Assets/outland/kenney_crosshairPack/PNG/Outline/crosshair061.png",
                "Assets/outland/kenney_crosshairPack/PNG/Outline/crosshair001.png"
            };

            foreach (string path in candidates)
            {
                if (!File.Exists(path)) continue;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                }
                Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null) return sp;
            }
            return null;
        }

        private static void CreateNeonVolume()
        {
            var volGo = new GameObject("NeonPostProcessVolume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;

            string profilePath = ArtPath + "/NeonVolumeProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }
            else
            {
                profile.components.Clear();
            }

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.65f);
            bloom.scatter.Override(0.55f);

            EditorUtility.SetDirty(profile);
            vol.sharedProfile = profile;
        }

        private static void BuildMiniDungeon(
            Texture2D floorTex,
            Texture2D wallTex,
            GameObject chaserPrefab,
            GameObject spreadPrefab,
            GameObject chestPrefab)
        {
            var dungeon = new GameObject("MiniDungeon");

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material floorMat = new Material(litShader) { mainTexture = floorTex };
            floorMat.mainTextureScale = new Vector2(12f, 12f);
            AssetDatabase.CreateAsset(floorMat, ArtPath + "/FloorMat.mat");

            Material wallMat = new Material(litShader) { mainTexture = wallTex };
            wallMat.mainTextureScale = new Vector2(10f, 2f);
            AssetDatabase.CreateAsset(wallMat, ArtPath + "/WallMat.mat");

            Material doorMat = new Material(litShader) { color = new Color(0.18f, 0.06f, 0.12f) };
            doorMat.EnableKeyword("_EMISSION");
            doorMat.SetColor("_EmissionColor", new Color(1f, 0.15f, 0.35f) * 1.8f);
            AssetDatabase.CreateAsset(doorMat, ArtPath + "/DoorMat.mat");

            // Room 0: Start Room (includes 1 starter test chest!)
            var startRoom = BuildRoomShell(dungeon.transform, "StartRoom", new Vector3(0f, 0f, 0f), 16f, 16f, false, true, floorMat, wallMat);
            var startChest = (GameObject)PrefabUtility.InstantiatePrefab(chestPrefab, startRoom.transform);
            startChest.transform.position = new Vector3(4.5f, 0f, 4f);

            BuildCorridor(dungeon.transform, "Corridor_0_1", new Vector3(0f, 0f, 11f), 6f, 6f, floorMat, wallMat);

            // Room 1: Combat Room 1
            var room1Go = BuildRoomShell(dungeon.transform, "CombatRoom_1", new Vector3(0f, 0f, 28f), 28f, 28f, true, true, floorMat, wallMat);
            CreateBox(room1Go.transform, "Pillar_L", new Vector3(-6f, 2f, 28f), new Vector3(2.5f, 4f, 2.5f), wallMat);
            CreateBox(room1Go.transform, "Pillar_R", new Vector3(6f, 2f, 28f), new Vector3(2.5f, 4f, 2.5f), wallMat);

            var door1S = CreateDoor(room1Go.transform, "Door_1_South", new Vector3(0f, 2.25f, 14f), doorMat);
            var door1N = CreateDoor(room1Go.transform, "Door_1_North", new Vector3(0f, 2.25f, 42f), doorMat);

            var r1Enemies = new List<EnemyBase>
            {
                SpawnEnemy(chaserPrefab, new Vector3(-8f, 0f, 34f), room1Go.transform),
                SpawnEnemy(chaserPrefab, new Vector3(8f, 0f, 34f), room1Go.transform),
                SpawnEnemy(chaserPrefab, new Vector3(-5f, 0f, 24f), room1Go.transform),
                SpawnEnemy(spreadPrefab, new Vector3(0f, 0f, 37f), room1Go.transform)
            };
            AttachRoomController(room1Go, "SECTOR 01", new Vector3(26f, 4f, 26f), new[] { door1S, door1N }, r1Enemies, chestPrefab);

            BuildCorridor(dungeon.transform, "Corridor_1_2", new Vector3(0f, 0f, 45f), 6f, 6f, floorMat, wallMat);

            // Room 2: Combat Room 2
            var room2Go = BuildRoomShell(dungeon.transform, "CombatRoom_2", new Vector3(0f, 0f, 64f), 32f, 32f, true, false, floorMat, wallMat);
            CreateBox(room2Go.transform, "Pillar_NW", new Vector3(-7f, 2f, 70f), new Vector3(2.5f, 4f, 2.5f), wallMat);
            CreateBox(room2Go.transform, "Pillar_NE", new Vector3(7f, 2f, 70f), new Vector3(2.5f, 4f, 2.5f), wallMat);
            CreateBox(room2Go.transform, "Pillar_SW", new Vector3(-7f, 2f, 58f), new Vector3(2.5f, 4f, 2.5f), wallMat);
            CreateBox(room2Go.transform, "Pillar_SE", new Vector3(7f, 2f, 58f), new Vector3(2.5f, 4f, 2.5f), wallMat);

            var door2S = CreateDoor(room2Go.transform, "Door_2_South", new Vector3(0f, 2.25f, 48f), doorMat);

            var r2Enemies = new List<EnemyBase>
            {
                SpawnEnemy(chaserPrefab, new Vector3(-10f, 0f, 68f), room2Go.transform),
                SpawnEnemy(chaserPrefab, new Vector3(10f, 0f, 68f), room2Go.transform),
                SpawnEnemy(chaserPrefab, new Vector3(0f, 0f, 74f), room2Go.transform),
                SpawnEnemy(spreadPrefab, new Vector3(-9f, 0f, 75f), room2Go.transform),
                SpawnEnemy(spreadPrefab, new Vector3(9f, 0f, 75f), room2Go.transform)
            };
            AttachRoomController(room2Go, "SECTOR 02 (CORE)", new Vector3(30f, 4f, 30f), new[] { door2S }, r2Enemies, chestPrefab);
        }

        private static GameObject BuildRoomShell(
            Transform parent, string name, Vector3 center, float width, float depth,
            bool southDoorway, bool northDoorway, Material floorMat, Material wallMat)
        {
            var room = new GameObject(name);
            room.transform.SetParent(parent);
            room.transform.position = center;

            CreateBox(room.transform, "Floor", center + new Vector3(0f, -0.5f, 0f), new Vector3(width, 1f, depth), floorMat);

            float wallH = 5f;
            float wallY = 2.5f;
            float halfW = width * 0.5f;
            float halfD = depth * 0.5f;

            CreateBox(room.transform, "Wall_West", center + new Vector3(-halfW - 0.5f, wallY, 0f), new Vector3(1f, wallH, depth + 2f), wallMat);
            CreateBox(room.transform, "Wall_East", center + new Vector3(halfW + 0.5f, wallY, 0f), new Vector3(1f, wallH, depth + 2f), wallMat);

            BuildWallWithDoorway(room.transform, "North", center + new Vector3(0f, wallY, halfD + 0.5f), width, wallH, northDoorway, wallMat);
            BuildWallWithDoorway(room.transform, "South", center + new Vector3(0f, wallY, -halfD - 0.5f), width, wallH, southDoorway, wallMat);

            return room;
        }

        private static void BuildWallWithDoorway(Transform parent, string side, Vector3 pos, float width, float wallH, bool hasDoorway, Material wallMat)
        {
            if (!hasDoorway)
            {
                CreateBox(parent, $"Wall_{side}", pos, new Vector3(width, wallH, 1f), wallMat);
                return;
            }

            float doorW = 6f;
            float segW = (width - doorW) * 0.5f;
            CreateBox(parent, $"Wall_{side}_L", pos + new Vector3(-(doorW * 0.5f + segW * 0.5f), 0f, 0f), new Vector3(segW, wallH, 1f), wallMat);
            CreateBox(parent, $"Wall_{side}_R", pos + new Vector3(doorW * 0.5f + segW * 0.5f, 0f, 0f), new Vector3(segW, wallH, 1f), wallMat);
        }

        private static void BuildCorridor(Transform parent, string name, Vector3 center, float width, float depth, Material floorMat, Material wallMat)
        {
            var cor = new GameObject(name);
            cor.transform.SetParent(parent);
            CreateBox(cor.transform, "Floor", center + new Vector3(0f, -0.5f, 0f), new Vector3(width, 1f, depth), floorMat);
            CreateBox(cor.transform, "Wall_W", center + new Vector3(-width * 0.5f - 0.5f, 2.5f, 0f), new Vector3(1f, 5f, depth), wallMat);
            CreateBox(cor.transform, "Wall_E", center + new Vector3(width * 0.5f + 0.5f, 2.5f, 0f), new Vector3(1f, 5f, depth), wallMat);
        }

        private static DoorController CreateDoor(Transform parent, string name, Vector3 pos, Material doorMat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(6f, 4.5f, 0.8f);
            go.GetComponent<Renderer>().sharedMaterial = doorMat;
            return go.AddComponent<DoorController>();
        }

        private static void AttachRoomController(
            GameObject roomGo, string roomName, Vector3 triggerSize,
            DoorController[] doors, List<EnemyBase> enemies, GameObject rewardPrefab)
        {
            var trigger = roomGo.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 2f, 0f);
            trigger.size = triggerSize;

            var rc = roomGo.AddComponent<RoomController>();
            var so = new SerializedObject(rc);
            so.FindProperty("roomName").stringValue = roomName;
            so.FindProperty("rewardPickupPrefab").objectReferenceValue = rewardPrefab;

            var doorsProp = so.FindProperty("doors");
            doorsProp.ClearArray();
            for (int i = 0; i < doors.Length; i++)
            {
                doorsProp.InsertArrayElementAtIndex(i);
                doorsProp.GetArrayElementAtIndex(i).objectReferenceValue = doors[i];
            }

            var enemiesProp = so.FindProperty("enemies");
            enemiesProp.ClearArray();
            for (int i = 0; i < enemies.Count; i++)
            {
                enemiesProp.InsertArrayElementAtIndex(i);
                enemiesProp.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static EnemyBase SpawnEnemy(GameObject prefab, Vector3 pos, Transform parent)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.position = pos;
            return inst.GetComponent<EnemyBase>();
        }

        private static void CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateHUDAndPlayer(
            GameObject playerBulletPrefab, WeaponData[] weapons, SecondaryWeaponData defaultSecondary,
            Sprite muzzleSp, Sprite hitmarkerSp, Sprite crosshairSp, Font hudFont)
        {
            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(0f, 1.05f, 0f);

            var cc = playerGo.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = Vector3.zero;

            var playerCtrl = playerGo.AddComponent<PlayerController>();
            var playerHealth = playerGo.AddComponent<PlayerHealth>();

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(playerGo.transform);
            pivot.transform.localPosition = new Vector3(0f, 0.65f, 0f);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(pivot.transform);
            camGo.transform.localPosition = Vector3.zero;
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 82f;
            cam.nearClipPlane = 0.05f;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            var urpCamData = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCamData.renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();

            var fireOrigin = new GameObject("FireOrigin");
            fireOrigin.transform.SetParent(camGo.transform);
            fireOrigin.transform.localPosition = new Vector3(0.26f, -0.18f, 0.75f);

            // Build sleek 3D Viewmodels (Main Gun + Secondary Blade & Shield)
            Transform viewmodelRoot = Build3DNeonShotgun(camGo.transform, out Light muzzleLight, out Material gunCoreMat);
            Transform bladeViewmodel = Build3DNeonBlade(camGo.transform);
            Transform shieldViewmodel = Build3DAegisShield(camGo.transform, out ShieldBlocker shieldBlocker);

            // Canvas
            var canvasGo = new GameObject("HUDCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Flash Overlay
            var flashGo = new GameObject("FlashOverlay");
            flashGo.transform.SetParent(canvasGo.transform, false);
            var flashImg = flashGo.AddComponent<Image>();
            flashImg.color = new Color(1, 1, 1, 0);
            flashImg.raycastTarget = false;
            var flashRect = flashImg.rectTransform;
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;

            // Crosshair (External Kenney Crosshair)
            var crossGo = new GameObject("Crosshair");
            crossGo.transform.SetParent(canvasGo.transform, false);
            var crossImg = crossGo.AddComponent<Image>();
            crossImg.sprite = crosshairSp;
            crossImg.color = new Color(0.35f, 0.95f, 1f, 0.92f);
            crossImg.raycastTarget = false;
            crossImg.preserveAspect = true;
            crossImg.rectTransform.sizeDelta = crosshairSp != null ? new Vector2(44f, 44f) : new Vector2(8f, 8f);

            // Hitmarker
            var hitGo = new GameObject("Hitmarker");
            hitGo.transform.SetParent(canvasGo.transform, false);
            var hitImg = hitGo.AddComponent<Image>();
            hitImg.sprite = hitmarkerSp;
            hitImg.color = new Color(1f, 0.25f, 0.35f, 0.95f);
            hitImg.raycastTarget = false;
            hitImg.rectTransform.sizeDelta = new Vector2(36f, 36f);

            // 2D HUD Weapon (disabled when using 3D Viewmodel) + Muzzle Flash
            var weaponImgGo = new GameObject("HUDWeapon");
            weaponImgGo.transform.SetParent(canvasGo.transform, false);
            var weaponImg = weaponImgGo.AddComponent<Image>();
            weaponImg.raycastTarget = false;
            var wRect = weaponImg.rectTransform;
            wRect.anchorMin = new Vector2(0.5f, 0f);
            wRect.anchorMax = new Vector2(0.5f, 0f);
            wRect.pivot = new Vector2(0.5f, 0f);
            wRect.anchoredPosition = new Vector2(240f, -15f);
            wRect.sizeDelta = new Vector2(460f, 345f);
            weaponImgGo.SetActive(false);

            var muzzleGo = new GameObject("HUDMuzzleFlash");
            muzzleGo.transform.SetParent(weaponImgGo.transform, false);
            var muzzleImg = muzzleGo.AddComponent<Image>();
            muzzleImg.sprite = muzzleSp;
            muzzleImg.raycastTarget = false;
            muzzleImg.enabled = false;
            var mRect = muzzleImg.rectTransform;
            mRect.anchoredPosition = new Vector2(95f, 80f);
            mRect.sizeDelta = new Vector2(140f, 140f);

            // Status Text (Orbitron Font)
            var textGo = new GameObject("StatusText");
            textGo.transform.SetParent(canvasGo.transform, false);
            var statusText = textGo.AddComponent<Text>();
            statusText.font = hudFont;
            statusText.fontSize = 24;
            statusText.color = new Color(0.85f, 0.96f, 1f);
            statusText.raycastTarget = false;
            var outline = textGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.1f, 0.2f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            var tRect = statusText.rectTransform;
            tRect.anchorMin = new Vector2(0f, 1f);
            tRect.anchorMax = new Vector2(0f, 1f);
            tRect.pivot = new Vector2(0f, 1f);
            tRect.anchoredPosition = new Vector2(30f, -25f);
            tRect.sizeDelta = new Vector2(1500f, 140f);

            // Interact Prompt Text (Center-Bottom)
            var promptGo = new GameObject("InteractPrompt");
            promptGo.transform.SetParent(canvasGo.transform, false);
            var promptText = promptGo.AddComponent<Text>();
            promptText.font = hudFont;
            promptText.fontSize = 26;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = new Color(0.25f, 0.95f, 1f);
            promptText.raycastTarget = false;
            var pOutline = promptGo.AddComponent<Outline>();
            pOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            pOutline.effectDistance = new Vector2(2f, -2f);
            var pRect = promptText.rectTransform;
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.anchoredPosition = new Vector2(0f, -120f);
            pRect.sizeDelta = new Vector2(700f, 60f);

            // Dash Bar BG + Fill
            var dashBgGo = new GameObject("DashBarBG");
            dashBgGo.transform.SetParent(canvasGo.transform, false);
            var dashBg = dashBgGo.AddComponent<Image>();
            dashBg.color = new Color(0.05f, 0.08f, 0.15f, 0.75f);
            var bgRect = dashBg.rectTransform;
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.pivot = new Vector2(0f, 1f);
            bgRect.anchoredPosition = new Vector2(30f, -110f);
            bgRect.sizeDelta = new Vector2(260f, 16f);

            var dashFillGo = new GameObject("DashBarFill");
            dashFillGo.transform.SetParent(dashBgGo.transform, false);
            var dashFill = dashFillGo.AddComponent<Image>();
            dashFill.color = new Color(0.15f, 0.95f, 1f, 1f);
            dashFill.type = Image.Type.Filled;
            dashFill.fillMethod = Image.FillMethod.Horizontal;
            var fillRect = dashFill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);

            // Wire ScreenEffects
            var fx = playerGo.AddComponent<ScreenEffects>();
            var fxSo = new SerializedObject(fx);
            fxSo.FindProperty("targetCamera").objectReferenceValue = cam;
            fxSo.FindProperty("cameraShakeTarget").objectReferenceValue = camGo.transform;
            fxSo.FindProperty("flashOverlay").objectReferenceValue = flashImg;
            fxSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire PlayerController cameraPivot
            var pcSo = new SerializedObject(playerCtrl);
            pcSo.FindProperty("cameraPivot").objectReferenceValue = pivot.transform;
            pcSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire WeaponController
            var wc = playerGo.AddComponent<WeaponController>();
            var wcSo = new SerializedObject(wc);
            var listProp = wcSo.FindProperty("weapons");
            listProp.ClearArray();
            for (int i = 0; i < weapons.Length; i++)
            {
                listProp.InsertArrayElementAtIndex(i);
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = weapons[i];
            }
            wcSo.FindProperty("projectilePrefab").objectReferenceValue = playerBulletPrefab;
            wcSo.FindProperty("fireOrigin").objectReferenceValue = fireOrigin.transform;
            wcSo.FindProperty("playerCamera").objectReferenceValue = cam;
            wcSo.FindProperty("playerController").objectReferenceValue = playerCtrl;
            wcSo.FindProperty("hudWeaponImage").objectReferenceValue = weaponImg;
            wcSo.FindProperty("hudWeaponRect").objectReferenceValue = wRect;
            wcSo.FindProperty("hudMuzzleFlash").objectReferenceValue = muzzleImg;
            wcSo.FindProperty("viewmodelRoot").objectReferenceValue = viewmodelRoot;
            wcSo.FindProperty("muzzleLight").objectReferenceValue = muzzleLight;
            wcSo.FindProperty("gunCoreMaterial").objectReferenceValue = gunCoreMat;
            wcSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire SecondaryWeaponController
            var sc = playerGo.AddComponent<SecondaryWeaponController>();
            var scSo = new SerializedObject(sc);
            scSo.FindProperty("equippedSecondary").objectReferenceValue = defaultSecondary;
            scSo.FindProperty("playerCamera").objectReferenceValue = cam;
            scSo.FindProperty("bladeViewmodel").objectReferenceValue = bladeViewmodel;
            scSo.FindProperty("shieldViewmodel").objectReferenceValue = shieldViewmodel;
            scSo.FindProperty("shieldBlocker").objectReferenceValue = shieldBlocker;
            scSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire InventorySystem
            var inv = playerGo.AddComponent<InventorySystem>();
            var invSo = new SerializedObject(inv);
            invSo.FindProperty("weaponController").objectReferenceValue = wc;
            invSo.FindProperty("secondaryController").objectReferenceValue = sc;
            invSo.ApplyModifiedPropertiesWithoutUndo();

            // Ensure EventSystem with InputSystemUIInputModule for UI Button clicks
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            // Build Inventory UI Panel
            BuildInventoryPanel(canvasGo.transform, hudFont, inv, wc, sc);

            // Wire GameHUD
            var hud = canvasGo.AddComponent<GameHUD>();
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            hudSo.FindProperty("playerController").objectReferenceValue = playerCtrl;
            hudSo.FindProperty("weaponController").objectReferenceValue = wc;
            hudSo.FindProperty("secondaryController").objectReferenceValue = sc;
            hudSo.FindProperty("inventorySystem").objectReferenceValue = inv;
            hudSo.FindProperty("statusText").objectReferenceValue = statusText;
            hudSo.FindProperty("interactPromptText").objectReferenceValue = promptText;
            hudSo.FindProperty("dashFillBar").objectReferenceValue = dashFill;
            hudSo.FindProperty("hitmarkerImage").objectReferenceValue = hitImg;
            hudSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildInventoryPanel(
            Transform canvasTransform, Font hudFont,
            InventorySystem inv, WeaponController wc, SecondaryWeaponController sc)
        {
            var panelGo = new GameObject("InventoryPanel");
            panelGo.transform.SetParent(canvasTransform, false);
            var bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.07f, 0.11f, 0.94f);
            var pRect = bg.rectTransform;
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.sizeDelta = new Vector2(1080f, 760f);

            // Title & Core Currency
            CreateUIText(panelGo.transform, "Title", "TACTICAL LOADOUT  [TAB]", hudFont, 26,
                new Vector2(-240f, 340f), new Vector2(540f, 45f), new Color(0.25f, 0.95f, 1f), TextAnchor.MiddleLeft);
            Text coreText = CreateUIText(panelGo.transform, "CoreCurrency", "CORE: 0", hudFont, 26,
                new Vector2(310f, 340f), new Vector2(360f, 45f), new Color(1f, 0.85f, 0.25f), TextAnchor.MiddleRight);

            // Equipped Main & Secondary labels
            Text eqMain = CreateUIText(panelGo.transform, "EquippedMain", "[MAIN] NONE", hudFont, 20,
                new Vector2(-250f, 290f), new Vector2(480f, 36f), Color.white, TextAnchor.MiddleLeft);
            Text eqSec = CreateUIText(panelGo.transform, "EquippedSec", "[SEC] NONE", hudFont, 20,
                new Vector2(250f, 290f), new Vector2(480f, 36f), Color.white, TextAnchor.MiddleLeft);

            // Backpack Header
            Text bagHeader = CreateUIText(panelGo.transform, "BagHeader", "BACKPACK (0/6)", hudFont, 18,
                new Vector2(0f, 248f), new Vector2(980f, 32f), new Color(0.65f, 0.78f, 0.9f), TextAnchor.MiddleLeft);

            var invUI = canvasTransform.gameObject.AddComponent<InventoryUI>();
            var uiSo = new SerializedObject(invUI);
            uiSo.FindProperty("panelRoot").objectReferenceValue = panelGo;
            uiSo.FindProperty("inventorySystem").objectReferenceValue = inv;
            uiSo.FindProperty("weaponController").objectReferenceValue = wc;
            uiSo.FindProperty("secondaryController").objectReferenceValue = sc;
            uiSo.FindProperty("coreCurrencyText").objectReferenceValue = coreText;
            uiSo.FindProperty("equippedMainText").objectReferenceValue = eqMain;
            uiSo.FindProperty("equippedSecText").objectReferenceValue = eqSec;
            uiSo.FindProperty("bagHeaderText").objectReferenceValue = bagHeader;

            var btnProp = uiSo.FindProperty("slotButtons");
            var txtProp = uiSo.FindProperty("slotTexts");
            var disProp = uiSo.FindProperty("dismantleButtons");
            btnProp.ClearArray();
            txtProp.ClearArray();
            disProp.ClearArray();

            // 6 Backpack Slots (3x2 Grid) + Dismantle Button per slot
            for (int i = 0; i < 6; i++)
            {
                int col = i % 3;
                int row = i / 3;
                float x = (col - 1) * 330f;
                float y = 175f - row * 145f;

                Button equipBtn = CreateUIButton(panelGo.transform, $"Slot_{i}", new Vector2(x, y), new Vector2(305f, 84f),
                    new Color(0.11f, 0.14f, 0.21f, 1f), new Color(0.20f, 0.30f, 0.42f, 1f));
                Text slotTxt = CreateUIText(equipBtn.transform, "Label", "— EMPTY —", hudFont, 18,
                    Vector2.zero, new Vector2(285f, 76f), new Color(0.45f, 0.5f, 0.58f), TextAnchor.MiddleCenter);

                Button disBtn = CreateUIButton(panelGo.transform, $"Dismantle_{i}", new Vector2(x, y - 62f), new Vector2(305f, 34f),
                    new Color(0.32f, 0.12f, 0.14f, 1f), new Color(0.55f, 0.18f, 0.22f, 1f));
                CreateUIText(disBtn.transform, "Label", "DISMANTLE (+25 CORE)", hudFont, 14,
                    Vector2.zero, new Vector2(290f, 30f), new Color(1f, 0.85f, 0.85f), TextAnchor.MiddleCenter);

                btnProp.InsertArrayElementAtIndex(i);
                btnProp.GetArrayElementAtIndex(i).objectReferenceValue = equipBtn;
                txtProp.InsertArrayElementAtIndex(i);
                txtProp.GetArrayElementAtIndex(i).objectReferenceValue = slotTxt;
                disProp.InsertArrayElementAtIndex(i);
                disProp.GetArrayElementAtIndex(i).objectReferenceValue = disBtn;
            }

            // Divider line
            var divGo = new GameObject("UpgradeDivider");
            divGo.transform.SetParent(panelGo.transform, false);
            var divImg = divGo.AddComponent<Image>();
            divImg.color = new Color(0.2f, 0.7f, 0.85f, 0.45f);
            divImg.rectTransform.anchoredPosition = new Vector2(0f, -62f);
            divImg.rectTransform.sizeDelta = new Vector2(980f, 3f);

            // 2-Branch Upgrade Frame (Skeleton)
            Text upgradeTarget = CreateUIText(panelGo.transform, "UpgradeTarget", "UPGRADE MODULE  |  TARGET: [MAIN]", hudFont, 19,
                new Vector2(-170f, -96f), new Vector2(640f, 36f), new Color(0.25f, 0.95f, 1f), TextAnchor.MiddleLeft);

            Button selMainBtn = CreateUIButton(panelGo.transform, "SelectMainUpgrade", new Vector2(260f, -96f), new Vector2(140f, 34f),
                new Color(0.14f, 0.22f, 0.30f, 1f), new Color(0.22f, 0.42f, 0.55f, 1f));
            CreateUIText(selMainBtn.transform, "Label", "MAIN", hudFont, 15, Vector2.zero, new Vector2(130f, 30f), Color.white, TextAnchor.MiddleCenter);

            Button selSecBtn = CreateUIButton(panelGo.transform, "SelectSecUpgrade", new Vector2(415f, -96f), new Vector2(140f, 34f),
                new Color(0.14f, 0.22f, 0.30f, 1f), new Color(0.22f, 0.42f, 0.55f, 1f));
            CreateUIText(selSecBtn.transform, "Label", "SECONDARY", hudFont, 15, Vector2.zero, new Vector2(130f, 30f), Color.white, TextAnchor.MiddleCenter);

            // Route A Card
            BuildUpgradeRouteCard(panelGo.transform, "RouteACard", new Vector2(-248f, -238f), hudFont,
                new Color(0.10f, 0.16f, 0.24f, 1f), new Color(0.25f, 0.95f, 1f),
                out Text routeATitle, out Text routeADesc, out Button routeABtn);

            // Route B Card
            BuildUpgradeRouteCard(panelGo.transform, "RouteBCard", new Vector2(248f, -238f), hudFont,
                new Color(0.18f, 0.11f, 0.22f, 1f), new Color(1f, 0.45f, 0.85f),
                out Text routeBTitle, out Text routeBDesc, out Button routeBBtn);

            uiSo.FindProperty("selectMainUpgradeButton").objectReferenceValue = selMainBtn;
            uiSo.FindProperty("selectSecUpgradeButton").objectReferenceValue = selSecBtn;
            uiSo.FindProperty("upgradeTargetText").objectReferenceValue = upgradeTarget;
            uiSo.FindProperty("routeATitleText").objectReferenceValue = routeATitle;
            uiSo.FindProperty("routeADescText").objectReferenceValue = routeADesc;
            uiSo.FindProperty("routeAButton").objectReferenceValue = routeABtn;
            uiSo.FindProperty("routeBTitleText").objectReferenceValue = routeBTitle;
            uiSo.FindProperty("routeBDescText").objectReferenceValue = routeBDesc;
            uiSo.FindProperty("routeBButton").objectReferenceValue = routeBBtn;

            uiSo.ApplyModifiedPropertiesWithoutUndo();
            panelGo.SetActive(false);
        }

        private static void BuildUpgradeRouteCard(
            Transform parent, string name, Vector2 pos, Font font, Color bgColor, Color titleColor,
            out Text titleText, out Text descText, out Button upgradeBtn)
        {
            var card = new GameObject(name);
            card.transform.SetParent(parent, false);
            var img = card.AddComponent<Image>();
            img.color = bgColor;
            img.rectTransform.anchoredPosition = pos;
            img.rectTransform.sizeDelta = new Vector2(470f, 215f);

            titleText = CreateUIText(card.transform, "Title", "ROUTE — [TBD]", font, 20,
                new Vector2(0f, 75f), new Vector2(430f, 36f), titleColor, TextAnchor.MiddleLeft);
            descText = CreateUIText(card.transform, "Desc", "Specialization pending", font, 16,
                new Vector2(0f, 5f), new Vector2(430f, 95f), new Color(0.82f, 0.86f, 0.92f), TextAnchor.UpperLeft);

            upgradeBtn = CreateUIButton(card.transform, "UpgradeBtn", new Vector2(0f, -72f), new Vector2(430f, 40f),
                new Color(0.18f, 0.20f, 0.26f, 1f), new Color(0.28f, 0.32f, 0.40f, 1f));
            CreateUIText(upgradeBtn.transform, "Label", "UPGRADE LOCKED  [TBD]", font, 15,
                Vector2.zero, new Vector2(410f, 34f), new Color(0.65f, 0.7f, 0.78f), TextAnchor.MiddleCenter);
        }

        private static Button CreateUIButton(Transform parent, string name, Vector2 pos, Vector2 size, Color normal, Color highlight)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = normal;
            var rect = img.rectTransform;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = highlight;
            colors.pressedColor = new Color(0.2f, 0.8f, 1f, 1f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.45f, 0.45f);
            btn.colors = colors;
            return btn;
        }

        private static Text CreateUIText(
            Transform parent, string name, string content, Font font, int fontSize,
            Vector2 pos, Vector2 size, Color color, TextAnchor align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = content;
            t.font = font;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            var r = t.rectTransform;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return t;
        }

        private static Transform Build3DNeonShotgun(Transform cameraTransform, out Light muzzleLight, out Material coreMat)
        {
            var root = new GameObject("Viewmodel_NeonShotgun");
            root.transform.SetParent(cameraTransform);
            root.transform.localPosition = new Vector3(0.28f, -0.24f, 0.48f);
            root.transform.localRotation = Quaternion.identity;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material bodyMat = new Material(litShader) { color = new Color(0.14f, 0.15f, 0.19f) };
            bodyMat.SetFloat("_Metallic", 0.65f);
            bodyMat.SetFloat("_Smoothness", 0.55f);
            AssetDatabase.CreateAsset(bodyMat, ArtPath + "/GunBodyMat.mat");

            Material railMat = new Material(litShader) { color = new Color(0.28f, 0.30f, 0.36f) };
            railMat.SetFloat("_Metallic", 0.8f);
            railMat.SetFloat("_Smoothness", 0.7f);
            AssetDatabase.CreateAsset(railMat, ArtPath + "/GunRailMat.mat");

            coreMat = new Material(litShader) { color = new Color(0.1f, 0.9f, 1f) };
            coreMat.EnableKeyword("_EMISSION");
            coreMat.SetColor("_EmissionColor", new Color(0.15f, 0.95f, 1f) * 2.4f);
            AssetDatabase.CreateAsset(coreMat, ArtPath + "/GunCoreMat.mat");

            // Main Receiver
            CreateViewPart(root.transform, "Receiver", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.085f, 0.11f, 0.34f), bodyMat);
            // Angled Foregrip / Pump
            CreateViewPart(root.transform, "Pump", PrimitiveType.Cube, new Vector3(0f, -0.055f, 0.22f), new Vector3(0.075f, 0.055f, 0.18f), railMat);
            // Twin Rail Barrels (Upper & Lower)
            CreateViewPart(root.transform, "Barrel_Top", PrimitiveType.Cube, new Vector3(0f, 0.032f, 0.32f), new Vector3(0.05f, 0.03f, 0.42f), railMat);
            CreateViewPart(root.transform, "Barrel_Bottom", PrimitiveType.Cube, new Vector3(0f, -0.01f, 0.28f), new Vector3(0.045f, 0.025f, 0.34f), bodyMat);
            // Glowing Neon Energy Core & Side Rails
            CreateViewPart(root.transform, "PlasmaCore", PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0.06f), new Vector3(0.045f, 0.08f, 0.045f), coreMat, Quaternion.Euler(90f, 0f, 0f));
            CreateViewPart(root.transform, "SideStrip_L", PrimitiveType.Cube, new Vector3(-0.044f, 0.015f, 0.18f), new Vector3(0.008f, 0.016f, 0.28f), coreMat);
            CreateViewPart(root.transform, "SideStrip_R", PrimitiveType.Cube, new Vector3(0.044f, 0.015f, 0.18f), new Vector3(0.008f, 0.016f, 0.28f), coreMat);

            // Muzzle Light
            var lightGo = new GameObject("MuzzleLight");
            lightGo.transform.SetParent(root.transform);
            lightGo.transform.localPosition = new Vector3(0f, 0.02f, 0.58f);
            muzzleLight = lightGo.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.range = 7f;
            muzzleLight.intensity = 4f;
            muzzleLight.color = new Color(0.2f, 0.95f, 1f);
            muzzleLight.enabled = false;

            return root.transform;
        }

        private static Transform Build3DNeonBlade(Transform cameraTransform)
        {
            var root = new GameObject("Viewmodel_NeonBlade");
            root.transform.SetParent(cameraTransform);
            root.transform.localPosition = new Vector3(-0.32f, -0.25f, 0.45f);
            root.transform.localRotation = Quaternion.Euler(10f, 15f, -15f);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material hiltMat = new Material(litShader) { color = new Color(0.16f, 0.17f, 0.22f) };
            AssetDatabase.CreateAsset(hiltMat, ArtPath + "/BladeHiltMat.mat");

            Material edgeMat = new Material(litShader) { color = new Color(0.2f, 0.95f, 1f) };
            edgeMat.EnableKeyword("_EMISSION");
            edgeMat.SetColor("_EmissionColor", new Color(0.2f, 0.95f, 1f) * 2.5f);
            AssetDatabase.CreateAsset(edgeMat, ArtPath + "/BladeEdgeMat.mat");

            CreateViewPart(root.transform, "Hilt", PrimitiveType.Cylinder, new Vector3(0f, -0.1f, 0f), new Vector3(0.03f, 0.08f, 0.03f), hiltMat);
            CreateViewPart(root.transform, "Guard", PrimitiveType.Cube, new Vector3(0f, -0.01f, 0f), new Vector3(0.08f, 0.02f, 0.04f), hiltMat);
            CreateViewPart(root.transform, "Blade", PrimitiveType.Cube, new Vector3(0f, 0.24f, 0f), new Vector3(0.018f, 0.48f, 0.045f), edgeMat);
            return root.transform;
        }

        private static Transform Build3DAegisShield(Transform cameraTransform, out ShieldBlocker blocker)
        {
            var root = new GameObject("Viewmodel_AegisShield");
            root.transform.SetParent(cameraTransform);
            root.transform.localPosition = new Vector3(-0.38f, -0.28f, 0.48f);
            root.transform.localRotation = Quaternion.identity;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material shieldMat = new Material(litShader) { color = new Color(0.15f, 0.85f, 1f, 0.45f) };
            shieldMat.SetFloat("_Surface", 1f); // Transparent in URP Lit
            shieldMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            shieldMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            shieldMat.SetInt("_ZWrite", 0);
            shieldMat.renderQueue = 3000;
            shieldMat.EnableKeyword("_EMISSION");
            shieldMat.SetColor("_EmissionColor", new Color(0.15f, 0.85f, 1f) * 1.6f);
            AssetDatabase.CreateAsset(shieldMat, ArtPath + "/ShieldMat.mat");

            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "ShieldBarrier";
            panel.tag = "Player";
            panel.transform.SetParent(root.transform);
            panel.transform.localPosition = Vector3.zero;
            panel.transform.localScale = new Vector3(0.75f, 0.65f, 0.04f);
            panel.GetComponent<Renderer>().sharedMaterial = shieldMat;
            blocker = panel.AddComponent<ShieldBlocker>();

            return root.transform;
        }

        private static GameObject CreateWeaponPickupPrefab()
        {
            var go = new GameObject("WeaponPickup");
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "Visual";
            Object.DestroyImmediate(mesh.GetComponent<Collider>());
            mesh.transform.SetParent(go.transform);
            mesh.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            mesh.transform.localScale = Vector3.one * 0.55f;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(litShader) { color = Color.cyan };
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.cyan * 2f);
            AssetDatabase.CreateAsset(mat, ArtPath + "/WeaponPickupMat.mat");
            mesh.GetComponent<Renderer>().sharedMaterial = mat;

            var pickup = go.AddComponent<WeaponPickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("visualRenderer").objectReferenceValue = mesh.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabPath}/WeaponPickup.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateChestPrefab(GameObject weaponPickupPrefab, WeaponData[] mainPool, SecondaryWeaponData[] secPool)
        {
            var go = new GameObject("Chest_Neon");
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material baseMat = new Material(litShader) { color = new Color(0.14f, 0.16f, 0.22f) };
            AssetDatabase.CreateAsset(baseMat, ArtPath + "/ChestBaseMat.mat");
            Material trimMat = new Material(litShader) { color = new Color(0.2f, 0.95f, 1f) };
            trimMat.EnableKeyword("_EMISSION");
            trimMat.SetColor("_EmissionColor", new Color(0.2f, 0.95f, 1f) * 2.2f);
            AssetDatabase.CreateAsset(trimMat, ArtPath + "/ChestTrimMat.mat");

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(go.transform);
            body.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            body.transform.localScale = new Vector3(1.2f, 0.7f, 0.85f);
            body.GetComponent<Renderer>().sharedMaterial = baseMat;

            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "NeonTrim";
            Object.DestroyImmediate(trim.GetComponent<Collider>());
            trim.transform.SetParent(go.transform);
            trim.transform.localPosition = new Vector3(0f, 0.68f, 0f);
            trim.transform.localScale = new Vector3(1.24f, 0.08f, 0.89f);
            trim.GetComponent<Renderer>().sharedMaterial = trimMat;

            var lidPivot = new GameObject("LidPivot");
            lidPivot.transform.SetParent(go.transform);
            lidPivot.transform.localPosition = new Vector3(0f, 0.7f, 0.42f);

            var lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "Lid";
            Object.DestroyImmediate(lid.GetComponent<Collider>());
            lid.transform.SetParent(lidPivot.transform);
            lid.transform.localPosition = new Vector3(0f, 0.12f, -0.42f);
            lid.transform.localScale = new Vector3(1.2f, 0.24f, 0.85f);
            lid.GetComponent<Renderer>().sharedMaterial = baseMat;

            var chest = go.AddComponent<ChestController>();
            var so = new SerializedObject(chest);
            so.FindProperty("weaponPickupPrefab").objectReferenceValue = weaponPickupPrefab;
            so.FindProperty("lidTransform").objectReferenceValue = lidPivot.transform;

            var mainProp = so.FindProperty("mainWeaponPool");
            mainProp.ClearArray();
            for (int i = 0; i < mainPool.Length; i++)
            {
                mainProp.InsertArrayElementAtIndex(i);
                mainProp.GetArrayElementAtIndex(i).objectReferenceValue = mainPool[i];
            }

            var secProp = so.FindProperty("secondaryWeaponPool");
            secProp.ClearArray();
            for (int i = 0; i < secPool.Length; i++)
            {
                secProp.InsertArrayElementAtIndex(i);
                secProp.GetArrayElementAtIndex(i).objectReferenceValue = secPool[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabPath}/Chest_Neon.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static SecondaryWeaponData CreateOrUpdateSecondary(string assetName, System.Action<SecondaryWeaponData> configure)
        {
            string path = $"{DataPath}/{assetName}.asset";
            var sec = AssetDatabase.LoadAssetAtPath<SecondaryWeaponData>(path);
            if (sec == null)
            {
                sec = ScriptableObject.CreateInstance<SecondaryWeaponData>();
                AssetDatabase.CreateAsset(sec, path);
            }
            configure(sec);
            EditorUtility.SetDirty(sec);
            return sec;
        }

        private static void CreateViewPart(
            Transform parent, string name, PrimitiveType type,
            Vector3 localPos, Vector3 localScale, Material mat, Quaternion? localRot = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (localRot.HasValue) go.transform.localRotation = localRot.Value;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static GameObject CreateBulletPrefab(string name, Sprite sprite, System.Type scriptType)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            var bb = go.AddComponent<BillboardSprite>();
            var bbSo = new SerializedObject(bb);
            bbSo.FindProperty("lockYAxis").boolValue = false;
            bbSo.ApplyModifiedPropertiesWithoutUndo();

            go.AddComponent(scriptType);
            string path = $"{PrefabPath}/{name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateEnemyPrefab(string name, Sprite sprite, EnemyPattern pattern, GameObject bulletPrefab, float hp, float interval, float bulletSpeed, int spreadCount)
        {
            var go = new GameObject(name);
            go.tag = "Enemy";

            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.height = 1.8f;
            col.radius = 0.5f;

            var vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform);
            vis.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            vis.transform.localScale = Vector3.one * 1.35f;
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            vis.AddComponent<BillboardSprite>();

            var enemy = go.AddComponent<EnemyBase>();
            var so = new SerializedObject(enemy);
            so.FindProperty("pattern").enumValueIndex = (int)pattern;
            so.FindProperty("maxHealth").floatValue = hp;
            so.FindProperty("enemyBulletPrefab").objectReferenceValue = bulletPrefab;
            so.FindProperty("fireInterval").floatValue = interval;
            so.FindProperty("bulletSpeed").floatValue = bulletSpeed;
            so.FindProperty("spreadCount").intValue = spreadCount;
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabPath}/{name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreatePickupPrefab(string name, Sprite sprite, PickupType type, int amount)
        {
            var go = new GameObject(name);
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.8f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            go.AddComponent<BillboardSprite>();

            var p = go.AddComponent<PickupItem>();
            var so = new SerializedObject(p);
            so.FindProperty("pickupType").enumValueIndex = (int)type;
            so.FindProperty("amount").intValue = amount;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabPath}/{name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static WeaponData CreateOrUpdateWeapon(string assetName, System.Action<WeaponData> configure)
        {
            string path = $"{DataPath}/{assetName}.asset";
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (weapon == null)
            {
                weapon = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(weapon, path);
            }
            configure(weapon);
            EditorUtility.SetDirty(weapon);
            return weapon;
        }

        private static Texture2D SaveTexture(string fileName, Texture2D tex, bool isSprite)
        {
            string fullPath = Path.Combine(ArtPath, fileName);
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(fullPath);
            importer.textureType = isSprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = isSprite ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
        }

        private static Sprite SaveSprite(string fileName, Texture2D tex, float ppu)
        {
            string fullPath = Path.Combine(ArtPath, fileName);
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(fullPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
        }

        private static Texture2D CreateNeonGridTexture(Color bg, Color line)
        {
            var tex = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool edge = x == 0 || y == 0 || x == 31 || y == 31;
                    bool sub = x == 16 || y == 16;
                    if (edge) tex.SetPixel(x, y, line);
                    else if (sub) tex.SetPixel(x, y, Color.Lerp(bg, line, 0.35f));
                    else tex.SetPixel(x, y, bg);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCircleTexture(int size, Color outer, Color inner)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            float rOuter = size * 0.45f;
            float rInner = size * 0.22f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (d <= rInner) tex.SetPixel(x, y, inner);
                    else if (d <= rOuter) tex.SetPixel(x, y, outer);
                    else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateMuzzleFlashTexture(Color tint)
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int dx = Mathf.Abs(x - 7);
                    int dy = Mathf.Abs(y - 7);
                    bool star = (dx <= 1 && dy <= 6) || (dy <= 1 && dx <= 6) || (dx + dy <= 4);
                    tex.SetPixel(x, y, star ? (dx + dy <= 2 ? Color.white : tint) : Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHitmarkerTexture()
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int dx = Mathf.Abs(x - 7);
                    int dy = Mathf.Abs(y - 7);
                    bool diag = Mathf.Abs(dx - dy) <= 1 && dx >= 3 && dx <= 6;
                    tex.SetPixel(x, y, diag ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateNeonDroneTexture(Color shell, Color core)
        {
            var tex = new Texture2D(24, 24, TextureFormat.RGBA32, false);
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 24; x++)
                {
                    int dx = Mathf.Abs(x - 12);
                    int dy = Mathf.Abs(y - 12);
                    Color c = Color.clear;
                    if (dx + dy <= 9)
                    {
                        c = shell;
                        if (dx + dy >= 8) c = Color.Lerp(shell, core, 0.55f); // neon rim
                        if (dx <= 2 && dy <= 2) c = core; // glowing eye
                        if (dx <= 1 && dy <= 1) c = Color.white;
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateNeonTurretTexture(Color shell, Color core)
        {
            var tex = new Texture2D(24, 24, TextureFormat.RGBA32, false);
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 24; x++)
                {
                    int dx = Mathf.Abs(x - 12);
                    int dy = Mathf.Abs(y - 12);
                    Color c = Color.clear;
                    bool inHex = dx <= 8 && dy <= 9 && (dx + dy <= 14);
                    if (inHex)
                    {
                        c = shell;
                        if (dx == 8 || dy == 9 || dx + dy == 14) c = core;
                        if (dx <= 3 && dy <= 2) c = core;
                        if (dx <= 1 && dy <= 1) c = Color.white;
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateNeonShotgunTexture()
        {
            var tex = new Texture2D(64, 48, TextureFormat.RGBA32, false);
            Color darkMetal = new Color(0.11f, 0.13f, 0.18f);
            Color midMetal = new Color(0.22f, 0.26f, 0.34f);
            Color neonCyan = new Color(0.15f, 0.92f, 1f);
            Color neonWhite = new Color(0.85f, 0.98f, 1f);

            for (int y = 0; y < 48; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    Color c = Color.clear;
                    // Stock / Grip
                    if (x >= 8 && x <= 24 && y >= 8 && y <= 22) c = darkMetal;
                    // Main Receiver
                    if (x >= 20 && x <= 44 && y >= 18 && y <= 31) c = midMetal;
                    // Upper & Lower SF Rail Barrels
                    if (x >= 38 && x <= 60 && y >= 24 && y <= 30) c = darkMetal;
                    if (x >= 34 && x <= 56 && y >= 18 && y <= 22) c = midMetal;

                    // Glowing Neon Cyan Plasma Core & Rail Strips
                    if (x >= 24 && x <= 38 && y >= 22 && y <= 25) c = neonCyan;
                    if (x >= 28 && x <= 34 && y >= 23 && y <= 24) c = neonWhite;
                    if (x >= 40 && x <= 59 && y == 27) c = neonCyan;
                    if (x == 60 && y >= 25 && y <= 29) c = neonWhite; // muzzle glow

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private static void EnsureFolders()
        {
            string[] folders = { RootPath, ArtPath, DataPath, PrefabPath, RootPath + "/Scenes" };
            foreach (var f in folders)
            {
                if (!Directory.Exists(f)) Directory.CreateDirectory(f);
            }
        }

        private static void EnsureTagExists(string tagName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName) return;
            }
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
