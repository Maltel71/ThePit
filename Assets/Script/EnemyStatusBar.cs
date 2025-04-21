using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyStatusBar : MonoBehaviour
{
    [Header("Status Bar Settings")]
    [SerializeField] private bool showHealthBar = true;
    [SerializeField] private bool showHungerBar = true;
    [SerializeField] private bool showFrustrationBar = true;
    [SerializeField] private float displayDistance = 15f; // Maxavstånd för att visa statusbars
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0); // Höjdoffset över fienden

    [Header("Overall Bar Size")]
    [SerializeField] private float barScale = 0.02f; // Huvudsaklig skala för alla bars (större värde = större bars)
    [SerializeField] private float barWidth = 1f; // Generell bredd för alla bars

    [Header("Health Bar")]
    [SerializeField] private Color healthBarColor = Color.red;
    [SerializeField] private float healthBarHeight = 0.15f;

    [Header("Hunger Bar")]
    [SerializeField] private Color hungerBarColor = new Color(1f, 0.7f, 0f); // Orange färg för hunger
    [SerializeField] private float hungerBarHeight = 0.15f;

    [Header("Frustration Bar")]
    [SerializeField] private Color frustrationBarColor = new Color(0.8f, 0f, 0.8f); // Lila färg för frustration
    [SerializeField] private float frustrationBarHeight = 0.15f;

    [Header("Layout")]
    [SerializeField] private float barSpacing = 0.05f; // Avstånd mellan bars

    // Referenser
    private EnemyController enemyController;
    private Transform mainCamera;
    private Canvas statusCanvas;
    private Image healthBarFill;
    private Image hungerBarFill;
    private Image frustrationBarFill;

    // Cache values
    private float updateInterval = 0.2f; // Hur ofta status bars uppdateras
    private float lastUpdateTime;

    private void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        mainCamera = Camera.main.transform;

        if (enemyController == null)
        {
            Debug.LogError("EnemyStatusBar behöver en EnemyController-komponent!");
            enabled = false;
            return;
        }

        CreateStatusBars();
    }

    private void CreateStatusBars()
    {
        // Skapa canvas för status bars
        GameObject canvasObj = new GameObject("StatusCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = offset;
        canvasObj.transform.localRotation = Quaternion.identity;

        statusCanvas = canvasObj.AddComponent<Canvas>();
        statusCanvas.renderMode = RenderMode.WorldSpace;

        // Lägg till en CanvasScaler för att hantera skalning
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;

        // Se till att status bars alltid vänder mot kameran
        canvasObj.AddComponent<Billboard>();

        // Beräkna total höjd för alla bars
        float totalHeight =
            (showHealthBar ? healthBarHeight : 0) +
            (showHungerBar ? hungerBarHeight : 0) +
            (showFrustrationBar ? frustrationBarHeight : 0) +
            barSpacing * (
                (showHealthBar && showHungerBar ? 1 : 0) +
                (showHungerBar && showFrustrationBar ? 1 : 0) +
                (showHealthBar && showFrustrationBar && !showHungerBar ? 1 : 0)
            );

        // Skapa container för bars
        GameObject containerObj = new GameObject("BarsContainer");
        containerObj.transform.SetParent(statusCanvas.transform);
        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.localPosition = Vector3.zero;
        containerRect.sizeDelta = new Vector2(barWidth, totalHeight);
        containerRect.localScale = new Vector3(barScale, barScale, barScale); // Skala för att passa i 3D-världen

        // Bestäm position för varje bar
        float currentYPos = 0;

        // Skapa health bar om aktiverad
        if (showHealthBar)
        {
            CreateBar(containerRect, "HealthBar", healthBarColor, currentYPos, out healthBarFill);
            currentYPos -= (healthBarHeight + barSpacing);
        }

        // Skapa hunger bar om aktiverad
        if (showHungerBar)
        {
            CreateBar(containerRect, "HungerBar", hungerBarColor, currentYPos, out hungerBarFill);
            currentYPos -= (hungerBarHeight + barSpacing);
        }

        // Skapa frustration bar om aktiverad
        if (showFrustrationBar)
        {
            CreateBar(containerRect, "FrustrationBar", frustrationBarColor, currentYPos, out frustrationBarFill);
        }
    }

    private void CreateBar(RectTransform parent, string name, Color color, float yPosition, out Image fillImage)
    {
        // Skapa bakgrund för bar
        GameObject barBackground = new GameObject(name + "Background");
        barBackground.transform.SetParent(parent);
        RectTransform bgRect = barBackground.AddComponent<RectTransform>();
        bgRect.localPosition = new Vector3(0, yPosition, 0);

        // Anpassa bredden och höjden baserat på vilken bar det är
        float width = barWidth;
        float height;
        if (name == "HealthBar") { height = healthBarHeight; }
        else if (name == "HungerBar") { height = hungerBarHeight; }
        else { height = frustrationBarHeight; } // FrustrationBar

        bgRect.sizeDelta = new Vector2(width, height);
        bgRect.localScale = Vector3.one;

        Image bgImage = barBackground.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Mörk bakgrund

        // Skapa fyllnad för bar
        GameObject barFill = new GameObject(name + "Fill");
        barFill.transform.SetParent(bgRect);
        RectTransform fillRect = barFill.AddComponent<RectTransform>();
        fillRect.localPosition = Vector3.zero;
        fillRect.sizeDelta = bgRect.sizeDelta;
        fillRect.localScale = Vector3.one;
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.pivot = new Vector2(0, 0.5f);

        fillImage = barFill.AddComponent<Image>();
        fillImage.color = color;
    }

    private void Update()
    {
        // Uppdatera status bars vid intervall för bättre prestanda
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateVisibility();
            UpdateBars();
            lastUpdateTime = Time.time;
        }
    }

    private void UpdateVisibility()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main.transform;
            if (mainCamera == null) return;
        }

        bool isVisible = false;
        float distanceToCamera = Vector3.Distance(mainCamera.position, transform.position);

        // Visa bara när inom displayDistance och fienden är vid liv
        if (distanceToCamera <= displayDistance && !enemyController.IsDead)
        {
            isVisible = true;

            // Rotera canvas mot kameran
            statusCanvas.transform.rotation = Quaternion.LookRotation(
                statusCanvas.transform.position - mainCamera.position
            );
        }

        statusCanvas.gameObject.SetActive(isVisible);
    }

    private void UpdateBars()
    {
        if (enemyController == null) return;

        // Uppdatera health bar
        if (showHealthBar && healthBarFill != null)
        {
            float healthRatio = enemyController.GetHealthPercentage();
            healthBarFill.rectTransform.localScale = new Vector3(healthRatio, 1, 1);

            // Ändra färg baserat på hälsonivå
            if (healthRatio <= 0.3f)
                healthBarFill.color = Color.red;
            else if (healthRatio <= 0.6f)
                healthBarFill.color = Color.yellow;
            else
                healthBarFill.color = healthBarColor;
        }

        // Uppdatera hunger bar
        if (showHungerBar && hungerBarFill != null)
        {
            float hungerRatio = enemyController.GetHungerPercentage();
            hungerBarFill.rectTransform.localScale = new Vector3(hungerRatio, 1, 1);

            // Ändra färg baserat på hungernivå
            if (hungerRatio <= 0.3f)
                hungerBarFill.color = Color.red;
            else if (hungerRatio <= 0.6f)
                hungerBarFill.color = Color.yellow;
            else
                hungerBarFill.color = hungerBarColor;
        }

        // Uppdatera frustration bar
        if (showFrustrationBar && frustrationBarFill != null)
        {
            float frustrationRatio = enemyController.GetFrustrationPercentage();
            frustrationBarFill.rectTransform.localScale = new Vector3(frustrationRatio, 1, 1);

            // Ändra färg baserat på frustrationsnivå
            if (frustrationRatio >= 0.8f)
                frustrationBarFill.color = Color.red;
            else if (frustrationRatio >= 0.5f)
                frustrationBarFill.color = new Color(1f, 0.5f, 0.8f); // Rosa
            else
                frustrationBarFill.color = frustrationBarColor;
        }
    }
}

// Enkel komponent för att alltid rotera mot kameran
public class Billboard : MonoBehaviour
{
    private Transform mainCamera;

    private void Start()
    {
        mainCamera = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (mainCamera == null) return;

        transform.LookAt(transform.position + mainCamera.rotation * Vector3.forward,
            mainCamera.rotation * Vector3.up);
    }
}