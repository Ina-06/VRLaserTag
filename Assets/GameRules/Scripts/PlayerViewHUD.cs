using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRLaserTag.Combat;

// Head-locked HUD for the local player, built at runtime (no scene or prefab setup needed):
//  - top left: health bar and ammo count
//  - bottom right: kill feed (eliminations, joins, leaves, match news)
//  - centre: hit marker when you land a hit (red when it kills), with a click
//  - edges: red flash when you are hit
//  - full view: "ELIMINATED" screen with the attacker and a respawn countdown
// Events come from MatchManager, which the host broadcasts to every headset.
public class PlayerViewHUD : MonoBehaviour
{
    private const float Distance = 0.6f;        // metres in front of the eyes
    private const float Scale = 0.0006f;        // canvas units to metres
    private const float Width = 900f, Height = 600f;
    private const int FeedLines = 4;
    private const float FeedLifetime = 5f;

    private static readonly Color Panel = new Color(0.03f, 0.06f, 0.12f, 0.75f);
    private static readonly Color Green = new Color(0.25f, 0.95f, 0.55f);
    private static readonly Color Yellow = new Color(1f, 0.82f, 0.2f);
    private static readonly Color Red = new Color(1f, 0.25f, 0.3f);
    private static readonly Color White = new Color(0.95f, 0.98f, 1f);

    private Canvas canvas;
    private Material uiMaterial;
    private Material textMaterial;

    private GameObject statusPanel;
    private RectTransform healthFill;
    private Image healthFillImage;
    private TMP_Text healthText;
    private TMP_Text ammoText;

    private readonly List<TMP_Text> feedTexts = new();
    private readonly List<(string text, float time)> feed = new();

    private GameObject hitMarker;
    private readonly List<Image> hitMarkerBars = new();
    private float hitMarkerUntil;

    private readonly List<Image> flashBars = new();
    private float flashStart = -10f;

    private GameObject deathScreen;
    private TMP_Text deathByText;
    private TMP_Text respawnText;
    private ulong lastAttacker;
    private float deathTime;
    private bool wasDead;

    private AudioSource audioSource;
    private AudioClip hitClip;

