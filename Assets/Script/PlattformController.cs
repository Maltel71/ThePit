using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : MonoBehaviour
{
    [Header("V�ningsinst�llningar")]
    [SerializeField] private Transform[] floorPositions; // Positioner f�r varje v�ning
    [SerializeField] private float waitTimeAtFloor = 5f; // Tid att v�nta vid varje v�ning i sekunder
    [SerializeField] private float moveSpeedDown = 2f; // Hastighet ned�t
    [SerializeField] private float moveSpeedUp = 4f; // Hastighet upp�t (snabbare)

    [Header("Debug")]
    [SerializeField] private int currentFloorIndex = 0; // Nuvarande v�ning (0 = h�gst upp)
    [SerializeField] private bool isMoving = false; // Om hissen r�r sig eller v�ntar

    private bool isGoingDown = true; // Riktning (ned�t = true, upp�t = false)

    private void Start()
    {
        // Kontrollera att vi har v�ningspositioner inst�llda
        if (floorPositions.Length > 0)
        {
            // Notera: Vi l�ter MovePlatform() hantera startpositionen
            StartCoroutine(MovePlatform());
        }
        else
        {
            Debug.LogError("Inga v�ningspositioner �r inst�llda p� PlatformController!");
        }
    }

    private IEnumerator MovePlatform()
    {
        // B�rja med att �ka till floor 1 (index 0) direkt utan att stanna
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

        // S�kerst�ll att vi �r exakt p� m�lpositionen
        transform.position = targetPosition;

        while (true) // Forts�tt i all o�ndlighet
        {
            // V�nta p� nuvarande v�ning
            isMoving = false;
            yield return new WaitForSeconds(waitTimeAtFloor);

            // Best�m n�sta v�ning
            isMoving = true;

            if (isGoingDown)
            {
                currentFloorIndex++;

                // Om vi n�tt botten, �ndra riktning
                if (currentFloorIndex >= floorPositions.Length - 1)
                {
                    currentFloorIndex = floorPositions.Length - 1;
                    isGoingDown = false;
                }
            }
            else // G�r upp�t
            {
                currentFloorIndex--;

                // Om vi n�tt toppen, �ndra riktning
                if (currentFloorIndex <= 0)
                {
                    currentFloorIndex = 0;
                    isGoingDown = true;
                }
            }

            // Flytta hissen till n�sta v�ning
            targetPosition = floorPositions[currentFloorIndex].position;
            float currentSpeed = isGoingDown ? moveSpeedDown : moveSpeedUp; // Anv�nd olika hastigheter beroende p� riktning

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    currentSpeed * Time.deltaTime
                );
                yield return null; // V�nta till n�sta frame
            }

            // S�kerst�ll att vi �r exakt p� m�lpositionen
            transform.position = targetPosition;
        }
    }

    // F�r debugging: Rita linjer mellan v�ningarna i Scene-vyn
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