using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : MonoBehaviour
{
    [Header("Våningsinställningar")]
    [SerializeField] private Transform[] floorPositions; // Positioner för varje våning
    [SerializeField] private float waitTimeAtFloor = 5f; // Standardtid att vänta vid varje våning i sekunder
    [SerializeField] private float waitTimeAtStartFloor = 10f; // Tid att vänta vid våning 0 (startvåningen)
    [SerializeField] private float moveSpeedDown = 2f; // Hastighet nedåt
    [SerializeField] private float moveSpeedUp = 4f; // Hastighet uppåt (snabbare)

    [Header("Debug")]
    [SerializeField] private int currentFloorIndex = 0; // Nuvarande våning (0 = högst upp)
    [SerializeField] private bool isMoving = false; // Om hissen rör sig eller väntar

    // Sekvens av våningar att besöka
    [SerializeField] private int[] floorSequence = new int[] { 0, 1, 2, 3, 0 };
    private int sequenceIndex = 0;

    private void Start()
    {
        // Kontrollera att vi har våningspositioner inställda
        if (floorPositions.Length > 0)
        {
            // Starta rörelsen
            StartCoroutine(MovePlatform());
        }
        else
        {
            Debug.LogError("Inga våningspositioner är inställda på PlatformController!");
        }
    }

    private IEnumerator MovePlatform()
    {
        // Börja med att placera hissen på startvåningen (våning 0)
        currentFloorIndex = floorSequence[0];
        transform.position = floorPositions[currentFloorIndex].position;

        while (true) // Fortsätt i all oändlighet
        {
            // Vänta på nuvarande våning (längre vid startvåningen)
            isMoving = false;
            if (currentFloorIndex == 0)
            {
                yield return new WaitForSeconds(waitTimeAtStartFloor);
            }
            else
            {
                yield return new WaitForSeconds(waitTimeAtFloor);
            }

            // Gå till nästa våning i sekvensen
            isMoving = true;
            sequenceIndex = (sequenceIndex + 1) % floorSequence.Length;
            int nextFloorIndex = floorSequence[sequenceIndex];

            // Bestäm hastigheten baserat på om vi åker upp eller ner
            bool isGoingDown = nextFloorIndex > currentFloorIndex;
            float currentSpeed = isGoingDown ? moveSpeedDown : moveSpeedUp;

            // Uppdatera nuvarande våning
            currentFloorIndex = nextFloorIndex;

            // Flytta hissen till nästa våning
            Vector3 targetPosition = floorPositions[currentFloorIndex].position;

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    currentSpeed * Time.deltaTime
                );
                yield return null; // Vänta till nästa frame
            }

            // Säkerställ att vi är exakt på målpositionen
            transform.position = targetPosition;
        }
    }

    // För debugging: Rita linjer mellan våningarna i Scene-vyn
    private void OnDrawGizmos()
    {
        if (floorPositions == null || floorPositions.Length <= 1)
            return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < floorPositions.Length - 1; i++)
        {
            if (floorPositions[i] != null && floorPositions[i + 1] != null)
            {
                Gizmos.DrawLine(floorPositions[i].position, floorPositions[i + 1].position);
            }
        }
    }
}