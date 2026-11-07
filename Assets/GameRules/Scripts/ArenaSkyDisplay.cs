using System.Collections;
using TMPro;
using UnityEngine;

public class ArenaSkyDisplay : MonoBehaviour
{
    [Header("Match")]
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private bool automaticStart = true;
    [SerializeField] private int startCountdown = 3;

    [Header("Sky Position")]
    [SerializeField] private Vector3 skyPosition = new Vector3(0f, 10f, 0f);
    [SerializeField] private Camera viewerCamera;

    [Header("Visual Style")]
    [SerializeField] private float mainFontSize = 8f;
    [SerializeField] private float subFontSize = 3.2f;
    [SerializeField] private float textScale = 0.45f;

    private TextMeshPro mainText;
    private TextMeshPro shadowText;
    private bool countdownRunning;
    private bool countdownCompleted;
    private MatchManager.MatchState previousState;

    private readonly Color cyan = new Color(0.00f, 0.95f, 1.00f);
    private readonly Color red = new Color(1.00f, 0.20f, 0.25f);
    private readonly Color green = new Color(0.20f, 1.00f, 0.50f);
    private readonly Color white = new Color(0.95f, 0.98f, 1.00f);

    private void Start()
    {
        if (matchManager == null)
            matchManager = MatchManager.Instance;

        if (matchManager == null)
        {
            Debug.LogError("ArenaSkyDisplay: MatchManager is missing.");
            enabled = false;
            return;
        }

        if (viewerCamera == null)
            viewerCamera = Camera.main;

        CreateSkyText();
        previousState = matchManager.CurrentState;

        Debug.Log("ArenaSkyDisplay initialized.");
    }

    private void CreateSkyText()
    {
        GameObject root = new GameObject("Arena Sky Announcement Root");
        root.transform.SetParent(transform, false);
        root.transform.position = skyPosition;
        root.transform.localScale = Vector3.one * textScale;

        // Shadow / glow text behind
        GameObject shadowObj = new GameObject("Sky Shadow Text");
        shadowObj.transform.SetParent(root.transform, false);
        shadowObj.transform.localPosition = new Vector3(0.08f, -0.08f, 0.15f);

        shadowText = shadowObj.AddComponent<TextMeshPro>();
        SetupTextBase(shadowText);
        shadowText.color = new Color(0f, 0f, 0f, 0.55f);

        // Main text
        GameObject mainObj = new GameObject("Sky Main Text");
        mainObj.transform.SetParent(root.transform, false);
        mainObj.transform.localPosition = Vector3.zero;

        mainText = mainObj.AddComponent<TextMeshPro>();
        SetupTextBase(mainText);
        mainText.color = cyan;

        SetDisplay("ARENA READY", cyan);
    }

    private void SetupTextBase(TextMeshPro text)
    {
        text.text = "ARENA READY";
        text.fontSize = mainFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 8f;
        text.lineSpacing = -15f;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(40f, 12f);
    }

    private void Update()
    {
        if (matchManager == null || mainText == null || shadowText == null)
            return;

        if (viewerCamera == null)
            viewerCamera = Camera.main;

        FaceViewer();

        MatchManager.MatchState state = matchManager.CurrentState;

        if (state == MatchManager.MatchState.Waiting &&
            previousState != MatchManager.MatchState.Waiting)
        {
            countdownCompleted = false;
        }

        previousState = state;

        if (state == MatchManager.MatchState.Waiting)
        {
            if (automaticStart && !countdownRunning && !countdownCompleted)
            {
                StartCoroutine(StartSequence());
            }
            else if (!countdownRunning)
            {
                SetDisplay("ARENA\nREADY", cyan);
            }

            return;
        }

        if (state == MatchManager.MatchState.Playing)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, matchManager.TimeRemaining));

            if (seconds <= 10)
            {
                SetDisplay(seconds + "\nSECONDS LEFT!", red);
            }
            else
            {
                SetDisplay(
                    "TIME\n" + string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60),
                    cyan
                );
            }

            return;
        }

        if (state == MatchManager.MatchState.Finished)
        {
            string winner = matchManager.WinnerMessage;

            if (string.IsNullOrEmpty(winner))
                winner = "NO WINNER";

            SetDisplay("MATCH FINISHED\n" + winner, green);
        }
    }

    private IEnumerator StartSequence()
    {
        countdownRunning = true;
        countdownCompleted = true;

        for (int i = Mathf.Max(1, startCountdown); i > 0; i--)
        {
            if (matchManager.CurrentState != MatchManager.MatchState.Waiting)
            {
                countdownRunning = false;
                yield break;
            }

            SetDisplay(i.ToString(), i == 1 ? red : cyan);
            yield return new WaitForSeconds(1f);
        }

        SetDisplay("GO!", green);

        if (matchManager.CurrentState == MatchManager.MatchState.Waiting)
            matchManager.StartMatch();

        yield return new WaitForSeconds(1f);

        countdownRunning = false;
    }

    private void SetDisplay(string message, Color color)
    {
        if (mainText == null || shadowText == null)
            return;

        mainText.text = message;
        shadowText.text = message;

        mainText.color = color;
    }

    private void FaceViewer()
    {
        if (viewerCamera == null || mainText == null)
            return;

        Transform root = mainText.transform.parent;
        if (root == null)
            return;

        Vector3 direction = root.position - viewerCamera.transform.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            root.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}