using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Zombar
{
    public sealed class ZombieBarGame : MonoBehaviour
    {
        private sealed class Patron
        {
            public string id;
            public string recipe;
            public GameObject root;
            public TextMesh sign;
            public BarTarget serveTarget;
            public BarTarget fightTarget;
            public BarTarget harvestTarget;
            public float patience;
            public float attackIn;
            public int health;
            public bool angry;
            public bool giant;
            public bool corpse;
        }

        private sealed class Trader
        {
            public GameObject root;
            public BarTarget offerTarget;
            public BarTarget dismissTarget;
            public string resourceId;
            public string resourceName;
            public int price;
            public float leavesAt;
        }

        public static ZombieBarGame Instance { get; private set; }
        public bool RightArmDisabled { get { return rightArmDisabled; } }
        public bool LeftArmDisabled { get { return leftArmDisabled; } }
        public bool GameOver => gameOver;
        public BarHud Hud { get; private set; }
        public int PhysicalQuality => Mathf.Clamp(55 + qualityLevel * 12, 0, 100);
        public void Tell(string message) => Notify(message);
        public bool TakePhysicalStock(PropKind ingredient)
        {
            if (gameOver) return false;
            if (ingredient == PropKind.Eye && eyes > 0) { eyes--; return true; }
            if (ingredient == PropKind.Flesh && flesh > 0) { flesh--; return true; }
            if (ingredient == PropKind.BloodBottle && blood > 0) { blood--; return true; }
            return false;
        }
        public void PhysicalServe(string id, int recipe, int quality)
        {
            if (gameOver || recipe < 0 || recipe >= recipes.Length) return;
            readyDrink = recipes[recipe]; drinkQuality = quality;
            ServePatron(id); RefreshHud();
        }
        public int CurrentOrder
        {
            get
            {
                foreach (var patron in patrons)
                    if (!patron.corpse && !patron.root.GetComponent<PhysicalPatron>().Approaching) return System.Array.IndexOf(recipes, patron.recipe);
                return -1;
            }
        }

        public void PhysicalPunch(string id) { if (!gameOver) PunchPatron(id); }
        public void PhysicalHarvest(string id) { if (!gameOver) HarvestCorpse(id); }
        public void PhysicalPump() { if (!gameOver) PumpFlood(); }
        public void PhysicalHeal() { if (!gameOver) RestoreBody(); }
        public void PhysicalDrawBlood() { if (!gameOver) DrawHelperBlood(); }
        public void PhysicalWipe() { if (!gameOver) { barDamage = Mathf.Max(0, barDamage - 1); Notify("Стойка вытерта."); } }
        public bool PhysicalExtinguish(Vector3 pourPosition)
        {
            if (gameOver || !fireActive || blood < 1) return false;
            var fire = GameObject.Find("ПОЖАР");
            if (fire == null || Vector3.Distance(pourPosition, fire.transform.position + Vector3.up * 0.4f) > 0.6f) return false;
            ExtinguishFire(); return true;
        }
        public void PhysicalPurchase(BarAction action)
        {
            if (gameOver) return;
            if (action == BarAction.UpgradeQuality) UpgradeQuality();
            else if (action == BarAction.UpgradeStrength) UpgradeStrength();
            else if (action == BarAction.BuyIceBox || action == BarAction.BuySprinkler || action == BarAction.BuyDrain) BuyEquipment(action);
        }

        [Header("Starting stock")]
        [SerializeField] private int blood = 2;
        [SerializeField] private int eyes = 2;
        [SerializeField] private int flesh = 2;
        [SerializeField] private int bones;
        [SerializeField] private int money = 20;

        private readonly List<Patron> patrons = new List<Patron>();
        private readonly string[] recipes = CocktailRecipes.Names;
        private readonly Color zombieGreen = new Color(0.28f, 0.53f, 0.38f);
        private readonly Color zombiePurple = new Color(0.39f, 0.27f, 0.48f);

        private Camera playerCamera;
        private Trader trader;
        private string readyDrink = "";
        private int drinkQuality;
        private int qualityLevel;
        private int strengthLevel;
        private bool iceBox;
        private bool sprinkler;
        private bool drainUpgrade;
        private bool fireActive;
        private bool floodActive;
        private bool gameOver;
        private bool rightArmDisabled;
        private bool leftArmDisabled;
        private bool eyeInjured;
        private float eventIn = 120f;
        private float traderIn = 32f;
        private float guestIn;
        private float hudIn;
        private float helperArmReady;
        private float helperEyeReady;
        private float helperBloodReady;
        private int floodPumps;
        private int servedCount;
        private int playerHealth = 3;
        private int barDamage;
        private float fireLeft;
        private Transform helperArm;
        private Transform helperEye;
        private float punchReady;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(Instance.gameObject);
            Instance = this;
            blood = Mathf.Max(1, blood);
            eyes = Mathf.Max(1, eyes);
            flesh = Mathf.Max(1, flesh);
        }

        private void OnEnable()
        {
            Instance = this;
            Hud = GetComponent<BarHud>();
        }

        private void Start()
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                GameObject cameraObject = new GameObject("Bar View Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 1.6f, -3.2f);
                cameraObject.transform.rotation = Quaternion.identity;
                playerCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                cameraObject.AddComponent<BarPlayerInput>();
            }

            Hud = gameObject.AddComponent<BarHud>();
            Hud.Initialize(playerCamera);
            if (BarAudio.Instance == null) gameObject.AddComponent<BarAudio>();
            var helper = GameObject.Find("Помощник · регенерирует");
            if (helper != null)
            {
                helperArm = helper.transform.Find("Левая рука");
                foreach (Transform part in helper.transform)
                    if (part.name.Contains("глаз") || part.name.Contains("Глаз")) { helperEye = part; break; }
            }
            Notify("Возьмите ингредиенты рукой, смешайте в шейкере и передайте стакан клиенту.");
            SpawnPatron(false);
            SpawnPatron(true);
            RefreshHud();
            if (Hud != null) Hud.Notify("Новая смена. Приготовьте коктейль по заказу клиента.");
        }

        private void Update()
        {
            if (helperArm != null) helperArm.gameObject.SetActive(Time.time >= helperArmReady);
            if (helperEye != null) helperEye.gameObject.SetActive(Time.time >= helperEyeReady);
            float dt = Time.deltaTime;
            if (!gameOver)
                TickPatrons(dt);
            if (fireActive && !gameOver)
            {
                fireLeft -= dt;
                if (fireLeft <= 0f)
                    FireSpreads();
            }

            if (!fireActive && !floodActive && !gameOver)
            {
                eventIn -= dt;
                if (eventIn <= 0f)
                    StartRandomEvent();
            }

            if (!gameOver && patrons.Count < 2)
            {
                guestIn -= dt;
                if (guestIn <= 0f)
                {
                    bool giant = servedCount >= 3 && Random.value < 0.34f;
                    SpawnPatron(giant);
                    guestIn = Random.Range(9f, 14f);
                }
            }

            if (Hud != null && hudIn <= 0f)
            {
                RefreshHud();
                hudIn = 0.25f;
            }
            hudIn -= dt;

        }

        public void Interact(BarTarget target)
        {
            if (target == null || gameOver)
                return;

            switch (target.action)
            {
                case BarAction.BrewBloodFlip: Brew(0); break;
                case BarAction.BrewRotSour: Brew(1); break;
                case BarAction.BrewEyeHighball: Brew(2); break;
                case BarAction.HarvestArm: HarvestHelperArm(); break;
                case BarAction.HarvestEye: HarvestHelperEye(); break;
                case BarAction.DrawBlood: DrawHelperBlood(); break;
                case BarAction.RestoreBody: RestoreBody(); break;
                case BarAction.ServePatron: ServePatron(target.subjectId); break;
                case BarAction.PunchPatron: PunchPatron(target.subjectId); break;
                case BarAction.HarvestCorpse: HarvestCorpse(target.subjectId); break;
                case BarAction.ExtinguishFire: ExtinguishFire(); break;
                case BarAction.PumpFlood: PumpFlood(); break;
                case BarAction.UpgradeQuality: UpgradeQuality(); break;
                case BarAction.UpgradeStrength: UpgradeStrength(); break;
                case BarAction.BuyIceBox: BuyEquipment(BarAction.BuyIceBox); break;
                case BarAction.BuySprinkler: BuyEquipment(BarAction.BuySprinkler); break;
                case BarAction.BuyDrain: BuyEquipment(BarAction.BuyDrain); break;
                case BarAction.CleanBar: CleanBar(); break;
                case BarAction.TradeOffer: TradeWithTrader(target.subjectId); break;
                case BarAction.DismissTrader: CloseTrader(true); break;
            }
            RefreshHud();
        }

        private void Brew(int recipe)
        {
            if (!string.IsNullOrEmpty(readyDrink)) { Notify("Сначала отдайте уже приготовленный коктейль."); return; }
            int needBlood = CocktailRecipes.Portions[recipe].x;
            int needEyes = CocktailRecipes.Portions[recipe].y;
            int needFlesh = CocktailRecipes.Portions[recipe].z;
            if (blood < needBlood || eyes < needEyes || flesh < needFlesh)
            {
                Notify("Не хватает ингредиентов. Помощник восстанавливается рядом.");
                return;
            }

            blood -= needBlood;
            eyes -= needEyes;
            flesh -= needFlesh;
            readyDrink = recipes[recipe];
            drinkQuality = Mathf.Clamp(50 + qualityLevel * 12 + Random.Range(-8, 13), 0, 100);
            BarAudio.Play("brew");
            Notify("Готово: " + readyDrink + " · качество " + drinkQuality + "%");
        }

        private void ServePatron(string id)
        {
            Patron patron = FindPatron(id);
            if (patron == null || patron.corpse)
            {
                Notify("Этот клиент уже ушёл.");
                return;
            }
            if (string.IsNullOrEmpty(readyDrink))
            {
                Notify("Сначала смешайте коктейль на стойке.");
                return;
            }

            if (readyDrink == patron.recipe)
            {
                int tip = 7 + Mathf.RoundToInt(drinkQuality * 0.08f) + (qualityLevel > 0 ? qualityLevel : 0);
                money += tip;
                servedCount++;
                BarAudio.Play("coin");
                RemovePatron(patron);
                readyDrink = "";
                Notify("Заказ принят! +" + tip + " жетонов.");
            }
            else
            {
                patron.patience = Mathf.Max(0f, patron.patience - 14f);
                readyDrink = "";
                Notify("Не тот рецепт! Клиент теряет терпение.");
            }
        }

        private void TickPatrons(float dt)
        {
            for (int i = patrons.Count - 1; i >= 0; i--)
            {
                Patron patron = patrons[i];
                if (patron.corpse || patron.root.GetComponent<PhysicalPatron>().Approaching)
                    continue;
                if (!patron.angry)
                {
                    patron.patience -= dt;
                    if (patron.patience <= 0f)
                    {
                        patron.angry = true;
                        patron.attackIn = patron.giant ? 3f : 6f;
                        SetPatronLook(patron, true);
                        if (!fireActive && !floodActive && Random.value < 0.4f) StartFire();
                        Notify(patron.giant ? "ТИТАН ВЗБЕСИЛСЯ! Его нельзя победить." : "Клиент буянит! Ударьте его или обслужите.");
                    }
                }
                else
                {
                    patron.attackIn -= dt;
                    if (patron.attackIn <= 0f)
                    {
                        DamagePlayer(patron.giant ? "Титан ударил по стойке и сорвал защиту" : "Дебошир оторвал часть руки или повредил глаз");
                        if (!patron.giant)
                        {
                            barDamage++;
                            if (barDamage >= 2)
                            {
                                barDamage = 0;
                                money = Mathf.Max(0, money - 3);
                            }
                        }
                        patron.attackIn = patron.giant ? 3.5f : 7.5f;
                    }
                }
                UpdatePatronSign(patron);
            }
        }

        private void PunchPatron(string id)
        {
            if (Time.time < punchReady) return;
            punchReady = Time.time + 0.4f;
            Patron patron = FindPatron(id);
            if (patron == null || patron.corpse)
                return;
            if (patron.giant)
            {
                DamagePlayer("Удары лишь разозлили титана. Этого громилу не победить!");
                SetPatronLook(patron, true);
                patron.angry = true;
                patron.attackIn = Mathf.Min(patron.attackIn, 2f);
                return;
            }

            if (!patron.angry)
            {
                patron.angry = true;
                patron.attackIn = 6f;
            }
            patron.health -= 1 + strengthLevel;
            BarAudio.Play("hit");
            SetPatronLook(patron, true);
            if (patron.health <= 0)
            {
                patron.corpse = true;
                patron.angry = false;
                if (patron.serveTarget != null) patron.serveTarget.gameObject.SetActive(false);
                if (patron.fightTarget != null) patron.fightTarget.gameObject.SetActive(false);
                patron.root.GetComponent<PhysicalPatron>().Defeat();
                Notify("Дебошир повержен. Его тело можно переработать в ингредиенты.");
            }
            else
            {
                Notify("Удар! Ещё " + patron.health + " попадание(й) до победы.");
            }
        }

        private void HarvestCorpse(string id)
        {
            Patron patron = FindPatron(id);
            if (patron == null || !patron.corpse)
                return;
            flesh += iceBox ? 4 : 2;
            blood += 1;
            bones++;
            RemovePatron(patron);
            BarAudio.Play("harvest");
            Notify("Получены плоть и кровь. Холодильник увеличивает добычу плоти.");
        }

        private void SpawnPatron(bool giant)
        {
            string id = "guest_" + System.Guid.NewGuid().ToString("N").Substring(0, 7);
            int recipe = servedCount == 0 ? (giant ? 1 : 0) : Random.Range(0, recipes.Length);
            bool leftSlotUsed = false;
            bool rightSlotUsed = false;
            for (int i = 0; i < patrons.Count; i++)
            {
                if (patrons[i].corpse) continue;
                if (patrons[i].root.transform.position.x < 0f) leftSlotUsed = true;
                else rightSlotUsed = true;
            }
            if (leftSlotUsed && rightSlotUsed) return;
            float x = leftSlotUsed ? (giant ? 1.05f : 0.72f) : -0.72f;
            Vector3 position = new Vector3(x, 0f, 0.55f);
            GameObject body = CreatePatronModel(id, position, giant);
            var visitor = body.GetComponent<PhysicalPatron>();
            visitor.order = recipe;
            visitor.Enter(position, patrons.Count > 0 || servedCount > 0);
            Patron patron = new Patron
            {
                id = id,
                recipe = recipes[recipe],
                root = body,
                patience = servedCount == 0 ? 300f : giant ? 180f : Random.Range(150f, 180f),
                health = 2,
                giant = giant,
                angry = false,
                attackIn = 6f
            };
            patrons.Add(patron);
            UpdatePatronSign(patron);
        }

        private bool nextPatronIsFemale;
        private GameObject CreatePatronModel(string id, Vector3 position, bool giant)
        {
            var prefab = Resources.Load<GameObject>("PhysicalBar/" + (giant ? "Titan" : nextPatronIsFemale ? "PatronFemale" : "PatronMale"));
            if (!giant) nextPatronIsFemale = !nextPatronIsFemale;
            if (prefab == null) throw new System.InvalidOperationException("Run PhysicalBarSceneBuilder.Build before playing.");
            var root = Instantiate(prefab, position, Quaternion.identity);
            root.name = giant ? "Титан-клиент" : "Зомби-клиент";

            var actor = root.AddComponent<PhysicalPatron>();
            actor.id = id; actor.giant = giant;
            var sign = CreateText("Заказ", root.transform, new Vector3(0, giant ? 2.15f : 1.78f, 0), 0.03f, 40, Color.white);
            var layout = sign.GetComponent<BarLabelLayout>();
            layout.width = 0.60f; layout.height = 0.12f;
            pendingSigns[id] = sign;
            return root;
        }

        private readonly Dictionary<string, TextMesh> pendingSigns = new Dictionary<string, TextMesh>();

        private void UpdatePatronSign(Patron patron)
        {
            if (patron.sign == null)
            {
                pendingSigns.TryGetValue(patron.id, out patron.sign);
                pendingSigns.Remove(patron.id);
            }
            if (patron.sign == null) return;
            string mood = patron.angry ? "БУЯНИТ" : "ЖДЁТ";
            string countdown = patron.angry ? "" : " " + Mathf.CeilToInt(patron.patience) + "с";
            patron.sign.text = (patron.giant ? "ТИТАН · " : "") + patron.recipe + "\n" + mood + countdown;
            patron.sign.color = patron.angry ? new Color(1f, 0.35f, 0.24f) : Color.white;
        }

        private void SetPatronLook(Patron patron, bool angry)
        {
            var actor = patron.root.GetComponent<PhysicalPatron>();
            if (actor != null) actor.SetAngry(angry);
            Renderer[] renderers = patron.root.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                string partName = renderers[i].gameObject.name;
                if (renderers[i] is MeshRenderer &&
                    (partName.StartsWith("Лицо ·") || partName.StartsWith("Кожа ·")))
                {
                    Material material = renderers[i].material;
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", angry ? new Color(0.65f, 0.2f, 0.2f) : zombieGreen);
                    else if (material.HasProperty("_Color")) material.color = angry ? new Color(0.65f, 0.2f, 0.2f) : zombieGreen;
                }
            }
        }

        private void RemovePatron(Patron patron)
        {
            if (patron.root != null) Destroy(patron.root);
            if (patron.serveTarget != null) Destroy(patron.serveTarget.gameObject);
            if (patron.fightTarget != null) Destroy(patron.fightTarget.gameObject);
            if (patron.harvestTarget != null) Destroy(patron.harvestTarget.gameObject);
            pendingSigns.Remove(patron.id);
            patrons.Remove(patron);
            guestIn = Random.Range(5f, 9f);
        }

        private Patron FindPatron(string id)
        {
            for (int i = 0; i < patrons.Count; i++)
                if (patrons[i].id == id) return patrons[i];
            return null;
        }

        private void HarvestHelperArm()
        {
            if (Time.time < helperArmReady) { Notify("Помощник отращивает новую конечность."); return; }
            flesh += iceBox ? 3 : 2;
            bones += 1;
            helperArmReady = Time.time + 16f;
            BarAudio.Play("harvest");
            Notify("Помощник отдал конечность. Он восстановится через 16 секунд.");
        }

        private void HarvestHelperEye()
        {
            if (Time.time < helperEyeReady) { Notify("Глаз помощника ещё восстанавливается."); return; }
            eyes += 2;
            helperEyeReady = Time.time + 12f;
            BarAudio.Play("harvest");
            Notify("Получено два глаза. Помощник уже восстанавливается.");
        }

        private void DrawHelperBlood()
        {
            if (Time.time < helperBloodReady) { Notify("Помощник снова наберётся крови через пару секунд."); return; }
            blood += 2;
            helperBloodReady = Time.time + 8f;
            Notify("Получена свежая кровь для коктейлей и тушения.");
        }

        private void RestoreBody()
        {
            if (!rightArmDisabled && !leftArmDisabled && !eyeInjured) { Notify("Пока нечего восстанавливать."); return; }
            if (blood < 2) { Notify("Нужно 2 порции крови для восстановления."); return; }
            blood -= 2;
            rightArmDisabled = false;
            leftArmDisabled = false;
            eyeInjured = false;
            playerHealth = 3;
            RemoveVignette();
            Notify("Помощник восстановил зрение и обе руки.");
        }

        private void DamagePlayer(string reason)
        {
            if (gameOver) return;
            BarAudio.Play("hurt");
            playerHealth--;
            if (!rightArmDisabled && !leftArmDisabled)
            {
                if (Random.value < 0.55f) rightArmDisabled = true;
                else leftArmDisabled = true;
            }
            else if (!eyeInjured)
            {
                eyeInjured = true;
                CreateVignette();
            }
            else if (!rightArmDisabled) rightArmDisabled = true;
            else if (!leftArmDisabled) leftArmDisabled = true;

            Notify(reason + " · травма. Помощник может восстановить тело.");
            if (playerHealth <= 0)
            {
                gameOver = true;
                Notify("Бар закрыт после трёх тяжёлых ударов. Возьмите и встряхните колокольчик для новой смены.");
            }
        }

        private void CreateVignette()
        {
            if (playerCamera == null || playerCamera.transform.Find("Зрение сужено") != null) return;
            Transform mask = new GameObject("Зрение сужено").transform;
            mask.SetParent(playerCamera.transform, false);
            Color tint = new Color(0.015f, 0.006f, 0.025f, 0.92f);
            Material material = CreateMaterial(tint);
            CreateMaskPanel(mask, material, new Vector3(-0.142f, 0f, 0.26f), new Vector3(0.11f, 0.54f, 1f));
            CreateMaskPanel(mask, material, new Vector3(0.142f, 0f, 0.26f), new Vector3(0.11f, 0.54f, 1f));
            CreateMaskPanel(mask, material, new Vector3(0f, 0.19f, 0.26f), new Vector3(0.42f, 0.09f, 1f));
            CreateMaskPanel(mask, material, new Vector3(0f, -0.19f, 0.26f), new Vector3(0.42f, 0.09f, 1f));
        }

        private void CreateMaskPanel(Transform parent, Material material, Vector3 position, Vector3 scale)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panel.name = "Поле зрения";
            panel.transform.SetParent(parent, false);
            panel.transform.localPosition = position;
            panel.transform.localScale = scale;
            Collider collider = panel.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            panel.GetComponent<Renderer>().material = material;
            panel.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            panel.GetComponent<Renderer>().receiveShadows = false;
        }

        private void RemoveVignette()
        {
            if (playerCamera == null) return;
            Transform mask = playerCamera.transform.Find("Зрение сужено");
            if (mask != null) Destroy(mask.gameObject);
        }

        private void StartRandomEvent()
        {
            if (Random.value < 0.58f)
            {
                StartFire();
            }
            else
            {
                floodActive = true;
                floodPumps = 0;
                MakeHazardVisual("ПОТОП", new Vector3(-1.4f, 0.12f, -0.4f), new Color(0.1f, 0.5f, 0.92f));
                Notify("Прорвало трубу! Трижды прокачайте ручной насос.");
            }
        }

        private void StartFire()
        {
            if (fireActive) return;
            fireActive = true;
            fireLeft = sprinkler ? 28f : 18f;
            MakeHazardVisual("ПОЖАР", new Vector3(1.35f, 0.65f, 0.1f), new Color(1f, 0.23f, 0.04f));
            BarAudio.Play("alarm");
            Notify("Пожар! Плесните кровью на огонь.");
        }

        private void FireSpreads()
        {
            fireActive = false;
            DestroyHazard("ПОЖАР");
            barDamage++;
            money = Mathf.Max(0, money - (sprinkler ? 3 : 6));
            Notify("Огонь распространился по бару. Утеряны жетоны и часть запасов.");
            blood = Mathf.Max(0, blood - 1);
            eventIn = Random.Range(42f, 58f);
        }

        private void ExtinguishFire()
        {
            if (!fireActive) { Notify("Сейчас пожара нет."); return; }
            if (blood < 1) { Notify("Чтобы потушить огонь, нужна хотя бы одна порция крови."); return; }
            blood--;
            fireActive = false;
            DestroyHazard("ПОЖАР");
            eventIn = Random.Range(48f, 65f);
            Notify("Кровь погасила пожар. Бар спасён!");
        }

        private void PumpFlood()
        {
            if (!floodActive) { Notify("Под стойкой сухо."); return; }
            floodPumps += drainUpgrade ? 2 : 1;
            if (floodPumps >= 3)
            {
                floodActive = false;
                DestroyHazard("ПОТОП");
                money += 3;
                eventIn = Random.Range(48f, 65f);
                Notify("Насос осушил бар! +3 жетона за быструю реакцию.");
            }
            else
            {
                Notify("Насос качает воду: " + floodPumps + "/3");
            }
        }

        private void UpgradeQuality()
        {
            int cost = 12 + qualityLevel * 10;
            if (money < cost) { Notify("Для рецептов нужно " + cost + " жетонов."); return; }
            money -= cost;
            qualityLevel++;
            Notify("Рецепты улучшены. Коктейли дают больше жетонов.");
        }

        private void UpgradeStrength()
        {
            int cost = 12 + strengthLevel * 10;
            if (money < cost) { Notify("Для силы нужно " + cost + " жетонов."); return; }
            money -= cost;
            strengthLevel++;
            Notify("Сила увеличена: один удар наносит " + (1 + strengthLevel) + " урона.");
        }

        private void BuyEquipment(BarAction item)
        {
            int cost = 16;
            if (money < cost) { Notify("Оборудование стоит 16 жетонов."); return; }
            switch (item)
            {
                case BarAction.BuyIceBox:
                    if (iceBox) { Notify("Холодильник уже установлен."); return; }
                    iceBox = true;
                    break;
                case BarAction.BuySprinkler:
                    if (sprinkler) { Notify("Кровавый спринклер уже установлен."); return; }
                    sprinkler = true;
                    break;
                case BarAction.BuyDrain:
                    if (drainUpgrade) { Notify("Усиленный насос уже установлен."); return; }
                    drainUpgrade = true;
                    break;
            }
            money -= cost;
            Notify("Новое оборудование установлено.");
        }

        private void CleanBar()
        {
            if (money < 2) { Notify("Уборка стоит 2 жетона."); return; }
            money -= 2;
            barDamage = Mathf.Max(0, barDamage - 1);
            Notify("Кровавые пятна убраны. Стойка цела.");
        }

        private void SpawnTrader()
        {
            string[] resourceIds = { "blood", "eyes", "flesh" };
            string[] resourceNames = { "КРОВЬ", "ГЛАЗА", "ПЛОТЬ" };
            int[] prices = { 4, 5, 3 };
            int offerIndex = Random.Range(0, resourceIds.Length);
            trader = new Trader
            {
                resourceId = resourceIds[offerIndex],
                resourceName = resourceNames[offerIndex],
                price = prices[offerIndex],
                leavesAt = Time.time + 28f
            };

            trader.root = new GameObject("Торговец · временный визит");
            trader.root.transform.position = new Vector3(5.1f, 0f, 2.35f);
            CreatePrimitive("Плащ", PrimitiveType.Capsule, trader.root.transform,
                new Vector3(0f, 0.8f, 0f), new Vector3(0.48f, 0.52f, 0.42f), new Color(0.24f, 0.18f, 0.32f));
            CreatePrimitive("Плащ · наплечник", PrimitiveType.Sphere, trader.root.transform,
                new Vector3(0f, 1.13f, 0f), new Vector3(0.65f, 0.3f, 0.48f), new Color(0.18f, 0.14f, 0.26f));
            CreatePrimitive("Плащ · застёжка", PrimitiveType.Sphere, trader.root.transform,
                new Vector3(0f, 1.12f, -0.27f), new Vector3(0.16f, 0.18f, 0.06f), new Color(0.84f, 0.51f, 0.19f));
            CreatePrimitive("Череп", PrimitiveType.Sphere, trader.root.transform,
                new Vector3(0f, 1.48f, 0f), new Vector3(0.43f, 0.43f, 0.4f), new Color(0.66f, 0.72f, 0.53f));
            CreatePrimitive("Череп · челюсть", PrimitiveType.Sphere, trader.root.transform,
                new Vector3(0f, 1.29f, -0.08f), new Vector3(0.31f, 0.14f, 0.3f), new Color(0.58f, 0.61f, 0.43f));
            CreatePrimitive("Череп · носовая впадина", PrimitiveType.Cube, trader.root.transform,
                new Vector3(0f, 1.45f, -0.207f), new Vector3(0.1f, 0.14f, 0.035f), new Color(0.16f, 0.12f, 0.17f));
            CreatePrimitive("Череп · левая глазница", PrimitiveType.Sphere, trader.root.transform,
                new Vector3(-0.12f, 1.54f, -0.18f), new Vector3(0.12f, 0.1f, 0.045f), new Color(0.055f, 0.035f, 0.08f));
            CreatePrimitive("Череп · правая глазница", PrimitiveType.Sphere, trader.root.transform,
                new Vector3(0.12f, 1.54f, -0.18f), new Vector3(0.12f, 0.1f, 0.045f), new Color(0.055f, 0.035f, 0.08f));
            CreatePrimitive("Глаз торговца", PrimitiveType.Sphere, trader.root.transform,
                new Vector3(-0.12f, 1.54f, -0.225f), new Vector3(0.075f, 0.075f, 0.04f), new Color(1f, 0.48f, 0.12f));
            CreatePrimitive("Череп · зубы", PrimitiveType.Cube, trader.root.transform,
                new Vector3(0f, 1.3f, -0.235f), new Vector3(0.28f, 0.045f, 0.025f), new Color(0.86f, 0.78f, 0.59f));
            CreatePrimitive("Капюшон", PrimitiveType.Sphere, trader.root.transform,
                new Vector3(0f, 1.66f, 0.2f), new Vector3(0.51f, 0.53f, 0.28f), new Color(0.17f, 0.12f, 0.25f));
            CreatePrimitive("Шляпа · поля", PrimitiveType.Cylinder, trader.root.transform,
                new Vector3(0f, 1.91f, 0.02f), new Vector3(0.56f, 0.045f, 0.56f), new Color(0.11f, 0.08f, 0.17f));
            CreatePrimitive("Шляпа · тулья", PrimitiveType.Cylinder, trader.root.transform,
                new Vector3(0f, 2f, 0.04f), new Vector3(0.32f, 0.11f, 0.32f), new Color(0.12f, 0.08f, 0.18f));
            CreatePrimitive("Сумка торговца", PrimitiveType.Cube, trader.root.transform,
                new Vector3(0.43f, 0.72f, -0.05f), new Vector3(0.32f, 0.4f, 0.2f), new Color(0.38f, 0.2f, 0.11f));
            CreatePrimitive("Сумка · пряжка", PrimitiveType.Cube, trader.root.transform,
                new Vector3(0.43f, 0.78f, -0.16f), new Vector3(0.12f, 0.12f, 0.04f), new Color(0.86f, 0.52f, 0.19f));
            CreatePrimitive("Посох торговца", PrimitiveType.Cylinder, trader.root.transform,
                new Vector3(-0.58f, 0.88f, 0.03f), new Vector3(0.055f, 0.86f, 0.055f), new Color(0.32f, 0.17f, 0.09f));
            CreatePrimitive("Посох · осколок", PrimitiveType.Sphere, trader.root.transform,
                new Vector3(-0.58f, 1.82f, 0.03f), new Vector3(0.16f, 0.18f, 0.14f), new Color(0.22f, 0.82f, 0.61f));
            TextMesh sign = CreateText("СТРАНСТВУЮЩИЙ ТОРГОВЕЦ", trader.root.transform,
                new Vector3(0f, 2.3f, 0f), 0.075f, 36, new Color(1f, 0.82f, 0.46f));
            sign.alignment = TextAlignment.Center;
            sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            trader.offerTarget = CreateTarget("ОБМЕН · " + trader.price + " Ж → 2 " + trader.resourceName,
                BarAction.TradeOffer, trader.resourceId, new Vector3(5.1f, 0.48f, 1.73f),
                new Color(0.58f, 0.39f, 0.16f), new Vector3(1.55f, 0.23f, 0.12f));
            trader.dismissTarget = CreateTarget("ПОЗЖЕ", BarAction.DismissTrader, "",
                new Vector3(5.1f, 0.19f, 1.73f), new Color(0.28f, 0.3f, 0.34f),
                new Vector3(0.82f, 0.17f, 0.11f));
            Notify("Торговец заглянул на 28 секунд. Предложение: 2 " + trader.resourceName + " за " + trader.price + " жетонов.");
        }

        private void TradeWithTrader(string resourceId)
        {
            if (trader == null || trader.resourceId != resourceId)
            {
                Notify("Торговец уже ушёл.");
                return;
            }
            if (money < trader.price)
            {
                Notify("Не хватает жетонов: торговец просит " + trader.price + ".");
                return;
            }

            money -= trader.price;
            switch (trader.resourceId)
            {
                case "blood": blood += 2; break;
                case "eyes": eyes += 2; break;
                case "flesh": flesh += 2; break;
            }
            string resourceName = trader.resourceName;
            CloseTrader(false);
            Notify("Обмен завершён: получено 2 " + resourceName + ".");
        }

        private void CloseTrader(bool dismissed)
        {
            if (trader == null) return;
            if (trader.root != null) Destroy(trader.root);
            if (trader.offerTarget != null) Destroy(trader.offerTarget.gameObject);
            if (trader.dismissTarget != null) Destroy(trader.dismissTarget.gameObject);
            trader = null;
            traderIn = Random.Range(42f, 58f);
            if (dismissed) Notify("Торговец поблагодарил за визит и ушёл.");
        }

        private void MakeHazardVisual(string label, Vector3 position, Color color)
        {
            if (GameObject.Find(label) != null) return;
            GameObject hazard = new GameObject(label);
            hazard.transform.position = position;
            GameObject flame = CreatePrimitive("Метка", PrimitiveType.Sphere, hazard.transform, Vector3.zero, new Vector3(0.38f, 0.38f, 0.38f), color);
            flame.transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);
            TextMesh text = CreateText(label + "!", hazard.transform, new Vector3(0f, 0.55f, 0f), 0.1f, 48, color);
            text.alignment = TextAlignment.Center;
            BarTarget target = hazard.AddComponent<BarTarget>();
            target.action = label == "ПОЖАР" ? BarAction.ExtinguishFire : BarAction.PumpFlood;
            target.displayLabel = label;
            SphereCollider collider = hazard.AddComponent<SphereCollider>();
            collider.radius = 0.6f;
            collider.isTrigger = true;
            hazard.AddComponent<BarHazardVisual>().Initialize(label == "ПОЖАР");
        }

        private void DestroyHazard(string label)
        {
            GameObject hazard = GameObject.Find(label);
            if (hazard != null) Destroy(hazard);
        }

        public void RestartRound()
        {
            Patron[] old = patrons.ToArray();
            for (int i = 0; i < old.Length; i++)
            {
                if (old[i].root != null) Destroy(old[i].root);
                if (old[i].serveTarget != null) Destroy(old[i].serveTarget.gameObject);
                if (old[i].fightTarget != null) Destroy(old[i].fightTarget.gameObject);
                if (old[i].harvestTarget != null) Destroy(old[i].harvestTarget.gameObject);
            }
            patrons.Clear();
            pendingSigns.Clear();
            helperArmReady = helperEyeReady = helperBloodReady = punchReady = 0f;
            CloseTrader(false);
            DestroyHazard("ПОЖАР");
            DestroyHazard("ПОТОП");
            blood = 2; eyes = 2; flesh = 2; bones = 0; money = 20;
            playerHealth = 3; barDamage = 0; servedCount = 0; qualityLevel = 0; strengthLevel = 0;
            rightArmDisabled = leftArmDisabled = eyeInjured = gameOver = fireActive = floodActive = false;
            iceBox = sprinkler = drainUpgrade = false;
            readyDrink = "";
            eventIn = 120f;
            traderIn = 32f;
            guestIn = 0f;
            RemoveVignette();
            SpawnPatron(false);
            SpawnPatron(true);
            RefreshHud();
            if (Hud != null) Hud.Notify("Новая смена. Приготовьте коктейль по заказу клиента.");
        }

        private void RefreshHud()
        {
            if (Hud == null) return;
            StringBuilder builder = new StringBuilder();
            builder.Append("НЕКРО-ПАБ  •  ").Append(money).Append(" жетонов    ♥ ").Append(playerHealth).Append("\n");
            builder.Append("Кровь ").Append(blood).Append("   Глаза ").Append(eyes).Append("   Плоть ").Append(flesh).Append("   Кости ").Append(bones).Append("\n");
            builder.Append("Заказы: ");
            int guests = 0;
            for (int i = 0; i < patrons.Count; i++)
            {
                if (patrons[i].corpse) continue;
                if (guests++ > 0) builder.Append("  |  ");
                builder.Append(patrons[i].recipe).Append(patrons[i].angry ? " [БУЯНИТ]" : " [" + Mathf.CeilToInt(patrons[i].patience) + "с]");
            }
            if (guests == 0) builder.Append("ожидайте гостей");
            builder.Append("\nВ стакане: ").Append(string.IsNullOrEmpty(readyDrink) ? "пусто" : readyDrink + " · " + drinkQuality + "%");
            if (fireActive) builder.Append("\n🔥 ПОЖАР: потушить кровью до ").Append(Mathf.CeilToInt(fireLeft)).Append("с");
            if (floodActive) builder.Append("\n🌊 ПОТОП: насос ").Append(Mathf.Min(floodPumps, 3)).Append("/3");
            if (trader != null) builder.Append("\nТорговец уходит через ").Append(Mathf.CeilToInt(trader.leavesAt - Time.time)).Append("с");
            if (rightArmDisabled) builder.Append("\nПравая рука отключена");
            if (leftArmDisabled) builder.Append("\nЛевая рука отключена");
            if (eyeInjured) builder.Append("\nЗрение сужено");
            if (gameOver) builder.Append("\nСМЕНА ЗАКРЫТА · встряхните колокольчик для новой смены");
            Hud.SetStatus(builder.ToString());
        }

        private void Notify(string message)
        {
            if (Hud != null) Hud.Notify(message);
            Debug.Log("[НЕКРО-ПАБ] " + message);
        }

        private BarTarget CreateTarget(string label, BarAction action, string id, Vector3 position, Color color, Vector3 size)
        {
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = "Действие · " + label;
            button.transform.position = position;
            button.transform.localScale = size;
            Material material = CreateMaterial(color);
            button.GetComponent<Renderer>().material = material;
            BarTarget target = button.AddComponent<BarTarget>();
            target.action = action;
            target.subjectId = id;
            target.displayLabel = label;
            TextMesh text = CreateText(label, button.transform, new Vector3(0f, 0f, -0.51f), 0.04f, 32, Color.white);
            text.transform.localScale = new Vector3(0.04f / size.x, 0.04f / size.y, 0.04f / size.z);
            text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var layout = text.GetComponent<BarLabelLayout>();
            layout.width = size.x * 0.88f;
            layout.height = size.y * 0.75f;
            text.alignment = TextAlignment.Center;
            return target;
        }

        private GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localScale = localScale;
            obj.GetComponent<Renderer>().material = CreateMaterial(color);
            return obj;
        }

        private TextMesh CreateText(string value, Transform parent, Vector3 localPosition, float scale, int fontSize, Color color)
        {
            GameObject textObject = new GameObject(value);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            textObject.transform.localScale = Vector3.one * scale;
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = value;
            text.fontSize = fontSize;
            text.characterSize = 1f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            text.fontStyle = FontStyle.Bold;
            textObject.AddComponent<BarLabelLayout>();
            return text;
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.08f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.48f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.48f);
            if (color.r > 0.8f && color.g < 0.55f && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.35f);
            }
            if (color.a < 1f)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_ZWrite", 0f);
                material.renderQueue = 3000;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            return material;
        }
    }
}



