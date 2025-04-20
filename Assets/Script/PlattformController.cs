using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : MonoBehaviour
{
    [Header("Våningsinställningar")]
    [SerializeField] private Transform[] floorPositions; // Positioner för varje våning
    [SerializeField] private float waitTimeAtFloor = 5f; // Tid att vänta vid varje våning i sekunder
    [SerializeField] private float moveSpeedDown = 2f; // Hastighet nedåt
    [SerializeField] private float moveSpeedUp = 4f; // Hastighet uppåt (snabbare)

    [Header("Debug")]
    [SerializeField] private int currentFloorIndex = 0; // Nuvarande våning (0 = högst upp)
    [SerializeField] private bool isMoving = false; // Om hissen rör sig eller väntar

    private bool isGoingDown = true; // Riktning (nedåt = true, uppåt = false)

    private void Start()
    {
        // Kontrollera att vi har våningspositioner inställda
        if (floorPositions.Length > 0)
        {
            // Notera: Vi låter MovePlatform() hantera startpositionen
            StartCoroutine(MovePlatform());
        }
        else
        {
            Debug.LogError("Inga våningspositioner är inställda på PlatformController!");
        }
    }

    private IEnumerator MovePlatform()
    {
        // Börja med att åka till floor 1 (index 0) direkt utan att stanna
        isMoving = true;
        currentFloorIndex = 0;
        Vector3 targetPosition = floorPositions[currentFloorIndex].position;

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeedDown * Time.deltaTime
            );
            yield return null;
        }

        // Säkerställ att vi är exakt på målpositionen
        transform.position = targetPosition;

        while (true) // Fortsätt i all oändlighet
        {
            // Vänta på nuvarande våning
            isMoving = false;
            yield return new WaitForSeconds(waitTimeAtFloor);

            // Bestäm nästa våning
            isMoving = true;

            if (isGoingDown)
            {
                currentFloorIndex++;

                // Om vi nått botten, ändra riktning
                if (currentFloorIndex >= floorPositions.Length - 1)
                {
                    currentFloorIndex = floorPositions.Length - 1;
                    isGoingDown = false;
                }
            }
            else // Går uppåt
            {
                currentFloorIndex--;

                // Om vi nått toppen, ändra riktning
                if (currentFloorIndex <= 0)
                {
                    currentFloorIndex = 0;
                    isGoingDown = true;
                }
            }

            // Flytta hissen till nästa våning
            targetPosition = floorPositions[currentFloorIndex].position;
            float currentSpeed = isGoingDown ? moveSpeedDown : moveSpeedUp; // Använd olika hastigheter beroende på riktning

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