    private NetworkObject localPlayer;
    private PlayerHealth localHealth;
    private PlayerIdentity localIdentity;
    private AmmoController localAmmo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (FindAnyObjectByType<MatchManager>() == null) return;
        if (FindAnyObjectByType<PlayerViewHUD>() != null) return;
        new GameObject("Player View HUD", typeof(RectTransform)).AddComponent<PlayerViewHUD>();
    }

    private void OnEnable()
    {
        MatchManager.FeedMessage += OnFeedMessage;
        MatchManager.DamageEvent += OnDamageEvent;
    }

    private void OnDisable()
    {
        MatchManager.FeedMessage -= OnFeedMessage;
        MatchManager.DamageEvent -= OnDamageEvent;
    }

    private void Start()
    {
        if (Application.isBatchMode) { enabled = false; return; }
        BuildCanvas();
        BuildStatusPanel();
        BuildFeed();
        BuildHitMarker();
        BuildFlash();
        BuildDeathScreen();
        BuildAudio();
    }

    // ---------- events ----------

    private void OnFeedMessage(string message)
    {
        feed.Add((message, Time.unscaledTime));
        while (feed.Count > FeedLines) feed.RemoveAt(0);
    }

    private void OnDamageEvent(ulong attackerId, ulong victimId, bool killed)
    {
        ulong me = localIdentity != null ? localIdentity.PlayerId : 0UL;
        if (me == 0) return;

        if (attackerId == me && victimId != me)
        {
            hitMarkerUntil = Time.unscaledTime + (killed ? 0.4f : 0.15f);
            foreach (Image bar in hitMarkerBars) bar.color = killed ? Red : White;
            if (audioSource != null && hitClip != null)
            {
                audioSource.pitch = killed ? 0.7f : 1f;
                audioSource.PlayOneShot(hitClip, killed ? 1f : 0.7f);
            }
        }

        if (victimId == me)
        {
            flashStart = Time.unscaledTime;
            if (killed)
            {
                lastAttacker = attackerId;
                deathTime = Time.unscaledTime;
            }
        }
    }

    // ---------- per frame ----------

    private void LateUpdate()
    {
        if (canvas == null) return;
        FindLocalPlayer();

        Camera camera = Camera.main;
        bool show = camera != null && localPlayer != null;
        canvas.enabled = show;
        if (!show) return;

        Transform eye = camera.transform;
        transform.SetPositionAndRotation(eye.position + eye.forward * Distance, eye.rotation);
        canvas.worldCamera = camera;

        UpdateStatus();
        UpdateFeed();
        UpdateHitMarker();
        UpdateFlash();
        UpdateDeathScreen();
    }

    private void FindLocalPlayer()
    {
        NetworkManager network = NetworkManager.Singleton;
        NetworkObject player = network != null && network.IsListening && network.LocalClient != null
            ? network.LocalClient.PlayerObject
            : null;
        if (player == localPlayer) return;

        localPlayer = player;
        localHealth = player != null ? player.GetComponent<PlayerHealth>() : null;
        localIdentity = player != null ? player.GetComponent<PlayerIdentity>() : null;
        localAmmo = player != null ? player.GetComponentInChildren<AmmoController>(true) : null;
        if (player == null) feed.Clear();
    }

    private void UpdateStatus()
    {
        if (localHealth != null)
        {
            int max = Mathf.Max(1, localHealth.MaxHealth);
            int hp = Mathf.Clamp(localHealth.CurrentHealth, 0, max);
            float fraction = (float)hp / max;
            healthFill.sizeDelta = new Vector2(300f * fraction, healthFill.sizeDelta.y);
            healthFillImage.color = fraction > 0.6f ? Green : fraction > 0.3f ? Yellow : Red;
            healthText.text = hp.ToString();
        }

        if (localAmmo != null)
            ammoText.text = localAmmo.IsReloading
                ? "AMMO  RELOADING..."
                : $"AMMO  {localAmmo.CurrentAmmo} / {localAmmo.MagazineCapacity}";
        else
            ammoText.text = "AMMO  -- / --";
    }

    private void UpdateFeed()
    {
        float now = Time.unscaledTime;
        feed.RemoveAll(entry => now - entry.time > FeedLifetime);

        // Newest entry on the bottom line.
        for (int line = 0; line < FeedLines; line++)
        {
            int index = feed.Count - FeedLines + line;
            TMP_Text text = feedTexts[line];
            if (index < 0) { text.text = ""; continue; }

            float age = now - feed[index].time;
            Color color = White;
            color.a = Mathf.Clamp01(FeedLifetime - age);   // fade out over the last second
            text.color = color;
            text.text = feed[index].text;
        }
    }

    private void UpdateHitMarker()
    {
        hitMarker.SetActive(Time.unscaledTime < hitMarkerUntil);
    }

    private void UpdateFlash()
    {
        float alpha = Mathf.Clamp01(1f - (Time.unscaledTime - flashStart) / 0.35f) * 0.55f;
        foreach (Image bar in flashBars)
        {
            Color color = Red;
            color.a = alpha;
            bar.color = color;
            bar.enabled = alpha > 0.01f;
        }
    }

    private void UpdateDeathScreen()
    {
        MatchManager match = MatchManager.Instance;
        bool dead = localHealth != null && localHealth.IsDead &&
                    match != null && match.CurrentState == MatchManager.MatchState.Playing;
        if (dead && !wasDead && Time.unscaledTime - deathTime > 1f)
        {
            // Health reached zero before the hit message arrived: start the countdown now.
            deathTime = Time.unscaledTime;
            lastAttacker = 0;
        }
        wasDead = dead;

        deathScreen.SetActive(dead);
        // Health and ammo are shown during a match; the lobby has the LAN menu instead.
        statusPanel.SetActive(!dead && match != null && match.CurrentState == MatchManager.MatchState.Playing);
        if (!dead) return;

        deathByText.text = lastAttacker != 0 && localIdentity != null && lastAttacker != localIdentity.PlayerId
            ? $"by Player {lastAttacker}"
            : "";
        float remaining = localHealth.RespawnDelay - (Time.unscaledTime - deathTime);
        respawnText.text = remaining > 0f
            ? $"Respawning in {Mathf.CeilToInt(remaining)}"
            : "Respawning...";
    }

    // ---------- building ----------

    private void BuildCanvas()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 300;
        var rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(Width, Height);
        rect.localScale = Vector3.one * Scale;

        // Draw on top of the world so walls closer than the HUD never hide it.
        uiMaterial = new Material(Canvas.GetDefaultCanvasMaterial());
        uiMaterial.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
    }

    private void BuildStatusPanel()
    {
        // Top left.
        RectTransform panel = Box("Status", transform, -Width / 2 + 190, Height / 2 - 70, 340, 110, Panel);
        statusPanel = panel.gameObject;

        Text(panel, "HEALTH", -85, 30, 150, 30, 22, White, TextAlignmentOptions.Left);
        healthText = Text(panel, "100", 120, 30, 80, 30, 24, White, TextAlignmentOptions.Right);

        Box("Health Back", panel, 0, 0, 300, 18, new Color(1f, 1f, 1f, 0.15f));
        healthFill = Box("Health Fill", panel, -150, 0, 300, 18, Green);
        healthFill.pivot = new Vector2(0f, 0.5f);
        healthFill.anchoredPosition = new Vector2(-150, 0);
        healthFillImage = healthFill.GetComponent<Image>();

        ammoText = Text(panel, "AMMO  -- / --", 0, -32, 300, 32, 24, White, TextAlignmentOptions.Left);
    }

    private void BuildFeed()
    {
        // Bottom right, newest line at the bottom.
        for (int line = 0; line < FeedLines; line++)
        {
            float y = -Height / 2 + 60 + (FeedLines - 1 - line) * 32;
            feedTexts.Add(Text(transform, "", Width / 2 - 220, y, 420, 30, 22, White,
                TextAlignmentOptions.Right));
        }
    }

    private void BuildHitMarker()
    {
        RectTransform root = Box("Hit Marker", transform, 0, 0, 1, 1, Color.clear);
        hitMarker = root.gameObject;
        foreach (float angle in new[] { 45f, -45f })
        {
            RectTransform bar = Box("Bar", root, 0, 0, 44, 6, White);
            bar.localRotation = Quaternion.Euler(0, 0, angle);
            hitMarkerBars.Add(bar.GetComponent<Image>());
        }
        hitMarker.SetActive(false);
    }

    private void BuildFlash()
    {
        const float thick = 70f;
        flashBars.Add(Box("Flash Top", transform, 0, Height / 2 - thick / 2, Width, thick, Red).GetComponent<Image>());
        flashBars.Add(Box("Flash Bottom", transform, 0, -Height / 2 + thick / 2, Width, thick, Red).GetComponent<Image>());
        flashBars.Add(Box("Flash Left", transform, -Width / 2 + thick / 2, 0, thick, Height, Red).GetComponent<Image>());
        flashBars.Add(Box("Flash Right", transform, Width / 2 - thick / 2, 0, thick, Height, Red).GetComponent<Image>());
        foreach (Image bar in flashBars) bar.enabled = false;
    }

    private void BuildDeathScreen()
    {
        // Large enough to fill the whole field of view at this distance.
        RectTransform screen = Box("Death Screen", transform, 0, 0, 6000, 6000, new Color(0.35f, 0f, 0.03f, 0.7f));
        deathScreen = screen.gameObject;
        Text(screen, "ELIMINATED", 0, 60, 800, 90, 72, Red, TextAlignmentOptions.Center);
        deathByText = Text(screen, "", 0, -10, 800, 50, 36, White, TextAlignmentOptions.Center);
        respawnText = Text(screen, "", 0, -70, 800, 50, 30, White, TextAlignmentOptions.Center);
        deathScreen.SetActive(false);
    }

    private void BuildAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;

        // Short synthetic "tick" so no audio asset is needed.
        const int rate = 44100;
        int length = rate / 20;
        var samples = new float[length];
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / rate;
            samples[i] = Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-t * 60f) * 0.6f;
        }
        hitClip = AudioClip.Create("Hit Marker", length, 1, rate, false);
        hitClip.SetData(samples, 0);
    }

    private RectTransform Box(string name, Transform parent, float x, float y, float width, float height, Color color)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5;
        rect.SetParent(parent, false);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.material = uiMaterial;
        image.raycastTarget = false;
        return rect;
    }

    private TMP_Text Text(Transform parent, string value, float x, float y, float width, float height,
        float size, Color color, TextAlignmentOptions alignment)
    {
        var rect = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5;
        rect.SetParent(parent, false);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        if (textMaterial == null && text.fontSharedMaterial != null)
        {
            textMaterial = new Material(text.fontSharedMaterial);
            textMaterial.SetFloat("unity_GUIZTestMode", (float)CompareFunction.Always);
        }
        if (textMaterial != null) text.fontSharedMaterial = textMaterial;
        return text;
    }

    private void OnDestroy()
    {
        if (uiMaterial != null) Destroy(uiMaterial);
        if (textMaterial != null) Destroy(textMaterial);
    }
}
