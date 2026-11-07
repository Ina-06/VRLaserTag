
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRLaserTag.Combat;

public class VRGameHUD : MonoBehaviour
{
    [Header("Game References")]
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerScore playerScore;
    [SerializeField] private AmmoController ammo;

    [Header("Wrist HUD Settings")]
    [SerializeField] private Vector3 hudPosition = Vector3.zero;
    [SerializeField] private float hudScale = 0.00045f;

    private GameObject hudCanvas;
    private TMP_Text statusText;
    private TMP_Text timerText;
    private TMP_Text healthText;
    private TMP_Text ammoText;
    private TMP_Text killsText;
    private TMP_Text deathsText;
    private TMP_Text footerText;
    private Image healthFill;

    private float nextUpdate;

    private readonly Color cyan =
        new Color(0.08f, 0.85f, 1f);

    private readonly Color green =
        new Color(0.25f, 0.95f, 0.55f);

    private readonly Color red =
        new Color(1f, 0.28f, 0.36f);

    private readonly Color white =
        new Color(0.95f, 0.98f, 1f);

    private readonly Color muted =
        new Color(0.55f, 0.67f, 0.80f);

    private readonly Color panelColor =
        new Color(0.06f, 0.12f, 0.22f, 0.96f);

    private void Awake()
    {
        Debug.Log("VRGameHUD: Awake on " + gameObject.name);
    }

    private void OnEnable()
    {
        Debug.Log("VRGameHUD: Enabled on " + gameObject.name);
    }

    private void Start()
    {
        Debug.Log("VRGameHUD: Starting HUD creation.");

        ResolveReferences();
        BuildHUD();
        RefreshHUD();

        Debug.Log("VRGameHUD: Tactical HUD created successfully!");
    }

    private void ResolveReferences()
    {
        if (matchManager == null)
            matchManager = MatchManager.Instance;

        if (playerHealth == null)
            playerHealth =
                GetComponentInParent<PlayerHealth>();

        if (playerScore == null)
            playerScore =
                GetComponentInParent<PlayerScore>();

        if (ammo == null)
            ammo = transform.root
                .GetComponentInChildren<AmmoController>(true);

        if (matchManager == null)
            Debug.LogWarning("VRGameHUD: MatchManager missing.");

        if (playerHealth == null)
            Debug.LogWarning("VRGameHUD: PlayerHealth missing.");

        if (playerScore == null)
            Debug.LogWarning("VRGameHUD: PlayerScore missing.");

        if (ammo == null)
            Debug.LogWarning("VRGameHUD: AmmoController missing.");
    }

