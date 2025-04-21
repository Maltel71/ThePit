using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : MonoBehaviour
{
    [Header("V�ningsinst�llningar")]
    [SerializeField] private Transform[] floorPositions; // Positioner f�r varje v�ning
    [SerializeField] private float waitTimeAtFloor = 5f; // Standardtid att v�nta vid varje v�ning i sekunder
    [SerializeField] private float waitTimeAtStartFloor = 10f; // Tid att v�nta vid v�ning 0 (startv�ningen)
    [SerializeField] private float moveSpeedDown = 2f; // Hastighet ned�t
    [SerializeField] private float moveSpeedUp = 4f; // Hastighet upp�t (snabbare)

    [Header("Debug")]
    [SerializeField] private int currentFloorIndex = 0; // Nuvarande v�ning (0 = h�gst upp)
    [SerializeField] private bool isMoving = false; // Om hissen r�r sig eller v�ntar

    // Sekvens av v�ningar att bes�ka
    [SerializeField] private int[] floorSequence = new int[] { 0, 1, 2, 3, 0 };
    private int sequenceIndex = 0;

    private void Start()
    {
        // Kontrollera att vi har v�ningspositioner inst�llda
        if (floorPositions.Length > 0)
        {
            // Starta r�relsen
            StartCoroutine(MovePlatform());
        }
        else
        {
            Debug.LogError("Inga v�ningspositioner �r inst�llda p� PlatformController!");
        }
    }

    private IEnumerator MovePlatform()
    {
        // B�rja med att placera hissen p� startv�ningen (v�ning 0)
        currentFloorIndex = floorSequence[0];
        transform.position = floorPositions[currentFloorIndex].position;

        while (true) // Forts�tt i all o�ndlighet
        {
            // V�nta p� nuvarande v�ning (l�ngre vid startv�ningen)
            isMoving = false;
            if (currentFloorIndex == 0)
            {
                yield return new WaitForSeconds(waitTimeAtStartFloor);
            }
            else
            {
                yield return new WaitForSeconds(waitTimeAtFloor);
            }

            // G� till n�sta v�ning i sekvensen
            isMoving = true;
            sequenceIndex = (sequenceIndex + 1) % floorSequence.Length;
            int nextFloorIndex = floorSequence[sequenceIndex];

            // Best�m hastigheten baserat p� om vi �ker upp eller ner
            bool isGoingDown = nextFloorIndex > currentFloorIndex;
            float currentSpeed = isGoingDown ? moveSpeedDown : moveSpeedUp;

            // Uppdatera nuvarande v�ning
            currentFloorIndex = nextFloorIndex;

            // Flytta hissen till n�sta v�ning
            Vector3 targetPosition = floorPositions[currentFloorIndex].position;

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