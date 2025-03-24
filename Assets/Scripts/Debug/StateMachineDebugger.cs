using UnityEngine;
using TMPro;

public class StateMachineDebugger : MonoBehaviour
{
    public JackRabbitController playerController;
    public TextMeshProUGUI debugText;

    private void Update()
    {
        if (playerController == null || debugText == null)
            return;

        string currentState = playerController.GetCurrentStateName();

        debugText.text =
            $"Current State: {currentState}\n" +
            $"Grounded: {playerController.IsGrounded()}\n" +
            $"Velocity Y: {playerController.CurrentVelocity.y:F2}\n" +
            $"Move Input: {playerController.MoveInput}\n" +
            $"Sprint Input: {playerController.SprintInput}\n" +
            $"Jump Input: {playerController.JumpInput}\n" +
            $"Remaining Jumps: {playerController.jumpsRemaining}\n";
    }
}