    private void BuildHUD()
    {
        if (hudCanvas != null)
            return;

        hudCanvas = new GameObject(
            "Tactical HUD Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );

        hudCanvas.transform.SetParent(transform, false);

        hudCanvas.transform.localPosition = hudPosition;
        hudCanvas.transform.localRotation = Quaternion.identity;
        hudCanvas.transform.localScale = Vector3.one * hudScale;

        Canvas canvas = hudCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 100;

        RectTransform canvasRect =
            hudCanvas.GetComponent<RectTransform>();

        canvasRect.sizeDelta = new Vector2(480, 320);

        CreatePanel(
            "Main Background", canvasRect,
            0, 0, 480, 320,
            new Color(0.02f, 0.045f, 0.09f, 0.96f)
        );

        CreatePanel(
            "Header", canvasRect,
            0, 130, 465, 45,
            new Color(0.05f, 0.16f, 0.26f, 1f)
        );

        CreateText(
            "Title", canvasRect,
            "LASER TAG",
            -112, 130, 210, 36,
            24, cyan
        );

        statusText = CreateText(
            "Status", canvasRect,
            "WAITING",
            128, 130, 185, 36,
            18, green
        );

        CreatePanel(
            "Timer Panel", canvasRect,
            -115, 50, 220, 90, panelColor
        );

        CreatePanel(
            "Ammo Panel", canvasRect,
            115, 50, 220, 90, panelColor
        );

        CreateText(
            "Timer Label", canvasRect,
            "TIME REMAINING",
            -115, 76, 210, 22,
            15, muted
        );

        timerText = CreateText(
            "Timer", canvasRect,
            "05:00",
            -115, 40, 210, 49,
            37, white
        );

        CreateText(
            "Ammo Label", canvasRect,
            "AMMUNITION",
            115, 76, 210, 22,
            15, muted
        );

        ammoText = CreateText(
            "Ammo", canvasRect,
            "10 / 10",
            115, 40, 210, 49,
            35, cyan
        );

        CreatePanel(
            "Health Panel", canvasRect,
            0, -40, 465, 80, panelColor
        );

        CreateText(
            "Health Label", canvasRect,
            "HEALTH",
            -140, -25, 170, 28,
            20, white
        );

        healthText = CreateText(
            "Health Value", canvasRect,
            "100 HP",
            140, -25, 170, 28,
            23, green
        );

        CreatePanel(
            "Health Bar Background", canvasRect,
            0, -62, 420, 12,
            new Color(0.20f, 0.27f, 0.37f)
        );

        RectTransform bar = CreatePanel(
            "Health Bar Fill", canvasRect,
            0, -62, 420, 12, green
        );

        healthFill = bar.GetComponent<Image>();
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillAmount = 1f;

        CreatePanel(
            "Kills Panel", canvasRect,
            -115, -113, 220, 42, panelColor
        );

        CreatePanel(
            "Deaths Panel", canvasRect,
            115, -113, 220, 42, panelColor
        );

        killsText = CreateText(
            "Kills", canvasRect,
            "KILLS  0",
            -115, -113, 215, 36,
            21, cyan
        );

        deathsText = CreateText(
            "Deaths", canvasRect,
            "DEATHS  0",
            115, -113, 215, 36,
            21, red
        );

        footerText = CreateText(
            "Footer", canvasRect,
            "ARENA SYSTEM READY",
            0, -148, 450, 24,
            14, muted
        );

        Debug.Log("VRGameHUD: Canvas hierarchy built.");
    }

    private RectTransform CreatePanel(
        string objectName,
        Transform parent,
        float x, float y,
        float width, float height,
        Color color)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image)
        );

        obj.transform.SetParent(parent, false);

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);

        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return rect;
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        string content,
        float x, float y,
        float width, float height,
        float fontSize,
        Color color)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        obj.transform.SetParent(parent, false);

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);

        TMP_Text label = obj.GetComponent<TMP_Text>();

        label.text = content;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;

        return label;
    }

    private void Update()
    {
        if (hudCanvas == null)
            return;

        if (Time.unscaledTime < nextUpdate)
            return;

        nextUpdate = Time.unscaledTime + 0.1f;

        if (matchManager == null)
            ResolveReferences();

        RefreshHUD();
    }

    private void RefreshHUD()
    {
        if (hudCanvas == null)
            return;

        if (matchManager != null)
        {
            int seconds = Mathf.CeilToInt(
                Mathf.Max(0f, matchManager.TimeRemaining)
            );

            timerText.text = string.Format(
                "{0:00}:{1:00}",
                seconds / 60,
                seconds % 60
            );

            statusText.text =
                matchManager.CurrentState.ToString().ToUpper();

            if (matchManager.CurrentState ==
                MatchManager.MatchState.Playing)
            {
                statusText.color = green;
            }
            else
            {
                statusText.color = cyan;
            }

            if (matchManager.CurrentState ==
                MatchManager.MatchState.Finished)
            {
                footerText.text = matchManager.WinnerMessage;
            }
            else if (matchManager.CurrentState ==
                     MatchManager.MatchState.Waiting)
            {
                footerText.text = "ARENA SYSTEM READY";
            }
            else
            {
                footerText.text = "MATCH IN PROGRESS";
            }
        }

        if (playerHealth != null)
        {
            int hp = playerHealth.CurrentHealth;

            healthText.text = hp + " HP";

            float fraction = Mathf.Clamp01(hp / 100f);

            healthFill.fillAmount = fraction;

            Color healthColor =
                fraction > 0.5f ? green :
                fraction > 0.25f ? Color.yellow : red;

            healthFill.color = healthColor;
            healthText.color = healthColor;

            if (playerHealth.IsDead &&
                matchManager != null &&
                matchManager.CurrentState ==
                MatchManager.MatchState.Playing)
            {
                footerText.text = "ELIMINATED - RESPAWNING";
            }
        }

        if (playerScore != null)
        {
            killsText.text = "KILLS  " + playerScore.Kills;
            deathsText.text = "DEATHS  " + playerScore.Deaths;
        }

        if (ammo != null)
        {
            ammoText.text =
                ammo.CurrentAmmo + " / " +
                ammo.MagazineCapacity;

            if (ammo.IsReloading &&
                matchManager != null &&
                matchManager.CurrentState ==
                MatchManager.MatchState.Playing &&
                (playerHealth == null || !playerHealth.IsDead))
            {
                footerText.text = "RELOADING";
            }
        }
        else
        {
            ammoText.text = "-- / --";
        }
    }

    private void OnDestroy()
    {
        if (hudCanvas != null)
            Destroy(hudCanvas);
    }
}
