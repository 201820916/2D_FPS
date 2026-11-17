using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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

        [MenuItem("Tools/GungeonFPS/Build Prototype Scene")]
        public static void BuildPrototype()
        {
            EnsureFolders();
            EnsureTagExists("Enemy");

            // 1. Generate pixel art textures & sprites
            Texture2D floorTex = SaveTexture("floor_tile.png", CreateFloorTexture(), false);
            Texture2D wallTex = SaveTexture("wall_tile.png", CreateWallTexture(), false);

            Sprite playerBulletSp = SaveSprite("bullet_player.png", CreateCircleTexture(16, new Color(1f, 0.95f, 0.4f), Color.white), 16);
            Sprite enemyBulletSp = SaveSprite("bullet_enemy.png", CreateCircleTexture(16, new Color(1f, 0.25f, 0.15f), new Color(1f, 0.9f, 0.6f)), 16);
            Sprite chaserSp = SaveSprite("enemy_bulletkin.png", CreateBulletKinTexture(new Color(0.85f, 0.65f, 0.2f), new Color(0.9f, 0.2f, 0.2f)), 16);
            Sprite spreadSp = SaveSprite("enemy_shotgunkin.png", CreateBulletKinTexture(new Color(0.85f, 0.22f, 0.22f), new Color(0.2f, 0.2f, 0.25f)), 16);

            Sprite pistolSp = SaveSprite("gun_pistol.png", CreateGunTexture(new Color(0.55f, 0.6f, 0.68f), new Color(0.35f, 0.22f, 0.15f)), 16);
            Sprite shotgunSp = SaveSprite("gun_shotgun.png", CreateShotgunTexture(), 16);
            Sprite bounceSp = SaveSprite("gun_bounce.png", CreateGunTexture(new Color(0.15f, 0.85f, 0.95f), new Color(0.1f, 0.25f, 0.45f)), 16);

            Sprite muzzleSp = SaveSprite("muzzle_flash.png", CreateMuzzleFlashTexture(), 16);
            Sprite hitmarkerSp = SaveSprite("hitmarker.png", CreateHitmarkerTexture(), 16);
            Sprite heartSp = SaveSprite("pickup_heart.png", CreateCircleTexture(16, new Color(0.95f, 0.2f, 0.35f), new Color(1f, 0.7f, 0.75f)), 16);
            Sprite blankSp = SaveSprite("pickup_blank.png", CreateCircleTexture(16, new Color(0.25f, 0.8f, 1f), Color.white), 16);

            // 2. Create WeaponData ScriptableObjects
            WeaponData pistol = CreateOrUpdateWeapon("Weapon_Pistol", w =>
            {
                w.weaponName = "Rusty Sidearm";
                w.hudSprite = pistolSp;
                w.projectileColor = new Color(1f, 0.95f, 0.35f);
                w.fireRate = 5.5f;
                w.pelletsPerShot = 1;
                w.spreadAngle = 1.2f;
                w.damage = 18f;
                w.projectileSpeed = 40f;
                w.projectileScale = 0.35f;
                w.knockbackForce = 4f;
                w.maxBounces = 0;
                w.magazineSize = 12;
                w.maxTotalAmmo = -1;
                w.reloadTime = 0.7f;
                w.automatic = true;
                w.recoilKickPixels = 26f;
                w.screenShake = 0.035f;
            });

            WeaponData shotgun = CreateOrUpdateWeapon("Weapon_Shotgun", w =>
            {
                w.weaponName = "Room Sweeper";
                w.hudSprite = shotgunSp;
                w.projectileColor = new Color(1f, 0.55f, 0.2f);
                w.fireRate = 1.6f;
                w.pelletsPerShot = 8;
                w.spreadAngle = 7.5f;
                w.damage = 14f;
                w.projectileSpeed = 36f;
                w.projectileScale = 0.32f;
                w.knockbackForce = 9f;
                w.maxBounces = 0;
                w.magazineSize = 6;
                w.maxTotalAmmo = 60;
                w.reloadTime = 1.0f;
                w.automatic = false;
                w.recoilKickPixels = 52f;
                w.screenShake = 0.11f;
            });

            WeaponData bounceGun = CreateOrUpdateWeapon("Weapon_BounceGun", w =>
            {
                w.weaponName = "Ricochet Blaster";
                w.hudSprite = bounceSp;
                w.projectileColor = new Color(0.2f, 0.95f, 1f);
                w.fireRate = 4.0f;
                w.pelletsPerShot = 1;
                w.spreadAngle = 0.8f;
                w.damage = 24f;
                w.projectileSpeed = 32f;
                w.projectileScale = 0.45f;
                w.knockbackForce = 5f;
                w.maxBounces = 3;
                w.magazineSize = 15;
                w.maxTotalAmmo = 120;
                w.reloadTime = 0.85f;
                w.automatic = true;
                w.recoilKickPixels = 32f;
                w.screenShake = 0.05f;
            });

            // 3. Create Prefabs
            GameObject playerBulletPrefab = CreateBulletPrefab("PlayerBullet", playerBulletSp, typeof(PlayerProjectile));
            GameObject enemyBulletPrefab = CreateBulletPrefab("EnemyBullet", enemyBulletSp, typeof(EnemyBullet));

            GameObject chaserPrefab = CreateEnemyPrefab("Enemy_Chaser", chaserSp, EnemyPattern.SingleChaser, enemyBulletPrefab, 60f, 1.5f, 8.5f, 1);
            GameObject spreadPrefab = CreateEnemyPrefab("Enemy_Spread", spreadSp, EnemyPattern.SpreadTurret, enemyBulletPrefab, 90f, 2.0f, 7.5f, 5);

            GameObject heartPickup = CreatePickupPrefab("Pickup_Heart", heartSp, PickupType.Health, 2);
            GameObject blankPickup = CreatePickupPrefab("Pickup_Blank", blankSp, PickupType.Blank, 1);

            // 4. Build Scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.62f);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.97f, 0.92f);
            lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            // Managers
            var systems = new GameObject("GameSystems");
            systems.AddComponent<ProjectilePool>();
            systems.AddComponent<RetroAudio>();
            systems.AddComponent<VfxSpawner>();

            // Mini-Dungeon (3 Connected Rooms along +Z)
            BuildMiniDungeon(floorTex, wallTex, chaserPrefab, spreadPrefab, heartPickup, blankPickup);

            // HUD & Player
            CreateHUDAndPlayer(
                playerBulletPrefab,
                new[] { pistol, shotgun, bounceGun },
                muzzleSp,
                hitmarkerSp);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[GungeonPrototypeBuilder] Upgraded 3-room prototype scene built at: " + ScenePath);
        }

        private static void BuildMiniDungeon(
            Texture2D floorTex,
            Texture2D wallTex,
            GameObject chaserPrefab,
            GameObject spreadPrefab,
            GameObject heartPickup,
            GameObject blankPickup)
        {
            var dungeon = new GameObject("MiniDungeon");

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material floorMat = new Material(litShader) { mainTexture = floorTex };
            floorMat.mainTextureScale = new Vector2(10f, 10f);
            AssetDatabase.CreateAsset(floorMat, ArtPath + "/FloorMat.mat");

            Material wallMat = new Material(litShader) { mainTexture = wallTex };
            wallMat.mainTextureScale = new Vector2(8f, 2f);
            AssetDatabase.CreateAsset(wallMat, ArtPath + "/WallMat.mat");

            Material doorMat = new Material(litShader) { color = new Color(0.75f, 0.3f, 0.2f) };
            AssetDatabase.CreateAsset(doorMat, ArtPath + "/DoorMat.mat");

            // Room 0: Start Room (16x16 at z=0, North doorway)
            BuildRoomShell(dungeon.transform, "StartRoom", new Vector3(0f, 0f, 0f), 16f, 16f, false, true, floorMat, wallMat);

            // Corridor 0 -> 1 (width 6, z=8..14)
            BuildCorridor(dungeon.transform, "Corridor_0_1", new Vector3(0f, 0f, 11f), 6f, 6f, floorMat, wallMat);

            // Room 1: Combat Room 1 (28x28 at z=28, South & North doorways)
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
            AttachRoomController(room1Go, "Chamber 1", new Vector3(26f, 4f, 26f), new[] { door1S, door1N }, r1Enemies, heartPickup);

            // Corridor 1 -> 2 (width 6, z=42..48)
            BuildCorridor(dungeon.transform, "Corridor_1_2", new Vector3(0f, 0f, 45f), 6f, 6f, floorMat, wallMat);

            // Room 2: Combat Room 2 (32x32 at z=64, South doorway only)
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
            AttachRoomController(room2Go, "Chamber 2 (Boss)", new Vector3(30f, 4f, 30f), new[] { door2S }, r2Enemies, blankPickup);
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

            // West & East walls
            CreateBox(room.transform, "Wall_West", center + new Vector3(-halfW - 0.5f, wallY, 0f), new Vector3(1f, wallH, depth + 2f), wallMat);
            CreateBox(room.transform, "Wall_East", center + new Vector3(halfW + 0.5f, wallY, 0f), new Vector3(1f, wallH, depth + 2f), wallMat);

            // North wall (with optional 6m doorway)
            BuildWallWithDoorway(room.transform, "North", center + new Vector3(0f, wallY, halfD + 0.5f), width, wallH, northDoorway, wallMat);
            // South wall (with optional 6m doorway)
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
            GameObject playerBulletPrefab, WeaponData[] weapons, Sprite muzzleSp, Sprite hitmarkerSp)
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
            cam.fieldOfView = 80f;
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();

            var fireOrigin = new GameObject("FireOrigin");
            fireOrigin.transform.SetParent(camGo.transform);
            fireOrigin.transform.localPosition = new Vector3(0.25f, -0.15f, 0.55f);

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

            // Crosshair + Hitmarker
            var crossGo = new GameObject("Crosshair");
            crossGo.transform.SetParent(canvasGo.transform, false);
            var crossImg = crossGo.AddComponent<Image>();
            crossImg.color = new Color(1f, 1f, 1f, 0.85f);
            crossImg.raycastTarget = false;
            crossImg.rectTransform.sizeDelta = new Vector2(8f, 8f);

            var hitGo = new GameObject("Hitmarker");
            hitGo.transform.SetParent(canvasGo.transform, false);
            var hitImg = hitGo.AddComponent<Image>();
            hitImg.sprite = hitmarkerSp;
            hitImg.color = new Color(1f, 0.25f, 0.25f, 0.95f);
            hitImg.raycastTarget = false;
            hitImg.rectTransform.sizeDelta = new Vector2(36f, 36f);

            // 2D HUD Weapon + Muzzle Flash
            var weaponImgGo = new GameObject("HUDWeapon");
            weaponImgGo.transform.SetParent(canvasGo.transform, false);
            var weaponImg = weaponImgGo.AddComponent<Image>();
            weaponImg.raycastTarget = false;
            var wRect = weaponImg.rectTransform;
            wRect.anchorMin = new Vector2(0.5f, 0f);
            wRect.anchorMax = new Vector2(0.5f, 0f);
            wRect.pivot = new Vector2(0.5f, 0f);
            wRect.anchoredPosition = new Vector2(220f, -10f);
            wRect.sizeDelta = new Vector2(420f, 315f);

            var muzzleGo = new GameObject("HUDMuzzleFlash");
            muzzleGo.transform.SetParent(weaponImgGo.transform, false);
            var muzzleImg = muzzleGo.AddComponent<Image>();
            muzzleImg.sprite = muzzleSp;
            muzzleImg.raycastTarget = false;
            muzzleImg.enabled = false;
            var mRect = muzzleImg.rectTransform;
            mRect.anchoredPosition = new Vector2(85f, 75f);
            mRect.sizeDelta = new Vector2(130f, 130f);

            // Status Text
            var textGo = new GameObject("StatusText");
            textGo.transform.SetParent(canvasGo.transform, false);
            var statusText = textGo.AddComponent<Text>();
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 26;
            statusText.color = Color.white;
            statusText.raycastTarget = false;
            var tRect = statusText.rectTransform;
            tRect.anchorMin = new Vector2(0f, 1f);
            tRect.anchorMax = new Vector2(0f, 1f);
            tRect.pivot = new Vector2(0f, 1f);
            tRect.anchoredPosition = new Vector2(30f, -25f);
            tRect.sizeDelta = new Vector2(1400f, 140f);

            // Dash Bar BG + Fill
            var dashBgGo = new GameObject("DashBarBG");
            dashBgGo.transform.SetParent(canvasGo.transform, false);
            var dashBg = dashBgGo.AddComponent<Image>();
            dashBg.color = new Color(0f, 0f, 0f, 0.55f);
            var bgRect = dashBg.rectTransform;
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.pivot = new Vector2(0f, 1f);
            bgRect.anchoredPosition = new Vector2(30f, -110f);
            bgRect.sizeDelta = new Vector2(240f, 16f);

            var dashFillGo = new GameObject("DashBarFill");
            dashFillGo.transform.SetParent(dashBgGo.transform, false);
            var dashFill = dashFillGo.AddComponent<Image>();
            dashFill.color = new Color(0.25f, 0.9f, 1f, 1f);
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
            wcSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire GameHUD
            var hud = canvasGo.AddComponent<GameHUD>();
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            hudSo.FindProperty("playerController").objectReferenceValue = playerCtrl;
            hudSo.FindProperty("weaponController").objectReferenceValue = wc;
            hudSo.FindProperty("statusText").objectReferenceValue = statusText;
            hudSo.FindProperty("dashFillBar").objectReferenceValue = dashFill;
            hudSo.FindProperty("hitmarkerImage").objectReferenceValue = hitImg;
            hudSo.ApplyModifiedPropertiesWithoutUndo();
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
            vis.transform.localScale = Vector3.one * 1.25f;
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

        private static Texture2D CreateFloorTexture()
        {
            var tex = new Texture2D(16, 16);
            Color c1 = new Color(0.28f, 0.3f, 0.36f);
            Color c2 = new Color(0.22f, 0.24f, 0.3f);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool checker = ((x / 8) + (y / 8)) % 2 == 0;
                    tex.SetPixel(x, y, checker ? c1 : c2);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateWallTexture()
        {
            var tex = new Texture2D(16, 16);
            Color mortar = new Color(0.25f, 0.23f, 0.28f);
            Color brick = new Color(0.45f, 0.43f, 0.5f);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool rowLine = y == 0 || y == 8;
                    int offset = (y < 8) ? 0 : 8;
                    bool colLine = (x + offset) % 16 == 0;
                    tex.SetPixel(x, y, (rowLine || colLine) ? mortar : brick);
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

        private static Texture2D CreateMuzzleFlashTexture()
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int dx = Mathf.Abs(x - 7);
                    int dy = Mathf.Abs(y - 7);
                    bool star = (dx <= 1 && dy <= 6) || (dy <= 1 && dx <= 6) || (dx + dy <= 4);
                    tex.SetPixel(x, y, star ? (dx + dy <= 2 ? Color.white : new Color(1f, 0.85f, 0.2f)) : Color.clear);
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

        private static Texture2D CreateBulletKinTexture(Color bodyColor, Color bandColor)
        {
            var tex = new Texture2D(24, 24, TextureFormat.RGBA32, false);
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 24; x++)
                {
                    Color c = Color.clear;
                    bool inBody = x >= 6 && x <= 17 && y >= 2 && y <= 18;
                    bool inTip = x >= 8 && x <= 15 && y >= 19 && y <= 21;
                    if (inBody || inTip)
                    {
                        c = bodyColor;
                        if (y == 14 || y == 15) c = bandColor;
                        if (y >= 10 && y <= 12 && (x == 9 || x == 14)) c = Color.black;
                        if (y == 2 || x == 6 || x == 17) c = bodyColor * 0.6f;
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateGunTexture(Color metal, Color grip)
        {
            var tex = new Texture2D(32, 24, TextureFormat.RGBA32, false);
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    Color c = Color.clear;
                    if (x >= 10 && x <= 26 && y >= 12 && y <= 17) c = metal;
                    if (x >= 11 && x <= 15 && y >= 3 && y <= 11) c = grip;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateShotgunTexture()
        {
            var tex = new Texture2D(32, 24, TextureFormat.RGBA32, false);
            Color metal = new Color(0.45f, 0.48f, 0.55f);
            Color wood = new Color(0.55f, 0.3f, 0.14f);
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    Color c = Color.clear;
                    if (x >= 8 && x <= 29 && y >= 12 && y <= 16) c = metal;
                    if (x >= 4 && x <= 14 && y >= 5 && y <= 11) c = wood;
                    if (x >= 17 && x <= 24 && y >= 10 && y <= 12) c = wood;
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
